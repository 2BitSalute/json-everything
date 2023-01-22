﻿using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Json.Path;

/// <summary>
/// Implements the `match()` function which determines if any substring within
/// a string matches a regular expression.
/// </summary>
public class SearchFunction : IPathFunctionDefinition
{
	/// <summary>
	/// Gets the function name.
	/// </summary>
	public string Name => "search";

	/// <summary>
	/// The minimum argument count accepted by the function.
	/// </summary>
	public int MinArgumentCount => 2;

	/// <summary>
	/// The maximum argument count accepted by the function.
	/// </summary>
	public int MaxArgumentCount => 2;

	/// <summary>
	/// Evaluates the function.
	/// </summary>
	/// <param name="arguments">A collection of nodelists where each nodelist in the collection corresponds to a single argument.</param>
	/// <returns>A nodelist.  If the evaluation fails, an empty nodelist is returned.</returns>
	public NodeList Evaluate(IEnumerable<NodeList> arguments)
	{
		var args = arguments.ToArray();
		if (args[0].TryGetSingleValue() is not JsonValue arg1Value || !arg1Value.TryGetValue<string>(out var text)) return NodeList.Empty;
		if (args[1].TryGetSingleValue() is not JsonValue arg2Value || !arg2Value.TryGetValue<string>(out var regex)) return NodeList.Empty;

		return (JsonValue)Regex.IsMatch(text, regex, RegexOptions.ECMAScript);
	}
}