'use client';

import { usePublicSubscriptions } from '@/hooks/useSubscription';
import { useBuySubscription } from '@/hooks/useWallet';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Check, Loader2 } from 'lucide-react';
import type { SubscriptionDto } from '@/types/subscription.types';

export default function RecruiterPricingPage() {
  const { data, isLoading } = usePublicSubscriptions({ role: 'RECRUITER' });
  const { mutate: buySubscription, isPending } = useBuySubscription();
  
  const subscriptions = data?.data || [];

  const handleBuy = (sub: SubscriptionDto) => {
    buySubscription({
      targetId: sub.id.toString(),
      targetType: 'SUBSCRIPTION',
      paymentGateway: 'PAYOS',
    });
  };

  if (isLoading) {
    return (
      <div className="flex h-[60vh] items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-zinc-500" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-2">
        <div className="inline-block bg-zinc-100 text-zinc-800 text-xs font-semibold tracking-wider uppercase px-3 py-1 rounded-full ring-1 ring-inset ring-zinc-200 self-start">
          Bảng giá doanh nghiệp
        </div>
        <h1 className="text-3xl font-bold tracking-tight text-zinc-900">
          Giải Pháp Tuyển Dụng Thông Minh
        </h1>
        <p className="text-muted-foreground">
          Tiếp cận ứng viên tiềm năng nhanh chóng hơn với các gói dịch vụ được thiết kế riêng cho nhà tuyển dụng. Quản lý linh hoạt, hiệu quả tối đa.
        </p>
      </div>

      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4 items-stretch">
        {subscriptions.map((sub, idx) => {
          const isPremium = idx === 1;
          
          return (
            <Card 
              key={sub.id} 
              className={`flex flex-col flex-1 relative rounded-2xl ${
                isPremium 
                  ? 'border-zinc-900 border-2 shadow-sm' 
                  : 'border-zinc-200 bg-white'
              }`}
            >
              {isPremium && (
                <div className="absolute top-0 left-1/2 -translate-x-1/2 -translate-y-1/2">
                  <span className="bg-zinc-900 text-zinc-50 text-[10px] font-bold uppercase tracking-wider py-1 px-3 rounded-full">
                    Gói Doanh Nghiệp
                  </span>
                </div>
              )}
              
              <CardHeader className="pt-10 pb-6 text-left">
                <CardTitle className="text-xl font-bold mb-2">{sub.name}</CardTitle>
                <div className="flex items-baseline gap-1">
                  <span className="text-5xl font-extrabold tracking-tight text-zinc-900">
                    {new Intl.NumberFormat('vi-VN').format(sub.price)}đ
                  </span>
                  <span className="text-sm font-medium text-zinc-500">
                    /{sub.durationDays} ngày
                  </span>
                </div>
                <CardDescription className="mt-4 text-zinc-500">
                  {isPremium 
                    ? 'Bộ công cụ toàn diện giúp doanh nghiệp tuyển dụng hiệu suất cao.' 
                    : 'Gói cơ bản phù hợp với nhu cầu tuyển dụng nhỏ lẻ.'}
                </CardDescription>
              </CardHeader>
              
              <CardContent className="flex-1 pb-8">
                <ul className="space-y-4">
                  {sub.featuresConfig.activeJobPostings !== null && (
                    <li className="flex gap-3 items-center">
                      <Check className="w-5 h-5 shrink-0 text-zinc-900" />
                      <span className="text-zinc-600 text-sm">
                        {sub.featuresConfig.activeJobPostings} Tin tuyển dụng Active
                      </span>
                    </li>
                  )}
                  {sub.featuresConfig.activeSourcingLimit !== null && (
                    <li className="flex gap-3 items-center">
                      <Check className="w-5 h-5 shrink-0 text-zinc-900" />
                      <span className="text-zinc-600 text-sm">
                        {sub.featuresConfig.activeSourcingLimit} lượt Sourcing ứng viên
                      </span>
                    </li>
                  )}
                  {sub.featuresConfig.highlightedJobs !== null && (
                    <li className="flex gap-3 items-center">
                      <Check className="w-5 h-5 shrink-0 text-zinc-900" />
                      <span className="text-zinc-600 text-sm">
                        {sub.featuresConfig.highlightedJobs} Tin nổi bật (Highlighted)
                      </span>
                    </li>
                  )}
                  {sub.featuresConfig.analytics && (
                    <li className="flex gap-3 items-center">
                      <Check className="w-5 h-5 shrink-0 text-zinc-900" />
                      <span className="text-zinc-600 text-sm">
                        Báo cáo & Phân tích chuyên sâu
                      </span>
                    </li>
                  )}
                </ul>
              </CardContent>
              
              <CardFooter className="pb-10 pt-0">
                <Button 
                  className="w-full h-12 text-sm font-semibold"
                  variant={isPremium ? 'default' : 'outline'}
                  onClick={() => handleBuy(sub)}
                  disabled={isPending}
                >
                  {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  {isPremium ? 'Nâng cấp Doanh nghiệp' : 'Chọn gói cơ bản'}
                </Button>
              </CardFooter>
            </Card>
          );
        })}
      </div>
      
      {subscriptions.length === 0 && !isLoading && (
        <div className="text-center p-12 bg-zinc-50 rounded-2xl border border-zinc-200 max-w-2xl mx-auto">
          <p className="text-zinc-500">Hiện tại chưa có gói cước nào dành cho Nhà tuyển dụng.</p>
        </div>
      )}
    </div>
  );
}
