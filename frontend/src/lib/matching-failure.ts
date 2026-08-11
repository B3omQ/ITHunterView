import type { MatchingResultDto } from '@/types/cv.types';

export function shouldOfferMatchingRetry(
  result: Pick<MatchingResultDto, 'status' | 'canRetry'>,
): boolean {
  return result.status === 'Failed' && result.canRetry === true;
}

export function getMatchingFailureMessage(
  errorCode?: string,
  fallback = 'Matching failed.',
): string {
  switch (errorCode) {
    case 'AI_OUTPUT_INVALID':
      return 'The AI response did not match the required contract. Your Coin/quota was refunded, and no automatic retry was started.';
    case 'MATCHING_CONFIGURATION_INVALID':
      return 'Matching is temporarily unavailable because its configuration is incompatible. Your Coin/quota was refunded.';
    case 'MATCHING_INPUT_INVALID':
      return 'The prepared CV or JD data is invalid. Your Coin/quota was refunded.';
    default:
      return fallback;
  }
}
