﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Json.Pointer;

namespace Json.Schema;

/// <summary>
/// Handles `dependencies`.
/// </summary>
[SchemaPriority(10)]
[SchemaKeyword(Name)]
[SchemaSpecVersion(SpecVersion.Draft6)]
[SchemaSpecVersion(SpecVersion.Draft7)]
[JsonConverter(typeof(DependenciesKeywordJsonConverter))]
public class DependenciesKeyword : IJsonSchemaKeyword, IKeyedSchemaCollector, IEquatable<DependenciesKeyword>
{
	/// <summary>
	/// The JSON name of the keyword.
	/// </summary>
	public const string Name = "dependencies";

	/// <summary>
	/// The collection of dependencies.
	/// </summary>
	public IReadOnlyDictionary<string, SchemaOrPropertyList> Requirements { get; }

	IReadOnlyDictionary<string, JsonSchema> IKeyedSchemaCollector.Schemas =>
		Requirements.Where(x => x.Value.Schema != null)
			.ToDictionary(x => x.Key, x => x.Value.Schema!);

	/// <summary>
	/// Creates a new <see cref="DependenciesKeyword"/>.
	/// </summary>
	/// <param name="values">The collection of dependencies.</param>
	public DependenciesKeyword(IReadOnlyDictionary<string, SchemaOrPropertyList> values)
	{
		Requirements = values;
	}

	/// <summary>
	/// Performs evaluation for the keyword.
	/// </summary>
	/// <param name="context">Contextual details for the evaluation process.</param>
	/// <param name="token">The cancellation token used by the caller.</param>
	public async Task Evaluate(EvaluationContext context, CancellationToken token)
	{
		context.EnterKeyword(Name);
		var schemaValueType = context.LocalInstance.GetSchemaValueType();
		if (schemaValueType != SchemaValueType.Object)
		{
			context.WrongValueKind(schemaValueType);
			return;
		}

		var obj = (JsonObject)context.LocalInstance!;
		if (!obj.VerifyJsonObject()) return;

		var overallResult = true;
		var evaluatedProperties = new List<string>();

		var tokenSource = new CancellationTokenSource();
		token.Register(tokenSource.Cancel);

		var tasks = Requirements.Select(async property =>
		{
			if (tokenSource.Token.IsCancellationRequested) return (property.Key, true);

			var localResult = true;

			context.Log(() => $"Evaluating property '{property.Key}'.");
			var requirements = property.Value;
			var name = property.Key;
			if (!obj.TryGetPropertyValue(name, out _))
			{
				context.Log(() => $"Property '{property.Key}' does not exist. Skipping.");
				return ((string?)null, true);
			}

			context.Options.LogIndentLevel++;
			if (requirements.Schema != null)
			{
				context.Log(() => "Found schema requirement.");
				var branch = context.ParallelBranch(context.EvaluationPath.Combine(name), requirements.Schema);
				await branch.Evaluate(tokenSource.Token);
				localResult = branch.LocalResult.IsValid;
				context.Log(() => $"Property '{property.Key}' {branch.LocalResult.IsValid.GetValidityString()}.");
			}
			else
			{
				context.Log(() => "Found property list requirement.");
				var missingDependencies = new List<string>();
				foreach (var dependency in requirements.Requirements!)
				{
					if (obj.TryGetPropertyValue(dependency, out _)) continue;

					localResult = false;
					missingDependencies.Add(dependency);
				}

				if (!missingDependencies.Any())
					evaluatedProperties.Add(name);
				else
				{
					context.Log(() => $"Missing properties [{string.Join(",", missingDependencies.Select(x => $"'{x}'"))}].");
					localResult = false;
				}
			}
			context.Options.LogIndentLevel--;

			return (property.Key, localResult);
		}).ToArray();

		if (tasks.Any())
		{
			if (context.ApplyOptimizations)
			{
				var failedValidation = await tasks.WhenAny(x => !x.Item2, tokenSource.Token);
				tokenSource.Cancel();

				overallResult = failedValidation == null;
			}
			else
			{
				await Task.WhenAll(tasks);
				overallResult = tasks.All(x => x.Result.Item2);
			}
		}

		if (!overallResult)
			context.LocalResult.Fail(Name, ErrorMessages.Dependencies, ("properties", JsonSerializer.Serialize(evaluatedProperties)));
		context.ExitKeyword(Name, context.LocalResult.IsValid);
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
	public bool Equals(DependenciesKeyword? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		if (Requirements.Count != other.Requirements.Count) return false;
		var byKey = Requirements.Join(other.Requirements,
				td => td.Key,
				od => od.Key,
				(td, od) => new { ThisDef = td.Value, OtherDef = od.Value })
			.ToArray();
		if (byKey.Length != Requirements.Count) return false;

		return byKey.All(g => Equals(g.ThisDef, g.OtherDef));
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object obj)
	{
		return Equals(obj as DependenciesKeyword);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return Requirements.GetStringDictionaryHashCode();
	}
}

internal class DependenciesKeywordJsonConverter : JsonConverter<DependenciesKeyword>
{
	public override DependenciesKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected object");

