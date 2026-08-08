import { AlertTriangle } from 'lucide-react';

type JdAnalysisQuality = 'COMPLETE' | 'PARTIAL' | 'INVALID';

interface JdAnalysisQualityNoticeProps {
  quality?: JdAnalysisQuality | null;
  scoreBasis?: string;
  coverage?: { acceptedGroupCount: number; inputGroupCount: number } | null;
}

export function JdAnalysisQualityNotice({
  quality,
  scoreBasis,
  coverage,
}: JdAnalysisQualityNoticeProps) {
  if (quality === 'INVALID') {
    return (
      <div
        role="status"
        className="flex items-start gap-3 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900"
      >
        <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
        <span>The structured JD analysis was unavailable. The original JD text is retained for matching.</span>
      </div>
    );
  }

  if (quality !== 'PARTIAL') return null;

  const coverageText = coverage
    ? ` ${coverage.acceptedGroupCount}/${coverage.inputGroupCount} requirement groups were read.`
    : '';
  const message = scoreBasis === 'accepted_requirements_only'
    ? `This result uses the JD requirements that could be read successfully.${coverageText} Some requirements were unavailable, so the score is indicative rather than a complete JD assessment.`
    : 'The JD analysis was partially available. The result is still usable, but review the requirement breakdown carefully.';

  return (
    <div
      role="status"
      className="flex items-start gap-3 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900"
    >
      <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
      <span>{message}</span>
    </div>
  );
}
