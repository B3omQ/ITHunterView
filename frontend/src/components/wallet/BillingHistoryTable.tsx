"use client";

import React, { useState } from "react";
import { useMyPayments } from "@/hooks/useWallet";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { format } from "date-fns";
import { ArrowLeft, ArrowRight, Loader2, SearchX } from "lucide-react";

export function BillingHistoryTable() {
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<string>("ALL");
  const [targetType, setTargetType] = useState<string>("ALL");
  const pageSize = 10;

  const { data: response, isLoading, isError } = useMyPayments({
    page,
    pageSize,
    ...(status !== "ALL" && { status }),
    ...(targetType !== "ALL" && { targetType }),
  });

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "SUCCESS":
      case "PAID":
        return <Badge className="bg-emerald-500/10 text-emerald-600 hover:bg-emerald-500/20 border-emerald-500/20">Success</Badge>;
      case "PENDING":
        return <Badge className="bg-amber-500/10 text-amber-600 hover:bg-amber-500/20 border-amber-500/20">Pending</Badge>;
      case "FAILED":
      case "CANCELLED":
        return <Badge className="bg-rose-500/10 text-rose-600 hover:bg-rose-500/20 border-rose-500/20">Failed/Cancelled</Badge>;
      default:
        return <Badge variant="outline">{status}</Badge>;
    }
  };

  const getTargetTypeLabel = (type: string, subName: string | null) => {
    if (type === "SUBSCRIPTION") {
      return subName ? `Plan: ${subName}` : "Upgrade Plan";
    }
    if (type === "WALLET_TOPUP" || type === "COIN_PACKAGE") {
      return "Top Up Coins";
    }
    return type;
  };

  const formatCurrency = (amount: number, currency: string) => {
    return new Intl.NumberFormat("vi-VN", {
      style: "currency",
      currency: currency || "VND",
    }).format(amount);
  };

  return (
    <div className="space-y-6">
      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-4 items-center justify-between">
        <div className="flex items-center gap-3 w-full sm:w-auto">
          <Select value={status} onValueChange={(val) => { setStatus(val || "ALL"); setPage(1); }}>
            <SelectTrigger className="w-[160px]">
              <SelectValue placeholder="Status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="ALL">All Statuses</SelectItem>
              <SelectItem value="SUCCESS">Success</SelectItem>
              <SelectItem value="PENDING">Pending</SelectItem>
              <SelectItem value="FAILED">Failed/Cancelled</SelectItem>
            </SelectContent>
          </Select>

          <Select value={targetType} onValueChange={(val) => { setTargetType(val || "ALL"); setPage(1); }}>
            <SelectTrigger className="w-[160px]">
              <SelectValue placeholder="Transaction Type" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="ALL">All Types</SelectItem>
              <SelectItem value="SUBSCRIPTION">Subscription</SelectItem>
              <SelectItem value="WALLET_TOPUP">Top Up Coins</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Table */}
      <div className="border rounded-xl bg-card overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow className="bg-muted/50 hover:bg-muted/50">
              <TableHead>Order ID</TableHead>
              <TableHead>Date</TableHead>
              <TableHead>Description</TableHead>
              <TableHead className="text-right">Amount</TableHead>
              <TableHead className="text-center">Status</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell colSpan={5} className="h-48 text-center">
                  <Loader2 className="h-6 w-6 animate-spin mx-auto text-muted-foreground" />
                </TableCell>
              </TableRow>
            ) : isError ? (
              <TableRow>
                <TableCell colSpan={5} className="h-48 text-center text-rose-500">
                  An error occurred while loading transaction history.
                </TableCell>
              </TableRow>
            ) : !response?.data?.items?.length ? (
              <TableRow>
                <TableCell colSpan={5} className="h-48 text-center">
                  <div className="flex flex-col items-center justify-center text-muted-foreground">
                    <SearchX className="h-8 w-8 mb-2 opacity-50" />
                    <p>No matching transactions found.</p>
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              response.data.items.map((payment) => (
                <TableRow key={payment.id}>
                  <TableCell className="font-medium text-muted-foreground">
                    #{payment.orderCode || payment.id.substring(0, 8)}
                  </TableCell>
                  <TableCell>
                    {format(new Date(payment.createdAt), "dd/MM/yyyy HH:mm")}
                  </TableCell>
                  <TableCell>
                    {getTargetTypeLabel(payment.targetType, payment.subscriptionName)}
                  </TableCell>
                  <TableCell className="text-right font-medium">
                    {formatCurrency(payment.amount, payment.currency)}
                  </TableCell>
                  <TableCell className="text-center">
                    {getStatusBadge(payment.status)}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {/* Pagination */}
      {response?.data && response.data.totalPages > 1 && (
        <div className="flex items-center justify-end gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            className="h-8 px-2 lg:px-3"
          >
            <ArrowLeft className="h-4 w-4 lg:mr-2" />
            <span className="hidden lg:inline">Previous</span>
          </Button>
          <div className="text-sm font-medium px-2 text-muted-foreground">
            Page {page} of {response.data.totalPages}
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage((p) => p + 1)}
            disabled={page >= response.data.totalPages}
            className="h-8 px-2 lg:px-3"
          >
            <span className="hidden lg:inline">Next</span>
            <ArrowRight className="h-4 w-4 lg:ml-2" />
          </Button>
        </div>
      )}
    </div>
  );
}
