import axios from 'axios';
import type { MatchJdRequest } from '../types/cv.types';

export type MatchingAttempt = {
  key: string;
  fingerprint: string;
};

/**
 * A request key is created once for a user intent and reused when the client
 * cannot tell whether the server accepted the request. The server owns the
 * idempotency record; the browser must not create a new key for every retry.
 */
export function createMatchingIdempotencyKey(prefix: 'submit' | 'retry' = 'submit'): string {
  const uuid = globalThis.crypto?.randomUUID?.();
  if (uuid) return `cv-jd-${prefix}-${uuid}`;

  // This fallback is only for older/non-browser test runtimes. It remains
  // within the backend's allowed key alphabet and is not used as security.
  return `cv-jd-${prefix}-${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;
}

/**
 * Produces a deterministic, local-only fingerprint so a changed CV/JD gets a
 * fresh idempotency intent. It is never sent to the API or logged.
 */
export function matchingRequestFingerprint(request: MatchJdRequest): string {
  return JSON.stringify({
    cvId: request.cvId ?? null,
    cvText: request.cvId ? null : request.cvText ?? null,
    cvFileName: request.cvId ? null : request.cvFileName ?? null,
    jobId: request.jobId ?? null,
    rawJdText: request.jobId ? null : request.rawJdText ?? null,
    jdTitle: request.jobId ? null : request.jdTitle ?? null,
  });
}

/**
 * A timeout, transport error, 408, 429, or 5xx does not tell the client
 * whether the API committed the job. Reuse the same key in those cases.
 */
export function isAmbiguousMatchingError(error: unknown): boolean {
  if (!axios.isAxiosError(error)) return true;
  const status = error.response?.status;
  return status === undefined || status === 408 || status === 429 || status >= 500;
}

export function getMatchingErrorMessage(error: unknown, fallback: string): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { message?: unknown } | undefined;
    if (typeof data?.message === 'string' && data.message.trim()) return data.message;
    if (error.message) return error.message;
  }
  if (error instanceof Error && error.message) return error.message;
  return fallback;
}
