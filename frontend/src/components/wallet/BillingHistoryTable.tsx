"use client"

import React, { useState, useMemo } from "react"
import { useRouter } from "next/navigation"
import { useMyPayments } from "@/hooks/useWallet"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { format } from "date-fns"
import {
  Search,
  X,
  RotateCcw,
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  ChevronLeft,
  ChevronRight,
  SearchX,
  Plus,
  Coins,
  Sparkles,
  CreditCard,
  Building2,
} from "lucide-react"
import { useTranslations } from "next-intl"

export function BillingHistoryTable() {
  const t = useTranslations("RecruiterBillingHistoryTable")
  const router = useRouter()
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [status, setStatus] = useState<string>("ALL")
  const [targetType, setTargetType] = useState<string>("ALL")
  const [search, setSearch] = useState("")

  const [sortField, setSortField] = useState<"date" | "amount">("date")
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("desc")

  // Data fetching hook complying with kinh-mantra.md (page -> hook -> service -> api-client -> backend)
  const { data: response, isLoading, isError } = useMyPayments({
    page,
    pageSize,
    ...(status !== "ALL" && { status }),
    ...(targetType !== "ALL" && { targetType }),
  })

  const paymentsData = response?.data?.items || []
  const totalCount = response?.data?.totalCount || 0
  const totalPages = response?.data?.totalPages || 1

  // Handle local search & sorting
  const filteredAndSortedPayments = useMemo(() => {
    let result = [...paymentsData]

    // Client-side search filtering by Order Code, Target Type, Gateway, or Subscription Name
    if (search.trim()) {
      const q = search.toLowerCase().trim()
      result = result.filter((p) => {
        const orderCodeStr = p.orderCode ? String(p.orderCode) : ""
        const idStr = p.id.toLowerCase()
        const subName = p.subscriptionName ? p.subscriptionName.toLowerCase() : ""
        const gateway = p.paymentGateway ? p.paymentGateway.toLowerCase() : ""
        const type = p.targetType ? p.targetType.toLowerCase() : ""
        return (
          orderCodeStr.includes(q) ||
          idStr.includes(q) ||
          subName.includes(q) ||
          gateway.includes(q) ||
          type.includes(q)
        )
      })
    }

    // Client-side sorting toggle
    result.sort((a, b) => {
      let comparison = 0
      if (sortField === "date") {
        const timeA = new Date(a.createdAt).getTime()
        const timeB = new Date(b.createdAt).getTime()
        comparison = timeA - timeB
      } else if (sortField === "amount") {
        comparison = (a.amount || 0) - (b.amount || 0)
      }
      return sortDirection === "asc" ? comparison : -comparison
    })

    return result
  }, [paymentsData, search, sortField, sortDirection])

  const handleSort = (field: "date" | "amount") => {
    if (sortField === field) {
      setSortDirection((prev) => (prev === "asc" ? "desc" : "asc"))
    } else {
      setSortField(field)
      setSortDirection("desc")
    }
  }

  const renderSortIcon = (field: "date" | "amount") => {
    if (sortField !== field) {
      return <ArrowUpDown className="ml-1.5 h-3.5 w-3.5 text-[#65676B]/60 dark:text-zinc-500" />
    }
    return sortDirection === "asc" ? (
      <ArrowUp className="ml-1.5 h-3.5 w-3.5 text-[#1877F2] dark:text-blue-400 font-bold" />
    ) : (
      <ArrowDown className="ml-1.5 h-3.5 w-3.5 text-[#1877F2] dark:text-blue-400 font-bold" />
    )
  }

  const handleResetFilters = () => {
    setSearch("")
    setStatus("ALL")
    setTargetType("ALL")
    setPage(1)
  }

  const isFilterActive = search !== "" || status !== "ALL" || targetType !== "ALL"

  // Status badge with subtle semantic pastel tones according to TABLE_STANDARD.md
  const getStatusBadge = (statusStr: string) => {
    const s = statusStr?.toUpperCase()
    switch (s) {
      case "SUCCESS":
      case "PAID":
        return (
          <Badge className="bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none">
            <span className="h-1.5 w-1.5 rounded-full bg-emerald-500 mr-1.5 shrink-0" />
            {t("badgeSuccess")}
          </Badge>
        )
      case "PENDING":
        return (
          <Badge className="bg-amber-50 dark:bg-amber-950/40 text-amber-700 dark:text-amber-300 border border-amber-200 dark:border-amber-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none">
            <span className="h-1.5 w-1.5 rounded-full bg-amber-500 mr-1.5 shrink-0" />
            {t("badgePending")}
          </Badge>
        )
      case "FAILED":
      case "CANCELLED":
      case "EXPIRED":
        return (
          <Badge className="bg-rose-50 dark:bg-rose-950/40 text-rose-700 dark:text-rose-300 border border-rose-200 dark:border-rose-800/60 rounded-full px-2.5 py-0.5 text-xs font-semibold shadow-none">
            <span className="h-1.5 w-1.5 rounded-full bg-rose-500 mr-1.5 shrink-0" />
            {s === "CANCELLED" ? t("badgeCancelled") : t("badgeFailed")}
          </Badge>
        )
      default:
        return (
          <Badge variant="outline" className="rounded-full px-2.5 py-0.5 text-xs font-medium">
            {statusStr}
          </Badge>
        )
    }
  }

  const getTargetTypeDisplay = (type: string, subName: string | null) => {
    if (type === "SUBSCRIPTION") {
      return (
        <div className="flex items-center gap-2">
          <div className="h-7 w-7 rounded-lg bg-blue-50 dark:bg-blue-950/50 flex items-center justify-center text-[#1877F2] shrink-0">
            <Sparkles className="h-3.5 w-3.5" />
          </div>
          <div className="flex flex-col">
            <span className="font-semibold text-sm text-[#050505] dark:text-zinc-100">
              {subName ? t("planName", { name: subName }) : t("planSub")}
            </span>
            <span className="text-xs text-[#65676B] dark:text-zinc-400">{t("planDesc")}</span>
          </div>
        </div>
      )
    }
    if (type === "WALLET_TOPUP" || type === "COIN_PACKAGE") {
      return (
        <div className="flex items-center gap-2">
          <div className="h-7 w-7 rounded-lg bg-amber-50 dark:bg-amber-950/50 flex items-center justify-center text-amber-600 dark:text-amber-400 shrink-0">
            <Coins className="h-3.5 w-3.5" />
          </div>
          <div className="flex flex-col">
            <span className="font-semibold text-sm text-[#050505] dark:text-zinc-100">
              {t("typeCoin")}
            </span>
            <span className="text-xs text-[#65676B] dark:text-zinc-400">{t("topupDesc")}</span>
          </div>
        </div>
      )
    }
    return (
      <div className="flex items-center gap-2">
        <div className="h-7 w-7 rounded-lg bg-slate-100 dark:bg-zinc-800 flex items-center justify-center text-zinc-600 dark:text-zinc-300 shrink-0">
          <CreditCard className="h-3.5 w-3.5" />
        </div>
        <span className="font-medium text-sm text-[#050505] dark:text-zinc-100">{type}</span>
      </div>
    )
  }

  const formatCurrency = (amount: number, currency: string) => {
    return new Intl.NumberFormat("vi-VN", {
      style: "currency",
      currency: currency || "VND",
    }).format(amount)
  }

  // Pagination calculation
  const startResult = totalCount > 0 ? (page - 1) * pageSize + 1 : 0
  const endResult = Math.min(page * pageSize, totalCount)

  return (
    <div className="space-y-4 w-full">
      {/* TẦNG 1: TOOLBAR (Search, Filters, Primary Actions) */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2.5 flex-1">
          {/* Search Bar */}
          <div className="relative w-full sm:w-72 md:w-80">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[#65676B] dark:text-zinc-400" />
            <Input
              value={search}
              onChange={(e) => {
                setSearch(e.target.value)
                setPage(1)
              }}
              placeholder={t("searchPlaceholder")}
              className="pl-9 pr-8 !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus-visible:ring-2 focus-visible:ring-[#1877F2] transition-all duration-150"
            />
            {search && (
              <button
                onClick={() => {
                  setSearch("")
                  setPage(1)
                }}
                className="absolute right-2.5 top-1/2 -translate-y-1/2 text-[#65676B] hover:text-[#050505] dark:hover:text-white transition-colors p-1 cursor-pointer"
                title={t("clearSearch")}
              >
                <X className="h-4 w-4" />
              </button>
            )}
          </div>

          {/* Status Filter */}
          <Select
            value={status}
            onValueChange={(val) => {
              if (val) setStatus(val)
              setPage(1)
            }}
          >
            <SelectTrigger className="w-full sm:w-[160px] !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus:ring-[#1877F2]">
              <SelectValue placeholder={t("statusPlaceholder")} />
            </SelectTrigger>
            <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
              <SelectItem value="ALL">{t("statusAll")}</SelectItem>
              <SelectItem value="SUCCESS">{t("statusSuccess")}</SelectItem>
              <SelectItem value="PENDING">{t("statusPending")}</SelectItem>
              <SelectItem value="FAILED">{t("statusFailed")}</SelectItem>
            </SelectContent>
          </Select>

          {/* Transaction Type Filter */}
          <Select
            value={targetType}
            onValueChange={(val) => {
              if (val) setTargetType(val)
              setPage(1)
            }}
          >
            <SelectTrigger className="w-full sm:w-[170px] !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus:ring-[#1877F2]">
              <SelectValue placeholder={t("typePlaceholder")} />
            </SelectTrigger>
            <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
              <SelectItem value="ALL">{t("typeAll")}</SelectItem>
              <SelectItem value="SUBSCRIPTION">{t("typeSubscription")}</SelectItem>
              <SelectItem value="WALLET_TOPUP">{t("typeCoin")}</SelectItem>
            </SelectContent>
          </Select>

          {/* Reset Filters Button */}
          {isFilterActive && (
            <Button
              onClick={handleResetFilters}
              variant="ghost"
              className="h-10 px-3 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 font-medium transition-colors cursor-pointer"
            >
              <RotateCcw className="h-3.5 w-3.5 mr-1.5" /> {t("clearFilters")}
            </Button>
          )}
        </div>
      </div>

      {/* TẦNG 2: MAIN TABLE CONTAINER (TABLE_STANDARD - SHADCN TABLE) */}
      <div className="rounded-lg border border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 overflow-hidden shadow-2xs w-full">
        <Table className="w-full text-left border-collapse table-fixed">
          {/* Table Header */}
          <TableHeader className="bg-slate-50 dark:bg-zinc-950 border-b border-[#CED0D4] dark:border-zinc-800">
            <TableRow className="hover:bg-transparent border-none">
              <TableHead className="w-[16%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                {t("colOrderCode")}
              </TableHead>

              <TableHead className="w-[20%] py-3 px-3">
                <button
                  onClick={() => handleSort("date")}
                  className={`flex items-center text-xs font-semibold uppercase tracking-wider ${
                    sortField === "date"
                      ? "text-[#1877F2] dark:text-blue-400"
                      : "text-[#65676B] dark:text-zinc-400"
                  } hover:text-[#050505] dark:hover:text-white transition-colors group cursor-pointer`}
                >
                  {t("colDate")}
                  {renderSortIcon("date")}
                </button>
              </TableHead>

              <TableHead className="w-[26%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                {t("colDesc")}
              </TableHead>

              <TableHead className="w-[13%] py-3 px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                {t("colGateway")}
              </TableHead>

              <TableHead className="w-[15%] py-3 px-3 text-right">
                <button
                  onClick={() => handleSort("amount")}
                  className={`inline-flex items-center justify-end w-full text-xs font-semibold uppercase tracking-wider ${
                    sortField === "amount"
                      ? "text-[#1877F2] dark:text-blue-400"
                      : "text-[#65676B] dark:text-zinc-400"
                  } hover:text-[#050505] dark:hover:text-white transition-colors group cursor-pointer`}
                >
                  {t("colAmount")}
                  {renderSortIcon("amount")}
                </button>
              </TableHead>

              <TableHead className="w-[10%] text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400 px-3 py-3">
                {t("colStatus")}
              </TableHead>
            </TableRow>
          </TableHeader>

          {/* Table Body */}
          <TableBody>
            {isLoading ? (
              // Loading Skeleton State (6 rows)
              Array.from({ length: pageSize || 6 }).map((_, index) => (
                <TableRow key={index} className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60">
                  <TableCell className="py-4 px-3">
                    <Skeleton className="h-5 w-20 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                  </TableCell>
                  <TableCell className="py-4 px-3">
                    <Skeleton className="h-5 w-28 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                  </TableCell>
                  <TableCell className="py-4 px-3">
                    <div className="flex items-center gap-2">
                      <Skeleton className="h-7 w-7 rounded-lg bg-slate-100 dark:bg-zinc-800 shrink-0" />
                      <div className="space-y-1 w-full">
                        <Skeleton className="h-4 w-3/4 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                        <Skeleton className="h-3 w-1/2 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                      </div>
                    </div>
                  </TableCell>
                  <TableCell className="py-4 px-3">
                    <Skeleton className="h-5 w-16 bg-slate-100 dark:bg-zinc-800 rounded-md" />
                  </TableCell>
                  <TableCell className="py-4 px-3 text-right">
                    <Skeleton className="h-5 w-20 bg-slate-100 dark:bg-zinc-800 rounded-md ml-auto" />
                  </TableCell>
                  <TableCell className="py-4 px-3 text-center">
                    <Skeleton className="h-6 w-16 bg-slate-100 dark:bg-zinc-800 rounded-full mx-auto" />
                  </TableCell>
                </TableRow>
              ))
            ) : isError ? (
              // Error State
              <TableRow>
                <TableCell colSpan={6} className="h-64 text-center">
                  <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                    <p className="font-semibold text-rose-600 dark:text-rose-400 text-base">{t("errLoadFailed")}</p>
                    <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                      {t("errLoadDesc")}
                    </p>
                  </div>
                </TableCell>
              </TableRow>
            ) : filteredAndSortedPayments.length === 0 ? (
              // Empty State
              <TableRow>
                <TableCell colSpan={6} className="h-72 text-center">
                  <div className="flex flex-col items-center justify-center max-w-sm mx-auto text-center">
                    <div className="h-12 w-12 rounded-full bg-[#E7F3FF] dark:bg-blue-950/50 flex items-center justify-center text-[#1877F2] dark:text-blue-400 mb-3">
                      <SearchX className="h-6 w-6" />
                    </div>
                    <p className="font-semibold text-[#050505] dark:text-zinc-100 text-base">
                      {t("noRecords")}
                    </p>
                    <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4">
                      {isFilterActive
                        ? t("noRecordsFilter")
                        : t("noRecordsEmpty")}
                    </p>
                    {isFilterActive && (
                      <Button
                        onClick={handleResetFilters}
                        variant="outline"
                        className="border-[#1877F2] text-[#1877F2] dark:border-blue-500 dark:text-blue-400 hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 cursor-pointer"
                      >
                        <RotateCcw className="h-4 w-4 mr-2" /> {t("clearAllFilters")}
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
            ) : (
              // Actual Data Rows
              filteredAndSortedPayments.map((payment) => (
                <TableRow
                  key={payment.id}
                  className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60 hover:bg-[#E7F3FF]/40 dark:hover:bg-blue-950/20 transition-colors duration-150 group"
                >
                  {/* Order Code */}
                  <TableCell className="py-3.5 px-3 align-middle">
                    <span className="font-mono text-xs font-bold text-[#050505] dark:text-zinc-200 bg-zinc-100 dark:bg-zinc-800/80 px-2 py-1 rounded border border-zinc-200 dark:border-zinc-700 inline-block">
                      #{payment.orderCode || payment.id.substring(0, 8)}
                    </span>
                  </TableCell>

                  {/* Date & Time */}
                  <TableCell className="py-3.5 px-3 align-middle text-sm text-[#65676B] dark:text-zinc-300 font-medium">
                    {payment.createdAt ? format(new Date(payment.createdAt), "dd/MM/yyyy HH:mm") : t("na")}
                  </TableCell>

                  {/* Description / Target Type */}
                  <TableCell className="py-3.5 px-3 align-middle">
                    {getTargetTypeDisplay(payment.targetType, payment.subscriptionName)}
                  </TableCell>

                  {/* Payment Gateway */}
                  <TableCell className="py-3.5 px-3 align-middle">
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold bg-zinc-100 dark:bg-zinc-800 text-zinc-700 dark:text-zinc-300 border border-zinc-200 dark:border-zinc-700">
                      {payment.paymentGateway || "PayOS"}
                    </span>
                  </TableCell>

                  {/* Amount */}
                  <TableCell className="py-3.5 px-3 align-middle text-right font-bold text-sm text-[#050505] dark:text-zinc-100">
                    {formatCurrency(payment.amount, payment.currency)}
                  </TableCell>

                  {/* Status */}
                  <TableCell className="py-3.5 px-3 align-middle text-center">
                    {getStatusBadge(payment.status)}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {/* TẦNG 3: PAGINATION FOOTER */}
      <div className="flex flex-col sm:flex-row items-center justify-between gap-3 pt-1 px-1">
        <div className="flex items-center space-x-3 text-sm text-[#65676B] dark:text-zinc-400">
          <div>
            {t.rich("showingText", { 
              start: startResult, 
              end: endResult, 
              total: totalCount,
              span: (chunks) => <span className="font-semibold text-[#050505] dark:text-zinc-200">{chunks}</span>
            })}
          </div>
          <Select
            value={String(pageSize)}
            onValueChange={(val) => {
              if (val) setPageSize(Number(val))
              setPage(1)
            }}
          >
            <SelectTrigger className="h-8 w-[110px] border-[#CED0D4] dark:border-zinc-800 text-xs font-medium focus:ring-[#1877F2]">
              <SelectValue placeholder={t("pageSizePlaceholder")} />
            </SelectTrigger>
            <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
              <SelectItem value="10">{t("perPage", { size: 10 })}</SelectItem>
              <SelectItem value="20">{t("perPage", { size: 20 })}</SelectItem>
              <SelectItem value="50">{t("perPage", { size: 50 })}</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Page Buttons */}
        <div className="flex items-center space-x-1.5">
          <Button
            variant="outline"
            size="icon"
            disabled={page === 1 || isLoading}
            onClick={() => setPage((prev) => Math.max(1, prev - 1))}
            className="h-8 w-8 border-[#CED0D4] dark:border-zinc-800 text-[#65676B] dark:text-zinc-400 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 disabled:opacity-40 cursor-pointer"
          >
            <ChevronLeft className="h-4 w-4" />
          </Button>

          {Array.from({ length: totalPages }).map((_, index) => {
            const pageNum = index + 1
            if (
              totalPages <= 5 ||
              pageNum === 1 ||
              pageNum === totalPages ||
              Math.abs(pageNum - page) <= 1
            ) {
              const isCurrent = pageNum === page
              return (
                <Button
                  key={pageNum}
                  variant={isCurrent ? "default" : "outline"}
                  disabled={isLoading}
                  onClick={() => setPage(pageNum)}
                  className={`h-8 w-8 text-xs font-semibold rounded-md shadow-2xs transition-all cursor-pointer ${
                    isCurrent
                      ? "bg-[#1877F2] hover:bg-[#166FE5] text-white border-[#1877F2]"
                      : "border-[#CED0D4] dark:border-zinc-800 text-[#050505] dark:text-zinc-300 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 dark:hover:text-blue-400"
                  }`}
                >
                  {pageNum}
                </Button>
              )
            }
            if (
              (pageNum === 2 && page > 3) ||
              (pageNum === totalPages - 1 && page < totalPages - 2)
            ) {
              return (
                <span key={pageNum} className="px-1 text-xs text-[#65676B]">
                  ...
                </span>
              )
            }
            return null
          })}

          <Button
            variant="outline"
            size="icon"
            disabled={page >= totalPages || isLoading}
            onClick={() => setPage((prev) => Math.min(totalPages, prev + 1))}
            className="h-8 w-8 border-[#CED0D4] dark:border-zinc-800 text-[#65676B] dark:text-zinc-400 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 disabled:opacity-40 cursor-pointer"
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  )
}
