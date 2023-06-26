﻿using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Json.More;

namespace Json.Schema;

/// <summary>
/// Handles `unevaluatedItems`.
/// </summary>
[SchemaPriority(30)]
[SchemaKeyword(Name)]
[SchemaSpecVersion(SpecVersion.Draft201909)]
[SchemaSpecVersion(SpecVersion.Draft202012)]
[SchemaSpecVersion(SpecVersion.DraftNext)]
[Vocabulary(Vocabularies.Applicator201909Id)]
[Vocabulary(Vocabularies.Applicator202012Id)]
[Vocabulary(Vocabularies.ApplicatorNextId)]
[DependsOnAnnotationsFrom(typeof(PrefixItemsKeyword))]
[DependsOnAnnotationsFrom(typeof(ItemsKeyword))]
[DependsOnAnnotationsFrom(typeof(AdditionalItemsKeyword))]
[DependsOnAnnotationsFrom(typeof(ContainsKeyword))]
[DependsOnAnnotationsFrom(typeof(UnevaluatedItemsKeyword))]
[JsonConverter(typeof(UnevaluatedItemsKeywordJsonConverter))]
public class UnevaluatedItemsKeyword : IJsonSchemaKeyword, ISchemaContainer, IEquatable<UnevaluatedItemsKeyword>
{
	/// <summary>
	/// The JSON name of the keyword.
	/// </summary>
	public const string Name = "unevaluatedItems";

	/// <summary>
	/// The schema by which to evaluate unevaluated items.
	/// </summary>
	public JsonSchema Schema { get; }

	/// <summary>
	/// Creates a new <see cref="UnevaluatedItemsKeyword"/>.
	/// </summary>
	/// <param name="value">The schema by which to evaluate unevaluated items.</param>
	public UnevaluatedItemsKeyword(JsonSchema value)
	{
		Schema = value ?? throw new ArgumentNullException(nameof(value));
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
		if (schemaValueType != SchemaValueType.Array)
		{
			context.WrongValueKind(schemaValueType);
			return;
		}

		context.Options.LogIndentLevel++;
		var overallResult = true;
		int startIndex = 0;
		var annotations = context.LocalResult.GetAllAnnotations(PrefixItemsKeyword.Name).ToList();
		if (annotations.Any())
		{
			// ReSharper disable once AccessToModifiedClosure
			context.Log(() => $"Annotations from {PrefixItemsKeyword.Name}: {annotations.ToJsonArray().AsJsonString()}.");
			if (annotations.Any(x => x!.AsValue().TryGetValue(out bool _))) // is only ever true or a number
			{
				context.ExitKeyword(Name, true);
				return;
			}
			startIndex = annotations.Max(x => x!.AsValue().TryGetValue(out int i) ? i : 0);
		}
		else
			context.Log(() => $"No annotations from {PrefixItemsKeyword.Name}.");
		annotations = context.LocalResult.GetAllAnnotations(ItemsKeyword.Name).ToList();
		if (annotations.Any())
		{
			// ReSharper disable once AccessToModifiedClosure
			context.Log(() => $"Annotations from {ItemsKeyword.Name}: {annotations.ToJsonArray().AsJsonString()}.");
			if (annotations.Any(x => x!.AsValue().TryGetValue(out bool _))) // is only ever true or a number
			{
				context.ExitKeyword(Name, true);
				return;
			}
			startIndex = annotations.Max(x => x!.AsValue().TryGetValue(out int i) ? i : 0);
		}
		else
			context.Log(() => $"No annotations from {ItemsKeyword.Name}.");
		annotations = context.LocalResult.GetAllAnnotations(AdditionalItemsKeyword.Name).ToList();
		if (annotations.Any()) // is only ever true
		{
			context.Log(() => $"Annotation from {AdditionalItemsKeyword.Name}: {annotations.ToJsonArray().AsJsonString()}.");
			context.ExitKeyword(Name, true);
			return;
		}
		context.Log(() => $"No annotations from {AdditionalItemsKeyword.Name}.");
		annotations = context.LocalResult.GetAllAnnotations(Name).ToList();
		if (annotations.Any()) // is only ever true
		{
			context.Log(() => $"Annotation from {Name}: {annotations.ToJsonArray().AsJsonString()}.");
			context.ExitKeyword(Name, true);
			return;
		}
		context.Log(() => $"No annotations from {Name}.");
		var array = (JsonArray)context.LocalInstance!;
		var indicesToEvaluate = Enumerable.Range(startIndex, array.Count - startIndex);
		if (context.Options.EvaluatingAs.HasFlag(SpecVersion.Draft202012) ||
		    context.Options.EvaluatingAs.HasFlag(SpecVersion.DraftNext) ||
		    context.Options.EvaluatingAs == SpecVersion.Unspecified)
		{
			var evaluatedByContains = context.LocalResult.GetAllAnnotations(ContainsKeyword.Name)
				.SelectMany(x => x!.AsArray().Select(j => j!.GetValue<int>()))
				.Distinct()
				.ToArray();
			if (evaluatedByContains.Any())
			{
				context.Log(() => $"Annotations from {ContainsKeyword.Name}: {annotations.ToJsonArray().AsJsonString()}.");
				indicesToEvaluate = indicesToEvaluate.Except(evaluatedByContains);
			}
			else
				context.Log(() => $"No annotations from {ContainsKeyword.Name}.");
		}

		var tokenSource = new CancellationTokenSource();
		token.Register(tokenSource.Cancel);

		var tasks = indicesToEvaluate.Select(async i =>
		{
			if (tokenSource.Token.IsCancellationRequested) return (i, true);

			context.Log(() => $"Evaluating item at index {i}.");
			var item = array[i];
			var branch = context.ParallelBranch(context.InstanceLocation.Combine(i), item ?? JsonNull.SignalNode,
				context.EvaluationPath.Combine(Name), Schema);
			await branch.Evaluate(tokenSource.Token);
			context.Log(() => $"Item at index {i} {branch.LocalResult.IsValid.GetValidityString()}.");

			return (i, branch.LocalResult.IsValid);
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

		context.Options.LogIndentLevel--;

		context.LocalResult.SetAnnotation(Name, true);
		if (!overallResult)
			context.LocalResult.Fail();
		context.ExitKeyword(Name, context.LocalResult.IsValid);
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
	public bool Equals(UnevaluatedItemsKeyword? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return Equals(Schema, other.Schema);
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object obj)
	{
		return Equals(obj as UnevaluatedItemsKeyword);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return Schema.GetHashCode();
	}
}

internal class UnevaluatedItemsKeywordJsonConverter : JsonConverter<UnevaluatedItemsKeyword>
{
	public override UnevaluatedItemsKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var schema = JsonSerializer.Deserialize<JsonSchema>(ref reader, options)!;

		return new UnevaluatedItemsKeyword(schema);
	}
	public override void Write(Utf8JsonWriter writer, UnevaluatedItemsKeyword value, JsonSerializerOptions options)
	{
		writer.WritePropertyName(UnevaluatedItemsKeyword.Name);
		JsonSerializer.Serialize(writer, value.Schema, options);
	}
}