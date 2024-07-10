﻿using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Json.Schema.Experiments;

public class DeprecatedKeywordHandler : IKeywordHandler
{
	public static DeprecatedKeywordHandler Instance { get; } = new();

	public string Name => "deprecated";
	public string[]? Dependencies { get; }

	private DeprecatedKeywordHandler() { }

	public KeywordEvaluation Handle(JsonNode? keywordValue, EvaluationContext context, IReadOnlyCollection<KeywordEvaluation> siblingEvaluations)
	{
		return KeywordEvaluation.Annotate;
	}

	IEnumerable<JsonNode?> IKeywordHandler.GetSubschemas(JsonNode? keywordValue) => [];
}