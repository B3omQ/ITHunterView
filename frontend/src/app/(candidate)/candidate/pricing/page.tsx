'use client';

import { usePublicSubscriptions } from '@/hooks/useSubscription';
import { useBuySubscription, useWalletBalance } from '@/hooks/useWallet';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Check, Loader2 } from 'lucide-react';
import type { SubscriptionDto } from '@/types/subscription.types';

export default function CandidatePricingPage() {
  const { data, isLoading } = usePublicSubscriptions({ role: 'CANDIDATE' });
  const { mutate: buySubscription, isPending } = useBuySubscription();
  const { data: walletData } = useWalletBalance();
  
  const currentPrice = walletData?.data?.activeSubscriptionPrice ?? 0;
  const subscriptions = (data?.data || []).filter(sub => sub.price > currentPrice);

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
      <div>
        <h1 className="text-3xl font-bold tracking-tight">
          Upgrade Your Application Experience
        </h1>
        <p className="text-muted-foreground mt-2">
          Unlock powerful AI features to optimize your CV, practice interviews, and land your dream job faster. Start for free, upgrade when you're ready.
        </p>
      </div>

      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3 items-stretch pt-4">
        {subscriptions.map((sub, idx) => {
          const isPro = sub.name.toLowerCase().includes('pro') || idx === 1;
          const isMastery = sub.name.toLowerCase().includes('mastery') || idx === 2;
          
          let cardClassName = 'border-zinc-200 bg-white';
          if (isMastery) {
            cardClassName = 'border-emerald-400 bg-white ring-1 ring-emerald-400 shadow-[0_0_15px_rgba(52,211,153,0.2)] z-10';
          } else if (isPro) {
            cardClassName = 'border-[#1877F2] bg-white ring-1 ring-[#1877F2] shadow-[0_0_15px_rgba(24,119,242,0.15)] z-10';
          }
          
          return (
              <Card 
              key={sub.id} 
              className={`flex flex-col flex-1 relative rounded-2xl transition-all ${cardClassName}`}
            >

              
              <CardHeader className="pt-6 pb-4 text-left">
                <CardTitle className="text-lg font-semibold text-zinc-900">{sub.name}</CardTitle>
                <div className="flex items-baseline gap-1 mt-1">
                  <span className="text-4xl font-bold tracking-tight text-zinc-900">
                    {new Intl.NumberFormat('en-US').format(sub.price)}
                  </span>
                  <span className="text-sm font-bold text-zinc-500 mr-1">
                    VND
                  </span>
                  {sub.durationDays < 36500 && (
                    <span className="text-sm font-medium text-zinc-500">
                      /{sub.durationDays} days
                    </span>
                  )}
                </div>
                <CardDescription className="mt-2 text-[13px] text-zinc-500">
                  {idx === 0 && 'Start with basic features. Experience the system.'}
                  {idx === 1 && 'Everything you need to quickly land your dream job.'}
                  {idx === 2 && 'Advanced tools for professionals to maximize opportunities.'}
                  {idx > 2 && 'Start with basic features. Experience the system.'}
                </CardDescription>
              </CardHeader>
              
              <CardContent className="flex-1 pb-6">
                <ul className="space-y-2.5">
                  {sub.featuresConfig.cvMatchLimit !== null && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-4 h-4 shrink-0 text-zinc-900 mt-0.5" />
                      <span className="text-zinc-600 text-sm">
                        {sub.featuresConfig.cvMatchLimit} CV-JD Matches
                      </span>
                    </li>
                  )}
                  {sub.featuresConfig.cvOptimizeLimit !== null && sub.featuresConfig.cvOptimizeLimit !== undefined && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-4 h-4 shrink-0 text-zinc-900 mt-0.5" />
                      <span className="text-zinc-600 text-sm">
                        {sub.featuresConfig.cvOptimizeLimit} CV Optimizations
                      </span>
                    </li>
                  )}
                  {sub.featuresConfig.mockInterviewLimit !== null && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-4 h-4 shrink-0 text-zinc-900 mt-0.5" />
                      <span className="text-zinc-600 text-sm">
                        {sub.featuresConfig.mockInterviewLimit} AI Mock Interviews
                      </span>
                    </li>
                  )}
                  {(sub.featuresConfig.learningPathLimit !== null && sub.featuresConfig.learningPathLimit !== undefined) || (sub.featuresConfig.learningPathSlotLimit !== null && sub.featuresConfig.learningPathSlotLimit !== undefined) ? (
                    <>
                      <li className="flex gap-3 items-start">
                        <Check className="w-4 h-4 shrink-0 text-zinc-900 mt-0.5" />
                        <span className="text-zinc-600 text-sm">
                          {sub.name === 'Basic' || sub.durationDays > 365 
                            ? `${sub.featuresConfig.learningPathLimit ?? 1} Lượt tạo Learning Path (duy nhất trong chu kỳ)` 
                            : `${sub.featuresConfig.learningPathLimit ?? sub.featuresConfig.learningPathSlotLimit} Lượt tạo Learning Path / tháng`}
                        </span>
                      </li>
                      <li className="flex gap-3 items-start">
                        <Check className="w-4 h-4 shrink-0 text-zinc-900 mt-0.5" />
                        <span className="text-zinc-600 text-sm">
                          {(sub.featuresConfig.learningPathSlotLimit === -1 || (sub.featuresConfig.learningPathSlotLimit && sub.featuresConfig.learningPathSlotLimit >= 999))
                            ? 'Vô hạn Slot lưu trữ lộ trình học'
                            : `${sub.featuresConfig.learningPathSlotLimit ?? 1} Slot lưu trữ lộ trình học`}
                        </span>
                      </li>
                    </>
                  ) : null}
                  {sub.featuresConfig.coinCredit !== null && sub.featuresConfig.coinCredit !== undefined && sub.featuresConfig.coinCredit > 0 && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-4 h-4 shrink-0 text-zinc-900 mt-0.5" />
                      <span className="text-zinc-600 text-sm">
                        Includes {new Intl.NumberFormat('en-US').format(sub.featuresConfig.coinCredit)} Coins
                      </span>
                    </li>
                  )}
                </ul>
              </CardContent>
              
              <CardFooter className="pb-6 pt-0 border-t-0 bg-transparent">
                <Button 
                  className={`w-full h-11 text-sm font-semibold transition-all ${
                    isMastery 
                      ? 'bg-gradient-to-r from-[#1877F2] to-emerald-400 hover:opacity-90 text-white shadow-sm'
                      : isPro 
                        ? 'bg-[#1877F2] hover:bg-[#1877F2]/90 text-white shadow-sm' 
                        : 'bg-zinc-100 hover:bg-zinc-200 text-zinc-900 border-transparent shadow-none'
                  }`}
                  variant={isPro || isMastery ? 'default' : 'outline'}
                  onClick={() => handleBuy(sub)}
                  disabled={isPending}
                >
                  {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  {idx === 0 && 'Start Using'}
                  {idx === 1 && 'Buy Now'}
                  {idx === 2 && 'Upgrade'}
                  {idx > 2 && 'Start Using'}
                </Button>
              </CardFooter>
            </Card>
          );
        })}
      </div>
      
      {subscriptions.length === 0 && !isLoading && (
        <div className="text-center p-12 bg-zinc-50 rounded-2xl border border-zinc-200 max-w-2xl mx-auto">
          <p className="text-zinc-500">There are currently no subscription plans available.</p>
        </div>
      )}
    </div>
  );
}
