import { Metadata } from "next";
import { BillingHistoryTable } from "@/components/wallet/BillingHistoryTable";

export const metadata: Metadata = {
  title: "Transaction History | ITHunterview",
  description: "Employer payment transaction history",
};

export default function RecruiterBillingHistoryPage() {
  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-10 space-y-5">
        {/* Top Header Section */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <h1 className="text-3xl font-extrabold text-[#050505] dark:text-zinc-50 tracking-tight">
              Transaction History
            </h1>
            <p className="text-[#65676B] dark:text-zinc-400 mt-1.5 text-sm">
              Track, inspect, and manage your company's Subscription and Coin Top-up payments
            </p>
          </div>
        </div>

        {/* Main Standardized 3-Tier Billing Table Component */}
        <BillingHistoryTable />
      </div>
    </div>
  );
}
