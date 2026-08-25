import React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import JobPostingsPage from './page';
import { useJobs } from '@/hooks/useJobs';
import { useWalletBalance } from '@/hooks/useWallet';
import { usePublicCoinConfig } from '@/hooks/useCoin';

vi.mock('next/navigation', () => ({
  useRouter: vi.fn(() => ({ push: vi.fn(), replace: vi.fn() })),
}));

vi.mock('next-intl', () => {
  const translations: Record<string, string> = {
    pushTop24h: 'Push Top 24h',
    pushNowFree: 'Use free quota',
    pushNowConfiguredFree: 'Push now for free',
    confirmPushCoin: 'Confirm {cost} Coin',
    pushPriceLoading: 'Loading current Push Top price...',
    pushPriceUnavailable: 'Current Push Top price is unavailable.',
    pushPriceUnavailableShort: 'Price unavailable',
    pushConfiguredFree: '0 Coin (Free by current configuration)',
    insufficientCoin: 'Insufficient Coin',
    insufficientPushMsg: 'Balance {balance} Coin is lower than price {cost} Coin.',
    pushTopStateChanged: 'Push Top state changed.',
    costPush: 'Push Top cost',
    showing: 'Showing',
    rowsPerPage: 'Rows per page',
    perPage: 'per page',
    pageTitle: 'Job Postings',
    pageDesc: 'Manage job postings',
  };
  const translate = (key: string, values?: Record<string, string | number>) => {
    let value = translations[key] || key;
    for (const [name, replacement] of Object.entries(values ?? {})) {
      value = value.replace(`{${name}}`, String(replacement));
    }
    return value;
  };
  translate.raw = translate;
  return {
    useLocale: vi.fn(() => 'en'),
    useTranslations: vi.fn(() => translate),
  };
});

vi.mock('@/hooks/useJobs', () => ({ useJobs: vi.fn() }));
vi.mock('@/hooks/useWallet', () => ({ useWalletBalance: vi.fn() }));
vi.mock('@/hooks/useCoin', () => ({ usePublicCoinConfig: vi.fn() }));
vi.mock('@/hooks/useSignalR', () => ({ useSignalR: vi.fn(() => null) }));

global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};
window.HTMLElement.prototype.scrollIntoView = vi.fn();

