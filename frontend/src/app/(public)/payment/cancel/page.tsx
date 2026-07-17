'use client';

import { useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { Suspense } from 'react';
import { XCircle, Loader2 } from 'lucide-react';
import { buttonVariants } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { useAuthStore } from '@/store/auth.store';
import { APP_ROUTES } from '@/lib/constants';

function CancelContent() {
  const searchParams = useSearchParams();
  const orderCode = searchParams.get('orderCode');

  // Lấy role hiện tại của người dùng
  const user = useAuthStore((state) => state.user);
  const isRecruiter = user?.role?.name?.toLowerCase() === 'recruiter';
  const retryPath = isRecruiter ? '/recruiter/billing' : APP_ROUTES.CANDIDATE.PRICING;

  return (
    <Card className="w-full max-w-md text-center shadow-lg border-zinc-200">
      <CardHeader className="pt-8 pb-4">
        <div className="mx-auto bg-red-100 p-3 rounded-full w-fit mb-4">
          <XCircle className="w-12 h-12 text-red-600" />
        </div>
        <CardTitle className="text-2xl font-bold text-zinc-900">
          Thanh toán đã bị hủy
        </CardTitle>
        <CardDescription className="text-zinc-500 mt-2">
          Giao dịch của bạn đã bị hủy hoặc không thành công.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4 pb-8">
        <Alert variant="destructive" className="text-left bg-red-50 border-red-200">
          <AlertTitle className="text-red-800">Lưu ý</AlertTitle>
          <AlertDescription className="text-red-700">
            Tài khoản của bạn chưa bị trừ tiền. Nếu bạn gặp sự cố khi thanh toán, vui lòng thử lại hoặc liên hệ hỗ trợ.
          </AlertDescription>
        </Alert>

        <div className="bg-zinc-50 rounded-lg p-4 border border-zinc-100">
          <div className="flex justify-between items-center text-sm mb-2">
            <span className="text-zinc-500">Mã đơn hàng</span>
            <span className="font-mono font-medium text-zinc-900">{orderCode || 'N/A'}</span>
          </div>
          <div className="flex justify-between items-center text-sm">
            <span className="text-zinc-500">Trạng thái</span>
            <span className="text-red-600 font-medium">Đã hủy</span>
          </div>
        </div>
      </CardContent>
      <CardFooter className="flex flex-col gap-3 pb-8">
        <Link href={retryPath} className={buttonVariants({ variant: "default", className: "w-full" })}>
          Thử lại
        </Link>
      </CardFooter>
    </Card>
  );
}

export default function PaymentCancelPage() {
  return (
    <div className="flex items-center justify-center min-h-[80vh] px-4">
      <Suspense fallback={
        <div className="flex justify-center items-center h-48 w-full max-w-md">
          <Loader2 className="h-8 w-8 animate-spin text-zinc-500" />
        </div>
      }>
        <CancelContent />
      </Suspense>
    </div>
  );
}
