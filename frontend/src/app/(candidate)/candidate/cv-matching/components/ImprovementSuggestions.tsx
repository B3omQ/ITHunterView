"use client";

import { ImprovementSuggestion } from "@/types/cv.types";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Lightbulb, ArrowRight, ArrowRightCircle } from "lucide-react";
import { useTranslations } from "next-intl";

interface ImprovementSuggestionsProps {
  improvements: ImprovementSuggestion[];
}

export function ImprovementSuggestions({ improvements }: ImprovementSuggestionsProps) {
  const t = useTranslations("CandidateCVMatching");

  if (!improvements || improvements.length === 0) {
    return null;
  }

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case "high": return "bg-red-100 text-red-800 border-red-200 dark:bg-red-900/30 dark:text-red-300";
      case "medium": return "bg-yellow-100 text-yellow-800 border-yellow-200 dark:bg-yellow-900/30 dark:text-yellow-300";
      default: return "bg-slate-100 text-slate-800 border-slate-200 dark:bg-slate-800 dark:text-slate-300";
    }
  };

  return (
    <Card className="border-muted">
      <CardHeader>
        <CardTitle className="text-lg flex items-center gap-2">
          <Lightbulb className="h-5 w-5 text-amber-500" />
          {t("suggestionsTitle")}
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {improvements.map((improvement, idx) => (
          <div key={idx} className="border rounded-lg p-4 bg-muted/20">
            <div className="flex items-start justify-between gap-4 mb-2">
              <div>
                <Badge variant="outline" className={`mb-2 capitalize text-[10px] h-5 ${getPriorityColor(improvement.priority)}`}>
                  {t("priorityLabel", { priority: improvement.priority === 'high' ? t("priorityHigh") : improvement.priority === 'medium' ? t("priorityMedium") : t("priorityLow") })}
                </Badge>
                <h4 className="font-semibold text-sm">{improvement.issue}</h4>
              </div>
              <Badge variant="secondary" className="text-[10px] truncate max-w-[100px]">
                {improvement.category}
              </Badge>
            </div>
            
            <p className="text-sm text-muted-foreground mb-4">
              <span className="font-medium text-foreground mr-1">{t("actionLabel")}</span>
              {improvement.action}
            </p>

            {improvement.example && (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mt-3 text-xs">
                <div className="bg-red-50/50 dark:bg-red-950/20 border border-red-100 dark:border-red-900/30 p-3 rounded-md">
                  <div className="text-red-800 dark:text-red-400 font-semibold mb-1 flex items-center gap-1">
                    <XCircleMini /> {t("insteadOf")}
                  </div>
                  <div className="text-muted-foreground italic">"{improvement.example.before}"</div>
                </div>
                <div className="bg-green-50/50 dark:bg-green-950/20 border border-green-100 dark:border-green-900/30 p-3 rounded-md">
                  <div className="text-green-800 dark:text-green-400 font-semibold mb-1 flex items-center gap-1">
                    <CheckCircleMini /> {t("tryThis")}
                  </div>
                  <div className="text-foreground">"{improvement.example.after}"</div>
                </div>
              </div>
            )}
          </div>
        ))}
      </CardContent>
    </Card>
  );
}

function XCircleMini() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10"/><path d="m15 9-6 6"/><path d="m9 9 6 6"/></svg>
  );
}

function CheckCircleMini() {
  return (
    <svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10"/><path d="m9 12 2 2 4-4"/></svg>
  );
}
