import { describe, expect, it } from 'vitest';
import axios from 'axios';
import {
  createMatchingIdempotencyKey,
  getMatchingErrorMessage,
  isAmbiguousMatchingError,
  matchingRequestFingerprint,
} from './matching-idempotency';

describe('matching idempotency', () => {
  it('creates a backend-safe key', () => {
    const key = createMatchingIdempotencyKey();
    expect(key).toMatch(/^cv-jd-submit-[a-z0-9-]+$/);
    expect(key.length).toBeLessThanOrEqual(128);
  });

  it('changes the fingerprint when either matching source changes', () => {
    const first = matchingRequestFingerprint({ cvText: 'a'.repeat(100), rawJdText: 'j'.repeat(100) });
    const same = matchingRequestFingerprint({ cvText: 'a'.repeat(100), rawJdText: 'j'.repeat(100) });
    const changed = matchingRequestFingerprint({ cvText: 'b'.repeat(100), rawJdText: 'j'.repeat(100) });

    expect(same).toBe(first);
    expect(changed).not.toBe(first);
  });

  it.each([408, 429, 500, 503])('keeps the key for ambiguous HTTP status %s', (status) => {
    const error = axios.AxiosError.from(new Error('request failed'), undefined, undefined, undefined, {
      status,
      statusText: 'error',
      headers: {},
      config: {},
      data: undefined,
    });
    expect(isAmbiguousMatchingError(error)).toBe(true);
  });

  it('treats a validation response as definitive', () => {
    const error = axios.AxiosError.from(new Error('bad request'), undefined, undefined, undefined, {
      status: 422,
      statusText: 'unprocessable',
      headers: {},
      config: {},
      data: { message: 'INVALID_MATCHING_REQUEST' },
    });
    expect(isAmbiguousMatchingError(error)).toBe(false);
    expect(getMatchingErrorMessage(error, 'fallback')).toBe('INVALID_MATCHING_REQUEST');
  });
});
