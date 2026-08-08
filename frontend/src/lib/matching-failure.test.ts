import { describe, expect, it } from 'vitest';
import {
  getMatchingFailureMessage,
  shouldOfferMatchingRetry,
} from './matching-failure';

describe('matching failure presentation', () => {
  it('does not offer another charged job for deterministic AI output failure', () => {
    expect(shouldOfferMatchingRetry({ status: 'Failed', canRetry: false })).toBe(false);
  });

  it('offers retry only when the backend explicitly allows it', () => {
    expect(shouldOfferMatchingRetry({ status: 'Failed', canRetry: true })).toBe(true);
  });

  it('explains that an invalid AI result was not charged', () => {
    expect(getMatchingFailureMessage('AI_OUTPUT_INVALID', 'Matching failed.')).toContain('refunded');
  });
});
