import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { JdAnalysisQualityNotice } from './JdAnalysisQualityNotice';

describe('JdAnalysisQualityNotice', () => {
  it('shows a user-facing warning for a partial JD without treating it as a failure', () => {
    render(
      <JdAnalysisQualityNotice
        quality="PARTIAL"
        scoreBasis="accepted_requirements_only"
      />,
    );

    expect(screen.getByRole('status')).toBeTruthy();
    expect(screen.getByText(/uses the JD requirements that could be read/i)).toBeTruthy();
  });

  it('does not render anything for a complete analysis', () => {
    const { container } = render(<JdAnalysisQualityNotice quality="COMPLETE" />);
    expect(container.firstChild).toBeNull();
  });
});
