"use client";

import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import type { MatchRequirementGroupReport, MatchRequirementItemReport } from "@/types/cv.types";
import { AlertCircle, Check, CheckCircle2, ChevronDown, Info, XCircle } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";

interface RequirementBreakdownProps {
  groups: MatchRequirementGroupReport[];
}

interface RequirementDisplayRow {
  id: string;
  label: string;
  score: number;
  importance?: string | null;
  operator?: string | null;
  isCriticalGap: boolean;
  items: MatchRequirementItemReport[];
}

const itemLabel = (item: MatchRequirementItemReport) =>
  item.normalizedText || item.detailVerbatim || item.rawMention || item.category || "Requirement";

function toDisplayRows(groups: MatchRequirementGroupReport[]): RequirementDisplayRow[] {
  return groups.flatMap((group, groupIndex) => {
    const groupKey = group.groupId || `group-${groupIndex}`;
    const items = group.items || [];
    if (group.operator === "one_of") {
      return [{
        id: groupKey,
        label: items.map(itemLabel).join(" | ") || group.requirementVerbatim || "Alternative requirement",
        score: group.groupScore,
        importance: group.importance,
        operator: group.operator,
        isCriticalGap: group.isCriticalGap,
        items,
      }];
    }
    if (group.operator === "at_least_n") {
      const minimum = group.minSatisfied ?? 1;
      return [{
        id: groupKey,
        label: `Cần ít nhất ${minimum}/${items.length}: ${items.map(itemLabel).join(" | ")}`,
        score: group.groupScore,
        importance: group.importance,
        operator: group.operator,
        isCriticalGap: group.isCriticalGap,
        items,
      }];
    }
    if (items.length === 0) {
      return [{
        id: groupKey,
        label: group.requirementVerbatim || "Requirement",
        score: group.groupScore,
        importance: group.importance,
        operator: group.operator,
        isCriticalGap: group.isCriticalGap,
        items,
      }];
    }
    return items.map((item, itemIndex) => ({
      id: item.itemId || `${groupKey}-item-${itemIndex}`,
      label: itemLabel(item),
      score: item.score,
      importance: group.importance,
      operator: group.operator,
      isCriticalGap: item.isCriticalGap || group.isCriticalGap,
      items: [item],
    }));
  });
}

export function RequirementBreakdown({ groups }: RequirementBreakdownProps) {
  const t = useTranslations("CandidateCVMatching");
  const rows = toDisplayRows(groups);
  if (rows.length === 0) return null;

  return (
    <Card className="border-muted">
      <CardHeader>
        <CardTitle className="text-lg">{t("reqBreakdownTitle")}</CardTitle>
        <CardDescription>{t("reqBreakdownDesc")}</CardDescription>
      </CardHeader>
      <CardContent className="space-y-3">
        {rows.map((row) => <RequirementRow key={row.id} row={row} />)}
      </CardContent>
    </Card>
  );
}

function RequirementRow({ row }: { row: RequirementDisplayRow }) {
  const t = useTranslations("CandidateCVMatching");
  const [isOpen, setIsOpen] = useState(false);
  const score = Math.min(1, Math.max(0, row.score));

  let Icon = XCircle;
  let iconClass = "text-red-500";
  let bgClass = "bg-red-500/10";
  let barClass = "bg-red-500";
  if (score === 1) {
    Icon = CheckCircle2;
    iconClass = "text-green-500";
    bgClass = "bg-green-500/10";
    barClass = "bg-green-500";
  } else if (score >= 0.75) {
    Icon = Check;
    iconClass = "text-blue-500";
    bgClass = "bg-blue-500/10";
    barClass = "bg-blue-500";
  } else if (score >= 0.5) {
    Icon = AlertCircle;
    iconClass = "text-yellow-500";
    bgClass = "bg-yellow-500/10";
    barClass = "bg-yellow-500";
  } else if (score >= 0.25) {
    Icon = Info;
    iconClass = "text-orange-500";
    bgClass = "bg-orange-500/10";
    barClass = "bg-orange-500";
  }

  return (
    <Collapsible
      open={isOpen}
      onOpenChange={setIsOpen}
      className="overflow-hidden rounded-lg border border-border/50 transition-all hover:border-border"
    >
      <CollapsibleTrigger className="group flex w-full items-center p-3 text-left transition-colors hover:bg-muted/30">
        <div className={`mr-3 shrink-0 rounded-full p-1.5 ${bgClass}`}>
          <Icon className={`h-4 w-4 ${iconClass}`} />
        </div>
        <div className="min-w-0 flex-1 pr-4">
          <div className="mb-1 flex flex-wrap items-center gap-2">
            <span className="text-sm font-medium">{row.label}</span>
            {row.importance && (
              <Badge variant={row.importance === "must_have" ? "default" : "secondary"} className="h-5 px-1.5 text-[10px] font-normal">
                {row.importance === "must_have" ? t("mustHave") : t("niceToHave")}
              </Badge>
            )}
            {row.operator && row.operator !== "all_of" && (
              <Badge variant="outline" className="h-5 px-1.5 text-[10px] font-normal">{row.operator}</Badge>
            )}
            {row.isCriticalGap && (
              <Badge variant="destructive" className="h-5 px-1.5 text-[10px] font-normal">{t("criticalGap")}</Badge>
            )}
          </div>
          <div className="mt-1.5 flex gap-1" aria-label={`Score ${Math.round(score * 100)} percent`}>
            {[0.25, 0.5, 0.75, 1].map((step) => (
              <div key={step} className={`h-1.5 w-6 rounded-full ${score >= step ? barClass : "bg-muted"}`} />
            ))}
          </div>
        </div>
        <div className="flex shrink-0 items-center gap-3">
          <span className="text-xs font-semibold">{Math.round(score * 100)}%</span>
          <ChevronDown className={`h-4 w-4 text-muted-foreground transition-transform ${isOpen ? "rotate-180" : ""}`} />
        </div>
      </CollapsibleTrigger>

      <CollapsibleContent>
        <div className="ml-[42px] space-y-3 px-4 pb-4 pt-1">
          {row.items.map((item, index) => (
            <div key={item.itemId || index} className="rounded-md border border-border/30 bg-muted/40 p-3 text-sm">
              {row.items.length > 1 && <p className="mb-1 font-medium">{itemLabel(item)} · {Math.round(item.score * 100)}%</p>}
              <p className="leading-relaxed text-muted-foreground">{item.reasoning || t("noReasoning")}</p>
              {item.evidence.map((evidence, evidenceIndex) => (
                <blockquote key={`${evidence.quotation}-${evidenceIndex}`} className="mt-2 border-l-2 pl-3 text-xs text-muted-foreground">
                  “{evidence.quotation}”{evidence.section ? ` — ${evidence.section}` : ""}
                </blockquote>
              ))}
            </div>
          ))}
        </div>
      </CollapsibleContent>
    </Collapsible>
  );
}
