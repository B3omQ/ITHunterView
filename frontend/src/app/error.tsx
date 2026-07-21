'use client';

import { useEffect } from 'react';
import Image from 'next/image';
import { Button } from '@/components/ui/button';
import { RefreshCcw, Home } from 'lucide-react';

export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    // Log the error to an error reporting service
    console.error('System Error:', error);
  }, [error]);

  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-white p-4">
      <div className="max-w-2xl w-full flex flex-col items-center text-center space-y-6">
        {/* Image at the top */}
        <div className="w-full flex justify-center mb-4">
          <Image
            src="/images/500Page.png"
            alt="500 - Internal Server Error"
            width={600}
            height={400}
            className="w-full max-w-[500px] h-auto object-contain"
            priority
          />
        </div>

        {/* Error Title */}
        <h1 className="text-4xl font-extrabold tracking-tight text-slate-900 sm:text-5xl">
          Internal Server Error
        </h1>

        {/* Small descriptive text */}
        <p className="text-base text-slate-500 max-w-sm mx-auto">
          We are experiencing some technical difficulties on our end. Please try again or return to the homepage.
        </p>

        {/* Action Buttons */}
        <div className="pt-4 flex flex-col sm:flex-row gap-3">
          <Button 
            size="lg" 
            variant="outline" 
            className="rounded-full px-8 text-base font-semibold shadow-sm flex items-center gap-2"
            onClick={() => window.location.href = '/'}
          >
            <Home className="w-4 h-4" />
            Home Page
          </Button>
          <Button 
            size="lg" 
            className="rounded-full px-8 text-base font-semibold shadow-md flex items-center gap-2"
            onClick={() => reset()}
          >
            <RefreshCcw className="w-4 h-4" />
            Try Again
          </Button>
        </div>
      </div>
    </div>
  );
}
