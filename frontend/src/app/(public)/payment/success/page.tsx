'use client';

import { useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { Suspense } from 'react';
import { CheckCircle2, Loader2 } from 'lucide-react';
import { buttonVariants } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuthStore } from '@/store/auth.store';
import { getDashboardPath } from '@/lib/constants';

function SuccessContent() {
  const searchParams = useSearchParams();
  const orderCode = searchParams.get('orderCode');
  
  // Lấy role hiện tại của người dùng (từ store)
  const user = useAuthStore((state) => state.user);
  const dashboardPath = getDashboardPath(user?.role?.name);

  return (
    <Card className="w-full max-w-md text-center shadow-lg border-zinc-200">
      <CardHeader className="pt-8 pb-4">
        <div className="mx-auto bg-green-100 p-3 rounded-full w-fit mb-4">
          <CheckCircle2 className="w-12 h-12 text-green-600" />
        </div>
        <CardTitle className="text-2xl font-bold text-zinc-900">
          Thanh toán thành công!
        </CardTitle>
        <CardDescription className="text-zinc-500 mt-2">
          Cảm ơn bạn đã sử dụng dịch vụ của ITHunterview.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4 pb-8">
        <div className="bg-zinc-50 rounded-lg p-4 border border-zinc-100">
          <div className="flex justify-between items-center text-sm mb-2">
            <span className="text-zinc-500">Mã đơn hàng</span>
            <span className="font-mono font-medium text-zinc-900">{orderCode || 'N/A'}</span>
          </div>
          <div className="flex justify-between items-center text-sm">
            <span className="text-zinc-500">Trạng thái</span>
            <span className="text-green-600 font-medium">Đã xác nhận</span>
          </div>
        </div>
        <p className="text-sm text-zinc-500">
          Hệ thống đang xử lý và sẽ tự động cập nhật gói dịch vụ vào tài khoản của bạn. 
          Bạn có thể kiểm tra trong lịch sử giao dịch.
        </p>
      </CardContent>
      <CardFooter className="flex flex-col gap-3 pb-8">
        <Link href={dashboardPath} className={buttonVariants({ variant: "default", className: "w-full" })}>
          Về trang chủ
        </Link>
      </CardFooter>
    </Card>
  );
}

export default function PaymentSuccessPage() {
  return (
    <div className="flex items-center justify-center min-h-[80vh] px-4">
      <Suspense fallback={
        <div className="flex justify-center items-center h-48 w-full max-w-md">
          <Loader2 className="h-8 w-8 animate-spin text-zinc-500" />
        </div>
      }>
        <SuccessContent />
      </Suspense>
    </div>
  );
}
