"use client";

import { RequirementScore, RequirementCategory } from "@/types/cv.types";
import { CheckCircle2, XCircle, AlertCircle, ChevronDown, Check, Info } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import { useState } from "react";
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/card";

interface RequirementBreakdownProps {
  scores: RequirementScore[];
}

const CATEGORY_ORDER: RequirementCategory[] = [
  "tech_skill",
  "experience",
  "seniority_fit",
  "domain_knowledge",
  "language",
  "education",
  "soft_skill",
];

const CATEGORY_LABELS: Record<RequirementCategory, string> = {
  tech_skill: "Technical Skills",
  experience: "Experience",
  seniority_fit: "Seniority Fit",
  domain_knowledge: "Domain Knowledge",
  language: "Language",
  education: "Education",
  soft_skill: "Soft Skills",
};

export function RequirementBreakdown({ scores }: RequirementBreakdownProps) {
  // Group by category
  const groupedScores = CATEGORY_ORDER.reduce((acc, category) => {
    const catScores = scores.filter(s => s.category === category);
    if (catScores.length > 0) {
      acc.push({ category, scores: catScores });
    }
    return acc;
  }, [] as { category: RequirementCategory; scores: RequirementScore[] }[]);

  return (
    <Card className="border-muted">
      <CardHeader>
        <CardTitle className="text-lg">JD Fit Requirement Breakdown</CardTitle>
        <CardDescription>Detailed mapping and scores of specific job requirements against your resume.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-8">
        {groupedScores.map(group => (
          <div key={group.category}>
            <div className="flex items-center gap-2 mb-3 border-b pb-2">
              <h3 className="text-sm font-bold uppercase tracking-wider text-foreground">
                {CATEGORY_LABELS[group.category]}
              </h3>
              {group.scores[0]?.categoryWeight && (
                <Badge variant="outline" className="text-[10px] font-mono text-muted-foreground">
                  w={group.scores[0].categoryWeight}
                </Badge>
              )}
            </div>
            <div className="space-y-3">
              {group.scores.map(req => (
                <RequirementRow key={req.reqId} req={req} />
              ))}
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}

function RequirementRow({ req }: { req: RequirementScore }) {
  const [isOpen, setIsOpen] = useState(false);

  // Status Icon & Colors based on 5 levels
  let Icon = AlertCircle;
  let iconClass = "text-yellow-500";
  let bgClass = "bg-yellow-500/10";
  
  if (req.handlerScore === 1.0) {
    Icon = CheckCircle2;
    iconClass = "text-green-500";
    bgClass = "bg-green-500/10";
  } else if (req.handlerScore >= 0.7) {
    Icon = Check;
    iconClass = "text-blue-500";
    bgClass = "bg-blue-500/10";
  } else if (req.handlerScore >= 0.5) {
    Icon = AlertCircle;
    iconClass = "text-yellow-500";
    bgClass = "bg-yellow-500/10";
  } else if (req.handlerScore >= 0.3) {
    Icon = Info;
    iconClass = "text-orange-500";
    bgClass = "bg-orange-500/10";
  } else {
    Icon = XCircle;
    iconClass = "text-red-500";
    bgClass = "bg-red-500/10";
  }

  // Display Name
  const displayName = req.normalizedText || req.entities?.skill_name || req.category;

  return (
    <Collapsible
      open={isOpen}
      onOpenChange={setIsOpen}
      className="border border-border/50 rounded-lg overflow-hidden transition-all hover:border-border"
    >
      <CollapsibleTrigger className="flex items-center w-full p-3 hover:bg-muted/30 transition-colors text-left group">
        <div className={`p-1.5 rounded-full mr-3 shrink-0 ${bgClass}`}>
          <Icon className={`h-4 w-4 ${iconClass}`} />
        </div>
        
        <div className="flex-1 min-w-0 pr-4">
          <div className="flex items-center gap-2 flex-wrap mb-1">
            <span className="font-medium text-sm truncate">{displayName}</span>
            <Badge variant={req.importance === "must_have" ? "default" : "secondary"} className="text-[10px] h-5 px-1.5 font-normal">
              {req.importance === "must_have" ? "Must Have" : "Nice To Have"}
            </Badge>
            {req.flag === "CRITICAL_GAP" && (
              <Badge variant="destructive" className="text-[10px] h-5 px-1.5 font-normal animate-pulse">
                Critical Gap
              </Badge>
            )}
          </div>
          {/* 5-Level Indicator */}
          <div className="flex gap-1 mt-1.5">
            {[0.3, 0.5, 0.7, 1.0].map((step, idx) => {
              const isActive = req.handlerScore >= step;
              const isZero = req.handlerScore === 0;
              let stepColor = "bg-muted";
              if (isActive) {
                if (req.handlerScore === 1.0) stepColor = "bg-green-500";
                else if (req.handlerScore >= 0.7) stepColor = "bg-blue-500";
                else if (req.handlerScore >= 0.5) stepColor = "bg-yellow-500";
                else stepColor = "bg-orange-500";
              } else if (isZero && idx === 0) {
                 // For 0 score, we show empty bars, but maybe one red bar? 
                 // Actually 0 means all are empty (bg-muted).
              }
              return (
                <div 
                  key={step} 
                  className={`h-1.5 w-6 rounded-full ${stepColor} transition-colors duration-300`} 
                  title={`Score Level`}
                />
              );
            })}
          </div>
        </div>

        <div className="flex items-center gap-3 shrink-0">
          <div className="text-right">
            <span className="text-xs font-semibold">{Math.round(req.handlerScore * 100)}%</span>
          </div>
          <ChevronDown className={`h-4 w-4 text-muted-foreground transition-transform duration-200 ${isOpen ? 'rotate-180' : ''}`} />
        </div>
      </CollapsibleTrigger>
      
      <CollapsibleContent>
        <div className="px-4 pb-4 pt-1 ml-[42px]">
          <div className="bg-muted/40 p-3 rounded-md border border-border/30 text-sm text-muted-foreground leading-relaxed">
            {req.reasoning || "No detailed reasoning provided."}
          </div>
        </div>
      </CollapsibleContent>
    </Collapsible>
  );
}
