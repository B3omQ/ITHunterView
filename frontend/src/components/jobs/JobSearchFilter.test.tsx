import React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { useRouter, useSearchParams } from 'next/navigation';
import { JobSearchFilter } from './JobSearchFilter';

vi.mock('next/navigation', () => ({
  useSearchParams: vi.fn(),
  useRouter: vi.fn(),
  usePathname: vi.fn(() => '/jobs'),
}));

vi.mock('next-intl', () => ({
  useTranslations: vi.fn(() => (key: string) => key),
}));

vi.mock('@/store/auth.store', () => ({
  useAuthStore: vi.fn(() => ({ user: null })),
}));

global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};
window.HTMLElement.prototype.scrollIntoView = vi.fn();

describe('JobSearchFilter page reset contract', () => {
  const replace = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useRouter).mockReturnValue({
      replace,
      push: vi.fn(),
      refresh: vi.fn(),
      back: vi.fn(),
      forward: vi.fn(),
      prefetch: vi.fn(),
    });
  });

  const setParams = (values: Record<string, string>) => {
    vi.mocked(useSearchParams).mockReturnValue(
      new URLSearchParams(values) as unknown as ReturnType<typeof useSearchParams>,
    );
  };

  const emittedParams = () => {
    expect(replace).toHaveBeenCalledTimes(1);
    const [emittedUrl] = replace.mock.calls[0] as [string];
    return new URL(emittedUrl, 'https://test.invalid').searchParams;
  };

  it('submitting a keyword writes the query and resets page to 1', () => {
    setParams({ page: '4', location: 'Hanoi' });
    render(<JobSearchFilter />);

    const input = screen.getByPlaceholderText('keywordPlaceholder');
    fireEvent.change(input, { target: { value: 'dotnet' } });
    fireEvent.submit(input.closest('form')!);

    const params = emittedParams();
    expect(params.get('query')).toBe('dotnet');
    expect(params.get('location')).toBe('Hanoi');
    expect(params.get('page')).toBe('1');
  });

  it('selecting a location applies that location and resets page to 1', () => {
    setParams({ page: '4' });
    render(<JobSearchFilter />);

    fireEvent.click(screen.getByText('allCities'));
    fireEvent.click(screen.getByText('Hà Nội'));

    const params = emittedParams();
    expect(params.get('location')).toBe('Hà Nội');
    expect(params.get('page')).toBe('1');
  });

  it('selecting a quick level applies the level and resets page to 1', () => {
    setParams({ page: '4' });
    render(<JobSearchFilter />);

    fireEvent.click(screen.getByText('level'));
    fireEvent.click(screen.getByText('Senior'));

    const params = emittedParams();
    expect(params.get('levels')).toBe('Senior');
    expect(params.get('page')).toBe('1');
  });

  it('applying negotiable with an active salary filter writes the flag and resets page to 1', () => {
    setParams({ page: '4', minSalary: '100' });
    render(<JobSearchFilter />);

    fireEvent.click(screen.getByText('$100 - $10000'));
    fireEvent.click(screen.getByText('includeNegotiable'));
    fireEvent.click(screen.getByRole('button', { name: 'apply' }));

    const params = emittedParams();
    expect(params.get('minSalary')).toBe('100');
    expect(params.get('includeNegotiable')).toBe('true');
    expect(params.get('page')).toBe('1');
  });

  it('clearing active filters removes them and resets page to 1', () => {
    setParams({ page: '4', levels: 'Senior', minSalary: '100', includeNegotiable: 'true' });
    render(<JobSearchFilter />);

    fireEvent.click(screen.getByRole('button', { name: 'clearFilters' }));

    const params = emittedParams();
    expect(params.has('levels')).toBe(false);
    expect(params.has('minSalary')).toBe(false);
    expect(params.has('includeNegotiable')).toBe(false);
    expect(params.get('page')).toBe('1');
  });
});
