'use client';

import { useWalletBalance, useBuySubscription } from '@/hooks/useWallet';
import { usePublicCoinConfig } from '@/hooks/useCoin';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Coins, Loader2, Wallet, Unlock, Briefcase, CalendarPlus, ArrowUpCircle } from 'lucide-react';
import { formatCurrency } from '@/lib/utils';
import { toast } from 'sonner';

export default function RecruiterTopUpPage() {
  const { data: balanceData, isLoading: isLoadingBalance } = useWalletBalance();
  const { data: configData, isLoading: isLoadingConfig } = usePublicCoinConfig();
  const { mutate: buyPackage, isPending: isBuying } = useBuySubscription();

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

  const currentBalance = balanceData?.data?.balance ?? 0;
  const packages = configData?.data?.packages ?? [];
  const featureCosts = configData?.data?.featureCosts;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Top Up Coins</h1>
        <p className="text-muted-foreground mt-2">Buy more coins to use job posting and candidate sourcing features.</p>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        {/* Số dư hiện tại */}
        <Card className="bg-primary/5 border-primary/20">
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-xl font-medium">Current Balance</CardTitle>
            <Wallet className="h-5 w-5 text-primary" />
          </CardHeader>
          <CardContent>
            {isLoadingBalance ? (
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
            ) : (
              <div className="flex items-baseline gap-2">
                <span className="text-4xl font-bold text-primary">{currentBalance}</span>
                <span className="text-lg font-semibold text-muted-foreground">Coins</span>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-lg font-medium">Cost per Usage</CardTitle>
          </CardHeader>
          <CardContent>
            {isLoadingConfig ? (
              <div className="flex justify-center p-4">
                <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
              </div>
            ) : (
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mt-2">
                <div className="flex flex-col items-center p-3 bg-muted/50 rounded-lg border">
                  <Unlock className="h-5 w-5 text-indigo-500 mb-2" />
                  <span className="text-sm font-medium text-center">Unlock CV</span>
                  <Badge variant="secondary" className="mt-1">{featureCosts?.unlockCv ?? 0} Coins</Badge>
                </div>
                <div className="flex flex-col items-center p-3 bg-muted/50 rounded-lg border">
                  <Briefcase className="h-5 w-5 text-blue-500 mb-2" />
                  <span className="text-sm font-medium text-center">Post Job</span>
                  <Badge variant="secondary" className="mt-1">{featureCosts?.postJob ?? 0} Coins</Badge>
                </div>
                <div className="flex flex-col items-center p-3 bg-muted/50 rounded-lg border">
                  <CalendarPlus className="h-5 w-5 text-amber-500 mb-2" />
                  <span className="text-sm font-medium text-center">Extend Job</span>
                  <Badge variant="secondary" className="mt-1">{featureCosts?.extendJob ?? 0} Coins</Badge>
                </div>
                <div className="flex flex-col items-center p-3 bg-muted/50 rounded-lg border">
                  <ArrowUpCircle className="h-5 w-5 text-emerald-500 mb-2" />
                  <span className="text-sm font-medium text-center">Push Top</span>
                  <Badge variant="secondary" className="mt-1">{featureCosts?.pushTop ?? 0} Coins</Badge>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <div className="space-y-4 pt-4">
        <h2 className="text-2xl font-semibold tracking-tight">Select a Coin Package</h2>
        
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
            <p className="text-muted-foreground">There are currently no coin packages available.</p>
          </div>
        ) : (
          <div className="grid gap-6 md:grid-cols-3 lg:grid-cols-4">
            {packages.map((pkg) => (
              <Card key={pkg.id} className="flex flex-col border-2 hover:border-primary transition-colors">
                <CardHeader className="text-center pb-4">
                  <CardTitle className="text-xl">{pkg.name}</CardTitle>
                  <CardDescription>Value Package</CardDescription>
                </CardHeader>
                <CardContent className="flex-1 text-center space-y-4">
                  <div className="flex items-center justify-center gap-2">
                    <Coins className="h-8 w-8 text-amber-500" />
                    <span className="text-4xl font-bold">{pkg.coins}</span>
                  </div>
                  <div className="text-2xl font-semibold text-primary">
                    {formatCurrency(pkg.price)}
                  </div>
                </CardContent>
                <CardFooter>
                  <Button 
                    className="w-full" 
                    onClick={() => handleBuyPackage(pkg.id)}
                    disabled={isBuying}
                  >
                    {isBuying ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : null}
                    Buy Now
                  </Button>
                </CardFooter>
              </Card>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
