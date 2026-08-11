'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { APP_ROUTES } from '@/lib/constants';

export default function LegacyCvOptimizePage() {
  const router = useRouter();

  useEffect(() => {
    router.replace(APP_ROUTES.CANDIDATE.OPTIMIZE_CV);
  }, [router]);

  return null;
}
