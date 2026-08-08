import { AlertTriangle } from 'lucide-react';
import type { CvAnalysisResult } from '@/types/cv.types';

interface CvAnalysisQualityNoticeProps {
  analysis?: CvAnalysisResult | null;
}

export function CvAnalysisQualityNotice({ analysis }: CvAnalysisQualityNoticeProps) {
  if (analysis?.quality !== 'PARTIAL') return null;

  const metricAvailability: Array<[string, boolean]> = analysis.coverage
    ? [
        ['job title', analysis.coverage.titleMetricsAvailable],
        ['skills', analysis.coverage.skillMetricsAvailable],
        ['experience', analysis.coverage.experienceMetricAvailable],
        ['domain', analysis.coverage.domainMetricsAvailable],
      ]
    : [];
  const unavailableMetrics = metricAvailability
    .filter(([, available]) => !available)
    .map(([label]) => label);

  return (
    <div
      role="status"
      className="flex items-start gap-3 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900"
    >
      <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden="true" />
      <div>
        <p className="font-medium">The report was created from the CV information we could read.</p>
        <p className="mt-1 text-amber-800">
          You can still review the full result below.
          {unavailableMetrics.length > 0
            ? ` Please double-check the ${unavailableMetrics.join(', ')} sections.`
            : ' Please double-check any highlighted sections if you need higher accuracy.'}
        </p>
      </div>
    </div>
  );
}
