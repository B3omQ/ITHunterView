using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Utils;

namespace ITHunterview.Service.Utils
{
    public sealed class ValidatedJobAnalysis
    {
        public string SchemaVersion { get; set; } = "jd-analysis/v2";
        public JdAnalysisQuality Quality { get; set; } = JdAnalysisQuality.COMPLETE;
        public List<string> JobTitlesNormalized { get; set; } = new();
        public List<ValidatedSkillMention> SkillsNormalized { get; set; } = new();
        public int TotalYearsExp { get; set; }
        public List<string> Domains { get; set; } = new();
        public List<ValidatedRequirementItem> RequirementsList { get; set; } = new();
        public List<ValidatedRequirementGroup> RequirementGroups { get; set; } = new();
        public JdAnalysisCoverage Coverage { get; set; } = new(0, 0, 0, 0, 0, 0, true);
        public List<JdAnalysisDiagnostic> Diagnostics { get; set; } = new();
    }

        public sealed class ValidatedSkillMention
        {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "tech_skill";
        public string RawMention { get; set; } = string.Empty;
        public string SourceSection { get; set; } = "requirements";
        public string Evidence { get; set; } = string.Empty;
            public decimal Confidence { get; set; } = 1.0m;
            public string Importance { get; set; } = "nice_to_have";
    }

    public sealed class ValidatedRequirementItem
    {
        public string ItemId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Importance { get; set; } = "must_have";
        public string SkillName { get; set; } = string.Empty;
        public string DetailVerbatim { get; set; } = string.Empty;
        public string RawMention { get; set; } = string.Empty;
        public string SourceSection { get; set; } = "requirements";
        public string Evidence { get; set; } = string.Empty;
        public List<string> Evidences { get; set; } = new();
        public int? MinYears { get; set; }
        public int? MaxYears { get; set; }
        public decimal Confidence { get; set; } = 1.0m;
    }

    public sealed class ValidatedRequirementGroup
    {
        public string GroupId { get; set; } = string.Empty;
        public string SourceRequirementId { get; set; } = string.Empty;
        public string Intent { get; set; } = JdAnalysisEffectiveContract.UnspecifiedIntent;
        public string Operator { get; set; } = "all_of";
        public int MinSatisfied { get; set; }
        public string Importance { get; set; } = "must_have";
        public string SourceSection { get; set; } = string.Empty;
        public string RequirementVerbatim { get; set; } = string.Empty;
        public List<ValidatedRequirementItem> Items { get; set; } = new();
    }

    public sealed class ValidationResult<T>
    {
        public bool IsValid { get; set; }
        public bool IsUsable => Data is not null && Quality != JdAnalysisQuality.INVALID;
        public JdAnalysisQuality Quality { get; set; } = JdAnalysisQuality.INVALID;
        public string? FailureCode { get; set; }
        public List<string> Errors { get; set; } = new();
        public T? Data { get; set; }
    }

    public interface IJdAnalysisResponseValidator
    {
        ValidationResult<ValidatedJobAnalysis> Validate(string providerOutput, JobAnalysisInputSnapshot input);
    }

        public class JdAnalysisResponseValidator : IJdAnalysisResponseValidator
        {
        private const int MaxProviderOutputCharacters = 262_144;
        private const int MaxRequirementGroups = 50;
        private const int MaxRequirementItems = 100;

        private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            "tech_skill", "experience", "domain_knowledge", "language", "education", "soft_skill"
        };

            private static readonly HashSet<string> AllowedImportances = new(StringComparer.OrdinalIgnoreCase)
        {
            "must_have", "nice_to_have"
            };

            private static readonly HashSet<string> AllowedSourceSections = new(StringComparer.OrdinalIgnoreCase)
            {
                "title", "description", "requirements"
            };

            private static readonly HashSet<string> SkillCandidateCategories = new(StringComparer.OrdinalIgnoreCase)
            {
                "tech_skill", "domain_knowledge", "language"
            };

        private static readonly HashSet<string> AllowedGroupOperators = new(StringComparer.OrdinalIgnoreCase)
        {
            "all_of", "one_of", "at_least_n"
        };

