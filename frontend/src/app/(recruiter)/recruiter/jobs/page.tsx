"use client"

import { useState, useEffect, useMemo } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { useJobs } from "@/hooks/useJobs"
import { useSignalR } from "@/hooks/useSignalR"
import { useWalletBalance } from "@/hooks/useWallet"
import { AiParseStatusBadge } from "@/components/shared/AiParseStatusBadge"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import {
  Search,
  Plus,
  Users,
  UserCheck,
  Pencil,
  MoreHorizontal,
  XCircle,
  ChevronLeft,
  ChevronRight,
  Loader2,
  Briefcase,
  Ban,
  CalendarPlus,
  Sparkles,
  Coins,
  AlertTriangle,
  Rocket,
  Flame,
  X,
  RotateCcw,
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  ArrowUpRight,
  ExternalLink,
  SearchX,
  Lightbulb,
  Copy,
  Check
} from "lucide-react"
import { useTranslations } from "next-intl"

export default function JobPostingsPage() {
  const router = useRouter()
  const t = useTranslations("RecruiterJobs")

  // 1. TanStack Query & Service state management (Strictly following kinh-mantra: page -> hook -> service)
  const {
    jobs: fetchedJobs,
    totalCount,
    page,
    setPage,
    pageSize,
    setPageSize,
    search,
    setSearch,
    status,
    setStatus,
    loading,
    closeJob,
    extendJob,
    pushTopJob,
    refresh
  } = useJobs(1, 10, "ALL")

  // 2. Local UI state for sorting & modals
  const [sortField, setSortField] = useState<"title" | "applicationCount" | "date">("date")
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("desc")
  const [copiedCode, setCopiedCode] = useState<string | null>(null)

  const handleCopyCode = (code: string, e: React.MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
    if (!code) return
    navigator.clipboard.writeText(code)
    setCopiedCode(code)
    setTimeout(() => setCopiedCode(null), 2000)
  }

  const { data: walletRes, refetch: refetchWallet } = useWalletBalance()
  const walletData = walletRes?.data
  const jobSlotsLimit = walletData?.jobSlotsLimit ?? 1
  const jobSlotsUsed = walletData?.jobSlotsUsed ?? 0
  const isSlotFull = jobSlotsLimit !== -1 && jobSlotsUsed >= jobSlotsLimit

  const jobExtendLimit = walletData?.jobExtendLimit ?? 0
  const jobExtendUsed = walletData?.jobExtendUsed ?? 0
  const isExtendQuotaFull = jobExtendLimit !== -1 && (jobExtendLimit === 0 || jobExtendUsed >= jobExtendLimit)
  const coinBalance = walletData?.balance ?? 0
  const extendCoinCost = 10000
  const canPayWithCoins = coinBalance >= extendCoinCost

  const [extendingJob, setExtendingJob] = useState<any | null>(null)
  const [extendSubmitting, setExtendSubmitting] = useState(false)

  const handleConfirmExtend = async () => {
    if (!extendingJob) return
    setExtendSubmitting(true)
    const res = await extendJob(extendingJob.id)
    setExtendSubmitting(false)
    if (res.success) {
      alert(res.message || t("extendSuccess"))
      setExtendingJob(null)
      refresh()
      refetchWallet()
    } else {
      alert(res.message || t("extendFail"))
    }
  }

  const jobPushTopLimit = walletData?.pushTopLimit ?? 0
  const jobPushTopUsed = walletData?.pushTopUsed ?? 0
  const isPushTopQuotaFull = jobPushTopLimit !== -1 && (jobPushTopLimit === 0 || jobPushTopUsed >= jobPushTopLimit)
  const pushTopCoinCost = 5000
  const canPayPushTopWithCoins = coinBalance >= pushTopCoinCost

  const [pushingTopJob, setPushingTopJob] = useState<any | null>(null)
  const [pushTopSubmitting, setPushTopSubmitting] = useState(false)

  const handleConfirmPushTop = async () => {
    if (!pushingTopJob) return
    setPushTopSubmitting(true)
    const res = await pushTopJob(pushingTopJob.id)
    setPushTopSubmitting(false)
    if (res.success) {
      alert(res.message || t("pushTopSuccess"))
      setPushingTopJob(null)
      refresh()
      refetchWallet()
    } else {
      alert(res.message || t("pushTopFail"))
    }
  }

  const connection = useSignalR("/hubs/notification")
  useEffect(() => {
    if (connection) {
      connection.on("JobStatusChanged", () => {
        refresh()
      })
    }
    return () => {
      if (connection) {
        connection.off("JobStatusChanged")
      }
    }
  }, [connection, refresh])

  // Handle actions
  const handleCloseJob = async (id: string) => {
    if (!confirm(t("closeConfirm"))) return
    const res = await closeJob(id)
    if (!res.success) {
      alert(res.message || t("closeFail"))
    }
  }

  const openCreateModal = () => router.push("/recruiter/jobs/new")
  const openEditModal = (jobId: string) => router.push(`/recruiter/jobs/${jobId}/edit`)

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const startResult = (page - 1) * pageSize + 1
  const endResult = Math.min(page * pageSize, totalCount)
  const isFilterActive = search !== "" || status !== "ALL"

  const handleResetFilters = () => {
    setSearch("")
    setStatus("ALL")
    setPage(1)
  }

  const handleSort = (field: "title" | "applicationCount" | "date") => {
    if (sortField === field) {
      setSortDirection(prev => (prev === "asc" ? "desc" : "asc"))
    } else {
      setSortField(field)
      setSortDirection("desc")
    }
  }

  // Sort displayed jobs cleanly in memory
  const sortedJobs = useMemo(() => {
    return [...fetchedJobs].sort((a, b) => {
      let comparison = 0
      if (sortField === "title") {
        comparison = (a.title || "").localeCompare(b.title || "", "en")
      } else if (sortField === "applicationCount") {
        comparison = (a.applicationCount || 0) - (b.applicationCount || 0)
      } else if (sortField === "date") {
        const timeA = new Date(a.publishedAt || a.createdAt || 0).getTime()
        const timeB = new Date(b.publishedAt || b.createdAt || 0).getTime()
        comparison = timeA - timeB
      }
      return sortDirection === "asc" ? comparison : -comparison
    })
  }, [fetchedJobs, sortField, sortDirection])

  const renderSortIcon = (field: "title" | "applicationCount" | "date") => {
    if (sortField !== field) return <ArrowUpDown className="ml-1.5 h-3.5 w-3.5 text-[#65676B]/60" />
    return sortDirection === "asc"
      ? <ArrowUp className="ml-1.5 h-3.5 w-3.5 text-[#1877F2] dark:text-blue-400 font-bold" />
      : <ArrowDown className="ml-1.5 h-3.5 w-3.5 text-[#1877F2] dark:text-blue-400 font-bold" />
  }

  const formatDate = (dateStr: string) => {
    if (!dateStr) return "N/A"
    return new Date(dateStr).toLocaleDateString("en-US", {
      day: "2-digit",
      month: "short",
      year: "numeric",
    })
  }

  // Render Status Badge matching Pill style for all statuses
  const renderStatusBadge = (job: any) => {
    if (job.isBanned) {
      return (
        <div className="flex flex-col items-center gap-0.5">
          <div className="inline-flex items-center justify-center gap-1.5 px-2.5 py-0.5 rounded-full bg-rose-50 dark:bg-rose-950/60 text-rose-700 dark:text-rose-300 border border-rose-200 dark:border-rose-800/60 text-xs font-bold shadow-none">
            <span className="h-2 w-2 rounded-full bg-rose-600 shrink-0" />
            <span>{t("banned")}</span>
          </div>
          <span className="text-[10px] text-rose-500 max-w-[110px] truncate" title={job.banReason}>
            {job.banReason}
          </span>
        </div>
      )
    }

    switch (job.status) {
      case "PUBLISHED":
        return (
          <div className="inline-flex items-center justify-center gap-1.5 px-2.5 py-0.5 rounded-full bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/60 text-xs font-semibold shadow-none">
            <span className="h-2 w-2 rounded-full bg-emerald-500 shrink-0" />
            <span>{t("active")}</span>
          </div>
        )
      case "DRAFT":
        return (
          <div className="inline-flex items-center justify-center gap-1.5 px-2.5 py-0.5 rounded-full bg-zinc-100 dark:bg-zinc-800/80 text-zinc-600 dark:text-zinc-300 border border-zinc-200 dark:border-zinc-700 text-xs font-semibold shadow-none">
            <span className="h-2 w-2 rounded-full bg-zinc-400 shrink-0" />
            <span>{t("draft")}</span>
          </div>
        )
      case "CLOSED":
        return (
          <div className="inline-flex items-center justify-center gap-1.5 px-2.5 py-0.5 rounded-full bg-rose-50 dark:bg-rose-950/40 text-rose-700 dark:text-rose-300 border border-rose-200 dark:border-rose-800/60 text-xs font-semibold shadow-none">
            <span className="h-2 w-2 rounded-full bg-rose-500 shrink-0" />
            <span>{t("closed")}</span>
          </div>
        )
      default:
        return (
          <div className="inline-flex items-center justify-center gap-1.5 px-2.5 py-0.5 rounded-full bg-zinc-100 dark:bg-zinc-800 text-zinc-600 dark:text-zinc-400 border border-zinc-200 dark:border-zinc-700 text-xs font-medium shadow-none">
            <span className="h-2 w-2 rounded-full bg-zinc-400 shrink-0" />
            <span>{job.status}</span>
          </div>
        )
    }
  }

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-10 space-y-5">

        {/* Top Header Card & Quota Lightbulb Popover */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <div className="flex flex-wrap items-center gap-3">
              <h1 className="text-3xl font-extrabold text-[#050505] dark:text-zinc-50 tracking-tight">{t("pageTitle")}</h1>
              {walletData && (
                <Popover>
                  <PopoverTrigger
                    title="Click to view job posting limits & quota info"
                    className={`group relative flex items-center justify-center h-8 w-8 rounded-full border shadow-2xs hover:shadow-md active:scale-95 transition-all cursor-pointer ${isSlotFull
                        ? "bg-amber-50 dark:bg-amber-950/40 border-amber-300 dark:border-amber-700/60 text-amber-800 dark:text-amber-300 hover:border-amber-400"
                        : "bg-amber-50/80 dark:bg-amber-950/30 border-amber-200 dark:border-amber-800 text-amber-700 dark:text-amber-300 hover:border-amber-300"
                      }`}
                  >
                    <div className="relative flex items-center justify-center">
                      <Lightbulb className={`h-4.5 w-4.5 transition-transform group-hover:scale-110 ${isSlotFull ? "text-amber-500 fill-amber-400/60 dark:text-amber-400" : "text-amber-500 fill-amber-300/50 dark:text-amber-400"}`} />
                      <Sparkles className={`h-2.5 w-2.5 absolute -top-1 -right-1 ${isSlotFull ? "text-orange-500" : "text-amber-400"}`} />
                    </div>
                  </PopoverTrigger>
                  <PopoverContent align="start" className="w-[380px] sm:w-[420px] p-4 rounded-2xl shadow-xl border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 z-50">
                    <div className="space-y-3.5">
                      <div className="flex items-center justify-between border-b pb-2.5 border-zinc-100 dark:border-zinc-800">
                        <div className="flex items-center gap-2 text-sm font-extrabold text-[#050505] dark:text-zinc-100">
                          <Briefcase className={`h-4 w-4 ${isSlotFull ? "text-amber-600 dark:text-amber-400" : "text-[#1877F2] dark:text-blue-400"}`} />
                          <span>{t("quotaTitle")}</span>
                        </div>
                        <span className={`text-xs font-bold px-2.5 py-0.5 rounded-full border ${isSlotFull
                            ? "bg-amber-100 dark:bg-amber-950/60 text-amber-800 dark:text-amber-300 border-amber-300"
                            : "bg-[#E7F3FF] dark:bg-blue-950/50 text-[#1877F2] dark:text-blue-300 border-blue-200 dark:border-blue-800"
                          }`}>
                          {isSlotFull ? t("quotaFullAlert") : t("quotaAvailAlert")}
                        </span>
                      </div>

                      <div className="space-y-2 text-xs text-[#65676B] dark:text-zinc-300">
                        {/* Trạng thái hạn mức */}
                        <div className="flex items-center justify-between p-2.5 rounded-xl bg-zinc-50 dark:bg-zinc-800/50 border border-zinc-100 dark:border-zinc-800">
                          <span className="font-medium text-zinc-600 dark:text-zinc-400">{t("quotaStatus")}</span>
                          <span className={`font-bold text-xs px-2 py-0.5 rounded-full border ${isSlotFull
                              ? "bg-amber-100 dark:bg-amber-950/60 text-amber-800 dark:text-amber-300 border-amber-300"
                              : "bg-[#E7F3FF] text-[#1877F2] border-[#1877F2]/20"
                            }`}>
                            {isSlotFull ? t("quotaFullPay") : t("quotaFreeAvail")}
                          </span>
                        </div>

                        {/* Hạn mức gói */}
                        <div className="flex items-center justify-between p-2.5 rounded-xl bg-zinc-50 dark:bg-zinc-800/50 border border-zinc-100 dark:border-zinc-800">
                          <span className="font-medium text-zinc-600 dark:text-zinc-400">{t("planLimit")}</span>
                          <span className="font-extrabold text-sm text-[#050505] dark:text-zinc-100">
                            {jobSlotsLimit === -1 ? t("unlimited") : (jobSlotsLimit === 1 ? t("freeSlot", { count: 1 }) : t("freeSlots", { count: jobSlotsLimit }))}
                          </span>
                        </div>

                        {/* Số tin đang hiển thị (Active) */}
                        <div className="flex items-center justify-between p-2.5 rounded-xl bg-zinc-50 dark:bg-zinc-800/50 border border-zinc-100 dark:border-zinc-800">
                          <span className="font-medium text-zinc-600 dark:text-zinc-400">{t("activeJobs")}</span>
                          <span className={`font-extrabold text-sm ${isSlotFull ? "text-amber-600 dark:text-amber-400 font-black" : "text-[#1877F2] dark:text-blue-400"}`}>
                            {jobSlotsUsed === 1 ? t("jobCount", { count: 1 }) : t("jobsCount", { count: jobSlotsUsed })}
                          </span>
                        </div>

                        {isSlotFull ? (
                          <div className="bg-amber-50/70 dark:bg-amber-950/20 p-3 rounded-xl border border-amber-200/80 dark:border-amber-900/50 space-y-1">
                            <p className="text-amber-900 dark:text-amber-300 font-medium leading-relaxed" dangerouslySetInnerHTML={{ __html: t.raw("quotaFullMsg").replace('{limit}', jobSlotsLimit) }} />
                          </div>
                        ) : (
                          <div className="bg-blue-50/60 dark:bg-blue-950/20 p-3 rounded-xl border border-blue-100 dark:border-blue-900/40">
                            <p className="text-blue-950 dark:text-blue-200 font-medium leading-relaxed" dangerouslySetInnerHTML={{ __html: t.raw("quotaAvailMsg").replace('{remaining}', jobSlotsLimit === -1 ? t('unlimited') : Math.max(0, (jobSlotsLimit || 0) - (jobSlotsUsed || 0))) }} />
                          </div>
                        )}
                      </div>

                      {isSlotFull && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => router.push("/recruiter/billing")}
                          className="w-full border-amber-300 dark:border-amber-700 hover:bg-amber-100/70 text-amber-800 dark:text-amber-300 font-bold h-9 shadow-none cursor-pointer transition-colors"
                        >
                          {t("upgradePlan")}
                        </Button>
                      )}
                    </div>
                  </PopoverContent>
                </Popover>
              )}
            </div>
            <p className="text-[#65676B] dark:text-zinc-400 mt-1.5 text-sm">{t("pageDesc")}</p>
          </div>
          {/* Primary Action Button (#1877F2) */}
          <Button
            onClick={openCreateModal}
            className="bg-[#1877F2] hover:bg-[#166FE5] text-white font-medium h-10 px-4 rounded-lg shadow-sm active:scale-[0.98] transition-all gap-2 cursor-pointer"
          >
            <Plus className="h-4 w-4" />
            {t("addNewJob")}
          </Button>
        </div>

        {/* TẦNG 1: TOOLBAR CÔNG CỤ (TABLE_STANDARD) */}
        <div className="flex items-center justify-between gap-3 pt-2">
          <div className="flex flex-wrap items-center gap-2.5 flex-1">
            {/* Search Bar */}
            <div className="relative w-full sm:w-72 md:w-80">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-[#65676B]" />
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
                  className="absolute right-2.5 top-1/2 -translate-y-1/2 text-[#65676B] hover:text-[#050505] dark:hover:text-white transition-colors p-1"
                  title="Clear search"
                >
                  <X className="h-4 w-4" />
                </button>
              )}
            </div>

            {/* Status Filter Dropdown */}
            <Select
              value={status}
              onValueChange={(val) => {
                if (val) setStatus(val)
                setPage(1)
              }}
            >
              <SelectTrigger className="w-full sm:w-[170px] !h-10 border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 focus:ring-[#1877F2]">
                <SelectValue placeholder={t("allStatuses")} />
              </SelectTrigger>
              <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
                <SelectItem value="ALL">{t("allStatuses")}</SelectItem>
                <SelectItem value="PUBLISHED">{t("statusActive")}</SelectItem>
                <SelectItem value="DRAFT">{t("statusDraft")}</SelectItem>
                <SelectItem value="CLOSED">{t("statusClosed")}</SelectItem>
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
        <div className="rounded-lg border border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 overflow-hidden shadow-2xs w-full overflow-x-auto">
          <Table className="w-full min-w-[1080px] text-left border-collapse table-fixed">
            <TableHeader className="bg-slate-50 dark:bg-zinc-950 border-b border-[#CED0D4] dark:border-zinc-800">
              <TableRow className="hover:bg-transparent border-none">
                <TableHead className="w-[16%] min-w-[165px] py-3 px-2.5 sm:px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  {t("colJobCode")}
                </TableHead>

                <TableHead className="w-[26%] min-w-[210px] py-3 px-2.5 sm:px-3">
                  <button
                    onClick={() => handleSort("title")}
                    className={`flex items-center text-xs font-semibold uppercase tracking-wider ${sortField === "title" ? "text-[#1877F2] dark:text-blue-400" : "text-[#65676B] dark:text-zinc-400"
                      } hover:text-[#050505] dark:hover:text-white transition-colors group cursor-pointer`}
                  >
                    {t("colJobTitle")}
                    {renderSortIcon("title")}
                  </button>
                </TableHead>

                <TableHead className="w-[11%] min-w-[110px] py-3 px-2.5 sm:px-3">
                  <button
                    onClick={() => handleSort("date")}
                    className={`flex items-center text-xs font-semibold uppercase tracking-wider ${sortField === "date" ? "text-[#1877F2] dark:text-blue-400" : "text-[#65676B] dark:text-zinc-400"
                      } hover:text-[#050505] dark:hover:text-white transition-colors group cursor-pointer`}
                  >
                    {t("colPostedDate")}
                    {renderSortIcon("date")}
                  </button>
                </TableHead>

                <TableHead className="w-[13%] min-w-[130px] py-3 px-2.5 sm:px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  {t("colAppDeadline")}
                </TableHead>

                <TableHead className="w-[13%] min-w-[130px] py-3 px-2.5 sm:px-3 text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400">
                  {t("colSysExpiry")}
                </TableHead>

                <TableHead className="w-[7%] min-w-[80px] text-center py-3 px-2.5 sm:px-3">
                  <button
                    onClick={() => handleSort("applicationCount")}
                    className={`inline-flex items-center justify-center text-xs font-semibold uppercase tracking-wider ${sortField === "applicationCount" ? "text-[#1877F2] dark:text-blue-400" : "text-[#65676B] dark:text-zinc-400"
                      } hover:text-[#050505] dark:hover:text-white transition-colors group cursor-pointer`}
                  >
                    {t("colApplicants")}
                    {renderSortIcon("applicationCount")}
                  </button>
                </TableHead>

                <TableHead className="w-[8%] min-w-[90px] text-center text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400 px-2.5 sm:px-3 py-3">
                  {t("colStatus")}
                </TableHead>

                <TableHead className="w-[6%] min-w-[70px] text-right text-xs font-semibold uppercase tracking-wider text-[#65676B] dark:text-zinc-400 px-2.5 sm:px-3 py-3">
                  {t("colActions")}
                </TableHead>
              </TableRow>
            </TableHeader>

            <TableBody>
              {loading && sortedJobs.length === 0 ? (
                // Loading Skeleton state (No overlay shift) - 8 columns
                Array.from({ length: pageSize || 6 }).map((_, index) => (
                  <TableRow key={index} className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60">
                    <TableCell className="py-4 px-2.5 sm:px-3"><Skeleton className="h-7 w-16 bg-slate-100 dark:bg-zinc-800 rounded-md" /></TableCell>
                    <TableCell className="py-4 px-2.5 sm:px-3">
                      <Skeleton className="h-5 w-4/5 bg-slate-100 dark:bg-zinc-800 rounded my-1" />
                    </TableCell>
                    <TableCell className="px-2.5 sm:px-3"><Skeleton className="h-4 w-[60%]" /></TableCell>
                    <TableCell className="px-2.5 sm:px-3"><Skeleton className="h-4 w-[70%]" /></TableCell>
                    <TableCell className="px-2.5 sm:px-3"><Skeleton className="h-4 w-[70%]" /></TableCell>
                    <TableCell className="px-2.5 sm:px-3 py-4 text-center"><Skeleton className="h-6 w-12 bg-slate-100 dark:bg-zinc-800 rounded-full mx-auto" /></TableCell>
                    <TableCell className="px-2.5 sm:px-3 py-4 text-center"><Skeleton className="h-6 w-20 bg-slate-100 dark:bg-zinc-800 rounded-full mx-auto" /></TableCell>
                    <TableCell className="px-2.5 sm:px-3 py-4 text-right"><Skeleton className="h-8 w-16 bg-slate-100 dark:bg-zinc-800 rounded ml-auto" /></TableCell>
                  </TableRow>
                ))
              ) : sortedJobs.length === 0 ? (
                // Empty State
                <TableRow>
                  <TableCell colSpan={8} className="h-72 text-center">
                    <div className="flex flex-col items-center justify-center max-w-sm mx-auto py-6">
                      <div className="h-12 w-12 rounded-full bg-[#E7F3FF] dark:bg-blue-950/60 flex items-center justify-center text-[#1877F2] dark:text-blue-400 mb-3">
                        <SearchX className="h-6 w-6" />
                      </div>
                      <p className="font-semibold text-[#050505] dark:text-zinc-100 text-base">{t("noJobsFound")}</p>
                      <p className="text-sm text-[#65676B] dark:text-zinc-400 mt-1 mb-4 text-center px-4">
                        {isFilterActive
                          ? t("noJobsMatch")
                          : t("noJobsYet")}
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
                // Actual Job Rows
                sortedJobs.map((job) => (
                  <TableRow
                    key={job.id}
                    className="border-b border-[#CED0D4]/60 dark:border-zinc-800/60 hover:bg-[#E7F3FF]/40 dark:hover:bg-blue-950/20 transition-colors duration-150 group"
                  >
                    {/* Cột 1: Mã Tin (Hiển thị trọn vẹn + 1-Click Copy Badge) */}
                    <TableCell className="py-3 px-2.5 sm:px-3 align-top font-mono text-xs whitespace-nowrap">
                      <button
                        type="button"
                        onClick={(e) => handleCopyCode(job.jobCode, e)}
                        title={copiedCode === job.jobCode ? "Đã sao chép mã tin!" : "Click để sao chép mã tin"}
                        className="group/code inline-flex items-center gap-1.5 px-2 py-1 rounded bg-slate-100 dark:bg-zinc-800/90 hover:bg-blue-50 dark:hover:bg-blue-950/50 border border-slate-200/90 dark:border-zinc-700/70 hover:border-blue-300 dark:hover:border-blue-700 text-zinc-800 dark:text-zinc-200 font-semibold transition-all cursor-pointer select-all mt-0.5 shadow-2xs"
                      >
                        <span>{job.jobCode}</span>
                        {copiedCode === job.jobCode ? (
                          <Check className="h-3 w-3 text-emerald-600 dark:text-emerald-400 shrink-0" />
                        ) : (
                          <Copy className="h-3 w-3 text-zinc-400 group-hover/code:text-blue-600 dark:group-hover/code:text-blue-400 transition-colors shrink-0" />
                        )}
                      </button>
                    </TableCell>

                    {/* Cột 2: Tên Việc Làm */}
                    <TableCell className="py-3 px-2.5 sm:px-3 align-top font-medium text-[#050505] dark:text-zinc-100">
                      <div className="flex flex-col gap-1.5 mt-0.5">
                        <Link
                          href={`/recruiter/jobs/${job.id}`}
                          className="font-bold text-sm text-[#050505] dark:text-zinc-100 group-hover:text-[#1877F2] dark:group-hover:text-blue-400 transition-colors block w-full line-clamp-1 truncate"
                          title={job.title}
                        >
                          {job.title}
                        </Link>
                        {/* Subtitle row for tags */}
                        <div className="flex items-center gap-2 min-h-[22px] overflow-hidden">
                          <AiParseStatusBadge status={job.parseStatus} error={job.parseError} className="text-[11px] px-2 py-0.5 font-medium shadow-none shrink-0" />
                          {job.pushedTopUntil && new Date(job.pushedTopUntil) >= new Date() && (
                            <span
                              title="Job is being featured in Top 24h on home page"
                              className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[11px] font-medium bg-amber-50 text-amber-700 border border-amber-200 dark:bg-amber-950/40 dark:text-amber-400 dark:border-amber-900/50 shrink-0"
                            >
                              <Flame className="h-3 w-3 text-orange-500 fill-orange-500 shrink-0" />
                              <span>Top 24h</span>
                            </span>
                          )}
                        </div>
                      </div>
                    </TableCell>

                    {/* Cột 3: Ngày Đăng */}
                    <TableCell className="px-2.5 sm:px-3 py-3 align-top text-xs text-[#050505] dark:text-zinc-300 font-medium font-mono whitespace-nowrap">
                      <div className="mt-1">
                        {formatDate(job.publishedAt || job.createdAt)}
                      </div>
                    </TableCell>

                    {/* Cột 4: Hạn nộp hồ sơ */}
                    <TableCell className="px-2.5 sm:px-3 py-3 align-top text-xs text-[#050505] dark:text-zinc-300 font-medium font-mono whitespace-nowrap">
                      <div className="mt-1">
                        {job.applicationDeadline ? (
                          <span className="text-blue-600 dark:text-blue-400 font-semibold">{formatDate(job.applicationDeadline)}</span>
                        ) : (
                          <span className="text-[#65676B] font-sans italic" title="Hạn ứng tuyển vô thời hạn">{t("noExpiry")}</span>
                        )}
                      </div>
                    </TableCell>

                    {/* Cột 4.5: Hạn hiển thị hệ thống */}
                    <TableCell className="px-2.5 sm:px-3 py-3 align-top text-xs text-[#050505] dark:text-zinc-300 font-medium font-mono whitespace-nowrap">
                        {job.expiresAt ? (
                          (() => {
                            const expDate = new Date(job.expiresAt);
                            const today = new Date();
                            const diffTime = expDate.getTime() - today.getTime();
                            const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
                            const isUrgent = diffDays <= 5 && diffDays > 0;
                            const isExpired = diffDays <= 0;

                            return (
                              <div className="mt-1 flex flex-col gap-1.5">
                                <span className={`font-medium flex flex-col items-start gap-1 ${isExpired ? 'text-red-600 dark:text-red-400' : isUrgent ? 'text-orange-600 dark:text-orange-400' : 'text-zinc-600 dark:text-zinc-300'}`}>
                                  <span>{formatDate(job.expiresAt)}</span>
                                  {isExpired ? (
                                    <span className="px-1.5 py-0.5 rounded text-[9px] font-bold bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400 border border-red-200 dark:border-red-800 w-fit">{t("hiddenBadge")}</span>
                                  ) : (
                                    <span className={`px-1.5 py-0.5 rounded text-[9px] font-bold border w-fit ${isUrgent ? 'bg-orange-100 text-orange-700 border-orange-200 dark:bg-orange-900/30 dark:text-orange-400 dark:border-orange-800' : 'bg-zinc-100 text-zinc-600 border-zinc-200 dark:bg-zinc-800 dark:text-zinc-400 dark:border-zinc-700'}`}>
                                      {t("daysLeft", { days: diffDays })}
                                    </span>
                                  )}
                                </span>
                              </div>
                            );
                          })()
                        ) : (
                          <span className="text-zinc-500 mt-1 block">-</span>
                        )}
                    </TableCell>

                    {/* Cột 5: Ứng Viên */}
                    <TableCell className="px-2.5 sm:px-3 py-3 align-top text-center">
                      <div className="mt-0.5">
                        <Link
                          href={`/recruiter/jobs/${job.id}/applicants`}
                          className="inline-flex items-center justify-center gap-1 text-xs font-extrabold text-[#1877F2] dark:text-blue-400 hover:text-[#166FE5] dark:hover:text-blue-300 transition-colors cursor-pointer"
                          title="Click to view candidate list"
                        >
                          <span>{job.applicationCount || 0}</span>
                          <ExternalLink className="h-3.5 w-3.5 text-[#1877F2] dark:text-blue-400 shrink-0" />
                        </Link>
                      </div>
                    </TableCell>

                    {/* Cột 6: Trạng Thái */}
                    <TableCell className="px-2.5 sm:px-3 py-3 align-top text-center">
                      <div className="mt-0.5">
                        {renderStatusBadge(job)}
                      </div>
                    </TableCell>

                    {/* Cột 7: Hành Động */}
                    <TableCell className="px-2.5 sm:px-3 py-3 align-top text-right">
                      <div className="flex items-center justify-end gap-1 mt-0.5">
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => openEditModal(job.id)}
                          title={job.isBanned ? "Banned jobs cannot be edited" : "Edit Job"}
                          disabled={job.isBanned}
                          className="h-8 w-8 text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 dark:hover:text-blue-400 disabled:opacity-30 disabled:hover:bg-transparent transition-colors cursor-pointer"
                        >
                          <Pencil className="h-4 w-4" />
                        </Button>

                        <Popover>
                          <PopoverTrigger className="inline-flex items-center justify-center h-8 w-8 rounded-md text-[#65676B] hover:text-[#1877F2] hover:bg-[#E7F3FF] dark:hover:bg-blue-950/40 dark:hover:text-blue-400 transition-colors cursor-pointer focus-visible:outline-hidden">
                            <MoreHorizontal className="h-4 w-4" />
                          </PopoverTrigger>
                          <PopoverContent align="end" className="w-52 p-1.5 rounded-xl border border-[#CED0D4] dark:border-zinc-800 bg-white dark:bg-zinc-900 shadow-xl flex flex-col gap-0.5">
                            {!job.isBanned && (
                              <button
                                onClick={() => setExtendingJob(job)}
                                className="flex items-center gap-2.5 w-full px-3 py-2 text-xs font-medium text-zinc-700 dark:text-zinc-200 hover:text-emerald-600 hover:bg-emerald-50 dark:hover:bg-emerald-950/40 dark:hover:text-emerald-400 rounded-lg transition-colors cursor-pointer text-left"
                              >
                                <CalendarPlus className="h-4 w-4 text-emerald-600 dark:text-emerald-400 shrink-0" />
                                <span>{t("extend15Days")}</span>
                              </button>
                            )}

                            {job.status === "PUBLISHED" && !job.isBanned && (
                              <button
                                onClick={() => setPushingTopJob(job)}
                                className="flex items-center gap-2.5 w-full px-3 py-2 text-xs font-medium text-zinc-700 dark:text-zinc-200 hover:text-amber-600 hover:bg-amber-50 dark:hover:bg-amber-950/30 dark:hover:text-amber-400 rounded-lg transition-colors cursor-pointer text-left"
                              >
                                <Rocket className="h-4 w-4 text-amber-500 fill-amber-500 shrink-0" />
                                <span>{t("pushTop24h")}</span>
                              </button>
                            )}

                            {job.status !== "CLOSED" && !job.isBanned && (
                              <>
                                <div className="h-[1px] bg-zinc-100 dark:bg-zinc-800 my-0.5" />
                                <button
                                  onClick={() => handleCloseJob(job.id)}
                                  className="flex items-center gap-2.5 w-full px-3 py-2 text-xs font-medium text-rose-600 dark:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-950/40 rounded-lg transition-colors cursor-pointer text-left"
                                >
                                  <XCircle className="h-4 w-4 text-rose-600 dark:text-rose-400 shrink-0" />
                                  <span>{t("closeJob")}</span>
                                </button>
                              </>
                            )}
                          </PopoverContent>
                        </Popover>
                      </div>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </div>

        {/* TẦNG 3: PAGINATION FOOTER (TABLE_STANDARD) */}
        {totalCount > 0 && (
          <div className="flex flex-col sm:flex-row items-center justify-between gap-4 pt-2 px-1 text-sm text-[#65676B] dark:text-zinc-400">
            <div className="flex items-center space-x-3">
              <div dangerouslySetInnerHTML={{ __html: t.raw("showing").replace('{start}', `<span class="font-semibold text-[#050505] dark:text-zinc-200">${startResult}</span>`).replace('{end}', `<span class="font-semibold text-[#050505] dark:text-zinc-200">${endResult}</span>`).replace('{total}', `<span class="font-semibold text-[#050505] dark:text-zinc-200">${totalCount}</span>`) }} />
              <Select
                value={String(pageSize)}
                onValueChange={(val) => {
                  setPageSize(Number(val))
                  setPage(1)
                }}
              >
                <SelectTrigger className="h-8 w-[110px] border-[#CED0D4] dark:border-zinc-800 text-xs font-medium focus:ring-[#1877F2] bg-white dark:bg-zinc-900">
                  <SelectValue placeholder={t("rowsPerPage")} />
                </SelectTrigger>
                <SelectContent className="border-[#CED0D4] dark:border-zinc-800">
                  <SelectItem value="7">{t("perPage", { count: 7 })}</SelectItem>
                  <SelectItem value="10">{t("perPage", { count: 10 })}</SelectItem>
                  <SelectItem value="20">{t("perPage", { count: 20 })}</SelectItem>
                  <SelectItem value="50">{t("perPage", { count: 50 })}</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center space-x-1.5">
              <Button
                variant="outline"
                size="icon"
                disabled={page === 1 || loading}
                onClick={() => setPage(prev => Math.max(1, prev - 1))}
                className="h-8 w-8 border-[#CED0D4] dark:border-zinc-800 text-[#65676B] dark:text-zinc-400 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 dark:hover:text-blue-400 disabled:opacity-40"
              >
                <ChevronLeft className="h-4 w-4" />
              </Button>

              {Array.from({ length: totalPages }).map((_, index) => {
                const pageNum = index + 1
                // Clean calculation to show near pages if totalPages is large
                if (totalPages > 7 && Math.abs(pageNum - page) > 2 && pageNum !== 1 && pageNum !== totalPages) {
                  if (pageNum === 2 || pageNum === totalPages - 1) {
                    return <span key={pageNum} className="px-1 text-xs text-[#65676B]">...</span>
                  }
                  return null
                }
                const isCurrent = pageNum === page
                return (
                  <Button
                    key={pageNum}
                    variant={isCurrent ? "default" : "outline"}
                    disabled={loading}
                    onClick={() => setPage(pageNum)}
                    className={`h-8 w-8 text-xs font-semibold rounded-md shadow-2xs transition-all ${isCurrent
                        ? "bg-[#1877F2] hover:bg-[#166FE5] text-white border-[#1877F2]"
                        : "border-[#CED0D4] dark:border-zinc-800 text-[#050505] dark:text-zinc-300 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 dark:hover:text-blue-400"
                      }`}
                  >
                    {pageNum}
                  </Button>
                )
              })}

              <Button
                variant="outline"
                size="icon"
                disabled={page === totalPages || loading}
                onClick={() => setPage(prev => Math.min(totalPages, prev + 1))}
                className="h-8 w-8 border-[#CED0D4] dark:border-zinc-800 text-[#65676B] dark:text-zinc-400 hover:bg-[#E7F3FF] hover:text-[#1877F2] dark:hover:bg-blue-950/40 dark:hover:text-blue-400 disabled:opacity-40"
              >
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
          </div>
        )}
      </div>

      {/* Extend Job Modal */}
      {extendingJob && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4 animate-in fade-in duration-200">
          <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-2xl max-w-lg w-full shadow-2xl overflow-hidden p-6 space-y-5">
            <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800/80 pb-4">
              <div className="flex items-center gap-3">
                <div className="p-2.5 bg-[#E7F3FF] dark:bg-blue-950/50 text-[#1877F2] dark:text-blue-400 rounded-xl">
                  <CalendarPlus className="h-6 w-6" />
                </div>
                <div>
                  <h3 className="text-lg font-semibold text-zinc-900 dark:text-white">{t("extendJobTitle")}</h3>
                  <p className="text-xs text-zinc-500 dark:text-zinc-400">{t("extendJobDesc")}</p>
                </div>
              </div>
              <button
                onClick={() => !extendSubmitting && setExtendingJob(null)}
                className="text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-200 p-1 rounded-lg"
              >
                ✕
              </button>
            </div>

            <div className="bg-zinc-50 dark:bg-zinc-800/40 border border-zinc-200/60 dark:border-zinc-700/60 rounded-xl p-4 space-y-3">
              <p className="text-sm font-medium text-zinc-800 dark:text-zinc-200 leading-relaxed">
                {t("extendWarningPrefix")} <span className="font-semibold text-[#1877F2] dark:text-blue-400">{extendingJob.title}</span>{t("extendWarningSuffix")}
              </p>
              <div className="flex items-center justify-between text-sm bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-700 p-3 rounded-lg">
                <div className="flex flex-col">
                  <span className="text-xs text-zinc-500 dark:text-zinc-400">{t("currentExpirySys")}</span>
                  <strong className="text-zinc-800 dark:text-zinc-200">{formatDate(extendingJob.expiresAt)}</strong>
                </div>
                <div className="flex items-center justify-center">
                  <span className="text-zinc-400 mx-2">→</span>
                  <span className="px-2.5 py-1 bg-[#E7F3FF] dark:bg-blue-900/30 text-[#1877F2] dark:text-blue-400 text-xs font-bold rounded-md border border-blue-200 dark:border-blue-800/50">
                    {t("plus15DaysBtn")}
                  </span>
                </div>
              </div>
            </div>

            {/* Quota & Billing Breakdown */}
            <div className="space-y-3">
              <h4 className="text-xs font-semibold uppercase tracking-wider text-zinc-400 dark:text-zinc-500">
                {t("planBenefits")}
              </h4>
              <div className="p-4 rounded-xl border border-zinc-200 dark:border-zinc-800 bg-gradient-to-r from-zinc-50 to-blue-50/20 dark:from-zinc-900 dark:to-blue-950/20 flex flex-col gap-2">
                <div className="flex items-center justify-between text-sm">
                  <span className="text-zinc-600 dark:text-zinc-400">{t("currentPlan")}</span>
                  <span className="font-semibold px-2.5 py-0.5 rounded-full text-xs bg-[#E7F3FF] text-[#1877F2] dark:bg-blue-950/60 dark:text-blue-300 border border-blue-200 dark:border-blue-800">
                    {walletData?.activeSubscriptionName || t("defaultPlan")}
                  </span>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-zinc-600 dark:text-zinc-400">{t("extendLimit")}</span>
                  <span className="font-medium text-zinc-900 dark:text-white">
                    {jobExtendLimit === -1 ? (
                      <span className="text-[#1877F2] font-semibold">{t("unlimitedFree")}</span>
                    ) : jobExtendLimit === 0 ? (
                      <span className="text-amber-600 dark:text-amber-400 font-semibold">{t("zeroUsesPay")}</span>
                    ) : (
                      <span dangerouslySetInnerHTML={{ __html: t.raw("usedUses").replace('{used}', jobExtendUsed.toString()).replace('{limit}', jobExtendLimit.toString()) }} />
                    )}
                  </span>
                </div>

                <div className="pt-2 border-t border-zinc-200/60 dark:border-zinc-800 flex items-center justify-between text-sm font-semibold">
                  <span className="text-zinc-700 dark:text-zinc-300">{t("costExtension")}</span>
                  {!isExtendQuotaFull ? (
                    <span className="text-[#1877F2] dark:text-blue-400 flex items-center gap-1">
                      <Sparkles className="h-4 w-4" /> {t("freeFromPlan")}
                    </span>
                  ) : (
                    <span className="text-amber-600 dark:text-amber-400 flex items-center gap-1">
                      <Coins className="h-4 w-4" /> {t("payAsYouGo")}
                    </span>
                  )}
                </div>
              </div>

              {/* Insufficient balance warning if out of quota and not enough coins */}
              {isExtendQuotaFull && !canPayWithCoins && (
                <div className="p-3.5 rounded-xl bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800/60 text-amber-900 dark:text-amber-200 text-xs space-y-2.5">
                  <div className="flex items-start gap-2">
                    <AlertTriangle className="h-4 w-4 text-amber-600 shrink-0 mt-0.5" />
                    <div>
                      <p className="font-semibold">{t("insufficientCoin")}</p>
                      <p className="text-amber-700 dark:text-amber-300/80 mt-0.5" dangerouslySetInnerHTML={{ __html: t.raw("insufficientExtendMsg").replace('{balance}', coinBalance.toLocaleString()) }} />
                    </div>
                  </div>
                  <div className="flex gap-2 justify-end">
                    <Link href="/recruiter/billing">
                      <Button size="sm" variant="outline" className="h-7 text-xs border-amber-300 dark:border-amber-700 bg-white dark:bg-zinc-900">
                        {t("upgradePlan")}
                      </Button>
                    </Link>
                    <Link href="/recruiter/billing">
                      <Button size="sm" className="h-7 text-xs bg-amber-600 hover:bg-amber-700 text-white">
                        {t("topupNow")}
                      </Button>
                    </Link>
                  </div>
                </div>
              )}
            </div>

            <div className="flex items-center justify-end gap-3 pt-3 border-t border-zinc-100 dark:border-zinc-800/80">
              <Button
                variant="outline"
                disabled={extendSubmitting}
                onClick={() => setExtendingJob(null)}
              >
                {t("cancel")}
              </Button>
              <Button
                disabled={extendSubmitting || (isExtendQuotaFull && !canPayWithCoins)}
                onClick={handleConfirmExtend}
                className="bg-[#1877F2] hover:bg-[#166FE5] text-white gap-2 px-5 shadow-sm"
              >
                {extendSubmitting && <Loader2 className="h-4 w-4 animate-spin" />}
                {!isExtendQuotaFull ? t("extendNowFree") : t("confirmCoin10k")}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Push Top Job Modal */}
      {pushingTopJob && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4 animate-in fade-in duration-200">
          <div className="bg-white dark:bg-zinc-900 rounded-2xl max-w-lg w-full p-6 shadow-2xl border border-[#1877F2]/30 space-y-6">
            <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800 pb-4">
              <div className="flex items-center gap-3">
                <div className="h-11 w-11 rounded-xl bg-[#1877F2] flex items-center justify-center text-white shadow-md shadow-blue-500/20">
                  <Rocket className="h-6 w-6 fill-white animate-bounce" />
                </div>
                <div>
                  <h3 className="text-lg font-bold text-zinc-900 dark:text-zinc-100 flex items-center gap-1.5">
                    {t("pushTopTitle")} <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-[#E7F3FF] dark:bg-blue-950/50 text-[#1877F2] dark:text-blue-300 border border-blue-200 dark:border-blue-800">{t("pushTopDuration")}</span>
                  </h3>
                  <p className="text-xs text-zinc-500 dark:text-zinc-400">{t("pushTopDesc")}</p>
                </div>
              </div>
              <button
                onClick={() => !pushTopSubmitting && setPushingTopJob(null)}
                className="text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-200 p-1 rounded-lg hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors"
              >
                <XCircle className="h-5 w-5" />
              </button>
            </div>

            <div className="space-y-4 text-sm">
              <div className="p-3.5 bg-blue-50/60 dark:bg-blue-950/20 rounded-xl border border-blue-100 dark:border-blue-900/40 text-zinc-800 dark:text-zinc-200 flex flex-col gap-1.5">
                <div className="flex items-center gap-2 font-medium">
                  <Flame className="h-4 w-4 text-[#1877F2] fill-[#1877F2]/20 shrink-0" />
                  <span>{t("jobPosting")} <strong className="text-[#1877F2] dark:text-blue-400 font-bold">{pushingTopJob.title}</strong></span>
                </div>
                <div className="text-xs text-zinc-600 dark:text-zinc-400 pl-6 space-y-1">
                  <div>{t("pushBenefit1")}</div>
                  <div>{t("pushBenefit2")}</div>
                  {pushingTopJob.pushedTopUntil && new Date(pushingTopJob.pushedTopUntil) >= new Date() && (
                    <div className="text-[#1877F2] dark:text-blue-400 font-semibold pt-1">
                      {t("pushAlreadyTop", { date: new Date(pushingTopJob.pushedTopUntil).toLocaleString('en-US') })}
                    </div>
                  )}
                </div>
              </div>

              {/* Account plan & quota info */}
              <div className="p-4 bg-zinc-50 dark:bg-zinc-800/50 rounded-xl border border-zinc-200/80 dark:border-zinc-700/80 space-y-3">
                <div className="flex items-center justify-between text-xs font-medium text-zinc-500 dark:text-zinc-400">
                  <span>{t("currentPlan")}</span>
                  <span className="font-bold text-[#1877F2] dark:text-blue-400 uppercase">
                    {walletData?.activeSubscriptionName || t("defaultPlan")}
                  </span>
                </div>

                <div className="flex items-center justify-between pt-2 border-t border-zinc-200/60 dark:border-zinc-700/60">
                  <span className="font-medium text-zinc-700 dark:text-zinc-300">{t("pushTopLimit")}</span>
                  <div className="text-right">
                    {jobPushTopLimit === -1 ? (
                      <span className="inline-flex items-center gap-1 text-xs font-bold text-[#1877F2] dark:text-blue-400 bg-[#E7F3FF] dark:bg-blue-950/50 px-2.5 py-0.5 rounded-full border border-blue-200 dark:border-blue-800">
                        <Sparkles className="h-3 w-3" /> {t("unlimited")}
                      </span>
                    ) : jobPushTopLimit === 0 ? (
                      <span className="text-xs text-zinc-500 dark:text-zinc-400">{t("noTopPushes")}</span>
                    ) : (
                      <span className="text-xs font-bold" dangerouslySetInnerHTML={{ __html: t.raw("usedUses").replace('{used}', jobPushTopUsed.toString()).replace('{limit}', jobPushTopLimit.toString()) }} />
                    )}
                  </div>
                </div>

                <div className="flex items-center justify-between pt-2 border-t border-zinc-200/60 dark:border-zinc-700/60">
                  <span className="font-medium text-zinc-700 dark:text-zinc-300">{t("costPush")}</span>
                  {!isPushTopQuotaFull ? (
                    <span className="text-xs font-bold text-[#1877F2] dark:text-blue-400 flex items-center gap-1" dangerouslySetInnerHTML={{ __html: t.raw("freeDeducted") }} />
                  ) : (
                    <div className="text-right">
                      <span className="text-sm font-bold text-amber-600 dark:text-amber-400 flex items-center justify-end gap-1">
                        <Coins className="h-4 w-4" /> 5,000 Coin
                      </span>
                      <div className="text-[11px] text-zinc-500 mt-0.5">
                        Balance: <strong>{coinBalance.toLocaleString()} Coin</strong>
                      </div>
                    </div>
                  )}
                </div>
              </div>

              {isPushTopQuotaFull && !canPayPushTopWithCoins && (
                <div className="p-3 bg-red-50/90 dark:bg-red-950/40 border border-red-200 dark:border-red-800/80 rounded-xl flex items-start gap-3 text-red-800 dark:text-red-300 text-xs">
                  <AlertTriangle className="h-5 w-5 text-red-600 dark:text-red-400 shrink-0 mt-0.5" />
                  <div className="flex-1">
                    <p className="font-bold">{t("insufficientCoin")}</p>
                    <p className="mt-0.5 opacity-90">
                      {t("insufficientPushMsg")}
                    </p>
                    <Link
                      href="/recruiter/billing"
                      className="mt-2 inline-flex items-center gap-1 px-3 py-1 bg-[#1877F2] hover:bg-[#166FE5] text-white font-semibold rounded-lg text-xs shadow-sm transition-all"
                    >
                      {t("topupUpgrade")}
                    </Link>
                  </div>
                </div>
              )}
            </div>

            <div className="flex items-center justify-end gap-3 pt-3 border-t border-zinc-100 dark:border-zinc-800/80">
              <Button
                variant="outline"
                disabled={pushTopSubmitting}
                onClick={() => setPushingTopJob(null)}
              >
                {t("cancel")}
              </Button>
              <Button
                disabled={pushTopSubmitting || (isPushTopQuotaFull && !canPayPushTopWithCoins)}
                onClick={handleConfirmPushTop}
                className="bg-[#1877F2] hover:bg-[#166FE5] text-white font-bold gap-2 px-5 shadow-md shadow-blue-500/20"
              >
                {pushTopSubmitting ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Rocket className="h-4 w-4 fill-white" />
                )}
                {!isPushTopQuotaFull ? t("pushNowFree") : t("confirmPush5k")}
              </Button>
            </div>
          </div>
        </div>
      )}

    </div>
  )
}
