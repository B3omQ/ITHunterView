import type {
  MatchCriticalGapReport,
  MatchEvidenceReport,
  MatchReport,
  MatchRequirementGroupReport,
  MatchRequirementItemReport,
  MatchingResultDto,
} from "@/types/cv.types";

const clampNullable = (value: number | null | undefined, min: number, max: number): number | null =>
  Number.isFinite(value) ? Math.min(max, Math.max(min, value as number)) : null;

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
  score: item.assessmentStatus === "unresolved" ? null : clampNullable(item.score, 0, 1),
  assessmentStatus: item.assessmentStatus === "unresolved" || item.score == null ? "unresolved" : "assessed",
  reasoning: typeof item.reasoning === "string" ? item.reasoning : "",
  evidence: normalizeEvidence(item.evidence),
  isCriticalGap: item.isCriticalGap === true,
});

const normalizeGroup = (group: MatchRequirementGroupReport): MatchRequirementGroupReport => ({
  ...group,
  groupScore: clampNullable(group.groupScore, 0, 1),
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
    const scorePercent = result.scoreAvailable === false
      ? null
      : clampNullable(result.scorePercent, 0, 100);
    return {
      reportContract: "match-report/v3",
      reportKind: result.reportKind ?? "legacy_summary",
      matchMethod: result.matchMethod ?? "legacy_unknown",
      scorePercent,
      scoreAvailable: result.scoreAvailable ?? scorePercent !== null,
      narrative: "Detailed matching breakdown is unavailable for this completed result.",
      requirementGroups: [],
      criticalGaps: [],
      warningFlags: [],
    };
  }

  const scoreAvailable = source.scoreAvailable ?? result.scoreAvailable ??
    Number.isFinite(source.scorePercent ?? result.scorePercent);
  const scorePercent = scoreAvailable
    ? clampNullable(source.scorePercent ?? result.scorePercent, 0, 100)
    : null;
  return {
    ...source,
    reportContract: source.reportContract ?? "match-report/v3",
    reportKind: source.reportKind ?? result.reportKind ?? "legacy_summary",
    matchMethod: source.matchMethod ?? result.matchMethod ?? "legacy_unknown",
    scorePercent,
    scoreAvailable: scoreAvailable && scorePercent !== null,
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
