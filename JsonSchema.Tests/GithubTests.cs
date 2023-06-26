using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Json.More;
using NUnit.Framework;
#pragma warning disable CS1998

namespace Json.Schema.Tests;

public class GithubTests
{
	private static string GetFile(int issue, string name)
	{
		return Path.Combine(TestContext.CurrentContext.WorkDirectory, "Files", $"Issue{issue}_{name}.json")
			.AdjustForPlatform();
	}

	private static string GetResource(int issue, string name)
	{
		return File.ReadAllText(GetFile(issue, name));
	}

	[Test]
	public async Task Issue18_SomethingNotValidatingRight()
	{
		var instance = JsonNode.Parse(@"{
    ""prop1"": {
        ""name"": ""a"",
        ""version"": 1
    },
    ""prop2"": {},
    ""prop4"": ""a"",
    ""prop5"": {},
    ""prop6"": {
        ""firstId"": ""428de96d-d5b2-4d12-8e88-37827099dd02"",
        ""secondId"": ""428de96d-d5b2-4d12-8e88-37827099dd02"",
        ""version"": ""test-version"",
        ""thirdId"": ""428de96d-d5b2-4d12-8e88-37827099dd02"",
        ""type"": ""test"",
        ""name"": ""testApp"",
        ""receiptTimestamp"": ""2019-02-05T12:36:31.2812022Z"",
        ""timestamp"": ""2012-04-21T12:36:31.2812022Z"",
        ""extra_key"": ""extra_val""
    },
    ""prop3"": {
        ""prop5"": {},
        ""metadata"": {},
        ""deleteAfter"": 3,
        ""allowExport"": true
    }
}");
		var schema = JsonSchema.FromText(@"{
	""$schema"": ""http://json-schema.org/draft-07/schema#"",
	""type"": ""object"",
	""required"": [""prop1"", ""prop2"", ""prop3"", ""prop4"", ""prop5"", ""prop6""],
	""properties"": {
	    ""prop1"": {
	        ""type"": ""object"",
	        ""required"": [""name"", ""version""],
	        ""additionalProperties"": false,
	        ""properties"": {
	            ""name"": {
	                ""type"": ""string"",
	                ""pattern"": ""^[-_]?([a-zA-Z][-_]?)+$""
	            },
	            ""version"": {
	                ""type"": ""integer"",
	                ""minimum"": 1
	            }
	        }
	    },
	    ""prop2"": {
	        ""$ref"": ""http://json-schema.org/draft-07/schema#""
	    },
	    ""prop3"": {
	        ""type"": ""object"",
	        ""required"": [
	        ""prop5"",
	        ""metadata""
	        ],
	        ""additionalProperties"": false,
	        ""properties"": {
	            ""prop5"": {
	                ""type"": ""object""
	            },
	            ""metadata"": {
	                ""type"": ""object""
	            },
	            ""deleteAfter"": {
	                ""type"": ""integer""
	            },
	            ""allowExport"": {
	                ""type"": ""boolean""
	            }
	        }
	    },
	    ""prop4"": {
	        ""type"": ""string"",
	        ""pattern"": ""^[-_]?([a-zA-Z][-_]?)+$""
	    },
	    ""prop5"": {
	        ""type"": ""object""
	    },
	    ""prop6"": {
	        ""type"": ""object"",
	        ""required"": [
	        ""firstId"",
	        ""secondId"",
	        ""version"",
	        ""thirdId"",
	        ""type"",
	        ""name"",
	        ""receiptTimestamp"",
	        ""timestamp""
	        ],
	        ""properties"": {
	            ""firstId"": {
	                ""type"": ""string"",
	                ""pattern"": ""[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}""
	            },
	            ""secondId"": {
	                ""type"": ""string"",
	                ""pattern"": ""[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}""
	            },
	            ""type"": {
	                ""type"": ""string"",
	                ""enum"": [""test"", ""lab"", ""stage"", ""prod""]
	            },
	            ""thirdId"": {
	                ""type"": ""string"",
	                ""pattern"": ""[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}""
	            },
	            ""version"": {
	                ""type"": ""string"",
	                ""minLength"": 1
	            },
	            ""name"": {
	                ""type"": ""string"",
	                ""minLength"": 1
	            },
	            ""receiptTimestamp"": {
	                ""type"": ""string"",
	                ""format"": ""date-time""
	            },
	            ""timestamp"": {
	                ""type"": ""string"",
	                ""format"": ""date-time""
	            }
	        },
	        ""additionalProperties"": {
	            ""type"": ""string""
	        }
	    }
	},
	""additionalProperties"": false
}");

		var result = await schema.Evaluate(instance, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });

