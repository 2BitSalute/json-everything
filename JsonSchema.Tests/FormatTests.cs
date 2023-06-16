using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Json.Schema.Tests;

public class FormatTests
{
	[Test]
	public async Task Ipv4_Pass()
	{
		JsonSchema schema = new JsonSchemaBuilder()
			.Format(Formats.Ipv4);

		var value = JsonNode.Parse("\"100.2.54.3\"");

		var result = await schema.Evaluate(value, new EvaluationOptions { RequireFormatValidation = true });

		Assert.True(result.IsValid);
	}
	[Test]
	public async Task Ipv4_Fail()
	{
		JsonSchema schema = new JsonSchemaBuilder()
			.Format(Formats.Ipv4);

		var value = JsonNode.Parse("\"100.2.5444.3\"");

		var result = await schema.Evaluate(value, new EvaluationOptions { RequireFormatValidation = true });

		Assert.False(result.IsValid);
	}

	[TestCase("2023-04-28T21:51:26.56Z")]
	[TestCase("2023-03-22T07:56:28.610645938Z")]
	[TestCase("2023-03-22 07:56:28.610645938Z")]
	[TestCase("2023-04-28T21:50:24-00:00")]
	[TestCase("2023-04-29t09:50:36+12:00")]
	[TestCase("2023-04-28 21:50:44Z")]
	[TestCase("2023-04-28_21:50:58.563Z")]
	[TestCase("2023-04-28_21:51:10Z")]
	public async Task DateTime_Pass(string dateString)
	{
		JsonSchema schema = new JsonSchemaBuilder()
			.Format(Formats.DateTime);

		var value = JsonNode.Parse($"\"{dateString}\"");

		var result = await schema.Evaluate(value, new EvaluationOptions { RequireFormatValidation = true });

		Assert.True(result.IsValid);
	}

	private static readonly Uri _formatAssertionMetaSchemaId = new("https://json-everything/test/format-assertion");
	private static readonly JsonSchema _formatAssertionMetaSchema =
		new JsonSchemaBuilder()
			.Schema(MetaSchemas.Draft202012Id)
			.Id(_formatAssertionMetaSchemaId)
			.Vocabulary(
				(Vocabularies.Core202012Id, true),
				(Vocabularies.Applicator202012Id, true),
				(Vocabularies.Metadata202012Id, true),
				(Vocabularies.FormatAssertion202012Id, false)
			)
			.DynamicAnchor("meta")
			.Title("format assertion meta-schema")
			.AllOf(
				new JsonSchemaBuilder().Ref(MetaSchemas.Core202012Id),
				new JsonSchemaBuilder().Ref(MetaSchemas.Applicator202012Id),
				new JsonSchemaBuilder().Ref(MetaSchemas.Metadata202012Id),
				new JsonSchemaBuilder().Ref(MetaSchemas.FormatAssertion202012Id)
			)
			.Type(SchemaValueType.Object | SchemaValueType.Boolean);

	[Test]
	public async Task UnknownFormat_Annotation_ReportsFormat()
	{
		var schemaText = $@"{{
	""$schema"": ""{MetaSchemas.Draft202012Id}"",
	""type"": ""string"",
	""format"": ""something-dumb""
}}";
		var schema = JsonSchema.FromText(schemaText);
		var instance = JsonNode.Parse("\"a value\"");

		var results = await schema.Evaluate(instance, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });

		results.AssertValid();
		var serialized = JsonSerializer.Serialize(results);
		Assert.IsTrue(serialized.Contains("something-dumb"));
	}

	[Test]
	public async Task UnknownFormat_Assertion_FailsValidation()
	{
		var options = new EvaluationOptions
		{
			OutputFormat = OutputFormat.Hierarchical,
			OnlyKnownFormats = true
		};

		var schemaText = $@"{{
	""$schema"": ""{_formatAssertionMetaSchemaId}"",
	""type"": ""string"",
	""format"": ""something-dumb""
}}";
		var schema = JsonSchema.FromText(schemaText);
		await options.SchemaRegistry.Register(_formatAssertionMetaSchema);
		var instance = JsonNode.Parse("\"a value\"");

		var results = await schema.Evaluate(instance, options);

		results.AssertInvalid();
		var serialized = JsonSerializer.Serialize(results);
		Assert.IsTrue(serialized.Contains("something-dumb"));
	}

	[Test]
	public async Task UnknownFormat_AnnotationWithAssertionOption_FailsValidation()
	{
		var schemaText = $@"{{
	""$schema"": ""{MetaSchemas.Draft202012Id}"",
	""type"": ""string"",
	""format"": ""something-dumb""
}}";
		var schema = JsonSchema.FromText(schemaText);
		var instance = JsonNode.Parse("\"a value\"");

		var results = await schema.Evaluate(instance, new EvaluationOptions
		{
			OutputFormat = OutputFormat.Hierarchical,
			RequireFormatValidation = true,
			OnlyKnownFormats = true
		});

		results.AssertInvalid();
		var serialized = JsonSerializer.Serialize(results);
		Assert.IsTrue(serialized.Contains("something-dumb"));
	}

	private class RegexBasedFormat : RegexFormat
	{
		public RegexBasedFormat()
			: base("hexadecimal", "^[0-9a-fA-F]+$")
		{
		}
	}

	[TestCase("\"1dd7fe33f97f42cf89c5789018bae64d\"", true)]
	[TestCase("\"nwvoiwe;oiabe23oi32\"", false)]
	[TestCase("true", true)]
	public async Task RegexBasedFormatWorksProperly(string jsonText, bool isValid)
	{
		Formats.Register(new RegexBasedFormat());

		var json = JsonNode.Parse(jsonText);
		JsonSchema schema = new JsonSchemaBuilder()
			.Format("hexadecimal");

		var results = await schema.Evaluate(json, new EvaluationOptions
		{
			OutputFormat = OutputFormat.Hierarchical,
			RequireFormatValidation = true
		});

		Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
		Assert.AreEqual(isValid, results.IsValid);
	}
}