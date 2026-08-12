"use client";

import type { MatchEvidenceReport } from "@/types/cv.types";
import { useTranslations } from "next-intl";

interface EvidenceListProps {
  evidence: MatchEvidenceReport[];
}

export function EvidenceList({ evidence }: EvidenceListProps) {
  const t = useTranslations("CandidateCVMatching");
  const validEvidence = evidence.filter((entry) => entry.quotation.trim().length > 0);

  if (validEvidence.length === 0) {
    return <p className="text-xs text-muted-foreground">{t("noEvidence")}</p>;
  }

  return (
    <div className="space-y-2" aria-label={t("evidenceLabel")}>
      {validEvidence.map((entry, index) => (
        <blockquote
          key={`${entry.quotation}-${entry.section ?? "unknown"}-${index}`}
          className="border-l-2 border-border pl-3 text-xs leading-relaxed text-muted-foreground"
        >
          “{entry.quotation}”
          {entry.section ? <span className="ml-1 font-medium">— {entry.section}</span> : null}
        </blockquote>
      ))}
    </div>
  );
}
