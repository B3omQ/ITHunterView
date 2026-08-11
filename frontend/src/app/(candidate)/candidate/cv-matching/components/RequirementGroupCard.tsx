"use client";

import { Badge } from "@/components/ui/badge";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import type { MatchRequirementGroupReport } from "@/types/cv.types";
import { ChevronDown } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { getRequirementItemLabel, RequirementItemRow } from "./RequirementItemRow";

interface RequirementGroupCardProps {
  group: MatchRequirementGroupReport;
  groupIndex: number;
}

const categoryKeys: Record<string, string> = {
  tech_skill: "categoryTechSkill",
  experience: "categoryExperience",
  domain_knowledge: "categoryDomainKnowledge",
  language: "categoryLanguage",
  education: "categoryEducation",
  soft_skill: "categorySoftSkill",
};

export function RequirementGroupCard({ group, groupIndex }: RequirementGroupCardProps) {
  const t = useTranslations("CandidateCVMatching");
  const [isOpen, setIsOpen] = useState(false);
  const items = group.items ?? [];
  const groupTitle = group.requirementVerbatim || group.intent || t("requirementGroupFallback", { index: groupIndex + 1 });
  const categoryLabels = [...new Set(items.map((item) => categoryKeys[item.category ?? ""] ?? "categoryOther"))];

  if (group.operator === "all_of" || !group.operator) {
    return (
      <section className="space-y-3 rounded-lg border p-4" aria-label={groupTitle}>
        <GroupHeader group={group} title={groupTitle} categoryLabels={categoryLabels} />
        {items.length > 0 ? items.map((item, itemIndex) => (
          <RequirementItemRow
            key={item.itemId ?? `${group.groupId ?? groupIndex}-${itemIndex}`}
            item={item}
            importance={group.importance}
          />
        )) : <p className="text-sm text-muted-foreground">{t("requirementDetailsUnavailable")}</p>}
      </section>
    );
  }

  const labels = items.map(getRequirementItemLabel);
  const summary = group.operator === "at_least_n"
    ? `${t("atLeastSummary", { required: group.minSatisfied ?? 1, total: items.length })}: ${labels.join(" | ")}`
    : labels.join(" | ") || groupTitle;

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen} className="overflow-hidden rounded-lg border">
      <CollapsibleTrigger className="w-full p-4 text-left hover:bg-muted/30">
        <GroupHeader group={group} title={groupTitle} categoryLabels={categoryLabels} />
        <div className="mt-3 flex items-start gap-3 rounded-md bg-muted/40 p-3">
          <span className="min-w-0 flex-1 text-sm font-medium">{summary}</span>
          {group.isCriticalGap ? <Badge variant="destructive">{t("criticalGap")}</Badge> : null}
          <span className="shrink-0 text-xs font-semibold">{Math.round(group.groupScore * 100)}%</span>
          <ChevronDown className={`h-4 w-4 shrink-0 text-muted-foreground transition-transform ${isOpen ? "rotate-180" : ""}`} />
        </div>
      </CollapsibleTrigger>
      <CollapsibleContent>
        <div className="space-y-2 border-t px-4 py-3">
          {items.map((item, itemIndex) => (
            <RequirementItemRow
              key={item.itemId ?? `${group.groupId ?? groupIndex}-${itemIndex}`}
              item={item}
              importance={group.importance}
              selected={Boolean(item.itemId && group.selectedItemIds.includes(item.itemId))}
              showImportance={false}
            />
          ))}
        </div>
      </CollapsibleContent>
    </Collapsible>
  );
}

function GroupHeader({
  group,
  title,
  categoryLabels,
}: {
  group: MatchRequirementGroupReport;
  title: string;
  categoryLabels: string[];
}) {
  const t = useTranslations("CandidateCVMatching");
  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center gap-2">
        <h3 className="text-sm font-semibold leading-relaxed">{title}</h3>
        {group.importance ? (
          <Badge variant={group.importance === "must_have" ? "default" : "secondary"}>
            {group.importance === "must_have" ? t("mustHave") : t("niceToHave")}
          </Badge>
        ) : null}
        {group.operator === "one_of" ? <Badge variant="outline">{t("operatorOneOf")}</Badge> : null}
        {group.operator === "at_least_n" ? <Badge variant="outline">{t("operatorAtLeastN")}</Badge> : null}
      </div>
      <div className="flex flex-wrap gap-1.5">
        {categoryLabels.map((key) => <Badge key={key} variant="outline" className="text-[10px] font-normal">{t(key)}</Badge>)}
      </div>
    </div>
  );
}