describe('Recruiter Jobs Page Push Top billing contract', () => {
  const pushTopJob = vi.fn();
  const refetchWallet = vi.fn();
  const refetchCoinConfig = vi.fn();

  const baseJob = {
    id: 'job-123',
    jobCode: 'JOB-12345',
    title: 'Senior .NET Developer',
    location: 'Ho Chi Minh',
    status: 'PUBLISHED' as const,
    applicationCount: 5,
    viewCount: 20,
    publishedAt: '2026-08-20T00:00:00Z',
    expiresAt: '2026-09-20T00:00:00Z',
    createdAt: '2026-08-20T00:00:00Z',
    skills: ['C#', '.NET'],
    pushedTopUntil: null,
    analysisRevision: 1,
    isBanned: false,
  };

  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(window, 'alert').mockImplementation(() => undefined);
    pushTopJob.mockResolvedValue({ success: true, message: 'OK' });

    vi.mocked(useJobs).mockReturnValue({
      jobs: [baseJob],
      totalCount: 1,
      page: 1,
      setPage: vi.fn(),
      pageSize: 10,
      setPageSize: vi.fn(),
      search: '',
      setSearch: vi.fn(),
      status: 'ALL',
      setStatus: vi.fn(),
      loading: false,
      closeJob: vi.fn(),
      extendJob: vi.fn(),
      pushTopJob,
      refresh: vi.fn(),
      isClosing: false,
      isExtending: false,
      isPushingTop: false,
      error: '',
    } as unknown as ReturnType<typeof useJobs>);
  });

  const setWallet = (pushTopLimit: number, pushTopUsed: number, balance: number) => {
    vi.mocked(useWalletBalance).mockReturnValue({
      data: {
        data: {
          pushTopLimit,
          pushTopUsed,
          balance,
          activeSubscriptionName: 'BASIC',
        },
      },
      refetch: refetchWallet,
    } as unknown as ReturnType<typeof useWalletBalance>);
  };

  const setCoinConfig = (options: {
    price?: number;
    isLoading?: boolean;
    isError?: boolean;
  }) => {
    vi.mocked(usePublicCoinConfig).mockReturnValue({
      data: options.price === undefined
        ? undefined
        : { data: { featureCosts: { pushTop: options.price } } },
      isLoading: options.isLoading ?? false,
      isError: options.isError ?? false,
      refetch: refetchCoinConfig,
    } as unknown as ReturnType<typeof usePublicCoinConfig>);
  };

  const openPushTopModal = () => {
    const title = screen.getByText(baseJob.title);
    const row = title.closest('tr');
    expect(row).not.toBeNull();
    const buttons = within(row!).getAllByRole('button');
    fireEvent.click(buttons[buttons.length - 1]);
    fireEvent.click(screen.getByText('Push Top 24h'));
  };

  it('uses an available subscription quota without depending on Coin price state', async () => {
    setWallet(3, 1, 0);
    setCoinConfig({ isLoading: true });
    render(<JobPostingsPage />);

    openPushTopModal();
    const confirm = screen.getByRole('button', { name: 'Use free quota' });
    expect(confirm).toBeEnabled();
    fireEvent.click(confirm);

    await waitFor(() => expect(pushTopJob).toHaveBeenCalledWith('job-123', {
      expectedPaymentMethod: 'SUBSCRIPTION_QUOTA',
      expectedCoinCost: null,
    }));
  });

  it('shows and confirms the exact positive backend Coin price', async () => {
    setWallet(0, 0, 8000);
    setCoinConfig({ price: 7200 });
    render(<JobPostingsPage />);

    openPushTopModal();
    expect(screen.getAllByText(/7,200 Coin/)).toHaveLength(2);
    fireEvent.click(screen.getByRole('button', { name: 'Confirm 7,200 Coin' }));

    await waitFor(() => expect(pushTopJob).toHaveBeenCalledWith('job-123', {
      expectedPaymentMethod: 'COIN',
      expectedCoinCost: 7200,
    }));
  });

  it('treats a configured zero price as free and still sends a Coin zero snapshot', async () => {
    setWallet(0, 0, 0);
    setCoinConfig({ price: 0 });
    render(<JobPostingsPage />);

    openPushTopModal();
    expect(screen.getByText('0 Coin (Free by current configuration)')).toBeInTheDocument();
    const confirm = screen.getByRole('button', { name: 'Push now for free' });
    expect(confirm).toBeEnabled();
    fireEvent.click(confirm);

    await waitFor(() => expect(pushTopJob).toHaveBeenCalledWith('job-123', {
      expectedPaymentMethod: 'COIN',
      expectedCoinCost: 0,
    }));
  });

  it.each([
    { label: 'loading', options: { isLoading: true } },
    { label: 'error', options: { isError: true } },
    { label: 'missing', options: {} },
  ])('disables Coin confirmation when price is $label', ({ options }) => {
    setWallet(0, 0, 10000);
    setCoinConfig(options);
    render(<JobPostingsPage />);

    openPushTopModal();
    const confirm = screen.getByRole('button', { name: /Price unavailable/ });
    expect(confirm).toBeDisabled();
    fireEvent.click(confirm);
    expect(pushTopJob).not.toHaveBeenCalled();
  });

  it('compares balance against the dynamic price and renders both values', () => {
    setWallet(0, 0, 6000);
    setCoinConfig({ price: 7200 });
    render(<JobPostingsPage />);

    openPushTopModal();
    expect(screen.getByText('Balance 6,000 Coin is lower than price 7,200 Coin.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Confirm 7,200 Coin' })).toBeDisabled();
  });

  it('on 409 refreshes wallet and Coin config, keeps the modal, and does not retry', async () => {
    setWallet(0, 0, 8000);
    setCoinConfig({ price: 7200 });
    pushTopJob.mockResolvedValue({
      success: false,
      status: 409,
      message: 'Giá Coin đã thay đổi.',
    });
    render(<JobPostingsPage />);

    openPushTopModal();
    fireEvent.click(screen.getByRole('button', { name: 'Confirm 7,200 Coin' }));

    await waitFor(() => {
      expect(refetchWallet).toHaveBeenCalledTimes(1);
      expect(refetchCoinConfig).toHaveBeenCalledTimes(1);
    });
    expect(pushTopJob).toHaveBeenCalledTimes(1);
    expect(screen.getByRole('button', { name: 'Confirm 7,200 Coin' })).toBeInTheDocument();
  });
});
