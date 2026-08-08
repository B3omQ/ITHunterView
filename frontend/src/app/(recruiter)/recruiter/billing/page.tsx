'use client';

import { usePublicSubscriptions } from '@/hooks/useSubscription';
import { useBuySubscription } from '@/hooks/useWallet';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Check, Loader2 } from 'lucide-react';
import type { SubscriptionDto } from '@/types/subscription.types';
import { useTranslations } from 'next-intl';

export default function RecruiterPricingPage() {
  const t = useTranslations("RecruiterBilling");
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
      <div>
        <h1 className="text-3xl font-bold tracking-tight">
          {t("pageTitle")}
        </h1>
        <p className="text-muted-foreground mt-2">
          {t("pageDesc")}
        </p>
      </div>

      <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4 items-stretch">
        {subscriptions.map((sub, idx) => {
          const isHiringPro = sub.name.toLowerCase().includes('pro');
          const isGrowth = sub.name.toLowerCase().includes('growth');
          const isStarter = sub.name.toLowerCase().includes('starter');
          
          let cardClassName = 'border-zinc-200 bg-white';
          if (isHiringPro) {
            cardClassName = 'border-[#609df5] bg-white ring-1 ring-[#1877F2] shadow-[0_0_15px_rgba(24,119,242,0.2)] z-10';
          } else if (isGrowth) {
            cardClassName = 'border-[#1877F2] bg-white ring-1 ring-[#1877F2] shadow-[0_0_15px_rgba(24,119,242,0.1)] z-10';
          } else if (isStarter) {
            cardClassName = 'ring-1 ring-[#1877F2]/40 bg-white shadow-md z-10';
          }
          
          return (
            <Card 
              key={sub.id} 
              className={`flex flex-col flex-1 relative rounded-2xl transition-all ${cardClassName}`}
            >

              
              <CardHeader className="pt-6 pb-4 text-left">
                <CardTitle className="text-xl font-bold mb-1">{sub.name}</CardTitle>
                <div className="flex items-baseline gap-1 mt-1 flex-wrap xl:flex-nowrap whitespace-nowrap">
                  <span className="text-3xl font-bold tracking-tight text-zinc-900">
                    {new Intl.NumberFormat('en-US').format(sub.price)}
                  </span>
                  <span className="text-sm font-bold text-zinc-500 mr-1">
                    VND
                  </span>
                  {sub.durationDays < 36500 && (
                    <span className="text-sm font-medium text-zinc-500">
                      {t("durationDays", { days: sub.durationDays })}
                    </span>
                  )}
                </div>
                <CardDescription className="mt-2 text-[13px] text-zinc-500">
                  {isHiringPro ? t("proDesc") : isGrowth ? t("growthDesc") : isStarter ? t("starterDesc") : t("basicDesc")}
                </CardDescription>
              </CardHeader>
              
              <CardContent className="flex-1 pb-6">
                <ul className="space-y-2.5">
                  {sub.featuresConfig.jobSlots != null && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-5 h-5 shrink-0 text-zinc-900" />
                      <span className="text-zinc-600 text-sm mt-0.5">
                        {t("featureJobSlots", { slots: sub.featuresConfig.jobSlots })}
                      </span>
                    </li>
                  )}
                  {sub.featuresConfig.unlockCvLimit != null && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-5 h-5 shrink-0 text-zinc-900" />
                      <span className="text-zinc-600 text-sm mt-0.5">
                        {t("featureUnlockCv", { limit: sub.featuresConfig.unlockCvLimit })}
                      </span>
                    </li>
                  )}
                  {sub.featuresConfig.pushTopLimit != null && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-5 h-5 shrink-0 text-zinc-900" />
                      <span className="text-zinc-600 text-sm mt-0.5">
                        {t("featurePushTop", { limit: sub.featuresConfig.pushTopLimit })}
                      </span>
                    </li>
                  )}
                  {sub.featuresConfig.jobExtendLimit != null && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-5 h-5 shrink-0 text-zinc-900" />
                      <span className="text-zinc-600 text-sm mt-0.5">
                        {t("featureJobExtend", { limit: sub.featuresConfig.jobExtendLimit })}
                      </span>
                    </li>
                  )}
                  {sub.featuresConfig.coinCredit != null && (
                    <li className="flex gap-3 items-start">
                      <Check className="w-5 h-5 shrink-0 text-zinc-900" />
                      <span className="text-zinc-600 text-sm mt-0.5">
                        {t("featureCoin", { coin: new Intl.NumberFormat('en-US').format(sub.featuresConfig.coinCredit) })}
                      </span>
                    </li>
                  )}
                </ul>
              </CardContent>
              
              <CardFooter className="pb-6 pt-0 border-t-0 bg-transparent">
                <Button 
                  className={`w-full h-11 text-sm font-semibold transition-all ${
                    isHiringPro 
                      ? 'bg-gradient-to-r from-[#0c4a9e] via-[#1877F2] to-[#609df5] hover:opacity-90 text-white shadow-sm'
                      : isGrowth 
                        ? 'bg-[#1877F2] hover:bg-[#1877F2]/90 text-white shadow-sm' 
                        : isStarter
                          ? 'bg-white border border-[#1877F2] hover:bg-[#1877F2]/5 text-[#1877F2] shadow-sm'
                          : 'bg-zinc-100 hover:bg-zinc-200 text-zinc-900 border-transparent shadow-none'
                  }`}
                  variant={isHiringPro || isGrowth ? 'default' : 'outline'}
                  onClick={() => handleBuy(sub)}
                  disabled={isPending}
                >
                  {isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  {isHiringPro ? t("btnPro") : isGrowth ? t("btnGrowth") : isStarter ? t("btnStarter") : t("btnBasic")}
                </Button>
              </CardFooter>
            </Card>
          );
        })}
      </div>
      
      {subscriptions.length === 0 && !isLoading && (
        <div className="text-center p-12 bg-zinc-50 rounded-2xl border border-zinc-200 max-w-2xl mx-auto">
          <p className="text-zinc-500">{t("noPlans")}</p>
        </div>
      )}
    </div>
  );
}
