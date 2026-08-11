'use client';

import { useState } from 'react';
import { useWalletBalance, useBuyCustomCoins, useBuySubscription, useCustomCoinTopupPrice } from '@/hooks/useWallet';
import { usePublicCoinConfig } from '@/hooks/useCoin';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import Image from 'next/image';
import { Coins, Loader2, Wallet, Zap, Sparkles, BrainCircuit } from 'lucide-react';
import { formatCurrency } from '@/lib/utils';
import { toast } from 'sonner';
import { useTranslations } from 'next-intl';

export default function TopUpPage() {
  const t = useTranslations("CandidateTopUp");
  const { data: balanceData, isLoading: isLoadingBalance } = useWalletBalance();
  const { data: configData, isLoading: isLoadingConfig } = usePublicCoinConfig();
  const { data: customCoinPriceData, isLoading: isLoadingCustomCoinPrice } = useCustomCoinTopupPrice();
  const { mutate: buyPackage, isPending: isBuying } = useBuySubscription();
  const { mutate: buyCustomCoins, isPending: isBuyingCustomCoins } = useBuyCustomCoins();
  const [customCoinAmount, setCustomCoinAmount] = useState('1');

  const handleBuyPackage = (packageId: string) => {
    buyPackage(
      { targetId: packageId, targetType: 'WALLET_TOPUP', paymentGateway: 'PAYOS' },
      {
        onSuccess: (res) => {
          if (res.data?.checkoutUrl) {
            window.location.href = res.data.checkoutUrl;
          } else {
            toast.error('Checkout link not found');
          }
        },
        onError: (error) => {
          toast.error(error.message || 'An error occurred while creating payment');
        },
      }
    );
  };

  const unitPrice = customCoinPriceData?.data?.pricePerCoinVnd;
  const parsedCustomCoinAmount = Number(customCoinAmount);
  const isCustomCoinAmountValid = Number.isInteger(parsedCustomCoinAmount)
    && parsedCustomCoinAmount >= 1
    && parsedCustomCoinAmount <= 100000;
  const customCoinTotal = isCustomCoinAmountValid && unitPrice
    ? parsedCustomCoinAmount * unitPrice
    : 0;

  const handleBuyCustomCoins = () => {
    if (!isCustomCoinAmountValid) {
      toast.error('Please enter a whole number of Coins from 1 to 100,000.');
      return;
    }

    buyCustomCoins(
      { coinAmount: parsedCustomCoinAmount, paymentGateway: 'PAYOS' },
      {
        onSuccess: (res) => {
          if (res.success && res.data?.checkoutUrl) {
            window.location.href = res.data.checkoutUrl;
          } else {
            toast.error(res.message || 'Checkout link not found');
          }
        },
        onError: (error) => {
          toast.error(error.message || 'An error occurred while creating payment');
        },
      }
    );
  };

  const currentBalance = balanceData?.data?.balance ?? 0;
  const packages = configData?.data?.packages ?? [];
  const featureCosts = configData?.data?.featureCosts;

  return (
    <div className="space-y-4">
      {/* Tier 1: Header & Context with Mascot */}
      <div className="relative overflow-hidden bg-primary/5 border border-primary/10 rounded-2xl p-5 sm:px-8 sm:py-8 flex flex-col md:flex-row justify-between items-center gap-4">
        {/* Decorative background blobs */}
        <div className="absolute top-0 right-0 -mr-20 -mt-20 w-64 h-64 bg-primary/10 rounded-full blur-3xl pointer-events-none"></div>
        <div className="absolute bottom-0 left-0 -ml-20 -mb-20 w-64 h-64 bg-primary/10 rounded-full blur-3xl pointer-events-none"></div>

        <div className="relative z-10 w-full md:w-3/5">
          <h1 className="text-2xl md:text-3xl font-bold tracking-tight text-primary">{t("title")}</h1>
          <p className="text-sm md:text-base text-muted-foreground mt-2 max-w-md">
            {t("subtitle")}
          </p>
          <div className="mt-4 inline-flex items-center gap-2.5 bg-white shadow-sm text-primary px-4 py-2 rounded-xl border border-primary/20">
            <Wallet className="h-5 w-5" />
            <span className="text-sm font-medium">{t("currentBalance")}</span>
            {isLoadingBalance ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <span className="text-xl font-bold">
                {new Intl.NumberFormat('en-US').format(currentBalance)} <span className="text-sm font-semibold">Coins</span>
              </span>
            )}
          </div>
        </div>
        
        <div className="relative z-10 w-36 h-36 md:w-48 md:h-48 hidden sm:block">
          <Image 
            src="/images/topupMascot2.png" 
            alt="Top Up Mascot" 
            fill 
            className="object-contain"
          />
        </div>
      </div>

      {/* Tier 2: Core Action */}
      <div>
        <Card className="mb-6 max-w-xl mx-auto border-primary/30">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Coins className="h-5 w-5 text-primary" />
              {t("customCoinTitle")}
            </CardTitle>
            <CardDescription>
              {t("customCoinDesc")}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {isLoadingCustomCoinPrice ? (
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Loader2 className="h-4 w-4 animate-spin" /> {t("loadingPrice")}
              </div>
            ) : unitPrice ? (
              <>
                <label className="space-y-2 block text-sm font-medium">
                  {t("coinsToTopUp")}
                  <Input
                    type="number"
                    min="1"
                    max="100000"
                    step="1"
                    inputMode="numeric"
                    value={customCoinAmount}
                    onChange={(event) => setCustomCoinAmount(event.target.value)}
                  />
                </label>
                <div className="rounded-lg border bg-muted/30 p-3 text-sm space-y-1">
                  <div className="flex justify-between text-muted-foreground">
                    <span>{t("unitPrice")}</span>
                    <span>{t("unitPriceValue", { price: formatCurrency(unitPrice) })}</span>
                  </div>
                  <div className="flex justify-between font-semibold text-foreground">
                    <span>{t("totalPayment")}</span>
                    <span>{formatCurrency(customCoinTotal)}</span>
                  </div>
                </div>
              </>
            ) : (
              <p className="text-sm text-destructive">{t("unavailable")}</p>
            )}
          </CardContent>
          <CardFooter>
            <Button
              className="w-full"
              onClick={handleBuyCustomCoins}
              disabled={!unitPrice || !isCustomCoinAmountValid || isBuyingCustomCoins}
            >
              {isBuyingCustomCoins ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
              {t("payForCustom")}
            </Button>
          </CardFooter>
        </Card>

        {isLoadingConfig ? (
          <div className="grid gap-6 md:grid-cols-3 lg:grid-cols-4">
            {[1, 2, 3, 4].map((i) => (
              <Card key={i} className="animate-pulse">
                <CardHeader className="h-24 bg-muted/50"></CardHeader>
                <CardContent className="h-16"></CardContent>
                <CardFooter className="h-16"></CardFooter>
              </Card>
            ))}
          </div>
        ) : packages.length === 0 ? (
          <div className="text-center p-8 bg-muted/20 rounded-lg border border-dashed">
            <p className="text-muted-foreground">{t("noPackages")}</p>
          </div>
        ) : (
          <div className="grid gap-6 md:grid-cols-3 max-w-4xl mx-auto">
            {packages.map((pkg, idx) => {
              const isPopular = idx === 1;
              return (
                <Card key={pkg.id} className="flex flex-col transition-all border-zinc-200 hover:border-primary/50 relative overflow-hidden">
                  {isPopular && (
                    <div className="absolute top-5 -right-10 w-40 bg-gradient-to-r from-[#1877F2] to-cyan-400 text-white text-[10px] font-bold uppercase tracking-wider text-center py-1 shadow-sm rotate-45 z-10">
                      {t("tagPopular")}
                    </div>
                  )}
                  <CardHeader className="text-center pb-4 pt-8">
                    <CardTitle className="text-xl font-bold">{pkg.name}</CardTitle>
                    <CardDescription>
                      {idx === 0 ? t("descBeginner") : idx === 1 ? t("descValue") : t("descPro")}
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="flex-1 text-center space-y-4">
                    <div className="flex items-center justify-center gap-2">
                      <Coins className="h-7 w-7 text-[#1877F2]" />
                      <span className="text-3xl font-bold">{new Intl.NumberFormat('en-US').format(pkg.coins)}</span>
                    </div>
                    <div className="text-2xl font-semibold text-primary">
                      {formatCurrency(pkg.price)}
                    </div>
                  </CardContent>
                  <CardFooter className="pb-6 pt-0 border-t-0 bg-transparent">
                    <Button
                      className="w-full h-11 text-sm font-semibold transition-all shadow-sm bg-[#1877F2] hover:bg-[#1877F2]/90 text-white"
                      variant="default"
                      onClick={() => handleBuyPackage(pkg.id)}
                      disabled={isBuying}
                    >
                      {isBuying ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
                      {t("btnBuyNow")}
                    </Button>
                  </CardFooter>
                </Card>
              )
            })}
          </div>
        )}
      </div>

      {/* Tier 3: Reference Info (Inline Banner) */}
      {!isLoadingConfig && featureCosts && (
        <div className="mt-4 flex flex-col sm:flex-row items-center justify-center gap-4 text-sm text-zinc-500">
          <div className="flex items-center gap-2 font-medium text-zinc-700">
            {t("refTitle")}
          </div>
          <div className="hidden sm:block text-zinc-300">|</div>
          <div className="flex flex-wrap justify-center gap-x-6 gap-y-2">
            <span className="flex items-center gap-1.5">
              <Sparkles className="h-4 w-4 text-[#1877F2]" />
              {t("refCvMatch")} <strong className="text-zinc-900">{new Intl.NumberFormat('en-US').format(featureCosts.cvJdMatching)}</strong>
            </span>
            <span className="flex items-center gap-1.5">
              <BrainCircuit className="h-4 w-4 text-[#1877F2]" />
              {t("refMockInterview")} <strong className="text-zinc-900">{new Intl.NumberFormat('en-US').format(featureCosts.mockInterview)}</strong>
            </span>
            <span className="flex items-center gap-1.5">
              <Zap className="h-4 w-4 text-[#1877F2]" />
              {t("refLearningPath")} <strong className="text-zinc-900">{new Intl.NumberFormat('en-US').format(featureCosts.learningPath)}</strong>
            </span>
          </div>
        </div>
      )}
    </div>
  );
}
