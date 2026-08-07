"use client";

import { Penalty } from "@/types/cv.types";
import { AlertTriangle, XOctagon } from "lucide-react";
import { useTranslations } from "next-intl";

interface PenaltyWarningPanelProps {
  penalties: Penalty[];
}

export function PenaltyWarningPanel({ penalties }: PenaltyWarningPanelProps) {
  const t = useTranslations("CandidateCVMatching");
  const triggeredPenalties = penalties?.filter(p => p.triggered) || [];

  if (triggeredPenalties.length === 0) return null;

  return (
    <div className="space-y-3 mb-6">
      {triggeredPenalties.map((penalty, idx) => {
        let title = t("penaltyApplied");
        let isSevere = false;

        switch (penalty.code) {
          case "RULE_TC1_01":
            title = t("mustHaveMissing");
            break;
          case "RULE_TC1_02":
            title = t("multipleCriticalMissing");
            isSevere = true;
            break;
          case "PNL_TC1_01":
            title = t("coreSkillGap", { points: penalty.deduction });
            isSevere = true;
            break;
          case "KSW_01":
            title = t("killSwitchActivated");
            isSevere = true;
            break;
          default:
            title = penalty.code;
            break;
        }

        return (
          <div key={idx} className={`border rounded-lg p-4 flex items-start gap-3 ${isSevere ? 'bg-destructive/10 border-destructive/30' : 'bg-orange-500/10 border-orange-500/30'}`}>
            <div className={`mt-0.5 shrink-0 ${isSevere ? 'text-destructive' : 'text-orange-500'}`}>
              {isSevere ? <XOctagon className="h-5 w-5" /> : <AlertTriangle className="h-5 w-5" />}
            </div>
            <div>
              <h4 className={`font-semibold mb-1 ${isSevere ? 'text-destructive' : 'text-orange-600 dark:text-orange-400'}`}>
                {title}
              </h4>
              <p className="text-sm text-foreground/80 leading-relaxed">
                {penalty.evidence}
              </p>
            </div>
          </div>
        );
      })}
    </div>
  );
}
