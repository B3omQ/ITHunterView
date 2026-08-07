"use client";

import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Progress } from "@/components/ui/progress";
import { MatchingOutput } from "@/types/cv.types";
import { useTranslations } from "next-intl";

interface ResultOverviewCardProps {
  jdFit: NonNullable<MatchingOutput['jdFit']>;
}

export function ResultOverviewCard({ jdFit }: ResultOverviewCardProps) {
  const t = useTranslations("CandidateCVMatching");
  // Determine color based on score or result string
  let badgeColor = "bg-green-100 text-green-800";
  let ringColor = "stroke-green-500";
  
  if (jdFit.result === "Not Suitable") {
    badgeColor = "bg-red-100 text-red-800";
    ringColor = "stroke-red-500";
  } else if (jdFit.result === "Partially Suitable") {
    badgeColor = "bg-yellow-100 text-yellow-800";
    ringColor = "stroke-yellow-500";
  } else if (jdFit.result === "Suitable") {
    badgeColor = "bg-blue-100 text-blue-800";
    ringColor = "stroke-blue-500";
  }

  // Calculate circumference for the SVG ring
  const radius = 45;
  const circumference = 2 * Math.PI * radius;
  const strokeDashoffset = circumference - (jdFit.score / 100) * circumference;

  return (
    <div className="space-y-4">
      {jdFit.killSwitchTriggered && (
        <div className="bg-destructive/15 border border-destructive/50 rounded-lg p-4 flex items-start gap-3">
          <div className="bg-destructive/20 p-2 rounded-full shrink-0">
            <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" className="text-destructive"><path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"/><path d="M12 9v4"/><path d="M12 17h.01"/></svg>
          </div>
          <div>
            <h3 className="text-destructive font-bold mb-1">{t.raw("killSwitchTitle")}</h3>
            <p className="text-sm text-foreground/80 leading-relaxed">
              {t.raw("killSwitchDesc")}
            </p>
          </div>
        </div>
      )}
      
      <Card className="border-muted bg-card">
        <CardContent className="p-6">
          <div className="flex flex-col md:flex-row gap-8 items-center md:items-start">
          {/* Circular Score Indicator */}
          <div className="relative flex items-center justify-center shrink-0">
            <svg className="w-32 h-32 transform -rotate-90">
              <circle
                className="stroke-muted"
                strokeWidth="8"
                fill="transparent"
                r={radius}
                cx="64"
                cy="64"
              />
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
                  strokeDashoffset: strokeDashoffset,
                  transition: "stroke-dashoffset 1s ease-in-out"
                }}
              />
            </svg>
            <div className="absolute flex flex-col items-center justify-center">
              <span className="text-3xl font-bold">{jdFit.score}</span>
              <span className="text-xs text-muted-foreground uppercase tracking-wider font-semibold mt-1">
                JD Fit
              </span>
            </div>
          </div>

          {/* Narrative & Badges */}
          <div className="flex-1 space-y-4">
            <div>
              <h2 className="text-2xl font-bold tracking-tight mb-2">{t("analysisResultTitle")}</h2>
              <Badge variant="outline" className={`${badgeColor} border-0 font-semibold px-3 py-1 text-sm mb-3`}>
                {jdFit.result === "Highly Suitable" ? t("highlySuitable") : jdFit.result === "Suitable" ? t("suitable") : jdFit.result === "Partially Suitable" ? t("partiallySuitable") : t("notSuitable")}
              </Badge>
              <p className="text-muted-foreground leading-relaxed">
                {jdFit.narrative}
              </p>
            </div>

            {/* Sub-pools Progress */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2">
              <div className="space-y-2">
                <div className="flex justify-between text-sm items-center">
                  <div className="flex items-center gap-2">
                    <span className="font-medium text-foreground/80">{t("poolA")}</span>
                    {jdFit.poolACapped && (
                      <Badge variant="destructive" className="text-[10px] h-5 px-1.5 font-normal uppercase tracking-wider">
                        Capped
                      </Badge>
                    )}
                  </div>
                  <span className="font-semibold">{jdFit.poolA.score}/{jdFit.poolA.max}</span>
                </div>
                <Progress value={(jdFit.poolA.score / jdFit.poolA.max) * 100} className={`h-2 ${jdFit.poolACapped ? 'bg-red-100 [&>div]:bg-red-500' : ''}`} />
              </div>
              <div className="space-y-2">
                <div className="flex justify-between items-center text-sm">
                  <span className="font-medium text-muted-foreground">{t("poolB")}</span>
                  <span className="font-semibold">{jdFit.poolB.score}/{jdFit.poolB.max}</span>
                </div>
                <Progress value={(jdFit.poolB.score / jdFit.poolB.max) * 100} className="h-2" />
              </div>
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
    </div>
  );
}
