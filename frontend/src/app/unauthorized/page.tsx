'use client';

import Link from 'next/link';
import Image from 'next/image';
import { Button } from '@/components/ui/button';
import { Home, LogOut } from 'lucide-react';
import { useAuthStore } from '@/store/auth.store';
import { useRouter } from 'next/navigation';

export default function UnauthorizedPage() {
  const { logout } = useAuthStore();
  const router = useRouter();

  const handleLogout = async () => {
    await logout();
    router.push('/login');
  };

  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-white p-4">
      <div className="max-w-2xl w-full flex flex-col items-center text-center space-y-6">
        {/* Image at the top */}
        <div className="w-full flex justify-center mb-4">
          <Image
            src="/images/403Page.png"
            alt="403 - Access Denied"
            width={600}
            height={400}
            className="w-full max-w-[500px] h-auto object-contain"
            priority
          />
        </div>

        {/* Title */}
        <h1 className="text-4xl font-extrabold tracking-tight text-slate-900 sm:text-5xl">
          Access Denied
        </h1>

        {/* Small descriptive text */}
        <p className="text-base text-slate-500 max-w-sm mx-auto">
          You don't have permission to access this page. Please make sure you are logged in with the correct account role.
        </p>

        {/* Action Buttons */}
        <div className="pt-4 flex flex-col sm:flex-row gap-3">
          <Link href="/">
            <Button 
              size="lg" 
              className="rounded-full px-8 text-base font-semibold shadow-md flex items-center gap-2 w-full sm:w-auto"
            >
              <Home className="w-4 h-4" />
              Return Home
            </Button>
          </Link>
          <Button 
            size="lg"
            variant="outline"
            className="rounded-full px-8 text-base font-semibold shadow-sm flex items-center gap-2 w-full sm:w-auto"
            onClick={handleLogout}
          >
            <LogOut className="w-4 h-4" />
            Sign out
          </Button>
        </div>
      </div>
    </div>
  );
}
