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
          Enterprise Pricing
        </div>
        <h1 className="text-3xl font-bold tracking-tight text-zinc-900">
          Smart Recruiting Solutions
        </h1>
        <p className="text-muted-foreground">
          Reach potential candidates faster with tailored service packages for employers. Flexible management, maximum efficiency.
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
                    Enterprise Plan
                  </span>
                </div>
              )}
              
              <CardHeader className="pt-10 pb-6 text-left">
                <CardTitle className="text-xl font-bold mb-2">{sub.name}</CardTitle>
                <div className="flex items-baseline gap-1">
                  <span className="text-5xl font-extrabold tracking-tight text-zinc-900">
                    {new Intl.NumberFormat('en-US').format(sub.price)} VND
                  </span>
                  {sub.durationDays < 36500 && (
                    <span className="text-sm font-medium text-zinc-500">
                      /{sub.durationDays} days
                    </span>
                  )}
                </div>
                <CardDescription className="mt-4 text-zinc-500">
                  {isPremium 
                    ? 'A comprehensive toolkit to help businesses recruit with high performance.' 
                    : 'A basic package suitable for occasional recruiting needs.'}
                </CardDescription>
              </CardHeader>
              
              <CardContent className="flex-1 pb-8">
                <ul className="space-y-4">
                  {sub.featuresConfig.jobSlots != null && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-5 h-5 shrink-0 text-zinc-900" />
                      <span className="text-zinc-600 text-sm mt-0.5">
                        {sub.featuresConfig.jobSlots} Active Job Postings
                      </span>
                    </li>
                  )}
                  {sub.featuresConfig.unlockCvLimit != null && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-5 h-5 shrink-0 text-zinc-900" />
                      <span className="text-zinc-600 text-sm mt-0.5">
                        {sub.featuresConfig.unlockCvLimit} CV Unlocks (Sourcing)
                      </span>
                    </li>
                  )}
                  {sub.featuresConfig.pushTopLimit != null && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-5 h-5 shrink-0 text-zinc-900" />
                      <span className="text-zinc-600 text-sm mt-0.5">
                        {sub.featuresConfig.pushTopLimit} Job Push-to-Top Credits
                      </span>
                    </li>
                  )}
                  {sub.featuresConfig.jobExtendLimit != null && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-5 h-5 shrink-0 text-zinc-900" />
                      <span className="text-zinc-600 text-sm mt-0.5">
                        {sub.featuresConfig.jobExtendLimit} Job Extension Credits
                      </span>
                    </li>
                  )}
                  {sub.featuresConfig.coinCredit != null && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-5 h-5 shrink-0 text-zinc-900" />
                      <span className="text-zinc-600 text-sm mt-0.5">
                        Includes {new Intl.NumberFormat('en-US').format(sub.featuresConfig.coinCredit)} Coins
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
                  {isPremium ? 'Upgrade to Enterprise' : 'Choose Basic Plan'}
                </Button>
              </CardFooter>
            </Card>
          );
        })}
      </div>
      
      {subscriptions.length === 0 && !isLoading && (
        <div className="text-center p-12 bg-zinc-50 rounded-2xl border border-zinc-200 max-w-2xl mx-auto">
          <p className="text-zinc-500">There are currently no subscription plans available for Recruiters.</p>
        </div>
      )}
    </div>
  );
}