		var dependencies = JsonSerializer.Deserialize<Dictionary<string, SchemaOrPropertyList>>(ref reader, options)!;
		return new DependenciesKeyword(dependencies);
	}
	public override void Write(Utf8JsonWriter writer, DependenciesKeyword value, JsonSerializerOptions options)
	{
		writer.WritePropertyName(DependenciesKeyword.Name);
		writer.WriteStartObject();
		foreach (var kvp in value.Requirements)
		{
			writer.WritePropertyName(kvp.Key);
			JsonSerializer.Serialize(writer, kvp.Value, options);
		}
		writer.WriteEndObject();
	}
}

/// <summary>
/// A holder for either a schema dependency or a requirements dependency.
/// </summary>
[JsonConverter(typeof(SchemaOrPropertyListJsonConverter))]
public class SchemaOrPropertyList : IEquatable<SchemaOrPropertyList>
{
	/// <summary>
	/// The schema dependency.
	/// </summary>
	public JsonSchema? Schema { get; }
	/// <summary>
	/// The property dependency.
	/// </summary>
	public List<string>? Requirements { get; }

	/// <summary>
	/// Creates a schema dependency.
	/// </summary>
	/// <param name="schema">The schema dependency.</param>
	public SchemaOrPropertyList(JsonSchema schema)
	{
		Schema = schema;
	}

	/// <summary>
	/// Creates a property dependency.
	/// </summary>
	/// <param name="requirements">The property dependency.</param>
	public SchemaOrPropertyList(IEnumerable<string> requirements)
	{
		Requirements = requirements.ToList();
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
	public bool Equals(SchemaOrPropertyList? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return Equals(Schema, other.Schema) && Requirements.ContentsEqual(other.Requirements);
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object obj)
	{
		return Equals(obj as SchemaOrPropertyList);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		unchecked
		{
			return ((Schema?.GetHashCode() ?? 0) * 397) ^ (Requirements?.GetCollectionHashCode() ?? 0);
		}
	}

	/// <summary>
	/// Implicitly creates a <see cref="SchemaOrPropertyList"/> from a <see cref="JsonSchema"/>.
	/// </summary>
	public static implicit operator SchemaOrPropertyList(JsonSchema schema)
	{
		return new SchemaOrPropertyList(schema);
	}

	/// <summary>
	/// Implicitly creates a <see cref="SchemaOrPropertyList"/> from a list of strings.
	/// </summary>
	public static implicit operator SchemaOrPropertyList(List<string> requirements)
	{
		return new SchemaOrPropertyList(requirements);
	}

	/// <summary>
	/// Implicitly creates a <see cref="SchemaOrPropertyList"/> from an array of strings.
	/// </summary>
	public static implicit operator SchemaOrPropertyList(string[] requirements)
	{
		return new SchemaOrPropertyList(requirements);
	}
}

internal class SchemaOrPropertyListJsonConverter : JsonConverter<SchemaOrPropertyList>
{
	public override SchemaOrPropertyList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.StartArray)
			return new SchemaOrPropertyList(JsonSerializer.Deserialize<List<string>>(ref reader, options)!);

		return new SchemaOrPropertyList(JsonSerializer.Deserialize<JsonSchema>(ref reader, options)!);
	}

	public override void Write(Utf8JsonWriter writer, SchemaOrPropertyList value, JsonSerializerOptions options)
	{
		if (value.Schema != null)
			JsonSerializer.Serialize(writer, value.Schema, options);
		else
			JsonSerializer.Serialize(writer, value.Requirements, options);
	}
}

public static partial class ErrorMessages
{
	private static string? _dependencies;

	/// <summary>
	/// Gets or sets the error message for <see cref="DependenciesKeyword"/>.
	/// </summary>
	/// <remarks>
	///	Available tokens are:
	///   - [[properties]] - the properties which failed to match the requirements
	/// </remarks>
	public static string Dependencies
	{
		get => _dependencies ?? Get();
		set => _dependencies = value;
	}
}