﻿using System.Diagnostics.CodeAnalysis;
using System;
using System.Text;
using System.Text.Json.Nodes;
using Json.More;

namespace Json.Path.Expressions;

internal class PathExpressionNode : ValueExpressionNode
{
	public JsonPath Path { get; }

	public PathExpressionNode(JsonPath path)
	{
		Path = path;
	}

	public override JsonNode? Evaluate(JsonNode? globalParameter, JsonNode? localParameter)
	{
		var parameter = Path.Scope == PathScope.Global
			? globalParameter
			: localParameter;

		var result = Path.Evaluate(parameter);

		if (result.Matches == null) return null;

		return result.Matches.Count == 1
			? result.Matches[0].Value ?? JsonNull.SignalNode
			: result.Matches;
	}

	public override void BuildString(StringBuilder builder)
	{
		Path.BuildString(builder);
	}

	public static implicit operator PathExpressionNode(JsonPath value)
	{
		return new PathExpressionNode(value);
	}

	public override string ToString()
	{
		return Path.ToString();
	}
}

internal class PathExpressionParser : IValueExpressionParser
{
	public bool TryParse(ReadOnlySpan<char> source, ref int index, [NotNullWhen(true)] out ValueExpressionNode? expression)
	{
		if (!PathParser.TryParse(source, ref index, out var path))
		{
			expression = null;
			return false;
		}

		expression = new PathExpressionNode(path);
		return true;
	}
}
