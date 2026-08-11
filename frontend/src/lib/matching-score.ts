import type { MatchHistoryDto, MatchMethodCode } from "@/types/cv.types";

export function getScorePercent(match: Pick<MatchHistoryDto, "scorePercent">): number {
  if (!Number.isFinite(match.scorePercent)) return 0;
  return Math.min(100, Math.max(0, match.scorePercent));
}

export function getMatchMethodLabel(method: MatchMethodCode): string {
  switch (method) {
    case "one_to_one_ai": return "AI requirement matching";
    case "raw_text_ai": return "AI matching from JD text";
    case "hardcode": return "Keyword matching";
    case "vector": return "Vector matching";
    default: return "Legacy matching";
  }
}

export function getMatchBandLabel(scorePercent: number): string {
  const score = Math.min(100, Math.max(0, Number.isFinite(scorePercent) ? scorePercent : 0));
  if (score >= 85) return "Rất phù hợp";
  if (score >= 70) return "Khá phù hợp";
  if (score >= 55) return "Phù hợp một phần";
  if (score >= 40) return "Độ phù hợp còn hạn chế";
  return "Độ phù hợp thấp";
}
