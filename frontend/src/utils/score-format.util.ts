/**
 * Safely formats a match score into an integer percentage (0 - 100).
 * Handles both 0-1 ratio scale (e.g., 0.58 => 58%) and 0-100 percentage scale (e.g., 58.0 => 58%).
 */
export function formatMatchScore(score: number | null | undefined): number {
  if (score === null || score === undefined || isNaN(score)) return 0;
  if (score > 1) {
    return Math.round(Math.min(100, score));
  }
  return Math.round(Math.min(100, Math.max(0, score * 100)));
}

/**
 * Returns formatted score display string, e.g. "58%" or "N/A".
 */
export function displayMatchScore(score: number | null | undefined): string {
  if (score === null || score === undefined || isNaN(score)) return 'N/A';
  return `${formatMatchScore(score)}%`;
}
