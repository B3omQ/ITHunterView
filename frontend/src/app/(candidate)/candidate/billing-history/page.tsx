import { Metadata } from "next";
import { BillingHistoryTable } from "@/components/wallet/BillingHistoryTable";

export const metadata: Metadata = {
  title: "Lịch sử giao dịch | ITHunterview",
  description: "Lịch sử các giao dịch thanh toán cá nhân",
};

export default function BillingHistoryPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Lịch sử giao dịch</h1>
        <p className="text-muted-foreground mt-1">
          Theo dõi các khoản thanh toán mua Gói và Nạp Coin của bạn.
        </p>
      </div>
      
      <BillingHistoryTable />
    </div>
  );
}
