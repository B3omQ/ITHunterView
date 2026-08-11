import { Metadata } from "next";
import { BillingHistoryTable } from "@/components/wallet/BillingHistoryTable";
import { useTranslations } from "next-intl";

import { getTranslations } from "next-intl/server";

export async function generateMetadata({ params: { locale } }: { params: { locale: string } }): Promise<Metadata> {
  const t = await getTranslations({ locale, namespace: "CandidateBillingHistory" });
  return {
    title: t("pageTitle"),
    description: t("subtitle"),
  };
}

export default function BillingHistoryPage() {
  const t = useTranslations("CandidateBillingHistory");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">{t("title")}</h1>
        <p className="text-muted-foreground mt-2">
          {t("subtitle")}
        </p>
      </div>
      
      <BillingHistoryTable />
    </div>
  );
}
