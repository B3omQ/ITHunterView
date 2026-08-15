using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Utils;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Service
{
    public interface ISkillNormalizationService
    {
        string Normalize(string value);
    }

    public class SkillNormalizationService : ISkillNormalizationService
    {
        public string Normalize(string value)
        {
            return StringNormalizationHelper.NormalizeITTerm(value);
        }
    }



    public sealed class SkillResolution
    {
        public string RawMention { get; set; } = string.Empty;
        public string NormalizedMention { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SourceSection { get; set; } = string.Empty;
        public string EvidenceText { get; set; } = string.Empty;
        public string Importance { get; set; } = "must_have";
        public int? SuggestedSkillId { get; set; }
        public string? SuggestedSkillName { get; set; }
        public int? ResolvedSkillId { get; set; }
        public string? ResolvedSkillName { get; set; }
        public SkillResolutionStatus ResolutionStatus { get; set; }
        public decimal? Confidence { get; set; }
    }

    public interface ISkillResolver
    {
        Task<IReadOnlyList<SkillResolution>> ResolveAsync(
            IReadOnlyList<ValidatedSkillMention> mentions,
            CancellationToken ct = default);
    }

    public class SkillResolver : ISkillResolver
    {
        private readonly ITHunterviewContext _context;
        private readonly ISkillNormalizationService _normalizationService;

        public SkillResolver(ITHunterviewContext context, ISkillNormalizationService normalizationService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _normalizationService = normalizationService ?? throw new ArgumentNullException(nameof(normalizationService));
        }

        public async Task<IReadOnlyList<SkillResolution>> ResolveAsync(
            IReadOnlyList<ValidatedSkillMention> mentions,
            CancellationToken ct = default)
        {
            if (mentions == null || mentions.Count == 0)
                return Array.Empty<SkillResolution>();

            var results = new List<SkillResolution>();
            var normalizedMap = mentions
                .Select(m => new { Mention = m, Normalized = _normalizationService.Normalize(m.Name) })
                .Where(x => !string.IsNullOrEmpty(x.Normalized))
                .ToList();

            var distinctNormalized = normalizedMap.Select(x => x.Normalized).Distinct().ToList();

            // 1. Batch query canonical active skills
            var canonicalSkills = await _context.Skills
                .AsNoTracking()
                .Where(s => s.Status == SkillStatus.ACTIVE && distinctNormalized.Contains(s.NormalizedName))
                .ToListAsync(ct);

            var canonicalGroup = canonicalSkills
                .GroupBy(s => s.NormalizedName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            // 2. Batch query active skill aliases
            var aliasMatches = await _context.SkillAliases
                .AsNoTracking()
                .Include(a => a.Skill)
                .Where(a => a.Skill.Status == SkillStatus.ACTIVE && distinctNormalized.Contains(a.NormalizedAliasName))
                .ToListAsync(ct);

            var aliasGroup = aliasMatches
                .GroupBy(a => a.NormalizedAliasName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Select(a => a.Skill).Where(s => s.Status == SkillStatus.ACTIVE).ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var item in normalizedMap)
            {
                var m = item.Mention;
                string norm = item.Normalized;

                var resolution = new SkillResolution
                {
                    RawMention = m.RawMention,
                    NormalizedMention = norm,
                    Category = m.Category,
                    SourceSection = m.SourceSection,
                    EvidenceText = m.Evidence,
                    Importance = m.Importance,
                    Confidence = m.Confidence
                };

                // Check canonical match first
                if (canonicalGroup.TryGetValue(norm, out var directSkills) && directSkills.Count > 0)
                {
                    if (directSkills.Count == 1)
                    {
                        var target = directSkills[0];
                        resolution.ResolutionStatus = SkillResolutionStatus.EXACT_CANONICAL;
                        resolution.SuggestedSkillId = target.Id;
                        resolution.SuggestedSkillName = target.Name;
                        resolution.ResolvedSkillId = target.Id;
                        resolution.ResolvedSkillName = target.Name;
                    }
                    else
                    {
                        resolution.ResolutionStatus = SkillResolutionStatus.AMBIGUOUS;
                    }
                }
                // Check alias match second
                else if (aliasGroup.TryGetValue(norm, out var aliasSkills) && aliasSkills.Count > 0)
                {
                    var distinctAliasSkills = aliasSkills.GroupBy(s => s.Id).Select(g => g.First()).ToList();
                    if (distinctAliasSkills.Count == 1)
                    {
                        var target = distinctAliasSkills[0];
                        resolution.ResolutionStatus = SkillResolutionStatus.EXACT_ALIAS;
                        resolution.SuggestedSkillId = target.Id;
                        resolution.SuggestedSkillName = target.Name;
                        resolution.ResolvedSkillId = target.Id;
                        resolution.ResolvedSkillName = target.Name;
                    }
                    else
                    {
                        resolution.ResolutionStatus = SkillResolutionStatus.AMBIGUOUS;
                    }
                }
                else
                {
                    resolution.ResolutionStatus = SkillResolutionStatus.UNMATCHED;
                }

                results.Add(resolution);
            }

            return results;
        }
    }
}
