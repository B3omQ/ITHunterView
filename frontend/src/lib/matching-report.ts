import type {
  MatchCriticalGapReport,
  MatchEvidenceReport,
  MatchReport,
  MatchRequirementGroupReport,
  MatchRequirementItemReport,
  MatchingResultDto,
} from "@/types/cv.types";

const clamp = (value: number | null | undefined, min: number, max: number) =>
  Math.min(max, Math.max(min, Number.isFinite(value) ? (value as number) : 0));

const normalizeEvidence = (evidence: MatchEvidenceReport[] | null | undefined): MatchEvidenceReport[] =>
  (Array.isArray(evidence) ? evidence : []).filter(
    (entry): entry is MatchEvidenceReport => !!entry && typeof entry.quotation === "string",
  );

const normalizeStringArray = (values: string[] | null | undefined): string[] =>
  (Array.isArray(values) ? values : []).filter(
    (value): value is string => typeof value === "string" && value.trim().length > 0,
  );

const normalizeItem = (item: MatchRequirementItemReport): MatchRequirementItemReport => ({
  ...item,
  score: clamp(item.score, 0, 1),
  reasoning: typeof item.reasoning === "string" ? item.reasoning : "",
  evidence: normalizeEvidence(item.evidence),
  isCriticalGap: item.isCriticalGap === true,
});

const normalizeGroup = (group: MatchRequirementGroupReport): MatchRequirementGroupReport => ({
  ...group,
  groupScore: clamp(group.groupScore, 0, 1),
  selectedItemIds: Array.isArray(group.selectedItemIds) ? group.selectedItemIds : [],
  satisfiedItemIds: normalizeStringArray(group.satisfiedItemIds),
  isCriticalGap: group.isCriticalGap === true,
  items: (Array.isArray(group.items) ? group.items : []).filter(Boolean).map(normalizeItem),
});

const normalizeGap = (gap: MatchCriticalGapReport): MatchCriticalGapReport => ({
  ...gap,
  code: typeof gap.code === "string" && gap.code ? gap.code : "CRITICAL_GAP",
  requirement: typeof gap.requirement === "string" ? gap.requirement : "",
  reasoning: typeof gap.reasoning === "string" ? gap.reasoning : "",
  evidence: normalizeEvidence(gap.evidence),
  affectedItemIds: normalizeStringArray(gap.affectedItemIds),
  requiredCount: Number.isInteger(gap.requiredCount) ? Math.max(0, gap.requiredCount as number) : null,
  satisfiedCount: Number.isInteger(gap.satisfiedCount) ? Math.max(0, gap.satisfiedCount as number) : null,
});

export function normalizeCompletedMatchReport(result: MatchingResultDto): MatchReport {
  const source = result.report;
  if (!source) {
    return {
      reportContract: "match-report/v2",
      reportKind: result.reportKind ?? "legacy_summary",
      matchMethod: result.matchMethod ?? "legacy_unknown",
      scorePercent: clamp(result.scorePercent, 0, 100),
      narrative: "Detailed matching breakdown is unavailable for this completed result.",
      requirementGroups: [],
      criticalGaps: [],
      warningFlags: [],
    };
  }

  return {
    ...source,
    reportContract: "match-report/v2",
    reportKind: source.reportKind ?? result.reportKind ?? "legacy_summary",
    matchMethod: source.matchMethod ?? result.matchMethod ?? "legacy_unknown",
    scorePercent: clamp(source.scorePercent ?? result.scorePercent, 0, 100),
    narrative: typeof source.narrative === "string" ? source.narrative : "",
    requirementGroups: (Array.isArray(source.requirementGroups) ? source.requirementGroups : [])
      .filter(Boolean)
      .map(normalizeGroup),
    criticalGaps: (Array.isArray(source.criticalGaps) ? source.criticalGaps : [])
      .filter(Boolean)
      .map(normalizeGap),
    warningFlags: normalizeStringArray(source.warningFlags),
  };
}
