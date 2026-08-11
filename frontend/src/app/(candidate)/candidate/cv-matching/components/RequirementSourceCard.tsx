"use client";

import { Badge } from "@/components/ui/badge";
import type { MatchRequirementSourceView } from "@/lib/matching-requirement-sources";
import { useTranslations } from "next-intl";
import { RequirementGroupCard } from "./RequirementGroupCard";

interface RequirementSourceCardProps {
  source: MatchRequirementSourceView;
  sourceIndex: number;
}

const sourceSectionKeys: Record<string, string> = {
  description: "sourceSectionDescription",
  requirements: "sourceSectionRequirements",
};

export function RequirementSourceCard({ source, sourceIndex }: RequirementSourceCardProps) {
  const t = useTranslations("CandidateCVMatching");
  const sourceClause = source.requirementVerbatim?.trim();
  const sectionKey = source.sourceSection ? sourceSectionKeys[source.sourceSection] : null;

  return (
    <section className="space-y-3 rounded-xl border border-border/80 bg-background p-4" aria-label={sourceClause || t("requirementGroupFallback", { index: sourceIndex + 1 })}>
      {sourceClause || sectionKey ? (
        <header className="flex flex-wrap items-start gap-2">
          {sourceClause ? <h3 className="min-w-0 flex-1 text-sm font-semibold leading-relaxed">{sourceClause}</h3> : null}
          {sectionKey ? <Badge variant="outline" className="shrink-0 text-[10px] font-normal">{t(sectionKey)}</Badge> : null}
        </header>
      ) : null}
      <div className="space-y-3">
        {source.groups.map((group, groupIndex) => (
          <RequirementGroupCard
            key={group.groupId ?? `${source.key}:group:${groupIndex}`}
            group={group}
            groupIndex={groupIndex}
            showSourceClause={false}
          />
        ))}
      </div>
    </section>
  );
}
