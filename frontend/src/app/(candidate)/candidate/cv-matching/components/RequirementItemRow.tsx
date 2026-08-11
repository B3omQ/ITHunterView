"use client";

import { Badge } from "@/components/ui/badge";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import type { MatchRequirementItemReport } from "@/types/cv.types";
import { AlertCircle, Check, CheckCircle2, ChevronDown, Info, XCircle } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { EvidenceList } from "./EvidenceList";

interface RequirementItemRowProps {
  item: MatchRequirementItemReport;
  importance?: string | null;
  selected?: boolean;
  showImportance?: boolean;
}

const categoryKeys: Record<string, string> = {
  tech_skill: "categoryTechSkill",
  experience: "categoryExperience",
  domain_knowledge: "categoryDomainKnowledge",
  language: "categoryLanguage",
  education: "categoryEducation",
  soft_skill: "categorySoftSkill",
};

export const getRequirementItemLabel = (item: MatchRequirementItemReport) =>
  item.normalizedText || item.detailVerbatim || item.rawMention || "Requirement";

export function RequirementItemRow({
  item,
  importance,
  selected = false,
  showImportance = true,
}: RequirementItemRowProps) {
  const t = useTranslations("CandidateCVMatching");
  const [isOpen, setIsOpen] = useState(false);
  const isUnresolved = item.assessmentStatus === "unresolved" || item.score == null;
  const score = isUnresolved ? null : Math.min(1, Math.max(0, item.score as number));
  const categoryKey = categoryKeys[item.category ?? ""] ?? "categoryOther";

  let Icon = XCircle;
  let iconClass = "text-red-500";
  let iconBackground = "bg-red-500/10";
  let barClass = "bg-red-500";
  if (isUnresolved) {
    Icon = Info;
    iconClass = "text-muted-foreground";
    iconBackground = "bg-muted";
    barClass = "bg-muted-foreground/30";
  } else if (score === 1) {
    Icon = CheckCircle2;
    iconClass = "text-green-600";
    iconBackground = "bg-green-500/10";
    barClass = "bg-green-500";
  } else if (score !== null && score >= 0.75) {
    Icon = Check;
    iconClass = "text-blue-600";
    iconBackground = "bg-blue-500/10";
    barClass = "bg-blue-500";
  } else if (score !== null && score >= 0.5) {
    Icon = AlertCircle;
    iconClass = "text-amber-600";
    iconBackground = "bg-amber-500/10";
    barClass = "bg-amber-500";
  } else if (score !== null && score >= 0.25) {
    Icon = Info;
    iconClass = "text-orange-600";
    iconBackground = "bg-orange-500/10";
    barClass = "bg-orange-500";
  }

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen} className="overflow-hidden rounded-md border bg-background">
      <CollapsibleTrigger className="flex w-full items-center gap-3 p-3 text-left hover:bg-muted/40">
        <span className={`shrink-0 rounded-full p-1.5 ${iconBackground}`}>
          <Icon className={`h-4 w-4 ${iconClass}`} />
        </span>
        <span className="min-w-0 flex-1">
          <span className="flex flex-wrap items-center gap-2">
            <span className="text-sm font-medium">{getRequirementItemLabel(item)}</span>
            <Badge variant="outline" className="text-[10px] font-normal">{t(categoryKey)}</Badge>
            {showImportance && importance ? (
              <Badge variant={importance === "must_have" ? "default" : "secondary"} className="text-[10px] font-normal">
                {importance === "must_have" ? t("mustHave") : t("niceToHave")}
              </Badge>
            ) : null}
            {selected ? <Badge variant="secondary" className="text-[10px] font-normal">{t("selectedAlternative")}</Badge> : null}
            {!isUnresolved && item.isCriticalGap ? <Badge variant="destructive" className="text-[10px] font-normal">{t("criticalGap")}</Badge> : null}
          </span>
          <span
            className="mt-2 flex gap-1"
            aria-label={isUnresolved ? t("scoreUnavailable") : t("scorePercentLabel", { score: Math.round((score ?? 0) * 100) })}
          >
            {[0.25, 0.5, 0.75, 1].map((step) => (
              <span key={step} className={`h-1.5 w-6 rounded-full ${score !== null && score >= step ? barClass : "bg-muted"}`} />
            ))}
          </span>
        </span>
        <span className="text-xs font-semibold">{score === null ? "—" : `${Math.round(score * 100)}%`}</span>
        <ChevronDown className={`h-4 w-4 text-muted-foreground transition-transform ${isOpen ? "rotate-180" : ""}`} />
      </CollapsibleTrigger>
      <CollapsibleContent>
        <div className="space-y-3 border-t px-4 py-3">
          <p className="text-sm leading-relaxed text-muted-foreground">
            {isUnresolved ? t("unresolvedRequirement") : item.reasoning || t("noReasoning")}
          </p>
          {!isUnresolved && <EvidenceList evidence={item.evidence} />}
        </div>
      </CollapsibleContent>
    </Collapsible>
  );
}
