﻿using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Pointer;

namespace Json.Schema;

/// <summary>
/// A serializer context for this library.
/// </summary>
[JsonSerializable(typeof(JsonSchema))]
[JsonSerializable(typeof(AdditionalItemsKeyword))]
[JsonSerializable(typeof(AdditionalPropertiesKeyword))]
[JsonSerializable(typeof(AllOfKeyword))]
[JsonSerializable(typeof(AnchorKeyword))]
[JsonSerializable(typeof(AnyOfKeyword))]
[JsonSerializable(typeof(CommentKeyword))]
[JsonSerializable(typeof(ConstKeyword))]
[JsonSerializable(typeof(ContainsKeyword))]
[JsonSerializable(typeof(ContentEncodingKeyword))]
[JsonSerializable(typeof(ContentMediaTypeKeyword))]
[JsonSerializable(typeof(ContentSchemaKeyword))]
[JsonSerializable(typeof(DefaultKeyword))]
[JsonSerializable(typeof(DefinitionsKeyword))]
[JsonSerializable(typeof(DefsKeyword))]
[JsonSerializable(typeof(DependenciesKeyword))]
[JsonSerializable(typeof(DependentRequiredKeyword))]
[JsonSerializable(typeof(DependentSchemasKeyword))]
[JsonSerializable(typeof(DeprecatedKeyword))]
[JsonSerializable(typeof(DescriptionKeyword))]
[JsonSerializable(typeof(DynamicAnchorKeyword))]
[JsonSerializable(typeof(DynamicRefKeyword))]
[JsonSerializable(typeof(ElseKeyword))]
[JsonSerializable(typeof(EnumKeyword))]
[JsonSerializable(typeof(ExamplesKeyword))]
[JsonSerializable(typeof(ExclusiveMaximumKeyword))]
[JsonSerializable(typeof(ExclusiveMinimumKeyword))]
[JsonSerializable(typeof(FormatKeyword))]
[JsonSerializable(typeof(IdKeyword))]
[JsonSerializable(typeof(IfKeyword))]
[JsonSerializable(typeof(ItemsKeyword))]
[JsonSerializable(typeof(MaxContainsKeyword))]
[JsonSerializable(typeof(MaximumKeyword))]
[JsonSerializable(typeof(MaxItemsKeyword))]
[JsonSerializable(typeof(MaxLengthKeyword))]
[JsonSerializable(typeof(MaxPropertiesKeyword))]
[JsonSerializable(typeof(MinContainsKeyword))]
[JsonSerializable(typeof(MinimumKeyword))]
[JsonSerializable(typeof(MinItemsKeyword))]
[JsonSerializable(typeof(MinLengthKeyword))]
[JsonSerializable(typeof(MinPropertiesKeyword))]
[JsonSerializable(typeof(MultipleOfKeyword))]
[JsonSerializable(typeof(NotKeyword))]
[JsonSerializable(typeof(OneOfKeyword))]
[JsonSerializable(typeof(PatternKeyword))]
[JsonSerializable(typeof(PatternPropertiesKeyword))]
[JsonSerializable(typeof(PrefixItemsKeyword))]
[JsonSerializable(typeof(PropertiesKeyword))]
[JsonSerializable(typeof(PropertyDependenciesKeyword))]
[JsonSerializable(typeof(PropertyNamesKeyword))]
[JsonSerializable(typeof(ReadOnlyKeyword))]
[JsonSerializable(typeof(RecursiveAnchorKeyword))]
[JsonSerializable(typeof(RecursiveRefKeyword))]
[JsonSerializable(typeof(RefKeyword))]
[JsonSerializable(typeof(RequiredKeyword))]
[JsonSerializable(typeof(SchemaKeyword))]
[JsonSerializable(typeof(ThenKeyword))]
[JsonSerializable(typeof(TitleKeyword))]
[JsonSerializable(typeof(TypeKeyword))]
[JsonSerializable(typeof(UnevaluatedItemsKeyword))]
[JsonSerializable(typeof(UnevaluatedPropertiesKeyword))]
[JsonSerializable(typeof(UniqueItemsKeyword))]
[JsonSerializable(typeof(UnrecognizedKeyword))]
[JsonSerializable(typeof(VocabularyKeyword))]
[JsonSerializable(typeof(WriteOnlyKeyword))]
[JsonSerializable(typeof(SchemaOrPropertyList))]
[JsonSerializable(typeof(PropertyDependency))]
[JsonSerializable(typeof(SchemaValueType))]
[JsonSerializable(typeof(EvaluationResults))]
[JsonSerializable(typeof(JsonPointer))]
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(IReadOnlyCollection<JsonNode>))]
[JsonSerializable(typeof(uint))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Dictionary<string, string[]>))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int[]))]
[JsonSerializable(typeof(Uri))]
#if DEBUG
[JsonSerializable(typeof(Experiments.EvaluationResults), TypeInfoPropertyName = "ExperimentsEvaluationResults")]
#endif
internal partial class JsonSchemaSerializerContext : JsonSerializerContext;