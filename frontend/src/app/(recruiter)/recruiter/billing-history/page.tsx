import { Metadata } from "next";
import { BillingHistoryTable } from "@/components/wallet/BillingHistoryTable";

export const metadata: Metadata = {
  title: "Transaction History | ITHunterview",
  description: "Employer payment transaction history",
};

export default function RecruiterBillingHistoryPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Transaction History</h1>
        <p className="text-muted-foreground mt-2">
          Track your company's Subscription and Coin Top-up payments.
        </p>
      </div>
      
      <BillingHistoryTable />
    </div>
  );
}
