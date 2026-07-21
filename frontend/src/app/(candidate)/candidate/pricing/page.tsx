'use client';

import { useState, useEffect } from 'react';
import { walletService } from '@/services/wallet.service';
import type { WalletBalanceDto } from '@/types/wallet.types';
import type { SubscriptionDto, CoinPackageDto } from '@/types/subscription.types';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Badge } from '@/components/ui/badge';
import { Coins, CheckCircle2 } from 'lucide-react';
import { toast } from 'sonner';

export default function CandidatePricingPage() {
  const [balance, setBalance] = useState<WalletBalanceDto | null>(null);
  const [coinPackages, setCoinPackages] = useState<CoinPackageDto[]>([]);
  const [subscriptions, setSubscriptions] = useState<SubscriptionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [paymentGateway, setPaymentGateway] = useState<string>('VNPAY');

  const fetchData = async () => {
    try {
      setLoading(true);
      const [balanceRes, coinRes, subRes] = await Promise.all([
        walletService.getWalletBalance(),
        walletService.getActiveCoinPackages(),
        walletService.getActiveSubscriptions(),
      ]);

      if (balanceRes.success && balanceRes.data) setBalance(balanceRes.data);
      if (coinRes.success && coinRes.data?.packages) setCoinPackages(coinRes.data.packages);
      if (subRes.success && subRes.data?.items) setSubscriptions(subRes.data.items);
    } catch (error) {
      console.error(error);
      toast.error('Có lỗi xảy ra khi tải dữ liệu');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleBuyCoin = async (pkgId: string) => {
    try {
      const res = await walletService.createPaymentRequest({
        paymentGateway: paymentGateway as any,
        targetType: 'WALLET_TOPUP',
        targetId: pkgId,
      });
      if (res.success) {
        toast.success('Đã tạo yêu cầu nạp coin. Vui lòng chờ Admin duyệt (Mock).');
      } else {
        toast.error(res.message || 'Có lỗi xảy ra');
      }
    } catch (error: any) {
      toast.error(error.response?.data?.message || 'Có lỗi xảy ra');
    }
  };

  const handleBuySubscription = async (subId: number) => {
    try {
      const res = await walletService.createPaymentRequest({
        paymentGateway: paymentGateway as any,
        targetType: 'SUBSCRIPTION',
        targetId: subId.toString(),
      });
      if (res.success) {
        toast.success('Đã tạo yêu cầu đăng ký gói. Vui lòng chờ Admin duyệt (Mock).');
      } else {
        toast.error(res.message || 'Có lỗi xảy ra');
      }
    } catch (error: any) {
      toast.error(error.response?.data?.message || 'Có lỗi xảy ra');
    }
  };

  if (loading) return <div className="p-8 text-center">Đang tải...</div>;

  return (
    <div className="container mx-auto py-8 space-y-8">
      <div className="flex justify-between items-center bg-primary/5 p-6 rounded-lg border border-primary/10">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Ví của tôi</h1>
          <p className="text-muted-foreground">Nạp coin hoặc nâng cấp tài khoản để trải nghiệm nhiều tính năng hơn.</p>
        </div>
        <div className="flex items-center gap-3 bg-white p-4 rounded-xl shadow-sm border">
          <div className="bg-yellow-100 p-3 rounded-full text-yellow-600">
            <Coins className="w-6 h-6" />
          </div>
          <div>
            <p className="text-sm text-muted-foreground font-medium">Số dư hiện tại</p>
            <p className="text-2xl font-bold text-foreground">{balance?.balance || 0} <span className="text-sm font-normal text-muted-foreground">Coin</span></p>
          </div>
        </div>
      </div>

      <div className="flex justify-end items-center gap-4">
        <span className="text-sm font-medium">Chọn cổng thanh toán:</span>
        <Select value={paymentGateway} onValueChange={(val) => val && setPaymentGateway(val)}>
          <SelectTrigger className="w-[180px]">
            <SelectValue placeholder="Chọn cổng thanh toán" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="VNPAY">VNPAY</SelectItem>
            <SelectItem value="MOMO">MOMO</SelectItem>
            <SelectItem value="STRIPE">Stripe</SelectItem>
            <SelectItem value="PAYPAL">PayPal</SelectItem>
            <SelectItem value="BANK_TRANSFER">Chuyển khoản NH</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <Tabs defaultValue="coins" className="w-full">
        <TabsList className="grid w-full max-w-md grid-cols-2 mb-8">
          <TabsTrigger value="coins">Nạp Coin</TabsTrigger>
          <TabsTrigger value="subscriptions">Gói Dịch Vụ</TabsTrigger>
        </TabsList>
        
        <TabsContent value="coins">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {coinPackages.map((pkg) => (
              <Card key={pkg.id} className="relative overflow-hidden flex flex-col hover:border-primary/50 transition-colors">
                <CardHeader className="text-center pb-2">
                  <CardTitle>{pkg.name}</CardTitle>
                  <CardDescription>Nhận ngay vào ví</CardDescription>
                </CardHeader>
                <CardContent className="text-center flex-1">
                  <div className="text-4xl font-bold text-primary flex items-center justify-center gap-2 my-4">
                    <Coins className="w-8 h-8" />
                    {pkg.coins}
                  </div>
                  <p className="text-2xl font-bold mt-4 text-foreground">
                    {pkg.price.toLocaleString('vi-VN')} ₫
                  </p>
                </CardContent>
                <CardFooter>
                  <Button className="w-full" size="lg" onClick={() => handleBuyCoin(pkg.id)}>
                    Nạp ngay
                  </Button>
                </CardFooter>
              </Card>
            ))}
          </div>
        </TabsContent>

        <TabsContent value="subscriptions">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {subscriptions.map((sub) => (
              <Card key={sub.id} className="relative flex flex-col hover:border-primary/50 transition-colors">
                <CardHeader className="text-center pb-4 border-b">
                  <CardTitle className="text-xl">{sub.name}</CardTitle>
                  <div className="mt-4">
                    <span className="text-3xl font-bold">{sub.price.toLocaleString('vi-VN')} ₫</span>
                    <span className="text-muted-foreground"> / {sub.durationDays} ngày</span>
                  </div>
                </CardHeader>
                <CardContent className="flex-1 pt-6">
                  <ul className="space-y-3">
                    <li className="flex items-center gap-2">
                      <CheckCircle2 className="w-5 h-5 text-green-500" />
                      <span>{sub.featuresConfig.cvMatchLimit ? `${sub.featuresConfig.cvMatchLimit} lượt so khớp CV` : 'Không giới hạn so khớp'}</span>
                    </li>
                    <li className="flex items-center gap-2">
                      <CheckCircle2 className="w-5 h-5 text-green-500" />
                      <span>{sub.featuresConfig.mockInterviewLimit ? `${sub.featuresConfig.mockInterviewLimit} lượt phỏng vấn Mock` : 'Không giới hạn Mock Interview'}</span>
                    </li>
                    <li className="flex items-center gap-2">
                      <CheckCircle2 className="w-5 h-5 text-green-500" />
                      <span>{sub.featuresConfig.cvOptimizeLimit ? `${sub.featuresConfig.cvOptimizeLimit} lượt tối ưu CV` : 'Không giới hạn tối ưu CV'}</span>
                    </li>
                  </ul>
                </CardContent>
                <CardFooter>
                  <Button className="w-full" size="lg" onClick={() => handleBuySubscription(sub.id)}>
                    Đăng ký gói
                  </Button>
                </CardFooter>
              </Card>
            ))}
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}
