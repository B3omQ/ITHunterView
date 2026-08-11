import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { AnalysisStateCard } from './AnalysisStateCard'

describe('AnalysisStateCard quality states', () => {
  const baseProps = {
    preview: null,
    lifecycle: 'READY' as const,
    isLoading: false,
    isCurrentAnalysis: true,
    onEditDraft: () => undefined,
    onRetryAnalysis: () => undefined,
    isRetrying: false,
  }

  it('keeps a partial analysis publishable and explains accepted coverage', () => {
    render(
      <AnalysisStateCard
        {...baseProps}
        preview={{
          jobId: 'job-1',
          hasAnalysisRun: true,
          analysisRunId: 'run-1',
          inputRevision: 1,
          currentJobRevision: 1,
          lifecycleState: 'READY',
          isCurrentAnalysis: true,
          status: 'READY',
          decisionVersion: 0,
          suggestions: [],
          otherRequirements: [],
          canFinalize: true,
          blockingReasons: [],
          finalActionLabel: 'Publish',
          finalTargetStatus: 'PUBLISHED',
          analysisQuality: 'PARTIAL',
          analysisCoverage: {
            inputGroupCount: 4,
            acceptedGroupCount: 3,
            discardedGroupCount: 1,
            inputItemCount: 4,
            acceptedItemCount: 3,
            discardedItemCount: 1,
            requirementSetComplete: false,
          },
        }}
      />,
    )

    expect(screen.getByText(/3\/4/)).toBeInTheDocument()
    expect(screen.getByText(/vẫn có thể kiểm tra và xuất bản/i)).toBeInTheDocument()
  })
})