        public ValidationResult<ValidatedJobAnalysis> Validate(string providerOutput, JobAnalysisInputSnapshot input)
        {
            var result = new ValidationResult<ValidatedJobAnalysis>();
            if (string.IsNullOrWhiteSpace(providerOutput))
            {
                result.IsValid = false;
                result.FailureCode = "EMPTY_MODEL_OUTPUT";
                result.Errors.Add("AI provider returned empty response.");
                return result;
            }

            if (providerOutput.Length > MaxProviderOutputCharacters)
            {
                return Invalid(result, "JD_ANALYSIS_PAYLOAD_TOO_LARGE", "AI provider output exceeds the bounded JD analysis payload size.");
            }

            string jsonContent = ExtractJsonPayload(providerOutput);
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                result.IsValid = false;
                result.FailureCode = "INVALID_JSON_FORMAT";
                result.Errors.Add("Could not find a valid JSON object in AI provider output.");
                return result;
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(jsonContent, new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 64
                });
            }
            catch (JsonException ex)
            {
                result.IsValid = false;
                result.FailureCode = "INVALID_JSON_FORMAT";
                result.Errors.Add($"JSON parsing failed: {ex.Message}");
                return result;
            }

            using (doc)
            {
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    result.IsValid = false;
                    result.FailureCode = "INVALID_JSON_ROOT";
                    result.Errors.Add("Root element must be a JSON object.");
                    return result;
                }

                if (!TryGetMechanicalProperty(root, "schema_version", out var schemaProp, out _) ||
                    schemaProp.ValueKind != JsonValueKind.String ||
                    (schemaProp.GetString() != "jd-analysis/v2" &&
                     schemaProp.GetString() != "jd-analysis/v3" &&
                     schemaProp.GetString() != "jd-analysis/v4" &&
                     schemaProp.GetString() != JdAnalysisOutputSchema.ProviderSchemaVersion))
                {
                    result.IsValid = false;
                    result.FailureCode = "UNSUPPORTED_SCHEMA_VERSION";
                    result.Errors.Add("Missing or invalid schema_version. Expected a supported JD analysis contract.");
                    return result;
                }

                if (!TryGetMechanicalProperty(root, "matching_metrics", out var metricsProp, out _) ||
                    metricsProp.ValueKind != JsonValueKind.Object)
                {
                    result.IsValid = false;
                    result.FailureCode = "MISSING_MATCHING_METRICS";
                    result.Errors.Add("Missing 'matching_metrics' object.");
                    return result;
                }

                var schemaVersion = schemaProp.GetString()!;
                if (schemaVersion == JdAnalysisOutputSchema.ProviderSchemaVersion)
                {
                    return ValidateV5(metricsProp, result);
                }

                if (schemaVersion == "jd-analysis/v4")
                {
                    return ValidateV4(metricsProp, result);
                }

                var validated = new ValidatedJobAnalysis { SchemaVersion = schemaVersion };
                string fullInputText = CombineInputText(input);

                if (!TryGetRequiredArray(metricsProp, "job_titles_normalized", result, out var titlesProp) ||
                    !TryGetRequiredArray(metricsProp, "skills_normalized", result, out var skillsProp) ||
                    !TryGetRequiredArray(metricsProp, "domains", result, out var domProp) ||
                    !metricsProp.TryGetProperty("total_years_exp", out var expProp) ||
                    expProp.ValueKind != JsonValueKind.Number || !expProp.TryGetInt32(out var years))
                {
                    result.IsValid = false;
                    result.FailureCode ??= "INVALID_MATCHING_METRICS";
                    if (!metricsProp.TryGetProperty("total_years_exp", out _)) result.Errors.Add("Missing required numeric field 'total_years_exp'.");
                    else if (result.Errors.Count == 0) result.Errors.Add("'total_years_exp' must be an integer.");
                    return result;
                }

                var hasRequirementsList = TryGetRequiredArray(metricsProp, "requirements_list", result, out var reqsProp);
                var hasRequirementGroups = TryGetRequiredArray(metricsProp, "requirement_groups", result, out var groupsProp);
                if (schemaVersion == "jd-analysis/v2" && !hasRequirementsList) return result;
                if (schemaVersion == "jd-analysis/v3" && !hasRequirementGroups) return result;
                result.FailureCode = null;
                result.Errors.Clear();

                foreach (var titleElem in titlesProp.EnumerateArray())
                {
                    if (titleElem.ValueKind != JsonValueKind.String)
                    {
                        return Invalid(result, "INVALID_TITLE", "Every job title must be a string.");
                    }
                    AddCanonical(validated.JobTitlesNormalized, titleElem.GetString());
                }
                validated.TotalYearsExp = Math.Max(0, years);

                foreach (var dElem in domProp.EnumerateArray())
                {
                    if (dElem.ValueKind != JsonValueKind.String)
                    {
                        return Invalid(result, "INVALID_DOMAIN", "Every domain must be a string.");
                    }
                    AddCanonical(validated.Domains, dElem.GetString());
                }

                // The model must provide a structurally valid skill projection, but
                // requirements_list is canonical to prevent two divergent AI lists.
                foreach (var sElem in skillsProp.EnumerateArray())
                {
                    if (sElem.ValueKind != JsonValueKind.Object) return Invalid(result, "INVALID_SKILL", "Every skill must be an object.");

                    string category = ReadString(sElem, "category");
                    string name = ReadString(sElem, "name");
                    string rawMention = ReadString(sElem, "raw_mention");
                    string section = ReadString(sElem, "source_section");
                    string evidence = ReadString(sElem, "evidence");
                    if (!SkillCandidateCategories.Contains(category) ||
                        !AllowedSourceSections.Contains(section) ||
                        string.IsNullOrWhiteSpace(name) ||
                        string.IsNullOrWhiteSpace(rawMention) ||
                        string.IsNullOrWhiteSpace(evidence))
                    {
                        return Invalid(result, "INVALID_SKILL", "Every skill must use a skill category and include name, raw_mention, source_section, and evidence.");
                    }
                    if (!IsEvidencePresent(evidence, fullInputText))
                    {
                        return Invalid(result, "EVIDENCE_NOT_IN_INPUT", $"Evidence for skill '{name}' is not present verbatim in the job input.");
                    }
                }

                if (schemaVersion == "jd-analysis/v3")
                {
                    if (!ParseV3Groups(groupsProp, fullInputText, validated, result)) return result;
                    // Preserve historical v3 behavior for stored snapshots. The v4
                    // provider path above intentionally performs structural mapping only.
                    if (!JdRequirementSemanticNormalizer.TryNormalize(validated, input, out var failureCode, out var failureMessage))
                    {
                        return Invalid(result, failureCode, failureMessage);
                    }
                }
                else foreach (var rElem in reqsProp.EnumerateArray())
                {
                    if (validated.RequirementsList.Count >= 50) break;
                    if (rElem.ValueKind != JsonValueKind.Object) return Invalid(result, "INVALID_REQUIREMENT", "Every requirement must be an object.");

                    string category = ReadString(rElem, "category");
                    string importance = ReadString(rElem, "importance");
                    string skillName = ReadString(rElem, "skill_name");
                    string detail = ReadString(rElem, "detail_verbatim");
                    string rawMention = ReadString(rElem, "raw_mention");
                    string section = ReadString(rElem, "source_section");
                    string evidence = ReadString(rElem, "evidence");
                    decimal confidence = ReadConfidence(rElem);

                    if (!AllowedCategories.Contains(category) || !AllowedImportances.Contains(importance) || !AllowedSourceSections.Contains(section))
                    {
                        return Invalid(result, "INVALID_REQUIREMENT_ENUM", "Requirement category, importance, or source_section is invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(skillName) || string.IsNullOrWhiteSpace(rawMention) || string.IsNullOrWhiteSpace(detail) || string.IsNullOrWhiteSpace(evidence))
                    {
                        return Invalid(result, "MISSING_REQUIREMENT_EVIDENCE", "Every requirement needs skill_name, raw_mention, detail_verbatim, and evidence.");
                    }
                    if (!IsEvidencePresent(evidence, fullInputText) || !IsEvidencePresent(detail, fullInputText))
                    {
                        return Invalid(result, "EVIDENCE_NOT_IN_INPUT", $"Evidence for requirement '{skillName}' is not present verbatim in the job input.");
                    }

                    validated.RequirementsList.Add(new ValidatedRequirementItem
                    {
                        Category = category.ToLowerInvariant(),
                        Importance = importance.ToLowerInvariant(),
                        SkillName = NormalizeToken(skillName),
                        DetailVerbatim = detail.Trim(),
                        RawMention = rawMention.Trim(),
                        SourceSection = section.ToLowerInvariant(),
                        Evidence = evidence.Trim(),
                        Confidence = confidence
                    });
                }

                try
                {
                    Canonicalize(validated);
                }
                catch (InvalidOperationException exception) when (exception.Message == "JD_ANALYSIS_GROUP_ID_COLLISION")
                {
                    return Invalid(result, "JD_ANALYSIS_DUPLICATE_REQUIREMENT_GROUP", "JD analysis contains duplicate semantic requirement groups.");
                }

                result.IsValid = true;
                result.Quality = JdAnalysisQuality.COMPLETE;
                validated.Quality = result.Quality;
                var groupCount = validated.RequirementGroups.Count;
                var itemCount = validated.RequirementGroups.Sum(group => group.Items.Count);
                validated.Coverage = new JdAnalysisCoverage(groupCount, groupCount, 0, itemCount, itemCount, 0, true);
                result.Data = validated;
                return result;
            }
        }

        private static ValidationResult<ValidatedJobAnalysis> ValidateV5(
            JsonElement metrics,
            ValidationResult<ValidatedJobAnalysis> result)
        {
            var validated = new ValidatedJobAnalysis
            {
                SchemaVersion = JdAnalysisOutputSchema.ProviderSchemaVersion
            };

            ReadV5StringArray(metrics, "job_titles_normalized", validated.JobTitlesNormalized, validated.Diagnostics);
            ReadV5StringArray(metrics, "domains", validated.Domains, validated.Diagnostics);
            if (TryReadV5NonNegativeInt(metrics, "total_years_exp", allowNull: false, out var yearsValue) &&
                yearsValue.HasValue)
            {
                validated.TotalYearsExp = yearsValue.Value;
            }
            else
            {
                AddDiagnostic(validated.Diagnostics, "INVALID_TOTAL_YEARS_EXP", "$.matching_metrics.total_years_exp");
            }

            if (!TryGetMechanicalProperty(metrics, "requirement_groups", out var groupsElement, out _) ||
                groupsElement.ValueKind != JsonValueKind.Array)
            {
                return Invalid(result, "MISSING_REQUIREMENT_GROUPS", "Missing required array 'requirement_groups'.");
            }

            var inputGroupCount = groupsElement.GetArrayLength();
            var inputItemCount = 0;
            var signatures = new HashSet<string>(StringComparer.Ordinal);
            var groupIndex = 0;
            foreach (var groupElement in groupsElement.EnumerateArray())
            {
                var path = $"$.matching_metrics.requirement_groups[{groupIndex}]";
                var rawItemCount = 0;
                if (groupElement.ValueKind == JsonValueKind.Object &&
                    TryGetMechanicalProperty(groupElement, "items", out var rawItems, out _) &&
                    rawItems.ValueKind == JsonValueKind.Array)
                {
                    rawItemCount = rawItems.GetArrayLength();
                    inputItemCount += rawItemCount;
                }

                if (groupIndex >= MaxRequirementGroups)
                {
                    AddDiagnostic(validated.Diagnostics, "REQUIREMENT_GROUP_LIMIT_EXCEEDED", path);
                    groupIndex++;
                    continue;
                }

                if (rawItemCount > MaxRequirementItems ||
                    validated.RequirementGroups.Sum(group => group.Items.Count) + rawItemCount > MaxRequirementItems)
                {
                    AddDiagnostic(validated.Diagnostics, "REQUIREMENT_ITEM_LIMIT_EXCEEDED", path);
                    groupIndex++;
                    continue;
                }

                if (!TryParseV5Group(groupElement, groupIndex, path, validated.Diagnostics, out var group))
                {
                    AddDiagnostic(validated.Diagnostics, "INVALID_REQUIREMENT_GROUP", path);
                    groupIndex++;
                    continue;
                }

                var signature = CreateV5GroupSignature(group);
                if (!signatures.Add(signature))
                {
                    AddDiagnostic(validated.Diagnostics, "EXACT_DUPLICATE_GROUP_REMOVED", path);
                    groupIndex++;
                    continue;
                }

                AssignStructuralIds(group, validated.RequirementGroups.Count + 1);
                validated.RequirementGroups.Add(group);
                groupIndex++;
            }

            if (validated.RequirementGroups.Count == 0)
            {
                return Invalid(result, "NO_USABLE_REQUIREMENT_GROUPS", "No structurally usable requirement group remains.");
            }

            PopulateV5SkillProjection(validated);

            var acceptedGroupCount = validated.RequirementGroups.Count;
            var acceptedItemCount = validated.RequirementGroups.Sum(group => group.Items.Count);
            var discardedGroupCount = Math.Max(0, inputGroupCount - acceptedGroupCount);
            var discardedItemCount = Math.Max(0, inputItemCount - acceptedItemCount);
            var complete = discardedGroupCount == 0 &&
                           discardedItemCount == 0 &&
                           !validated.Diagnostics.Any(diagnostic => IsLossyV5Diagnostic(diagnostic.Code));
            validated.Coverage = new JdAnalysisCoverage(
                inputGroupCount,
                acceptedGroupCount,
                discardedGroupCount,
                inputItemCount,
                acceptedItemCount,
                discardedItemCount,
                complete);
            validated.Quality = complete ? JdAnalysisQuality.COMPLETE : JdAnalysisQuality.PARTIAL;

            result.IsValid = complete;
            result.Quality = validated.Quality;
            result.FailureCode = complete ? null : "PARTIAL_JD_ANALYSIS";
            result.Data = validated;
            return result;
        }

        private static bool TryParseV5Group(
            JsonElement element,
            int groupIndex,
            string path,
            List<JdAnalysisDiagnostic> diagnostics,
            out ValidatedRequirementGroup group)
        {
            group = new ValidatedRequirementGroup();
            if (element.ValueKind != JsonValueKind.Object ||
                !TryGetMechanicalProperty(element, "items", out var itemsElement, out _) ||
                itemsElement.ValueKind != JsonValueKind.Array ||
                itemsElement.GetArrayLength() == 0)
            {
                return false;
            }

            if (!TryReadMechanicalString(element, "operator", out var operation) ||
                !TryReadMechanicalString(element, "importance", out var importance) ||
                !TryReadMechanicalString(element, "requirement_verbatim", out var requirementVerbatim))
            {
                return false;
            }
            operation = operation.ToLowerInvariant();
            importance = importance.ToLowerInvariant();
            if (!JdAnalysisEffectiveContract.Operators.Contains(operation) ||
                !JdAnalysisEffectiveContract.Importances.Contains(importance) ||
                string.IsNullOrWhiteSpace(requirementVerbatim))
            {
                return false;
            }

            if (!TryReadMechanicalString(element, "source_requirement_id", out var sourceRequirementId))
            {
                return false;
            }
            if (!IsProviderSourceRequirementId(sourceRequirementId))
            {
                sourceRequirementId = $"req-recovered-{groupIndex + 1:000}";
                AddDiagnostic(diagnostics, "SOURCE_REQUIREMENT_ID_RECOVERED", path + ".source_requirement_id");
            }

            if (!TryReadMechanicalString(element, "intent", out var intent))
            {
                return false;
            }
            intent = intent.ToLowerInvariant();
            if (!JdAnalysisEffectiveContract.ProviderIntents.Contains(intent))
            {
                intent = JdAnalysisEffectiveContract.UnspecifiedIntent;
                AddDiagnostic(diagnostics, "INTENT_UNSPECIFIED", path + ".intent");
            }

            if (!TryReadMechanicalString(element, "source_section", out var sourceSection))
            {
                return false;
            }
            sourceSection = sourceSection.ToLowerInvariant();
            if (!JdAnalysisEffectiveContract.SourceSections.Contains(sourceSection))
            {
                sourceSection = JdAnalysisEffectiveContract.UnknownSourceSection;
                AddDiagnostic(diagnostics, "SOURCE_SECTION_UNKNOWN", path + ".source_section");
            }

            var parsedItems = new List<ValidatedRequirementItem>();
            var hasInvalidItem = false;
            var itemIndex = 0;
            foreach (var itemElement in itemsElement.EnumerateArray())
            {
                if (TryParseV5Item(
                        itemElement,
                        importance,
                        sourceSection,
                        requirementVerbatim,
                        path + $".items[{itemIndex}]",
                        diagnostics,
                        out var item))
                {
                    parsedItems.Add(item);
                }
                else
                {
                    hasInvalidItem = true;
                    AddDiagnostic(diagnostics, "INVALID_REQUIREMENT_ITEM", path + $".items[{itemIndex}]");
                }

                itemIndex++;
            }

            if (hasInvalidItem || parsedItems.Count != itemsElement.GetArrayLength())
            {
                return false;
            }

            int minSatisfied;
            if (operation == "all_of")
            {
                minSatisfied = parsedItems.Count;
            }
            else if (operation == "one_of")
            {
                minSatisfied = 1;
            }
            else if (!TryReadV5NonNegativeInt(element, "min_satisfied", allowNull: false, out var parsedMinSatisfied) ||
                     !parsedMinSatisfied.HasValue ||
                     (minSatisfied = parsedMinSatisfied.Value) < 1 ||
                     minSatisfied > parsedItems.Count)
            {
                return false;
            }

            group = new ValidatedRequirementGroup
            {
                SourceRequirementId = sourceRequirementId,
                Intent = intent,
                Operator = operation,
                MinSatisfied = minSatisfied,
                Importance = importance,
                SourceSection = sourceSection,
                RequirementVerbatim = requirementVerbatim,
                Items = parsedItems
            };
            return true;
        }

        private static bool TryParseV5Item(
            JsonElement element,
            string importance,
            string sourceSection,
            string requirementVerbatim,
            string path,
            List<JdAnalysisDiagnostic> diagnostics,
            out ValidatedRequirementItem item)
        {
            item = new ValidatedRequirementItem();
            if (element.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!TryReadMechanicalString(element, "category", out var category) ||
                !TryReadMechanicalString(element, "skill_name", out var skillName) ||
                !TryReadMechanicalString(element, "raw_mention", out var rawMention))
            {
                return false;
            }
            category = category.ToLowerInvariant();
            if (!JdAnalysisEffectiveContract.Categories.Contains(category) ||
                string.IsNullOrWhiteSpace(skillName) ||
                string.IsNullOrWhiteSpace(rawMention))
            {
                return false;
            }

            if (!TryReadV5Year(element, "min_years", out var minYears))
            {
                AddDiagnostic(diagnostics, "INVALID_MIN_YEARS", path + ".min_years");
                return false;
            }

            if (!TryReadV5Year(element, "max_years", out var maxYears))
            {
                AddDiagnostic(diagnostics, "INVALID_MAX_YEARS", path + ".max_years");
                return false;
            }

            if (minYears.HasValue && maxYears.HasValue && minYears > maxYears)
            {
                AddDiagnostic(diagnostics, "INVALID_YEAR_RANGE", path);
                return false;
            }

            item = new ValidatedRequirementItem
            {
                Category = category,
                Importance = importance,
                SkillName = skillName,
                DetailVerbatim = requirementVerbatim,
                RawMention = rawMention,
                SourceSection = sourceSection,
                Evidence = requirementVerbatim,
                Evidences = new List<string> { requirementVerbatim },
                MinYears = minYears,
                MaxYears = maxYears,
                Confidence = 1m
            };
            return true;
        }

        private static bool TryReadV5Year(JsonElement element, string property, out int? value) =>
            TryReadV5NonNegativeInt(element, property, allowNull: true, out value);

        private static bool TryReadV5NonNegativeInt(
            JsonElement element,
            string property,
            bool allowNull,
            out int? value)
        {
            value = null;
            if (!TryGetMechanicalProperty(element, property, out var numberElement, out var collision))
            {
                return !collision && allowNull;
            }

            if (numberElement.ValueKind == JsonValueKind.Null)
            {
                return allowNull;
            }

            int number;
            if (numberElement.ValueKind == JsonValueKind.Number)
            {
                if (!numberElement.TryGetInt32(out number)) return false;
            }
            else if (numberElement.ValueKind == JsonValueKind.String)
            {
                var text = numberElement.GetString();
                if (string.IsNullOrEmpty(text) ||
                    text.Any(character => character is < '0' or > '9') ||
                    !int.TryParse(text, System.Globalization.NumberStyles.None,
                        System.Globalization.CultureInfo.InvariantCulture, out number))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            if (number < 0) return false;
            value = number;
            return true;
        }

        private static void ReadV5StringArray(
            JsonElement metrics,
            string property,
            List<string> destination,
            List<JdAnalysisDiagnostic> diagnostics)
        {
            if (!TryGetMechanicalProperty(metrics, property, out var values, out _) || values.ValueKind != JsonValueKind.Array)
            {
                AddDiagnostic(diagnostics, $"INVALID_{property.ToUpperInvariant()}", $"$.matching_metrics.{property}");
                return;
            }

            var index = 0;
            foreach (var value in values.EnumerateArray())
            {
                if (value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
                {
                    destination.Add(value.GetString()!);
                }
                else
                {
                    AddDiagnostic(diagnostics, $"INVALID_{property.ToUpperInvariant()}_ITEM", $"$.matching_metrics.{property}[{index}]");
                }
                index++;
            }
        }

        private static bool IsProviderSourceRequirementId(string value)
        {
            return value.Length == 7 &&
                   value.StartsWith("req-", StringComparison.Ordinal) &&
                   value.AsSpan(4).IndexOfAnyExceptInRange('0', '9') < 0;
        }

        private static void AssignStructuralIds(ValidatedRequirementGroup group, int groupOrdinal)
        {
            group.GroupId = $"grp-{groupOrdinal:000}";
            for (var itemIndex = 0; itemIndex < group.Items.Count; itemIndex++)
            {
                group.Items[itemIndex].ItemId = $"{group.GroupId}:item-{itemIndex + 1:000}";
            }
        }

        private static string CreateV5GroupSignature(ValidatedRequirementGroup group)
        {
            var transport = new
            {
                group.SourceRequirementId,
                group.Intent,
                group.Operator,
                group.MinSatisfied,
                group.Importance,
                group.SourceSection,
                group.RequirementVerbatim,
                Items = group.Items.Select(item => new
                {
                    item.Category,
                    item.SkillName,
                    item.RawMention,
                    item.MinYears,
                    item.MaxYears
                })
            };
            return JsonSerializer.Serialize(transport);
        }

        private static void PopulateV5SkillProjection(ValidatedJobAnalysis validated)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in validated.RequirementGroups)
            {
                foreach (var item in group.Items.Where(item => item.Category == "tech_skill"))
                {
                    if (!names.Add(item.SkillName))
                    {
                        continue;
                    }

                    validated.SkillsNormalized.Add(new ValidatedSkillMention
                    {
                        Name = item.SkillName,
                        Category = item.Category,
                        RawMention = item.RawMention,
                        SourceSection = group.SourceSection,
                        Evidence = group.RequirementVerbatim,
                        Importance = group.Importance,
                        Confidence = 1m
                    });
                }
            }
        }

        private static ValidationResult<ValidatedJobAnalysis> ValidateV4(
            JsonElement metrics,
            ValidationResult<ValidatedJobAnalysis> result)
        {
            var validated = new ValidatedJobAnalysis
            {
                // v4 is the provider contract. Everything downstream consumes one
                // stable, expanded v3 representation.
                SchemaVersion = "jd-analysis/v3"
            };

            ReadV4StringArray(metrics, "job_titles_normalized", validated.JobTitlesNormalized, validated.Diagnostics);
            ReadV4StringArray(metrics, "domains", validated.Domains, validated.Diagnostics);
            if (metrics.TryGetProperty("total_years_exp", out var yearsElement) &&
                yearsElement.ValueKind == JsonValueKind.Number &&
                yearsElement.TryGetInt32(out var years) && years >= 0)
            {
                validated.TotalYearsExp = years;
            }
            else
            {
                validated.Diagnostics.Add(new JdAnalysisDiagnostic("INVALID_TOTAL_YEARS_EXP", "$.matching_metrics.total_years_exp"));
            }

            if (!metrics.TryGetProperty("requirement_groups", out var groupsElement) ||
                groupsElement.ValueKind != JsonValueKind.Array)
            {
                return Invalid(result, "MISSING_REQUIREMENT_GROUPS", "Missing required array 'requirement_groups'.");
            }

            var inputGroupCount = groupsElement.GetArrayLength();
            var inputItemCount = 0;
            var acceptedItemCount = 0;
            var groupIndex = 0;
            foreach (var groupElement in groupsElement.EnumerateArray())
            {
                var path = $"$.matching_metrics.requirement_groups[{groupIndex}]";
                groupIndex++;
                if (groupElement.ValueKind == JsonValueKind.Object &&
                    groupElement.TryGetProperty("items", out var rawItems) && rawItems.ValueKind == JsonValueKind.Array)
                {
                    inputItemCount += rawItems.GetArrayLength();
                }

                if (validated.RequirementGroups.Count >= 50 ||
                    !TryParseV4Group(groupElement, out var group))
                {
                    AddDiagnostic(validated.Diagnostics, "INVALID_REQUIREMENT_GROUP", path);
                    continue;
                }

                if (acceptedItemCount + group.Items.Count > 100)
                {
                    AddDiagnostic(validated.Diagnostics, "REQUIREMENT_ITEM_LIMIT_EXCEEDED", path);
                    continue;
                }

                acceptedItemCount += group.Items.Count;
                validated.RequirementGroups.Add(group);
            }

            if (inputGroupCount > 0 && validated.RequirementGroups.Count == 0)
            {
                return Invalid(result, "NO_USABLE_REQUIREMENT_GROUPS", "No structurally usable requirement group remains.");
            }

            try
            {
                Canonicalize(validated);
            }
            catch (InvalidOperationException exception) when (exception.Message == "JD_ANALYSIS_GROUP_ID_COLLISION")
            {
                return Invalid(result, "JD_ANALYSIS_DUPLICATE_REQUIREMENT_GROUP", "JD analysis contains duplicate semantic requirement groups.");
            }

            var acceptedGroupCount = validated.RequirementGroups.Count;
            acceptedItemCount = validated.RequirementGroups.Sum(group => group.Items.Count);
            var discardedGroupCount = Math.Max(0, inputGroupCount - acceptedGroupCount);
            var discardedItemCount = Math.Max(0, inputItemCount - acceptedItemCount);
            var complete = validated.Diagnostics.Count == 0 && discardedGroupCount == 0 && discardedItemCount == 0;
            validated.Coverage = new JdAnalysisCoverage(
                inputGroupCount,
                acceptedGroupCount,
                discardedGroupCount,
                inputItemCount,
                acceptedItemCount,
                discardedItemCount,
                complete);
            validated.Quality = complete ? JdAnalysisQuality.COMPLETE : JdAnalysisQuality.PARTIAL;

            result.IsValid = complete;
            result.Quality = validated.Quality;
            result.FailureCode = complete ? null : "PARTIAL_JD_ANALYSIS";
            result.Data = validated;
            return result;
        }

        private static bool TryParseV4Group(
            JsonElement element,
            out ValidatedRequirementGroup group)
        {
            group = new ValidatedRequirementGroup();
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array ||
                items.GetArrayLength() == 0)
            {
                return false;
            }

            var operation = ReadString(element, "operator").ToLowerInvariant();
            var importance = ReadString(element, "importance").ToLowerInvariant();
            var sourceSection = ReadString(element, "source_section").ToLowerInvariant();
            var requirementVerbatim = ReadString(element, "requirement_verbatim");
            if (!AllowedGroupOperators.Contains(operation) || !AllowedImportances.Contains(importance) ||
                !AllowedSourceSections.Contains(sourceSection) || string.IsNullOrWhiteSpace(requirementVerbatim))
            {
                return false;
            }

            int minSatisfied;
            if (operation == "all_of") minSatisfied = items.GetArrayLength();
            else if (operation == "one_of") minSatisfied = 1;
            else if (!element.TryGetProperty("min_satisfied", out var minElement) ||
                     minElement.ValueKind != JsonValueKind.Number || !minElement.TryGetInt32(out minSatisfied) ||
                     minSatisfied < 1 || minSatisfied > items.GetArrayLength())
            {
                return false;
            }

            group = new ValidatedRequirementGroup
            {
                Operator = operation,
                MinSatisfied = minSatisfied,
                Importance = importance,
                SourceSection = sourceSection,
                RequirementVerbatim = requirementVerbatim
            };

            var itemIndex = 0;
            foreach (var item in items.EnumerateArray())
            {
                if (!TryParseV4Item(item, importance, sourceSection, requirementVerbatim, out var parsedItem))
                {
                    return false;
                }

                group.Items.Add(parsedItem);
                itemIndex++;
            }

            return itemIndex > 0;
        }

        private static bool TryParseV4Item(
            JsonElement item,
            string importance,
            string sourceSection,
            string requirementVerbatim,
            out ValidatedRequirementItem parsed)
        {
            parsed = new ValidatedRequirementItem();
            if (item.ValueKind != JsonValueKind.Object) return false;

            var category = ReadString(item, "category").ToLowerInvariant();
            var skillName = NormalizeToken(ReadString(item, "skill_name"));
            var rawMention = ReadString(item, "raw_mention");
            if (!AllowedCategories.Contains(category) || string.IsNullOrWhiteSpace(skillName) ||
                string.IsNullOrWhiteSpace(rawMention) ||
                !TryReadOptionalNonNegativeInt(item, "min_years", out var minYears) ||
                !TryReadOptionalNonNegativeInt(item, "max_years", out var maxYears) ||
                (minYears.HasValue && maxYears.HasValue && minYears > maxYears))
            {
                return false;
            }

            parsed = new ValidatedRequirementItem
            {
                Category = category,
                Importance = importance,
                SkillName = skillName,
                DetailVerbatim = requirementVerbatim,
                RawMention = rawMention,
                SourceSection = sourceSection,
                Evidence = requirementVerbatim,
                Evidences = new List<string> { requirementVerbatim },
                MinYears = minYears,
                MaxYears = maxYears,
                Confidence = 1m
            };
            return true;
        }

        private static bool TryReadOptionalNonNegativeInt(JsonElement item, string property, out int? value)
        {
            value = null;
            if (!item.TryGetProperty(property, out var element) || element.ValueKind == JsonValueKind.Null) return true;
            if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var number) || number < 0) return false;
            value = number;
            return true;
        }

        private static void ReadV4StringArray(
            JsonElement metrics,
            string property,
            List<string> destination,
            List<JdAnalysisDiagnostic> diagnostics)
        {
            if (!metrics.TryGetProperty(property, out var values) || values.ValueKind != JsonValueKind.Array)
            {
                AddDiagnostic(diagnostics, $"INVALID_{property.ToUpperInvariant()}", $"$.matching_metrics.{property}");
                return;
            }

            var index = 0;
            foreach (var value in values.EnumerateArray())
            {
                if (value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
                {
                    AddCanonical(destination, value.GetString());
                }
                else
                {
                    AddDiagnostic(diagnostics, $"INVALID_{property.ToUpperInvariant()}_ITEM", $"$.matching_metrics.{property}[{index}]");
                }
                index++;
            }
        }

        private static void AddDiagnostic(List<JdAnalysisDiagnostic> diagnostics, string code, string path)
        {
            if (diagnostics.Count < 100 && !diagnostics.Any(item => item.Code == code && item.JsonPath == path))
            {
                diagnostics.Add(new JdAnalysisDiagnostic(code, path));
            }
        }

        private static bool IsLossyV5Diagnostic(string code) => code is not
            "SOURCE_REQUIREMENT_ID_RECOVERED";

        private static bool TryReadMechanicalString(
            JsonElement element,
            string property,
            out string value)
        {
            value = string.Empty;
            if (!TryGetMechanicalProperty(element, property, out var propertyValue, out var collision))
            {
                return !collision;
            }

            if (propertyValue.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            value = propertyValue.GetString()?.Trim() ?? string.Empty;
            return true;
        }

        private static bool TryGetMechanicalProperty(
            JsonElement element,
            string property,
            out JsonElement value,
            out bool collision)
        {
            value = default;
            collision = false;
            if (element.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var found = false;
            foreach (var candidate in element.EnumerateObject())
            {
                if (!string.Equals(candidate.Name, property, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (found)
                {
                    collision = true;
                    value = default;
                    return false;
                }

                found = true;
                value = candidate.Value;
            }

            return found;
        }

        private static bool ParseV3Groups(
            JsonElement groupsProp,
            string fullInputText,
            ValidatedJobAnalysis validated,
            ValidationResult<ValidatedJobAnalysis> result)
        {
            if (groupsProp.GetArrayLength() > 50)
            {
                Invalid(result, "TOO_MANY_REQUIREMENT_GROUPS", "At most 50 requirement groups are allowed.");
                return false;
            }

            var itemCount = 0;
            foreach (var groupElement in groupsProp.EnumerateArray())
            {
                if (groupElement.ValueKind != JsonValueKind.Object)
                {
                    Invalid(result, "INVALID_REQUIREMENT_GROUP", "Every requirement group must be an object.");
                    return false;
                }

                var operation = ReadString(groupElement, "operator").ToLowerInvariant();
                var importance = ReadString(groupElement, "importance").ToLowerInvariant();
                if (!AllowedGroupOperators.Contains(operation) || !AllowedImportances.Contains(importance) ||
                    !TryGetRequiredArray(groupElement, "items", result, out var items))
                {
                    if (result.FailureCode == null) Invalid(result, "INVALID_REQUIREMENT_GROUP", "Group operator, importance, or items is invalid.");
                    return false;
                }

                if (items.GetArrayLength() == 0)
                {
                    Invalid(result, "INVALID_GROUP_CARDINALITY", "A requirement group must contain at least one item.");
                    return false;
                }

                int minSatisfied = groupElement.TryGetProperty("min_satisfied", out var minSatisfiedProp) &&
                    minSatisfiedProp.ValueKind == JsonValueKind.Number && minSatisfiedProp.TryGetInt32(out var min)
                    ? min : operation == "all_of" ? items.GetArrayLength() : 1;

                if ((operation == "all_of" && minSatisfied != items.GetArrayLength()) ||
                    (operation == "one_of" && minSatisfied != 1) ||
                    (operation == "at_least_n" && (minSatisfied < 1 || minSatisfied > items.GetArrayLength())))
                {
                    Invalid(result, "INVALID_GROUP_CARDINALITY", "Group operator and min_satisfied are inconsistent.");
                    return false;
                }

                var group = new ValidatedRequirementGroup
                {
                    Operator = operation,
                    MinSatisfied = minSatisfied,
                    Importance = importance
                };

                foreach (var item in items.EnumerateArray())
                {
                    itemCount++;
                    if (itemCount > 100)
                    {
                        Invalid(result, "TOO_MANY_REQUIREMENT_ITEMS", "At most 100 requirement items are allowed.");
                        return false;
                    }
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        Invalid(result, "INVALID_REQUIREMENT", "Every requirement item must be an object.");
                        return false;
                    }

                    var evidences = ReadStringArray(item, "evidences");
                    var category = ReadString(item, "category").ToLowerInvariant();
                    var skillName = NormalizeToken(ReadString(item, "skill_name"));
                    var detail = ReadString(item, "detail_verbatim");
                    var rawMention = ReadString(item, "raw_mention");
                    var section = ReadString(item, "source_section").ToLowerInvariant();
                    int? minYears = ReadOptionalNonNegativeInt(item, "min_years", result);
                    int? maxYears = ReadOptionalNonNegativeInt(item, "max_years", result);
                    if (result.FailureCode != null || !AllowedCategories.Contains(category) ||
                        !AllowedSourceSections.Contains(section) || string.IsNullOrWhiteSpace(skillName) ||
                        string.IsNullOrWhiteSpace(detail) || string.IsNullOrWhiteSpace(rawMention) || evidences.Count == 0 ||
                        (minYears.HasValue && maxYears.HasValue && minYears > maxYears) ||
                        !IsEvidencePresent(detail, fullInputText) || evidences.Any(e => !IsEvidencePresent(e, fullInputText)))
                    {
                        if (result.FailureCode == null) Invalid(result, "MISSING_REQUIREMENT_EVIDENCE", "Every v3 item needs valid evidence and fields from the JD input.");
                        return false;
                    }

                    group.Items.Add(new ValidatedRequirementItem
                    {
                        Category = category,
                        Importance = importance,
                        SkillName = skillName,
                        DetailVerbatim = detail,
                        RawMention = rawMention,
                        SourceSection = section,
                        Evidence = evidences[0],
                        Evidences = evidences,
                        MinYears = minYears,
                        MaxYears = maxYears,
                        Confidence = ReadConfidence(item)
                    });
                }

                validated.RequirementGroups.Add(group);
            }

            return true;
        }

        private static List<string> ReadStringArray(JsonElement item, string property)
        {
            if (!item.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array) return new List<string>();
            var values = new List<string>();
            foreach (var element in value.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(element.GetString())) values.Add(element.GetString()!.Trim());
            }
            return values.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value, StringComparer.Ordinal).ToList();
        }

        private static int? ReadOptionalNonNegativeInt(JsonElement item, string property, ValidationResult<ValidatedJobAnalysis> result)
        {
            if (!item.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null) return null;
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var years) || years < 0)
            {
                Invalid(result, "INVALID_DURATION_CONSTRAINT", $"'{property}' must be a non-negative integer or null.");
                return null;
            }
            return years;
        }

        private static bool TryGetRequiredArray(JsonElement metrics, string property, ValidationResult<ValidatedJobAnalysis> result, out JsonElement value)
        {
            value = default;
            if (metrics.TryGetProperty(property, out value) && value.ValueKind == JsonValueKind.Array) return true;
            result.FailureCode = "MISSING_REQUIRED_ARRAY";
            result.Errors.Add($"Missing required array '{property}'.");
            return false;
        }

        private static ValidationResult<ValidatedJobAnalysis> Invalid(ValidationResult<ValidatedJobAnalysis> result, string code, string message)
        {
            result.IsValid = false;
            result.FailureCode = code;
            result.Errors.Add(message);
            return result;
        }

        private static string ReadString(JsonElement item, string property) =>
            item.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()?.Trim() ?? string.Empty
                : string.Empty;

        private static decimal ReadConfidence(JsonElement item)
        {
            return item.TryGetProperty("confidence", out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var confidence)
                ? Math.Clamp(confidence, 0m, 1m)
                : 0.95m;
        }

        private static void AddCanonical(List<string> values, string? value)
        {
            var normalized = NormalizeToken(value);
            if (!string.IsNullOrWhiteSpace(normalized)) values.Add(normalized);
        }

        private static string NormalizeToken(string? value) => NormalizeWhitespace(value ?? string.Empty).ToLowerInvariant();

        private static void Canonicalize(ValidatedJobAnalysis validated)
        {
            validated.JobTitlesNormalized = validated.JobTitlesNormalized.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.Ordinal).ToList();
            validated.Domains = validated.Domains.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.Ordinal).ToList();

            if (validated.RequirementGroups.Count > 0)
            {
                validated.RequirementGroups = validated.RequirementGroups
                    .GroupBy(group => $"{group.Operator}|{group.MinSatisfied}|{group.Importance}|{string.Join(",", group.Items.OrderBy(item => item.SkillName, StringComparer.Ordinal).Select(item => $"{item.Category}:{item.SkillName}:{item.MinYears}:{item.MaxYears}"))}", StringComparer.OrdinalIgnoreCase)
                    .Select(group =>
                    {
                        var merged = group.First();
                        merged.Items = group.SelectMany(candidate => candidate.Items)
                            .GroupBy(item => $"{item.Category}|{item.SkillName}|{item.MinYears}|{item.MaxYears}", StringComparer.OrdinalIgnoreCase)
                            .Select(items =>
                            {
                                var item = items.First();
                                item.Evidences = items.SelectMany(value => value.Evidences.DefaultIfEmpty(value.Evidence))
                                    .Where(value => !string.IsNullOrWhiteSpace(value))
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .OrderBy(value => value, StringComparer.Ordinal)
                                    .ToList();
                                item.Evidence = item.Evidences.FirstOrDefault() ?? string.Empty;
                                return item;
                            })
                            .OrderBy(item => item.Category, StringComparer.Ordinal)
                            .ThenBy(item => item.SkillName, StringComparer.Ordinal)
                            .ToList();
                        return merged;
                    })
                    .OrderBy(group => group.Importance, StringComparer.Ordinal)
                    .ThenBy(group => group.Operator, StringComparer.Ordinal)
                    .ThenBy(group => string.Join("|", group.Items.Select(item => item.SkillName)), StringComparer.Ordinal)
                    .ToList();

                var assignedIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (var requirementGroup in validated.RequirementGroups)
                {
                    var itemTokens = requirementGroup.Items
                        .Select(item => JdRequirementSemanticNormalizer.CreateItemToken(
                            item.Category,
                            item.SkillName,
                            item.MinYears,
                            item.MaxYears))
                        .ToList();
                    var groupId = JdRequirementSemanticNormalizer.CreateGroupId(
                        requirementGroup.Importance,
                        requirementGroup.Operator,
                        requirementGroup.MinSatisfied,
                        itemTokens);
                    if (!assignedIds.Add(groupId))
                    {
                        throw new InvalidOperationException("JD_ANALYSIS_GROUP_ID_COLLISION");
                    }

                    requirementGroup.GroupId = groupId;
                }

                validated.RequirementsList = validated.RequirementGroups
                    .SelectMany(group => group.Items)
                    .ToList();
            }

            validated.RequirementsList = validated.RequirementsList
                .GroupBy(r => $"{r.Category}|{r.Importance}|{r.SkillName}|{r.Evidence}", StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderBy(r => r.SourceSection, StringComparer.Ordinal).ThenBy(r => r.DetailVerbatim, StringComparer.Ordinal).First())
                .OrderBy(r => r.SourceSection, StringComparer.Ordinal)
                .ThenBy(r => r.Category, StringComparer.Ordinal)
                .ThenBy(r => r.SkillName, StringComparer.Ordinal)
                .ToList();

            validated.SkillsNormalized = validated.RequirementsList
                .Where(r => SkillCandidateCategories.Contains(r.Category))
                .GroupBy(r => $"{r.Category}|{r.SkillName}", StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var selected = g.OrderByDescending(r => r.Importance == "must_have")
                        .ThenBy(r => r.SourceSection, StringComparer.Ordinal)
                        .ThenBy(r => r.Evidence, StringComparer.Ordinal)
                        .First();
                    return new ValidatedSkillMention
                    {
                        Name = selected.SkillName,
                        Category = selected.Category,
                        Importance = selected.Importance,
                        RawMention = selected.RawMention,
                        SourceSection = selected.SourceSection,
                        Evidence = selected.Evidence,
                        Confidence = selected.Confidence
                    };
                })
                .OrderBy(s => s.Category, StringComparer.Ordinal)
                .ThenBy(s => s.Name, StringComparer.Ordinal)
                .Take(40)
                .ToList();
        }

        private static string ExtractJsonPayload(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;

            string text = rawText.Trim();
            // Handle markdown code block wrapper ```json ... ```
            if (text.StartsWith("```"))
            {
                int firstNewline = text.IndexOf('\n');
                int lastFence = text.LastIndexOf("```");
                if (firstNewline >= 0 && lastFence > firstNewline)
                {
                    text = text.Substring(firstNewline + 1, lastFence - firstNewline - 1).Trim();
                }
            }

            int firstBrace = text.IndexOf('{');
            int lastBrace = text.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                return text.Substring(firstBrace, lastBrace - firstBrace + 1);
            }

            return text;
        }

        private static string CombineInputText(JobAnalysisInputSnapshot input)
        {
            return $"{input.Title} {input.Description} {input.Requirements}".ToLowerInvariant();
        }

        private static bool IsEvidencePresent(string evidence, string fullInputText)
        {
            if (string.IsNullOrWhiteSpace(evidence)) return true;
            string normalizedEvidence = NormalizeWhitespace(evidence).ToLowerInvariant();
            string normalizedInput = NormalizeWhitespace(fullInputText).ToLowerInvariant();
            return normalizedInput.Contains(normalizedEvidence);
        }

        private static string NormalizeWhitespace(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return string.Join(" ", input.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
