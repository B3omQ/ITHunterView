'use client';

import { useState, useEffect } from 'react';
import { walletService } from '@/services/wallet.service';
import type { SubscriptionDto } from '@/types/subscription.types';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { CheckCircle2 } from 'lucide-react';
import { toast } from 'sonner';

export default function RecruiterPricingPage() {
  const [subscriptions, setSubscriptions] = useState<SubscriptionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [paymentGateway, setPaymentGateway] = useState<string>('VNPAY');

  const fetchSubscriptions = async () => {
    try {
      setLoading(true);
      const res = await walletService.getActiveSubscriptions();
      if (res.success && res.data?.items) {
        setSubscriptions(res.data.items);
      }
    } catch (error) {
      console.error(error);
      toast.error('Có lỗi xảy ra khi tải dữ liệu gói dịch vụ');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSubscriptions();
  }, []);

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
    <div className="container mx-auto py-8 space-y-8 max-w-5xl">
      <div className="text-center mb-12">
        <h1 className="text-3xl font-bold text-foreground mb-4">Nâng cấp tài khoản Nhà Tuyển Dụng</h1>
        <p className="text-muted-foreground text-lg max-w-2xl mx-auto mb-8">
          Chọn gói dịch vụ phù hợp để mở khóa các tính năng mạnh mẽ, tăng cường khả năng tiếp cận ứng viên tiềm năng và quản lý tuyển dụng hiệu quả hơn.
        </p>
        <div className="flex justify-center items-center gap-4">
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
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
        {subscriptions.map((sub) => (
          <Card key={sub.id} className="relative flex flex-col hover:border-primary/50 transition-all hover:shadow-lg">
            <CardHeader className="text-center pb-6 border-b bg-primary/5 rounded-t-xl">
              <CardTitle className="text-2xl text-primary">{sub.name}</CardTitle>
              <div className="mt-6">
                <span className="text-4xl font-bold text-foreground">{sub.price.toLocaleString('vi-VN')} ₫</span>
                <span className="text-muted-foreground"> / {sub.durationDays} ngày</span>
              </div>
            </CardHeader>
            <CardContent className="flex-1 pt-8 px-6">
              <ul className="space-y-4">
                <li className="flex items-start gap-3">
                  <CheckCircle2 className="w-5 h-5 text-green-500 shrink-0 mt-0.5" />
                  <span className="text-sm">
                    {sub.featuresConfig.activeJobPostings ? `Tối đa ${sub.featuresConfig.activeJobPostings} tin tuyển dụng hiển thị cùng lúc` : 'Không giới hạn số tin tuyển dụng'}
                  </span>
                </li>
                <li className="flex items-start gap-3">
                  <CheckCircle2 className="w-5 h-5 text-green-500 shrink-0 mt-0.5" />
                  <span className="text-sm">
                    {sub.featuresConfig.activeSourcingLimit ? `${sub.featuresConfig.activeSourcingLimit} lượt xem hồ sơ ứng viên/tháng` : 'Không giới hạn lượt xem hồ sơ'}
                  </span>
                </li>
                <li className="flex items-start gap-3">
                  <CheckCircle2 className="w-5 h-5 text-green-500 shrink-0 mt-0.5" />
                  <span className="text-sm">
                    {sub.featuresConfig.highlightedJobs ? `${sub.featuresConfig.highlightedJobs} tin tuyển dụng nổi bật` : 'Không có tin tuyển dụng nổi bật'}
                  </span>
                </li>
                <li className="flex items-start gap-3">
                  <CheckCircle2 className="w-5 h-5 text-green-500 shrink-0 mt-0.5" />
                  <span className="text-sm">
                    {sub.featuresConfig.analytics ? 'Xem báo cáo phân tích nâng cao' : 'Báo cáo thống kê cơ bản'}
                  </span>
                </li>
              </ul>
            </CardContent>
            <CardFooter className="p-6 pt-0 mt-6">
              <Button className="w-full h-12 text-lg" size="lg" onClick={() => handleBuySubscription(sub.id)}>
                Đăng ký ngay
              </Button>
            </CardFooter>
          </Card>
        ))}
      </div>
      
      {subscriptions.length === 0 && (
        <div className="text-center p-12 bg-muted/20 rounded-xl border border-dashed">
          <p className="text-muted-foreground">Hiện tại chưa có gói dịch vụ nào dành cho nhà tuyển dụng.</p>
        </div>
      )}
    </div>
  );
}
