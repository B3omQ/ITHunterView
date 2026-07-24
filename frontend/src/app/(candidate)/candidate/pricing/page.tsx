'use client';

import { usePublicSubscriptions } from '@/hooks/useSubscription';
import { useBuySubscription } from '@/hooks/useWallet';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Check, Loader2 } from 'lucide-react';
import type { SubscriptionDto } from '@/types/subscription.types';

export default function CandidatePricingPage() {
  const { data, isLoading } = usePublicSubscriptions({ role: 'CANDIDATE' });
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
      <div>
        <h1 className="text-3xl font-bold tracking-tight">
          Upgrade Your Application Experience
        </h1>
        <p className="text-muted-foreground mt-2">
          Unlock powerful AI features to optimize your CV, practice interviews, and land your dream job faster. Start for free, upgrade when you're ready.
        </p>
      </div>

      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3 items-stretch">
        {subscriptions.map((sub, idx) => {
          const isPremium = idx === 1; // Giả sử gói thứ 2 là gói Premium
          
          return (
              <Card 
              key={sub.id} 
              className={`flex flex-col flex-1 relative rounded-2xl transition-all ${
                isPremium 
                  ? 'border-zinc-300 bg-white ring-1 ring-zinc-900 shadow-md' 
                  : 'border-zinc-200 bg-zinc-50/50'
              }`}
            >
              {isPremium && (
                <div className="absolute top-0 left-1/2 -translate-x-1/2 -translate-y-1/2">
                  <span className="bg-zinc-900 text-zinc-50 text-[11px] font-semibold tracking-wide py-1 px-4 rounded-full shadow-sm">
                    Recommended
                  </span>
                </div>
              )}
              
              <CardHeader className="pt-8 pb-5 text-left">
                <CardTitle className="text-lg font-semibold mb-1 text-zinc-900">{sub.name}</CardTitle>
                <div className="flex items-baseline gap-1 mt-2">
                  <span className="text-4xl font-bold tracking-tight text-zinc-900">
                    {new Intl.NumberFormat('en-US').format(sub.price)} VND
                  </span>
                  {sub.durationDays < 36500 && (
                    <span className="text-sm font-medium text-zinc-500">
                      /{sub.durationDays} days
                    </span>
                  )}
                </div>
                <CardDescription className="mt-3 text-sm text-zinc-500">
                  {isPremium 
                    ? 'Everything you need to quickly land your dream job.' 
                    : 'Start with basic features. Experience the system.'}
                </CardDescription>
              </CardHeader>
              
              <CardContent className="flex-1 pb-8">
                <ul className="space-y-3.5">
                  {sub.featuresConfig.cvMatchLimit !== null && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-4 h-4 shrink-0 text-zinc-900 mt-0.5" />
                      <span className="text-zinc-600 text-sm">
                        {sub.featuresConfig.cvMatchLimit} CV-JD Matches
                      </span>
                    </li>
                  )}
                  {sub.featuresConfig.learningPathSlotLimit !== null && sub.featuresConfig.learningPathSlotLimit !== undefined && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-4 h-4 shrink-0 text-zinc-900 mt-0.5" />
                      <span className="text-zinc-600 text-sm">
                        {sub.featuresConfig.learningPathSlotLimit} Learning Path Generations
                      </span>
                    </li>
                  )}
                  {sub.featuresConfig.aiRefreshUnlimited === true && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-4 h-4 shrink-0 text-zinc-900 mt-0.5" />
                      <span className="text-zinc-600 text-sm">
                        Unlimited AI Profile Refreshes
                      </span>
                    </li>
                  )}
                  {sub.featuresConfig.premiumBadge === true && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-4 h-4 shrink-0 text-zinc-900 mt-0.5" />
                      <span className="text-zinc-600 text-sm">
                        Premium Profile Badge
                      </span>
                    </li>
                  )}
                  {sub.featuresConfig.coinCredit !== null && sub.featuresConfig.coinCredit !== undefined && sub.featuresConfig.coinCredit > 0 && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-4 h-4 shrink-0 text-zinc-900 mt-0.5" />
                      <span className="text-zinc-600 text-sm">
                        Includes {new Intl.NumberFormat('en-US').format(sub.featuresConfig.coinCredit)} Coins
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
                </ul>
              </CardContent>
              
              <CardFooter className="pb-8 pt-0">
                <Button 
                  className={`w-full h-11 text-sm font-semibold transition-all ${
                    isPremium 
                      ? 'bg-blue-600 hover:bg-blue-700 text-white shadow-sm' 
                      : 'bg-white hover:bg-zinc-50 text-zinc-900 border-zinc-200'
                  }`}
                  variant={isPremium ? 'default' : 'outline'}
                  onClick={() => handleBuy(sub)}
                  disabled={isPending}
                >
                  {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  {isPremium ? 'Buy Now' : 'Start Using'}
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
