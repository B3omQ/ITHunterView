import Link from 'next/link';
import Image from 'next/image';
import { Button } from '@/components/ui/button';

export default function NotFound() {
  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-white p-4">
      <div className="max-w-2xl w-full flex flex-col items-center text-center space-y-6">
        {/* Image at the top */}
        <div className="w-full flex justify-center mb-4">
          <Image
            src="/images/404Page.png"
            alt="404 - Page Not Found"
            width={600}
            height={400}
            className="w-full max-w-[500px] h-auto object-contain"
            priority
          />
        </div>

        {/* Page not found (highlight) */}
        <h1 className="text-4xl font-extrabold tracking-tight text-slate-900 sm:text-5xl">
          Page not found
        </h1>

        {/* Small descriptive text */}
        <p className="text-base text-slate-500 max-w-sm mx-auto">
          The page you are looking for might have been removed had its name changed or is temporarily unavailable.
        </p>

        {/* Home Page button */}
        <div className="pt-4">
          <Link href="/">
            <Button size="lg" className="rounded-full px-8 text-base font-semibold shadow-md">
              Home Page
            </Button>
          </Link>
        </div>
      </div>
    </div>
  );
}
