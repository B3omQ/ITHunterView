"use client";

import { CriticalGap, Penalty } from "@/types/cv.types";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import { AlertTriangle, XCircle } from "lucide-react";
import { Alert, AlertTitle, AlertDescription } from "@/components/ui/alert";
import { useTranslations } from "next-intl";

interface CriticalGapsPanelProps {
  criticalGaps: CriticalGap[];
  penalties: Penalty[];
}

export function CriticalGapsPanel({ criticalGaps, penalties }: CriticalGapsPanelProps) {
  const t = useTranslations("CandidateCVMatching");

  if (criticalGaps.length === 0) {
    return null;
  }

  return (
    <Card className="border-red-200/50 dark:border-red-900/30">
      <CardHeader className="pb-3">
        <CardTitle className="text-lg flex items-center gap-2 text-destructive">
          <AlertTriangle className="h-5 w-5" />
          {t("criticalGapsTitle")}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {criticalGaps.map((gap, index) => (
          <Alert key={`gap-${index}`} variant="destructive" className="bg-red-50/50 dark:bg-red-950/20 border-red-200 dark:border-red-900/50">
            <XCircle className="h-4 w-4" />
            <AlertTitle className="font-semibold text-red-800 dark:text-red-300">
              {gap.requirement}
            </AlertTitle>
            <AlertDescription className="mt-2 text-red-700/90 dark:text-red-400">
              <p className="mb-2">{gap.gapDescription}</p>
              <div className="text-xs bg-red-100 dark:bg-red-900/40 p-2 rounded-md font-medium">
                <span className="opacity-80 uppercase tracking-wider text-[10px] block mb-1">{t("suggestionLabel")}</span>
                {gap.suggestion}
              </div>
            </AlertDescription>
          </Alert>
        ))}
      </CardContent>
    </Card>
  );
}