		result.AssertValid();
	}

	[Test]
	public void Issue19_Draft4ShouldInvalidateAsUnrecognizedSchema_NoOption()
	{
		var schema = JsonSchema.FromText("{\"$schema\":\"http://json-schema.org/draft-04/schema#\",\"type\":\"string\"}");
		var instance = JsonNode.Parse("\"some string\"");

		Assert.ThrowsAsync<JsonSchemaException>(() => schema.Evaluate(instance));
	}

	[Test]
	public void Issue19_Draft4ShouldInvalidateAsUnrecognizedSchema_WithOption()
	{
		var schema = JsonSchema.FromText("{\"$schema\":\"http://json-schema.org/draft-04/schema#\",\"type\":\"string\"}");
		var instance = JsonNode.Parse("\"some string\"");

		Assert.ThrowsAsync<JsonSchemaException>(() => schema.Evaluate(instance, new EvaluationOptions
		{
			OutputFormat = OutputFormat.Hierarchical
		}));
	}

	[TestCase(SpecVersion.Draft7, @"{}", true)]
	[TestCase(SpecVersion.Draft7, @"{""abc"": 1}", false)]
	[TestCase(SpecVersion.Draft7, @"{""abc"": 1, ""d7"": 7}", true)]
	[TestCase(SpecVersion.Draft7, @"{""abc"": 1, ""d9"": 9}", false)]
	[TestCase(SpecVersion.Draft7, @"{""abc"": 1, ""d7"": 7, ""d9"": 9}", true)]
	[TestCase(SpecVersion.Draft201909, @"{}", true)]
	[TestCase(SpecVersion.Draft201909, @"{""abc"": 1}", false)]
	[TestCase(SpecVersion.Draft201909, @"{""abc"": 1, ""d7"": 7}", false)]
	[TestCase(SpecVersion.Draft201909, @"{""abc"": 1, ""d9"": 9}", true)]
	[TestCase(SpecVersion.Draft201909, @"{""abc"": 1, ""d7"": 7, ""d9"": 9}", true)]
	public async Task Issue19_SchemaShouldOnlyUseSpecifiedDraftKeywords(SpecVersion version, string instance, bool isValid)
	{
		var schema = JsonSerializer.Deserialize<JsonSchema>(@"
{
    ""dependencies"": {
        ""abc"": [ ""d7"" ]
    },
    ""dependentRequired"": {
        ""abc"": [ ""d9"" ]
    }
}")!;
		var opts = new EvaluationOptions
		{
			EvaluateAs = version,
			OutputFormat = OutputFormat.Hierarchical
		};
		var element = JsonNode.Parse(instance);

		var val = await schema.Evaluate(element, opts);
		Console.WriteLine("Elem `{0}` got validation `{1}`", instance, val.IsValid);
		if (isValid) val.AssertValid();
		else val.AssertInvalid();
	}

	[Test]
	public async Task Issue29_SchemaFromFileWithoutIdShouldInheritUriFromFilePath()
	{
		var schemaFile = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Files", "issue29-schema-without-id.json")
			.AdjustForPlatform();

		var jsonStr = @"{
  ""abc"": {
    ""abc"": {
        ""abc"": ""abc""
    }
  }
}";
		var schema = JsonSchema.FromFile(schemaFile);
		var json = JsonNode.Parse(jsonStr);
		var validation = await schema.Evaluate(json, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });

		validation.AssertValid();
	}

	[Test]
	public async Task Issue29_SchemaFromFileWithIdShouldKeepUriFromId()
	{
		var schemaFile = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Files", "issue29-schema-with-id.json")
			.AdjustForPlatform();

		var jsonStr = @"{
  ""abc"": {
    ""abc"": {
        ""abc"": ""abc""
    }
  }
}";
		var schema = JsonSchema.FromFile(schemaFile);
		var json = JsonNode.Parse(jsonStr);
		var validation = await schema.Evaluate(json, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });

		validation.AssertValid();
	}

	[Test]
	public async Task Issue29_SchemaWithOnlyFileNameIdShouldUseDefaultBaseUri()
	{
		var schemaStr = @"{
  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
  ""$id"": ""mySchema.json"",
  ""properties"": {
      ""abc"": { ""$ref"": ""mySchema.json"" }
  },
  ""additionalProperties"": false
}";
		var jsonStr = @"{
  ""abc"": {
    ""abc"": {
        ""abc"": ""abc""
    }
  }
}";
		var schema = JsonSerializer.Deserialize<JsonSchema>(schemaStr)!;
		var json = JsonNode.Parse(jsonStr);
		var validation = await schema.Evaluate(json, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });

		validation.AssertValid();
	}

	[Test]
	public void Issue76_GetHashCodeIsNotConsistent()
	{
		var schema1Str = @"
{
  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
  ""required"": [
    ""a"",
    ""b""
  ],
  ""properties"": {
    ""a"": { ""const"": ""a"" },
    ""b"": { ""const"": ""b"" }
  }
}";

		var schema2Str = @"
{
  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
  ""required"": [
    ""b"",
    ""a""
  ],
  ""properties"": {
    ""a"": { ""const"": ""a"" },
    ""b"": { ""const"": ""b"" }
  }
}";

		var schema3Str = @"
{
  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
  ""required"": [
    ""a"",
    ""b""
  ],
  ""properties"": {
    ""b"": { ""const"": ""b"" },
    ""a"": { ""const"": ""a"" }
  }
}";

		var schema4Str = @"
{
  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
  ""properties"": {
    ""a"": { ""const"": ""a"" },
    ""b"": { ""const"": ""b"" }
  },
  ""required"": [
    ""a"",
    ""b""
  ]
}";
		var schema1 = JsonSerializer.Deserialize<JsonSchema>(schema1Str)!;
		var schema2 = JsonSerializer.Deserialize<JsonSchema>(schema2Str)!;
		var schema3 = JsonSerializer.Deserialize<JsonSchema>(schema3Str)!;
		var schema4 = JsonSerializer.Deserialize<JsonSchema>(schema4Str)!;

		Assert.IsTrue(schema1.Equals(schema2));
		Assert.AreEqual(schema1.GetHashCode(), schema2.GetHashCode());
		Assert.IsTrue(schema1.Equals(schema3));
		Assert.AreEqual(schema1.GetHashCode(), schema3.GetHashCode());
		Assert.IsTrue(schema1.Equals(schema4));
		Assert.AreEqual(schema1.GetHashCode(), schema4.GetHashCode());
	}

	[Test]
	public void Issue76_GetHashCodeIsNotConsistent_WhitespaceInObjectValue()
	{
		var schema1Str = @"
{
  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
  ""required"": [
    ""a"",
    ""b""
  ],
  ""properties"": {
    ""a"": { ""const"": { ""left"": ""right"" } },
    ""b"": { ""const"": ""b"" }
  }
}";

		var schema2Str = @"
{
  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
  ""required"": [
    ""b"",
    ""a""
  ],
  ""properties"": {
    ""a"": { ""const"": {""left"":""right""} },
    ""b"": { ""const"": ""b"" }
  }
}";
		var schema1 = JsonSerializer.Deserialize<JsonSchema>(schema1Str)!;
		var schema2 = JsonSerializer.Deserialize<JsonSchema>(schema2Str)!;

		Assert.IsTrue(schema1.Equals(schema2));
		Assert.AreEqual(schema1.GetHashCode(), schema2.GetHashCode());
	}

	[Test]
	public async Task Issue79_RefsTryingToResolveParent()
	{
		var schema1Str = @"
{
  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
  ""$id"": ""schema1.json"",
  ""definitions"": {
    ""myDef"": {
      ""properties"": {
        ""abc"": { ""type"": ""string"" }
      }
    }
  },
  ""$ref"": ""#/definitions/myDef""
}";
		var schema2Str = @"
{
  ""$schema"": ""http://json-schema.org/draft-07/schema#"",
  ""$id"": ""schema2.json"",
  ""$ref"": ""schema1.json""
}";
		var jsonStr = @"{ ""abc"": ""s"" }";
		var schema1 = JsonSerializer.Deserialize<JsonSchema>(schema1Str)!;
		var schema2 = JsonSerializer.Deserialize<JsonSchema>(schema2Str)!;
		var json = JsonNode.Parse(jsonStr);
		var uri1 = new Uri("https://json-everything.net/schema1.json");
		var uri2 = new Uri("https://json-everything.net/schema2.json");
		var map = new Dictionary<Uri, JsonSchema>
		{
			{ uri1, schema1 },
			{ uri2, schema2 },
		};
		var options = new EvaluationOptions
		{
			OutputFormat = OutputFormat.Hierarchical,
			SchemaRegistry =
			{
				Fetch = async uri =>
				{
					Assert.True(map.TryGetValue(uri, out var ret), "Unexpected uri: {0}", uri);
					return ret;
				}
			}
		};
		var result = await schema2.Evaluate(json, options);
		result.AssertValid();
		Assert.AreEqual(result.Details[0].Details[0].SchemaLocation, "https://json-everything.net/schema1.json#");
	}

	[Test]
	public async Task Issue79_RefsTryingToResolveParent_Explanation()
	{
		var schemaText = @"{
  ""$id"": ""https://mydomain.com/outer"",
  ""properties"": {
    ""foo"": {
      ""$id"": ""https://mydomain.com/foo"",
      ""properties"": {
        ""inner1"": {
          ""$anchor"": ""bar"",
          ""type"": ""string""
        },
        ""inner2"": {
          ""$ref"": ""#bar""
        }
      }
    },
    ""bar"": {
      ""$anchor"": ""bar"",
      ""type"": ""integer""
    }
  }
}";
		var passingText = @"
{
  ""foo"": {
    ""inner2"": ""value""
  }
}";
		var failingText = @"
{
  ""foo"": {
    ""inner2"": 42
  }
}";

		var schema = JsonSerializer.Deserialize<JsonSchema>(schemaText);
		var passing = JsonNode.Parse(passingText);
		var failing = JsonNode.Parse(failingText);

		var passingResult = await schema!.Evaluate(passing, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
		passingResult.AssertValid();
		var failingResult = await schema.Evaluate(failing, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
		failingResult.AssertInvalid();
	}

	[Test]
	public void Issue97_IdentifyCircularReferences()
	{
		JsonSchema schema = new JsonSchemaBuilder()
			.Ref("#/$defs/string")
			.Defs(("string", new JsonSchemaBuilder().Ref("#/$defs/string")));

		var json = JsonNode.Parse("\"value\"");

		Assert.ThrowsAsync<JsonSchemaException>(() => schema.Evaluate(json));
	}

	[Test]
	public void Issue97_IdentifyComplexCircularReferences()
	{
		JsonSchema schema = new JsonSchemaBuilder()
			.Ref("#/$defs/a")
			.Defs(
				("a", new JsonSchemaBuilder().Ref("#/$defs/b")),
				("b", new JsonSchemaBuilder().Ref("#/$defs/a"))
			);

		var json = JsonNode.Parse("\"value\"");

		Assert.ThrowsAsync<JsonSchemaException>(() => schema.Evaluate(json));
	}

	[SchemaKeyword(Name)]
	[SchemaSpecVersion(SpecVersion.Draft201909 | SpecVersion.Draft202012)]
	private class MinDateKeyword : IJsonSchemaKeyword, IEquatable<MinDateKeyword>
	{
		// ReSharper disable once InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
		private const string Name = "minDate";
#pragma warning restore IDE1006 // Naming Styles

		public bool Equals(MinDateKeyword? other)
		{
			throw new NotImplementedException();
		}

		public Task Evaluate(EvaluationContext context, CancellationToken token)
		{
			throw new NotImplementedException();
		}
	}

	[Test]
	public void Issue191_SelfReferentialCustomMetaschemaShouldError()
	{
		var metaSchemaId = new Uri("https://myserver.net/meta-schema");

		var vocabId = "https://myserver.net/my-vocab";

		var metaSchema = JsonSchema.FromText(GetResource(191, "MetaSchema"));

		SchemaKeywordRegistry.Register<MinDateKeyword>();

		VocabularyRegistry.Global.Register(new Vocabulary(vocabId, typeof(MinDateKeyword)));

		Assert.ThrowsAsync<JsonSchemaException>(() => SchemaRegistry.Global.Register(metaSchemaId, metaSchema));
	}

	[Test]
	public async Task Issue208_BundlingNotWorking()
	{
		JsonSchema externalSchema = new JsonSchemaBuilder()
			.Schema(MetaSchemas.Draft202012Id)
			.Id("https://my-external-schema")
			.Type(SchemaValueType.Object)
			.Properties(
				("first", new JsonSchemaBuilder().Type(SchemaValueType.String))
			);

		var options = new EvaluationOptions
		{
			OutputFormat = OutputFormat.Hierarchical
		};
		await options.SchemaRegistry.Register(new Uri("https://my-external-schema"), externalSchema);

		JsonSchema mySchema = new JsonSchemaBuilder()
			.Schema(MetaSchemas.Draft202012Id)
			.Id("https://my-schema")
			.Type(SchemaValueType.Object)
			.Properties(
				("first", new JsonSchemaBuilder().Ref("https://my-external-schema")),
				("second", new JsonSchemaBuilder()
					.Schema(MetaSchemas.Draft202012Id)
					.Id("https://my-inner-schema")
					.Type(SchemaValueType.Object)
					.Properties(
						("second", new JsonSchemaBuilder().Ref("#/$defs/my-inner-ref"))
					)
					.Defs(
						("my-inner-ref", new JsonSchemaBuilder().Type(SchemaValueType.String))
					)
				)
			);

		var instance = JsonNode.Parse("{\"first\":{\"first\":\"first\"},\"second\":{\"second\":\"second\"}}");

		var result = await mySchema.Evaluate(instance, options);
		result.AssertValid();

	}

	[Test]
	public void Issue212_CouldNotResolveAnchorReference_FromFile()
	{
		// This validation fails because the file uses `id` instead of `$id`.
		// See https://github.com/gregsdennis/json-everything/issues/212#issuecomment-1033423550
		var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "Files", "issue212_schema.json")
			.AdjustForPlatform();
		var schema = JsonSchema.FromFile(path);

		var instance = JsonNode.Parse("{\"ContentDefinitionId\": \"fa81bc1d-3efe-4192-9e03-31e9898fef90\"}");

		Assert.ThrowsAsync<JsonSchemaException>(() => schema.Evaluate(instance, new EvaluationOptions
		{
			ValidateAgainstMetaSchema = true
		}));
	}

	[Test]
	public async Task Issue212_CouldNotResolveAnchorReference_Inline()
	{
		JsonSchema schema = new JsonSchemaBuilder()
			.Schema(MetaSchemas.Draft7Id)
			.Id("http://messagebroker.fff.pl/rpn/dci/kkt.json")
			.Type(SchemaValueType.Object)
			.Title("JSON Schema ")
			.Definitions(
				("#guid", new JsonSchemaBuilder()
					.Id("#guid")
					.Title("Definicja obligatoryjnego GUID (regex)")
					.Type(SchemaValueType.String)
					.Examples(
						"09C9A8DA-B40F-4E3A-9746-7B10AFEC2C4F",
						"2d9bbb00-878f-49b0-9d48-767ce3e12dee"
					)
					.MinLength(36)
					.MaxLength(36)
					.Pattern("^([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$")
				)
			)
			.Required("ContentDefinitionId")
			.Properties(
				("ContentDefinitionId", new JsonSchemaBuilder()
					.Ref("#guid")
					.Title("Identyfikator definicji ")
				)
			)
			.AdditionalProperties(false);

		var instance = JsonNode.Parse("{\"ContentDefinitionId\": \"fa81bc1d-3efe-4192-9e03-31e9898fef90\"}");

		var res = await schema.Evaluate(instance, new EvaluationOptions
		{
			OutputFormat = OutputFormat.Hierarchical,
			ValidateAgainstMetaSchema = true
		});
		res.AssertValid();
	}

	[Test]
	public async Task Issue216_AdditionalPropertiesShouldRelyOnDeclarationsForDraft7()
	{
		JsonSchema schema = new JsonSchemaBuilder()
			.Schema(MetaSchemas.Draft7Id)
			.Type(SchemaValueType.Object)
			.Properties(
				("foo", new JsonSchemaBuilder().Type(SchemaValueType.Integer)),
				("bar", new JsonSchemaBuilder().Type(SchemaValueType.String))
			)
			.AdditionalProperties(false);

		var instance = JsonNode.Parse("{\"foo\":1,\"bar\":false}");

		var result = await schema.Evaluate(instance, new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });

		result.AssertInvalid();
		var nodes = new List<EvaluationResults> { result };
		while (nodes.Any())
		{
			var node = nodes.First();
			nodes.Remove(node);
			Assert.AreNotEqual("#/additionalProperties", node.EvaluationPath.ToString());
			nodes.AddRange(node.Details);
		}
	}

	[Test]
	public async Task Issue226_MessageInValidResult()
	{
		var schemaText = GetResource(226, "schema");
		var instanceText = GetResource(226, "instance");

		var schema = JsonSchema.FromText(schemaText);
		var instance = JsonNode.Parse(instanceText);

		var result = await schema.Evaluate(instance, new EvaluationOptions { OutputFormat = OutputFormat.List });

		result.AssertValid();
	}

	[Test]
	public async Task Issue352_ConcurrentValidationsWithReferences()
	{
		var schema = JsonSchema.FromText(@"{
            ""$schema"": ""http://json-schema.org/draft-07/schema#"",
            ""type"": ""object"",
            ""properties"": {
                ""id"": {
                    ""type"": ""integer""
                },        
                ""interval1"": {
                    ""$ref"": ""#/components/schemas/interval""
                }
            },
            ""components"": {
                ""schemas"": {
                    ""interval"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""from"": {
                                ""type"": ""number""
                            },
                            ""to"": {
                                ""type"": ""number""
                            }
                        }
                    }
                }
            }
            }");

		var instance = new JsonObject
		{
			["id"] = 123,
			["interval1"] = new JsonObject
			{
				["to"] = 3.0
			}
		};

		var numberOfMessages = 100;
		var jsonMessages = new List<JsonNode?>();
		for (int j = 0; j < numberOfMessages; j++)
		{
			jsonMessages.Add(instance.Copy());
		}

		await Task.WhenAll(jsonMessages.Select(async json =>
		{
			EvaluationResults result;
			try
			{
				result = await schema.Evaluate(json, new EvaluationOptions
				{
					OutputFormat = OutputFormat.List,
					RequireFormatValidation = true
				});
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}

			result.AssertValid();
		}));
	}

	[Test]
	public async Task Issue417_BundleWithItemsFails()
	{
		// items: true
		var singleItemSchema = new JsonSchemaBuilder()
			.Items(JsonSchema.True)
			.Build();
		await singleItemSchema.Bundle(); // throws
		// items: [true, true]
		var multiItemSchema = new JsonSchemaBuilder()
			.Items(new[] { JsonSchema.True, JsonSchema.True })
			.Build();
		await multiItemSchema.Bundle(); // throws
	}

	[Test]
	public async Task Issue419_SecondLevelReferences()
	{
		var level2 = JsonSchema.FromFile(GetFile(419, "level2"));
		await SchemaRegistry.Global.Register(level2);

		var level1 = JsonSchema.FromFile(GetFile(419, "level1"));
		await SchemaRegistry.Global.Register(level1);

		var rootSchema = JsonSchema.FromFile(GetFile(419, "root"));

		var config = JsonNode.Parse(GetResource(419, "config"));

		var result = await rootSchema.Evaluate(config);

		result.AssertValid();
	}

	[Test]
	public async Task Issue426_FileFragmentRefs()
	{
		var schema = JsonSchema.FromFile(GetFile(426, "schema"));
		await SchemaRegistry.Global.Register(schema);
		
		var data = JsonNode.Parse(await File.ReadAllTextAsync(GetFile(426, "data")));
		
		var result = await schema.Evaluate(data);

		result.AssertValid();
	}

	[Test]
	public async Task Issue432_UnresolvedLocalRef()
	{
		var schema = JsonSchema.FromText(@"{
  ""$schema"": ""http://json-schema.org/draft-06/schema#"",
  ""$ref"": ""#/definitions/Order"",
  ""definitions"": {
    ""Order"": {
      ""type"": ""object"",
      ""additionalProperties"": true,
      ""properties"": {
        ""orderId"": {
          ""type"": ""string"",
          ""format"": ""uuid""
        }
      },
      ""required"": [
        ""orderId""
      ],
      ""title"": ""Order""
    }
  }
}");
		var instance = JsonNode.Parse("{ \"orderId\": \"3cb65f2d-4049-43c1-b185-1943765acd9\" }");

		var result = await schema.Evaluate(instance);
		
		result.AssertValid();
	}

	[Test]
	public async Task Issue432_UnresolvedLocalRef_Again()
	{
		var schema = JsonSchema.FromText(@"{
  ""$schema"": ""http://json-schema.org/draft-06/schema#"",
  ""$ref"": ""#/definitions/Order"",
  ""definitions"": {
    ""Order"": {
      ""type"": ""object"",
      ""additionalProperties"": true,
      ""properties"": {
        ""orderId"": {
          ""type"": ""string"",
          ""format"": ""uuid""
        },
        ""customer"": {
          ""$ref"": ""#/definitions/Customer""
        }
      },
      ""required"": [
        ""customer"",
        ""orderId""
      ],
      ""title"": ""Order""
    },
    ""Customer"": {
      ""type"": ""object"",
      ""additionalProperties"": false,
      ""properties"": {
        ""name"": {
          ""type"": ""string""
        }
      },
      ""required"": [
        ""name""
      ],
      ""title"": ""Customer""
    }
  }
}");
		var instance = JsonNode.Parse(@"{
  ""orderId"": ""3cb65f2d-4049-4c1-b185-194365acdf99"",
  ""customer"": {
    ""name"": ""Some Customer""
  }
}");

		var result = await schema.Evaluate(instance);
		
		result.AssertValid();
	}

	[Test]
	public async Task Issue435_NonCircularRefThrowing()
	{
		var schema = JsonSchema.FromText(@"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",

  ""type"": ""array"",
  ""items"": { ""$ref"": ""#/$defs/DerivedType"" },

  ""$defs"": {

    ""BaseType"": {
      ""type"": ""object"",
      ""properties"": {
        ""field1"": { ""type"": ""string"" }
      }
    },

    ""DerivedType"": {
      ""allOf"": [
        { ""$ref"": ""#/$defs/BaseType"" },
        { ""properties"": { ""field2"": { ""type"": ""string"" } } }
      ]
    }
  }
}");

		//var instance = new JsonArray
		//{
		//	new JsonObject
		//	{
		//		["field1"] = "foo",
		//		["field2"] = "bar"
		//	}
		//};

		var instance = JsonNode.Parse(@"[
  {
    ""field1"": ""foo"",
    ""field2"": ""bar""
  }
]");

		var result = await schema.Evaluate(instance, new EvaluationOptions{OutputFormat = OutputFormat.List});

		result.AssertValid();
	}

	[Test]
	public async Task Issue435_NonCircularRefThrowing_File()
	{
		var file = GetFile(435, "schema");

		var schema = JsonSchema.FromFile(file);

		var instance = new JsonArray
		{
			new JsonObject
			{
				["field1"] = "foo",
				["field2"] = "bar"
			}
		};

		var result = await schema.Evaluate(instance, new EvaluationOptions{OutputFormat = OutputFormat.List});

		result.AssertValid();
	}
}