import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { MatchCvsSection } from './MatchCvsSection';
import { recruiterService } from '@/services/recruiter.service';
import type { RecruiterCvScanResultDto } from '@/types/cv.types';

vi.mock('@/services/recruiter.service', () => ({
  recruiterService: {
    getJobMatches: vi.fn(),
    matchJobWithCvsHardcode: vi.fn(),
    unlockCandidateCv: vi.fn(),
  },
}));

vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

describe('R-09/R-10/R-11 Recruiter unlock in MatchCvsSection', () => {
  const jobId = 'test-job-id-123';
  const scanResultId = 'scan-res-abc-1';

  const lockedCandidate: RecruiterCvScanResultDto = {
    scanResultId,
    anonymousLabel: 'Ứng viên #1',
    rank: 1,
    matchScore: 85,
    isUnlocked: false,
    unlockCost: 50,
    matchedAt: '2026-08-16T10:00:00Z',
    candidateName: null,
    candidateEmail: null,
    candidatePhone: null,
    cvFileName: null,
    fileUrl: null,
    candidateUserId: null,
    cvId: null,
  };

  const unlockedCandidate: RecruiterCvScanResultDto = {
    scanResultId,
    anonymousLabel: 'Ứng viên #1',
    rank: 1,
    matchScore: 85,
    isUnlocked: true,
    unlockCost: 50,
    matchedAt: '2026-08-16T10:00:00Z',
    candidateName: 'Nguyễn Văn A',
    candidateEmail: 'nguyenvana@example.test',
    candidatePhone: '+84900000001',
    cvFileName: 'nguyen_van_a_cv.pdf',
    fileUrl: 'https://signed.storage/retained-unlocks/nguyen_van_a_cv.pdf',
    candidateUserId: 'cand-user-123',
    cvId: 'cv-123',
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('posts scanResultId and never posts cvId as authority', async () => {
    vi.mocked(recruiterService.getJobMatches).mockResolvedValue({
      success: true,
      data: {
        success: true,
        message: '',
        data: {
          items: [lockedCandidate],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        },
      },
    });

    vi.mocked(recruiterService.unlockCandidateCv).mockResolvedValue({
      success: true,
      data: {
        success: true,
        message: 'Mở khóa thành công',
        unlockedVia: 'COINS',
        coinsSpent: 50,
        unlockId: 'unlock-1',
        scanResultId,
        unlockedAt: '2026-08-16T10:05:00Z',
        isRetainedCopy: true,
      },
    });

    render(<MatchCvsSection jobId={jobId} jobStatus="PUBLISHED" jobParseStatus="SUCCESS" />);

    await waitFor(() => {
      expect(screen.getByText('Ứng viên #1')).toBeInTheDocument();
    });

    // Click Unlock button to open modal
    const unlockBtn = screen.getByRole('button', { name: /Mở khóa CV/i });
    fireEvent.click(unlockBtn);

    // Click Confirm unlock in modal
    const confirmBtn = screen.getByRole('button', { name: /Xác nhận Mở khóa/i });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(recruiterService.unlockCandidateCv).toHaveBeenCalledTimes(1);
      expect(recruiterService.unlockCandidateCv).toHaveBeenCalledWith(scanResultId);
      // Ensure cvId is NOT passed
      expect(recruiterService.unlockCandidateCv).not.toHaveBeenCalledWith(expect.stringContaining('cv-123'));
    });
  });

  it('does not render identity or links before explicit success', async () => {
    vi.mocked(recruiterService.getJobMatches).mockResolvedValue({
      success: true,
      data: {
        success: true,
        message: '',
        data: {
          items: [lockedCandidate],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        },
      },
    });

    render(<MatchCvsSection jobId={jobId} jobStatus="PUBLISHED" jobParseStatus="SUCCESS" />);

    await waitFor(() => {
      expect(screen.getByText('Ứng viên #1')).toBeInTheDocument();
    });

    // Should NOT show candidate name, email, phone, view profile or view CV
    expect(screen.queryByText('Nguyễn Văn A')).not.toBeInTheDocument();
    expect(screen.queryByText('nguyenvana@example.test')).not.toBeInTheDocument();
    expect(screen.queryByText('+84900000001')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /View Profile/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /View CV/i })).not.toBeInTheDocument();
    expect(screen.getByText('85%')).toBeInTheDocument();
    expect(screen.getByText(/Khóa/)).toBeInTheDocument();
  });

  it('refreshes the same latest-run query after unlock', async () => {
    vi.mocked(recruiterService.getJobMatches)
      .mockResolvedValueOnce({
        success: true,
        data: {
          success: true,
          message: '',
          data: {
            items: [lockedCandidate],
            totalCount: 1,
            page: 1,
            pageSize: 20,
          },
        },
      })
      .mockResolvedValueOnce({
        success: true,
        data: {
          success: true,
          message: '',
          data: {
            items: [unlockedCandidate],
            totalCount: 1,
            page: 1,
            pageSize: 20,
          },
        },
      });

    vi.mocked(recruiterService.unlockCandidateCv).mockResolvedValue({
      success: true,
      data: {
        success: true,
        message: 'Mở khóa thành công',
        unlockedVia: 'COINS',
        coinsSpent: 50,
        unlockId: 'unlock-1',
        scanResultId,
        unlockedAt: '2026-08-16T10:05:00Z',
        isRetainedCopy: true,
      },
    });

    render(<MatchCvsSection jobId={jobId} jobStatus="PUBLISHED" jobParseStatus="SUCCESS" />);

    await waitFor(() => {
      expect(screen.getByText('Ứng viên #1')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /Mở khóa CV/i }));
    fireEvent.click(screen.getByRole('button', { name: /Xác nhận Mở khóa/i }));

    await waitFor(() => {
      expect(recruiterService.getJobMatches).toHaveBeenCalledTimes(2);
      expect(recruiterService.getJobMatches).toHaveBeenNthCalledWith(1, jobId, 1, 20);
      expect(recruiterService.getJobMatches).toHaveBeenNthCalledWith(2, jobId, 1, 20);
      expect(screen.getByText('Nguyễn Văn A')).toBeInTheDocument();
      expect(screen.getByText('nguyenvana@example.test')).toBeInTheDocument();
    });
  });

  it('shows an already-unlocked retained CV without charging again', async () => {
    vi.mocked(recruiterService.getJobMatches).mockResolvedValue({
      success: true,
      data: {
        success: true,
        message: '',
        data: {
          items: [unlockedCandidate],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        },
      },
    });

    render(<MatchCvsSection jobId={jobId} jobStatus="PUBLISHED" jobParseStatus="SUCCESS" />);

    await waitFor(() => {
      expect(screen.getByText('Nguyễn Văn A')).toBeInTheDocument();
      expect(screen.getByText('nguyenvana@example.test')).toBeInTheDocument();
      expect(screen.getByText('+84900000001')).toBeInTheDocument();
    });

    // Unlock button should NOT be rendered for already unlocked candidate
    expect(screen.queryByRole('button', { name: /Mở khóa CV/i })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: /View Profile/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /View CV/i })).toBeInTheDocument();
    expect(recruiterService.unlockCandidateCv).not.toHaveBeenCalled();
  });

  it('does not turn available subscription quota into unlocked UI state', async () => {
    // When isUnlocked is false, even if candidate exists in scan, it remains locked
    vi.mocked(recruiterService.getJobMatches).mockResolvedValue({
      success: true,
      data: {
        success: true,
        message: '',
        data: {
          items: [lockedCandidate],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        },
      },
    });

    render(<MatchCvsSection jobId={jobId} jobStatus="PUBLISHED" jobParseStatus="SUCCESS" />);

    await waitFor(() => {
      expect(screen.getByText('Ứng viên #1')).toBeInTheDocument();
    });

    // UI strictly respects isUnlocked: false
    expect(screen.getByRole('button', { name: /Mở khóa CV/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /View Profile/i })).not.toBeInTheDocument();
  });
});
