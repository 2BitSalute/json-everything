﻿using System.Globalization;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Json.Schema.Tests;

public class LocalizationTests
{
	[Test]
	public async Task MinimumReturnsDefaultErrorMessage()
	{
		JsonSchema schema = new JsonSchemaBuilder()
			.Type(SchemaValueType.Number)
			.Minimum(10);

		var instance = JsonNode.Parse("5");

		var results = await schema.Evaluate(instance, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });

		var message = results.Errors!["minimum"];

		Assert.AreEqual("5 is less than or equal to 10", message);
	}

	[Test]
	[Ignore("Can't test localization since resource file is in a separate dll now.")]
	public async Task MinimumReturnsDefaultErrorMessageButInSpanish()
	{
		try
		{
			ErrorMessages.Culture = CultureInfo.GetCultureInfo("es-es");

			JsonSchema schema = new JsonSchemaBuilder()
				.Type(SchemaValueType.Number)
				.Minimum(10);

			var instance = JsonNode.Parse("5");

			var results = await schema.Evaluate(instance, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });

			var message = results.Errors!["minimum"];

			Assert.AreEqual("5 es menor o igual que 10", message);
		}
		finally
		{
			ErrorMessages.Culture = null;
		}
	}

	[Test]
	public async Task MinimumReturnsCustomErrorMessage()
	{
		try
		{
			ErrorMessages.Minimum = "This is a custom error message with [[received]] and [[limit]]";

			JsonSchema schema = new JsonSchemaBuilder()
				.Type(SchemaValueType.Number)
				.Minimum(10);

			var instance = JsonNode.Parse("5");

			var results = await schema.Evaluate(instance, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });

			var message = results.Errors!["minimum"];

			Assert.AreEqual("This is a custom error message with 5 and 10", message);
		}
		finally
		{
			ErrorMessages.Minimum = null!;
		}
	}
}