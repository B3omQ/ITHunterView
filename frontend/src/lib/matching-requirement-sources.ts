import type { MatchRequirementGroupReport } from "@/types/cv.types";

export interface MatchRequirementSourceView {
  key: string;
  sourceRequirementId?: string | null;
  requirementVerbatim?: string | null;
  sourceSection?: string | null;
  sourceOrder?: number | null;
  groups: MatchRequirementGroupReport[];
}

interface MutableSourceView extends MatchRequirementSourceView {
  firstReportIndex: number;
}

export function buildRequirementSourceViews(
  groups: MatchRequirementGroupReport[],
): MatchRequirementSourceView[] {
  const grouped = new Map<string, MutableSourceView>();

  groups.forEach((group, reportIndex) => {
    const sourceRequirementId = normalize(group.sourceRequirementId);
    const requirementVerbatim = normalize(group.requirementVerbatim);
    const sourceSection = normalize(group.sourceSection);
    const identity = sourceRequirementId
      ? `${sourceRequirementId}\u001f${requirementVerbatim}\u001f${sourceSection}`
      : `missing\u001f${reportIndex}`;
    const existing = grouped.get(identity);
    if (existing) {
      existing.groups.push(group);
      if (typeof group.sourceOrder === "number") {
        existing.sourceOrder = typeof existing.sourceOrder === "number"
          ? Math.min(existing.sourceOrder, group.sourceOrder)
          : group.sourceOrder;
      }
      return;
    }

    grouped.set(identity, {
      key: sourceRequirementId
        ? `source:${sourceRequirementId}:${stableSuffix(`${requirementVerbatim}\u001f${sourceSection}`)}`
        : `group:${group.groupId ?? "anonymous"}:${reportIndex}`,
      sourceRequirementId: group.sourceRequirementId,
      requirementVerbatim: group.requirementVerbatim,
      sourceSection: group.sourceSection,
      sourceOrder: group.sourceOrder,
      groups: [group],
      firstReportIndex: reportIndex,
    });
  });

  return [...grouped.values()]
    .sort((left, right) => {
      const leftOrder = typeof left.sourceOrder === "number" ? left.sourceOrder : Number.MAX_SAFE_INTEGER;
      const rightOrder = typeof right.sourceOrder === "number" ? right.sourceOrder : Number.MAX_SAFE_INTEGER;
      return leftOrder - rightOrder || left.firstReportIndex - right.firstReportIndex;
    })
    .map((view) => ({
      key: view.key,
      sourceRequirementId: view.sourceRequirementId,
      requirementVerbatim: view.requirementVerbatim,
      sourceSection: view.sourceSection,
      sourceOrder: view.sourceOrder,
      groups: view.groups,
    }));
}

function normalize(value?: string | null): string {
  return value?.trim().replace(/\s+/g, " ") ?? "";
}

function stableSuffix(value: string): string {
  let hash = 2166136261;
  for (let index = 0; index < value.length; index += 1) {
    hash ^= value.charCodeAt(index);
    hash = Math.imul(hash, 16777619);
  }
  return (hash >>> 0).toString(16).padStart(8, "0");
}
