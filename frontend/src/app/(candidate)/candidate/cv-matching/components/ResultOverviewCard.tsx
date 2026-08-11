"use client";

import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import type { MatchReport } from "@/types/cv.types";
import { useTranslations } from "next-intl";

interface ResultOverviewCardProps {
  report: MatchReport;
}

const methodLabelKeys: Record<MatchReport["matchMethod"], string> = {
  one_to_one_ai: "methodOneToOneAi",
  raw_text_ai: "methodRawTextAi",
  hardcode: "methodHardcode",
  vector: "methodVector",
  legacy_unknown: "methodLegacy",
};

const resultLabelKeys: Record<string, string> = {
  VERY_SUITABLE: "resultVerySuitable",
  QUITE_SUITABLE: "resultQuiteSuitable",
  PARTIAL_FIT: "resultPartialFit",
  LIMITED_FIT: "resultLimitedFit",
  LOW_FIT: "resultLowFit",
};

export function ResultOverviewCard({ report }: ResultOverviewCardProps) {
  const t = useTranslations("CandidateCVMatching");
  const scoreAvailable = report.scoreAvailable && report.scorePercent != null;
  const score = scoreAvailable ? Math.min(100, Math.max(0, report.scorePercent as number)) : null;
  const radius = 45;
  const circumference = 2 * Math.PI * radius;
  const strokeDashoffset = circumference - ((score ?? 0) / 100) * circumference;

  let badgeColor = "bg-red-100 text-red-800";
  let ringColor = "stroke-red-500";
  if (score !== null && score >= 85) {
    badgeColor = "bg-green-100 text-green-800";
    ringColor = "stroke-green-500";
  } else if (score !== null && score >= 70) {
    badgeColor = "bg-blue-100 text-blue-800";
    ringColor = "stroke-blue-500";
  } else if (score !== null && score >= 55) {
    badgeColor = "bg-yellow-100 text-yellow-800";
    ringColor = "stroke-yellow-500";
  } else if (score !== null && score >= 40) {
    badgeColor = "bg-orange-100 text-orange-800";
    ringColor = "stroke-orange-500";
  }

  const resultLabel = report.resultCode && resultLabelKeys[report.resultCode]
    ? t(resultLabelKeys[report.resultCode])
    : report.resultLabel || t("matchingResultFallback");

  return (
    <div className="space-y-4">
      <Card className="border-muted bg-card">
        <CardContent className="p-6">
          <div className="flex flex-col items-center gap-8 md:flex-row md:items-start">
            {scoreAvailable && score !== null ? <div className="relative flex shrink-0 items-center justify-center">
              <svg aria-label={`JD fit ${score.toFixed(1)} percent`} className="h-32 w-32 -rotate-90">
                <circle className="stroke-muted" strokeWidth="8" fill="transparent" r={radius} cx="64" cy="64" />
                <circle
                  className={ringColor}
                  strokeWidth="8"
                  strokeLinecap="round"
                  fill="transparent"
                  r={radius}
                  cx="64"
                  cy="64"
                  style={{
                    strokeDasharray: circumference,
                    strokeDashoffset,
                    transition: "stroke-dashoffset 1s ease-in-out",
                  }}
                />
              </svg>
              <div className="absolute flex flex-col items-center justify-center">
                <span className="text-3xl font-bold">{score.toFixed(1)}</span>
                <span className="mt-1 text-xs font-semibold uppercase tracking-wider text-muted-foreground">JD Fit</span>
              </div>
            </div> : null}

            <div className="flex-1 space-y-4">
              <div>
                <h2 className="mb-2 text-2xl font-bold tracking-tight">{t("analysisResultTitle")}</h2>
                <div className="mb-3 flex flex-wrap gap-2">
                  {scoreAvailable ? (
                    <Badge variant="outline" className={`${badgeColor} border-0 px-3 py-1 text-sm font-semibold`}>
                      {resultLabel}
                    </Badge>
                  ) : null}
                  <Badge variant="secondary">{t(methodLabelKeys[report.matchMethod])}</Badge>
                </div>
                {report.narrative && <p className="leading-relaxed text-muted-foreground">{report.narrative}</p>}
              </div>

              {report.reportKind === "legacy_summary" && (
                <p className="rounded-md border bg-muted/40 px-3 py-2 text-sm text-muted-foreground">
                  {t("legacyResultNotice")}
                </p>
              )}
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
