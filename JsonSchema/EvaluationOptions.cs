﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Json.Schema;

/// <summary>
/// Allows configuration of the evaluation process.
/// </summary>
public class EvaluationOptions
{
	private ILog? _log;
	private HashSet<Type>? _ignoredAnnotationTypes;

	/// <summary>
	/// The default settings.
	/// </summary>
	public static EvaluationOptions Default { get; } = new();

	/// <summary>
	/// Indicates which specification version to process as.  This will filter the keywords
	/// of a schema based on their support.
	/// </summary>
	public SpecVersion EvaluateAs { get; set; }
	/// <summary>
	/// Indicates whether the schema should be validated against its `$schema` value.
	/// this is not typically necessary.
	/// </summary>
	public bool ValidateAgainstMetaSchema { get; set; }
	/// <summary>
	/// Specifies the output format.
	/// </summary>
	public OutputFormat OutputFormat { get; set; }

	/// <summary>
	/// The local schema registry.  If a schema is not found here, it will
	/// automatically check the global registry as well.
	/// </summary>
	public SchemaRegistry SchemaRegistry { get; }

	/// <summary>
	/// The local vocabulary registry.  If a schema is not found here, it will
	/// automatically check the global registry as well.
	/// </summary>
	public VocabularyRegistry VocabularyRegistry { get; } = new();

	/// <summary>
	/// Gets or sets the indent level for the log.
	/// </summary>
	public int LogIndentLevel { get; set; }

	/// <summary>
	/// Gets or sets a log which will output processing information.
	/// </summary>
	public ILog Log
	{
		get => _log ?? NullLog.Instance;
		set => _log = value;
	}

	/// <summary>
	/// Specifies whether the `format` keyword should be required to provide
	/// validation results.  Default is false, which just produces annotations
	/// for drafts 2019-09 and prior or follows the behavior set forth by the
	/// format-annotation vocabulary requirement in the `$vocabulary` keyword in
	/// a meta-schema declaring draft 2020-12.
	/// </summary>
	public bool RequireFormatValidation { get; set; }

	/// <summary>
	/// Specifies whether the `format` keyword should fail validations for
	/// unknown formats.  Default is false.
	/// </summary>
	/// <remarks>
	///	This option is applied whether `format` is using annotation or
	/// assertion behavior.
	/// </remarks>
	public bool OnlyKnownFormats { get; set; }

	/// <summary>
	/// Specifies whether custom keywords that aren't defined in vocabularies
	/// should be processed.  Only applies to vocab-enabled JSON Schema versions
	/// (e.g. draft 2019-09 &amp; 20200-12).  Default is false.
	/// </summary>
	public bool ProcessCustomKeywords { get; set; }

	/// <summary>
	/// If enabled, annotations that are dropped as a result of a failing
	/// subschema will be reported in a `droppedAnnotations` property in
	/// the output.
	/// </summary>
	public bool PreserveDroppedAnnotations { get; set; }

	/// <summary>
	/// Gets the set of keyword types from which annotations will be ignored.
	/// </summary>
	public IEnumerable<Type>? IgnoredAnnotations => _ignoredAnnotationTypes;

	internal SpecVersion EvaluatingAs { get; set; }

	static EvaluationOptions()
	{
		// It's necessary to call this from here because
		// SchemaRegistry.Global is defined to look at the default options.
		var task = Default.SchemaRegistry.InitializeMetaSchemas();
		if (task.IsCompleted) return;

		task.RunSynchronously();
	}

	/// <summary>
	/// Create a new instance of the <see cref="EvaluationOptions"/> class.
	/// </summary>
	public EvaluationOptions()
	{
		SchemaRegistry = new SchemaRegistry();
	}

	/// <summary>
	/// Creates a deep copy of the options.
	/// </summary>
	/// <param name="other">The source options.</param>
	/// <returns>A new options instance with the same settings.</returns>
	public static EvaluationOptions From(EvaluationOptions other)
	{
		var options = new EvaluationOptions
		{
			EvaluateAs = other.EvaluateAs,
			OutputFormat = other.OutputFormat,
			ValidateAgainstMetaSchema = other.ValidateAgainstMetaSchema,
			RequireFormatValidation = other.RequireFormatValidation,
			ProcessCustomKeywords = other.ProcessCustomKeywords,
			LogIndentLevel = other.LogIndentLevel,
			Log = other._log ?? Default.Log,
			OnlyKnownFormats = other.OnlyKnownFormats,
			PreserveDroppedAnnotations = other.PreserveDroppedAnnotations,
			_ignoredAnnotationTypes = other._ignoredAnnotationTypes == null
				? null
				: new HashSet<Type>(other._ignoredAnnotationTypes)
		};
		options.SchemaRegistry.CopyFrom(other.SchemaRegistry);
		options.VocabularyRegistry.CopyFrom(other.VocabularyRegistry);
		return options;
	}

	/// <summary>
	/// Ignores annotations from the specified keyword.
	/// </summary>
	/// <typeparam name="T">The keyword type which should not have annotations.</typeparam>
	public void IgnoreAnnotationsFrom<T>()
		where T : IJsonSchemaKeyword
	{
		_ignoredAnnotationTypes ??= new HashSet<Type>();

		_ignoredAnnotationTypes.Add(typeof(T));
	}

	/// <summary>
	/// 
	/// </summary>
	public void IgnoreAllAnnotations()
	{
		_ignoredAnnotationTypes = new HashSet<Type>(SchemaKeywordRegistry.KeywordTypes);
	}

	/// <summary>
	/// Clears ignored annotations.
	/// </summary>
	public void ClearIgnoredAnnotations()
	{
		_ignoredAnnotationTypes = null;
	}

	/// <summary>
	/// Restores annotation collection for the specified keyword.
	/// </summary>
	public void CollectAnnotationsFrom<T>()
	{
		_ignoredAnnotationTypes?.Remove(typeof(T));
	}

	internal IEnumerable<IJsonSchemaKeyword> FilterKeywords(IEnumerable<IJsonSchemaKeyword> keywords, SpecVersion declaredVersion)
	{
		var evaluatingAs = declaredVersion == SpecVersion.Unspecified ? EvaluatingAs : declaredVersion;

		if (evaluatingAs is SpecVersion.Draft6 or SpecVersion.Draft7)
			return DisallowSiblingRef(keywords, evaluatingAs);

		return AllowSiblingRef(keywords, evaluatingAs);
	}

	private static IEnumerable<IJsonSchemaKeyword> DisallowSiblingRef(IEnumerable<IJsonSchemaKeyword> keywords, SpecVersion version)
	{
		var refKeyword = keywords.OfType<RefKeyword>().SingleOrDefault();

		return refKeyword != null ? new[] { refKeyword } : FilterBySpecVersion(keywords, version);
	}

	private static IEnumerable<IJsonSchemaKeyword> AllowSiblingRef(IEnumerable<IJsonSchemaKeyword> keywords, SpecVersion version)
	{
		return FilterBySpecVersion(keywords, version);
	}

	private static IEnumerable<IJsonSchemaKeyword> FilterBySpecVersion(IEnumerable<IJsonSchemaKeyword> keywords, SpecVersion version)
	{
		if (version == SpecVersion.Unspecified) return keywords;

		return keywords.Where(k => k.SupportsVersion(version));
	}
}