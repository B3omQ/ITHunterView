import type { MatchingProcessingStage } from '@/types/cv.types';

export const MATCHING_PROGRESS_STEPS = [
  'progressQueued',
  'progressPreparingCv',
  'progressPreparingJd',
  'progressMatchingRequirements',
  'progressFinalizing',
  'progressCompleted',
] as const;

export type MatchingProgressStepKey = (typeof MATCHING_PROGRESS_STEPS)[number];
export type MatchingProgressDisplayStage = MatchingProcessingStage | 'submitting';

export interface MatchingProgressView {
  stage: MatchingProgressDisplayStage;
  progressPercent: number;
  currentStepIndex: number | null;
  completedStepCount: number;
  isSubmitting: boolean;
  isWaitingForRetry: boolean;
}

const activeStageProgress: Record<
  Exclude<MatchingProcessingStage, 'waiting_for_retry' | 'completed' | 'failed'>,
  Pick<MatchingProgressView, 'progressPercent' | 'currentStepIndex' | 'completedStepCount'>
> = {
  queued: { progressPercent: 10, currentStepIndex: 0, completedStepCount: 0 },
  preparing_cv: { progressPercent: 30, currentStepIndex: 1, completedStepCount: 1 },
  preparing_jd: { progressPercent: 55, currentStepIndex: 2, completedStepCount: 2 },
  matching_requirements: { progressPercent: 85, currentStepIndex: 3, completedStepCount: 3 },
  finalizing: { progressPercent: 95, currentStepIndex: 4, completedStepCount: 4 },
};

function resolveStage(
  status: string | undefined,
  processingStage: MatchingProcessingStage | undefined,
): MatchingProcessingStage {
  if (status === 'Completed') return 'completed';
  if (status === 'Failed') return 'failed';
  if (status === 'Pending') return 'queued';
  if (status === 'RetryScheduled') {
    return processingStage === 'finalizing' ? 'finalizing' : 'waiting_for_retry';
  }
  if (status === 'Processing') {
    return processingStage === 'queued'
      || processingStage === 'preparing_cv'
      || processingStage === 'preparing_jd'
      || processingStage === 'matching_requirements'
      || processingStage === 'finalizing'
      ? processingStage
      : 'preparing_cv';
  }
  return 'queued';
}

export function getMatchingProgress(
  status?: string,
  processingStage?: MatchingProcessingStage,
  isSubmitting = false,
): MatchingProgressView {
  if (isSubmitting && !status) {
    return {
      stage: 'submitting',
      progressPercent: 5,
      currentStepIndex: null,
      completedStepCount: 0,
      isSubmitting: true,
      isWaitingForRetry: false,
    };
  }

  const stage = resolveStage(status, processingStage);
  if (stage === 'completed') {
    return {
      stage,
      progressPercent: 100,
      currentStepIndex: null,
      completedStepCount: MATCHING_PROGRESS_STEPS.length,
      isSubmitting: false,
      isWaitingForRetry: false,
    };
  }

  if (stage === 'waiting_for_retry') {
    return {
      stage,
      progressPercent: 10,
      currentStepIndex: null,
      completedStepCount: 1,
      isSubmitting: false,
      isWaitingForRetry: true,
    };
  }

  if (stage === 'failed') {
    return {
      stage,
      progressPercent: 0,
      currentStepIndex: null,
      completedStepCount: 0,
      isSubmitting: false,
      isWaitingForRetry: false,
    };
  }

  return {
    stage,
    ...activeStageProgress[stage],
    isSubmitting: false,
    isWaitingForRetry: false,
  };
}
