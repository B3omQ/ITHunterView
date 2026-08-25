import { beforeEach, describe, expect, it, vi } from 'vitest';
import { recruiterService } from './recruiter.service';
import api from '@/services/api-client';
import type { PushTopBillingExpectation } from '@/types/job.types';

vi.mock('@/services/api-client', () => ({
  default: {
    post: vi.fn(),
    get: vi.fn(),
    put: vi.fn(),
    delete: vi.fn(),
  },
}));

describe('recruiterService.pushTopJob transport contract', () => {
  const postMock = vi.mocked(api.post);

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it.each<PushTopBillingExpectation>([
    {
      expectedPaymentMethod: 'SUBSCRIPTION_QUOTA',
      expectedCoinCost: null,
    },
    {
      expectedPaymentMethod: 'COIN',
      expectedCoinCost: 7200,
    },
  ])('sends the exact confirmed billing snapshot %#', async (expectation) => {
    postMock.mockResolvedValue({
      data: { success: true, message: 'Success' },
    });

    await recruiterService.pushTopJob('job-1', expectation);

    expect(postMock).toHaveBeenCalledWith(
      '/api/jobpostings/job-1/push-top',
      expectation,
    );
  });

  it('preserves HTTP 409 so the page can refresh and require reconfirmation', async () => {
    postMock.mockRejectedValue({
      isAxiosError: true,
      message: 'Request failed with status code 409',
      response: {
        status: 409,
        data: { message: 'Giá Coin đã thay đổi.' },
      },
    });

    const result = await recruiterService.pushTopJob('job-1', {
      expectedPaymentMethod: 'COIN',
      expectedCoinCost: 7200,
    });

    expect(result).toEqual(expect.objectContaining({
      success: false,
      status: 409,
      message: 'Giá Coin đã thay đổi.',
    }));
  });
});
