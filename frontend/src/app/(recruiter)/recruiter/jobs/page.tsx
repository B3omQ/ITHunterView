"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import Link from "next/link"
import { useJobs } from "@/hooks/useJobs"
import { useSignalR } from "@/hooks/useSignalR"
import { useWalletBalance } from "@/hooks/useWallet"
import { recruiterService } from "@/services/recruiter.service"
import { AiParseStatusBadge } from "@/components/shared/AiParseStatusBadge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { 
  Search, 
  Plus, 
  Users, 
  Pencil, 
  Eye, 
  XCircle, 
  ChevronLeft, 
  ChevronRight,
  Loader2,
  Briefcase,
  MapPin,
  Calendar,
  Layers,
  Target,
  Monitor,
  Ban,
  CalendarPlus,
  Sparkles,
  Coins,
  AlertTriangle
} from "lucide-react"

export default function JobPostingsPage() {
  const router = useRouter()
  const pageSize = 7 // Matches mockup showing 7 items
  
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
    const res = await recruiterService.extendJob(extendingJob.id)
    setExtendSubmitting(false)
    if (res.success) {
      alert(res.message || "Đã gia hạn tin tuyển dụng thành công!")
      setExtendingJob(null)
      refresh()
      refetchWallet()
    } else {
      alert(res.message || "Gia hạn không thành công.")
    }
  }

  const {
    jobs,
    totalCount,
    page,
    setPage,
    search,
    setSearch,
    status,
    setStatus,
    loading,
    closeJob,
    refresh
  } = useJobs(1, pageSize)

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

  // Handle Close Job Posting
  const handleCloseJob = async (id: string) => {
    if (!confirm("Are you sure you want to close this job posting? This action will set its status to Closed.")) return
    const res = await closeJob(id)
    if (!res.success) {
      alert(res.message || "Failed to close job posting")
    }
  }

  const openCreateModal = () => {
    router.push("/recruiter/jobs/new")
  }

  const openEditModal = (jobId: string) => {
    router.push(`/recruiter/jobs/${jobId}/edit`)
  }

  const openViewModal = (jobId: string) => {
    router.push(`/recruiter/jobs/${jobId}`)
  }

  // Helpers for pagination calculations
  const totalPages = Math.ceil(totalCount / pageSize)
  const startResult = (page - 1) * pageSize + 1
  const endResult = Math.min(page * pageSize, totalCount)

  // Format date helper (matching mockup style: "May 28, 2026")
  const formatDate = (dateStr: string) => {
    if (!dateStr) return "N/A"
    return new Date(dateStr).toLocaleDateString("en-US", {
      month: "short",
      day: "numeric",
      year: "numeric",
    })
  }

  // Render Status Badge
  const renderStatusBadge = (job: any) => {
    if (job.isBanned) {
      return (
        <div className="flex flex-col items-center gap-1">
          <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-400 border border-red-200 dark:border-red-900/50">
            <Ban className="h-3 w-3" />
            BANNED
          </span>
          <span className="text-[10px] text-red-500 max-w-[100px] truncate" title={job.banReason}>
            {job.banReason}
          </span>
        </div>
      )
    }

    switch (job.status) {
      case "PUBLISHED":
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-900/50">
            <span className="h-1.5 w-1.5 rounded-full bg-emerald-500 animate-pulse"></span>
            Active
          </span>
        )
      case "DRAFT":
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-zinc-50 text-zinc-600 dark:bg-zinc-800/40 dark:text-zinc-400 border border-zinc-200 dark:border-zinc-700/50">
            <span className="h-1.5 w-1.5 rounded-full bg-zinc-400"></span>
            Draft
          </span>
        )
      case "CLOSED":
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-rose-50 text-rose-600 dark:bg-rose-950/40 dark:text-rose-400 border border-rose-200 dark:border-rose-900/50">
            <span className="h-1.5 w-1.5 rounded-full bg-rose-500"></span>
            Closed
          </span>
        )
      default:
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-zinc-50 text-zinc-600 dark:bg-zinc-800/40 dark:text-zinc-400 border border-zinc-200 dark:border-zinc-700/50">
            {job.status}
          </span>
        )
    }
  }

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-8 space-y-4">
        
        {/* Top Header Card */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 py-2">
          <div>
            <h1 className="text-3xl font-extrabold text-zinc-900 dark:text-zinc-50 tracking-tight">Job Postings</h1>
            <p className="text-zinc-500 dark:text-zinc-400 mt-1.5 text-sm">Manage and track your open positions</p>
          </div>
          <Button 
            onClick={openCreateModal}
            className="bg-blue-600 hover:bg-blue-700 text-white font-medium shadow-md shadow-blue-500/10 hover:shadow-blue-500/20 active:scale-98 transition-all gap-2"
          >
            <Plus className="h-4.5 w-4.5" />
            Create New Job
          </Button>
        </div>

        {/* Job Slots & Quota Status Banner */}
        {walletData && (
          <div className={`p-4 rounded-xl border flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 shadow-xs transition-all ${
            isSlotFull 
              ? "bg-amber-50/80 dark:bg-amber-950/20 border-amber-200 dark:border-amber-800/50" 
              : "bg-blue-50/50 dark:bg-blue-950/20 border-blue-100 dark:border-blue-900/40"
          }`}>
            <div className="flex items-start sm:items-center gap-3.5">
              <div className={`p-2.5 rounded-lg shrink-0 mt-0.5 sm:mt-0 ${isSlotFull ? "bg-amber-100 text-amber-700 dark:bg-amber-900/50 dark:text-amber-300" : "bg-blue-100 text-blue-700 dark:bg-blue-900/50 dark:text-blue-300"}`}>
                <Briefcase className="h-5 w-5" />
              </div>
              <div className="space-y-1">
                <div className="flex flex-wrap items-center gap-2 text-sm font-semibold text-zinc-900 dark:text-zinc-100">
                  <span>Tin đang Active: <strong className="text-blue-600 dark:text-blue-400 font-bold">{jobSlotsUsed}</strong></span>
                  <span className="text-zinc-300 dark:text-zinc-600">•</span>
                  <span>Hạn mức theo gói: <strong className="text-zinc-800 dark:text-zinc-200">{jobSlotsLimit === -1 ? "Vô hạn" : jobSlotsLimit}</strong> tin</span>
                  {walletData.activeSubscriptionName ? (
                    <span className="px-2 py-0.5 rounded-full text-[11px] font-medium bg-blue-100 dark:bg-blue-900/50 text-blue-700 dark:text-blue-300">
                      Gói {walletData.activeSubscriptionName}
                    </span>
                  ) : (
                    <span className="px-2 py-0.5 rounded-full text-[11px] font-medium bg-zinc-200 dark:bg-zinc-800 text-zinc-700 dark:text-zinc-300">
                      Gói Free (Mặc định)
                    </span>
                  )}
                  {isSlotFull && (
                    <span className="px-2 py-0.5 rounded-full text-[11px] font-medium bg-amber-100 text-amber-800 dark:bg-amber-900/60 dark:text-amber-300 border border-amber-300 dark:border-amber-700">
                      Đã dùng hết Slot miễn phí
                    </span>
                  )}
                </div>
                <p className="text-xs text-zinc-600 dark:text-zinc-400 leading-relaxed">
                  {isSlotFull 
                    ? `Hiện bạn đã vượt quá hạn mức ${jobSlotsLimit === -1 ? "vô hạn" : jobSlotsLimit} tin miễn phí trong gói. Khi đăng thêm tin Active mới, hệ thống sẽ sử dụng 20,000 Coin từ ví cho mỗi tin (Số dư hiện tại: ${(walletData.balance || 0).toLocaleString()} Coin). Lưu dưới dạng Draft (Bản nháp) thì hoàn toàn miễn phí.`
                    : `Gói hiện tại cho phép bạn duy trì tối đa ${jobSlotsLimit === -1 ? "vô số" : jobSlotsLimit} việc làm Active đồng thời. Bạn có thể đăng thêm ${jobSlotsLimit === -1 ? "vô số" : (jobSlotsLimit || 0) - (jobSlotsUsed || 0)} tin miễn phí mà không mất Coin.`}
                </p>
              </div>
            </div>
            {isSlotFull && (
              <Button 
                variant="outline"
                size="sm"
                onClick={() => router.push("/recruiter/billing")}
                className="shrink-0 border-amber-300 dark:border-amber-700 hover:bg-amber-100/50 text-amber-800 dark:text-amber-300 text-xs font-medium self-end sm:self-center"
              >
                Nâng cấp gói ngay
              </Button>
            )}
          </div>
        )}

        {/* Filter Card */}
        <div className="flex flex-col sm:flex-row items-center gap-4 justify-between py-2 border-b border-zinc-200 dark:border-zinc-800 mb-4">
            <div className="relative w-full sm:max-w-md">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-zinc-400" />
              <Input
                placeholder="Search by Title or Job Code..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="pl-9 h-10 w-full bg-zinc-50/50 dark:bg-zinc-950/50 border-zinc-200 dark:border-zinc-800 focus-visible:ring-blue-500"
              />
            </div>
            
            <div className="flex items-center gap-2 w-full sm:w-auto">
              <span className="text-sm font-medium text-zinc-500 dark:text-zinc-400 shrink-0">Status:</span>
              <select
                value={status}
                onChange={(e) => {
                  setStatus(e.target.value)
                  setPage(1)
                }}
                className="h-10 w-full sm:w-44 rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
              >
                <option value="ALL">All Statuses</option>
                <option value="PUBLISHED">Active</option>
                <option value="DRAFT">Draft</option>
                <option value="CLOSED">Closed</option>
              </select>
            </div>
        </div>

        {/* Job Card Grid View */}
        <div className="relative min-h-[400px]">
          {loading && (
            <div className="absolute inset-0 bg-white/70 dark:bg-zinc-950/70 z-10 flex items-center justify-center backdrop-blur-xs rounded-2xl">
              <Loader2 className="h-8 w-8 text-blue-500 animate-spin" />
            </div>
          )}

          {jobs.length > 0 ? (
            <div className="bg-white dark:bg-zinc-900 border border-zinc-200/80 dark:border-zinc-800/80 rounded-xl overflow-x-auto shadow-sm">
              <table className="w-full text-sm text-left">
                <thead className="bg-zinc-50/80 dark:bg-zinc-950/80 border-b border-zinc-200/80 dark:border-zinc-800/80 text-zinc-500 font-semibold text-xs uppercase tracking-wider">
                  <tr>
                    <th className="px-5 py-4 w-1/3 min-w-[250px]">Job Details</th>
                    <th className="px-5 py-4 min-w-[150px]">Location</th>
                    <th className="px-5 py-4 min-w-[120px]">Dates</th>
                    <th className="px-5 py-4 text-center min-w-[100px]">Applicants</th>
                    <th className="px-5 py-4 text-center min-w-[120px]">Status</th>
                    <th className="px-5 py-4 text-right min-w-[120px]">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-zinc-100 dark:divide-zinc-800/50">
                  {jobs.map((job) => (
                    <tr key={job.id} className="hover:bg-zinc-50/50 dark:hover:bg-zinc-800/20 transition-colors group">
                      <td className="px-5 py-3 align-top">
                        <div className="flex flex-col gap-1">
                          <div className="flex items-center gap-2">
                            <span className="inline-flex px-1.5 py-0.5 rounded text-[10px] font-bold bg-zinc-100 text-zinc-500 dark:bg-zinc-800 dark:text-zinc-400 border border-zinc-200/50 dark:border-zinc-700/50">
                              {job.jobCode}
                            </span>
                            <Link href={`/recruiter/jobs/${job.id}`} className="font-bold text-sm text-zinc-900 dark:text-zinc-50 hover:text-blue-600 dark:hover:text-blue-400 transition-colors line-clamp-2">
                              {job.title}
                            </Link>
                          </div>
                          <div className="flex flex-wrap gap-2 mt-1">
                            {job.level && (
                              <div className="flex items-center gap-1 text-[11px] text-zinc-500 dark:text-zinc-400">
                                <Briefcase className="h-3 w-3 text-indigo-500" /> {job.level}
                              </div>
                            )}
                            {job.workingModel && (
                              <div className="flex items-center gap-1 text-[11px] text-zinc-500 dark:text-zinc-400">
                                <Monitor className="h-3 w-3 text-cyan-500" /> {job.workingModel}
                              </div>
                            )}
                            {job.jobExpertise && (
                              <div className="flex items-center gap-1 text-[11px] text-zinc-500 dark:text-zinc-400">
                                <Target className="h-3 w-3 text-rose-500" /> {job.jobExpertise}
                              </div>
                            )}
                          </div>
                        </div>
                      </td>
                      <td className="px-5 py-3 align-top">
                        <div className="flex items-center gap-1 text-sm text-zinc-600 dark:text-zinc-300">
                          <MapPin className="h-3.5 w-3.5 shrink-0 text-zinc-400" />
                          <span className="line-clamp-2">{job.location}</span>
                        </div>
                      </td>
                      <td className="px-5 py-3 align-top">
                        <div className="flex flex-col gap-1 text-xs text-zinc-500 dark:text-zinc-400 whitespace-nowrap">
                          <div><span className="font-medium text-zinc-700 dark:text-zinc-300">Posted:</span> {formatDate(job.publishedAt || job.createdAt)}</div>
                          {job.expiresAt && (
                            <div><span className="font-medium text-zinc-700 dark:text-zinc-300">Expires:</span> {formatDate(job.expiresAt)}</div>
                          )}
                        </div>
                      </td>
                      <td className="px-5 py-3 align-top text-center">
                        <Link 
                          href={`/recruiter/jobs/${job.id}/applicants`} 
                          className="inline-flex items-center justify-center gap-1.5 px-3 py-1 bg-blue-50 text-blue-700 hover:bg-blue-100 rounded-full text-xs font-semibold transition-colors dark:bg-blue-900/30 dark:text-blue-400 dark:hover:bg-blue-900/50"
                        >
                          <Users className="h-3.5 w-3.5" />
                          {job.applicationCount}
                        </Link>
                      </td>
                      <td className="px-5 py-3 align-top text-center">

                        <div className="flex flex-col items-center gap-1.5">
                          {renderStatusBadge(job)}
                          <AiParseStatusBadge status={job.parseStatus} error={job.parseError} />
                        </div>

                      </td>
                      <td className="px-5 py-3 align-top text-right">
                        <div className="flex items-center justify-end gap-1">
                          <Button 
                            variant="ghost" 
                            size="icon" 
                            onClick={() => openEditModal(job.id)}
                            title={job.isBanned ? "Cannot edit banned job" : "Edit Job"}
                            disabled={job.isBanned}
                            className="h-8 w-8 text-blue-600 hover:text-blue-700 hover:bg-blue-50 dark:text-blue-400 dark:hover:bg-blue-900/30 disabled:opacity-30 disabled:hover:bg-transparent"
                          >
                            <Pencil className="h-4 w-4" />
                          </Button>
                          <Button 
                            variant="ghost" 
                            size="icon" 
                            onClick={() => openViewModal(job.id)}
                            title="View Details"
                            className="h-8 w-8 text-zinc-600 hover:text-zinc-900 hover:bg-zinc-100 dark:text-zinc-400 dark:hover:bg-zinc-800"
                          >
                            <Eye className="h-4 w-4" />
                          </Button>
                          {!job.isBanned && (
                            <Button 
                              variant="ghost" 
                              size="icon" 
                              onClick={() => setExtendingJob(job)}
                              title="Gia hạn tin tuyển dụng (thêm 15 ngày)"
                              className="h-8 w-8 text-emerald-600 hover:text-emerald-700 hover:bg-emerald-50 dark:text-emerald-400 dark:hover:bg-emerald-900/30"
                            >
                              <CalendarPlus className="h-4 w-4" />
                            </Button>
                          )}
                          {job.status !== "CLOSED" && !job.isBanned && (
                            <Button 
                              variant="ghost" 
                              size="icon" 
                              onClick={() => handleCloseJob(job.id)}
                              title="Close Job"
                              className="h-8 w-8 text-red-600 hover:text-red-700 hover:bg-red-50 dark:text-red-400 dark:hover:bg-red-900/30"
                            >
                              <XCircle className="h-4 w-4" />
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div className="bg-white dark:bg-zinc-900 rounded-xl shadow-xs border border-zinc-200/80 dark:border-zinc-800/80 p-16 text-center text-zinc-500 dark:text-zinc-400">
              No job postings found matching the filters.
            </div>
          )}

          {/* Pagination */}
          {totalCount > 0 && (
            <div className="px-6 py-4 flex flex-col sm:flex-row items-center justify-between border-t border-zinc-200 dark:border-zinc-800 gap-4 bg-zinc-50/20 dark:bg-zinc-950/10">
              <span className="text-sm text-zinc-500 dark:text-zinc-400">
                Showing <strong className="font-semibold text-zinc-700 dark:text-zinc-300">{startResult}</strong> to{" "}
                <strong className="font-semibold text-zinc-700 dark:text-zinc-300">{endResult}</strong> of{" "}
                <strong className="font-semibold text-zinc-700 dark:text-zinc-300">{totalCount}</strong> results
              </span>

              <div className="flex items-center gap-1">
                <Button
                  variant="outline"
                  size="icon"
                  className="h-8 w-8"
                  disabled={page === 1}
                  onClick={() => setPage((p) => p - 1)}
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>

                {Array.from({ length: totalPages }).map((_, index) => {
                  const pageNum = index + 1
                  const isCurrent = pageNum === page
                  return (
                    <Button
                      key={pageNum}
                      variant={isCurrent ? "default" : "outline"}
                      className={`h-8 w-8 font-medium ${
                        isCurrent 
                          ? "bg-blue-600 hover:bg-blue-700 text-white" 
                          : "text-zinc-700 dark:text-zinc-300"
                      }`}
                      onClick={() => setPage(pageNum)}
                    >
                      {pageNum}
                    </Button>
                  )
                })}

                <Button
                  variant="outline"
                  size="icon"
                  className="h-8 w-8"
                  disabled={page === totalPages}
                  onClick={() => setPage((p) => p + 1)}
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Extend Job Modal */}
      {extendingJob && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4 animate-in fade-in duration-200">
          <div className="bg-white dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 rounded-2xl max-w-lg w-full shadow-2xl overflow-hidden p-6 space-y-5">
            <div className="flex items-center justify-between border-b border-zinc-100 dark:border-zinc-800/80 pb-4">
              <div className="flex items-center gap-3">
                <div className="p-2.5 bg-emerald-100 dark:bg-emerald-900/30 text-emerald-600 dark:text-emerald-400 rounded-xl">
                  <CalendarPlus className="h-6 w-6" />
                </div>
                <div>
                  <h3 className="text-lg font-semibold text-zinc-900 dark:text-white">Gia hạn tin tuyển dụng</h3>
                  <p className="text-xs text-zinc-500 dark:text-zinc-400">Thêm 15 ngày hiển thị active cho công việc</p>
                </div>
              </div>
              <button 
                onClick={() => !extendSubmitting && setExtendingJob(null)}
                className="text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-200 p-1 rounded-lg"
              >
                ✕
              </button>
            </div>

            <div className="bg-zinc-50 dark:bg-zinc-800/40 border border-zinc-200/60 dark:border-zinc-700/60 rounded-xl p-4 space-y-2">
              <p className="text-sm font-medium text-zinc-800 dark:text-zinc-200 line-clamp-1">
                📌 Tin tuyển dụng: <span className="font-semibold text-emerald-600 dark:text-emerald-400">{extendingJob.title}</span>
              </p>
              <div className="flex items-center justify-between text-xs text-zinc-500 dark:text-zinc-400 pt-1 border-t border-zinc-200/40 dark:border-zinc-700/40">
                <span>Hết hạn hiện tại: <strong className="text-zinc-700 dark:text-zinc-300">{formatDate(extendingJob.expiresAt)}</strong></span>
                <span className="text-emerald-600 dark:text-emerald-400 font-semibold">+ 15 ngày</span>
              </div>
            </div>

            {/* Quota & Billing Breakdown */}
            <div className="space-y-3">
              <h4 className="text-xs font-semibold uppercase tracking-wider text-zinc-400 dark:text-zinc-500">
                Quyền lợi Gói & Thanh toán
              </h4>
              <div className="p-4 rounded-xl border border-zinc-200 dark:border-zinc-800 bg-gradient-to-r from-zinc-50 to-emerald-50/20 dark:from-zinc-900 dark:to-emerald-950/20 flex flex-col gap-2">
                <div className="flex items-center justify-between text-sm">
                  <span className="text-zinc-600 dark:text-zinc-400">Gói dịch vụ hiện tại:</span>
                  <span className="font-semibold px-2.5 py-0.5 rounded-full text-xs bg-emerald-100 text-emerald-800 dark:bg-emerald-900/40 dark:text-emerald-300">
                    {walletData?.activeSubscriptionName || "Gói Mặc định (Free)"}
                  </span>
                </div>
                <div className="flex items-center justify-between text-sm">
                  <span className="text-zinc-600 dark:text-zinc-400">Hạn mức gia hạn trong gói:</span>
                  <span className="font-medium text-zinc-900 dark:text-white">
                    {jobExtendLimit === -1 ? (
                      <span className="text-emerald-600 font-semibold">♾️ Vô hạn (Miễn phí)</span>
                    ) : jobExtendLimit === 0 ? (
                      <span className="text-amber-600 dark:text-amber-400 font-semibold">0 lượt (Thanh toán lẻ)</span>
                    ) : (
                      <span>Đã dùng <strong>{jobExtendUsed}</strong> / <strong>{jobExtendLimit}</strong> lượt</span>
                    )}
                  </span>
                </div>
                
                <div className="pt-2 border-t border-zinc-200/60 dark:border-zinc-800 flex items-center justify-between text-sm font-semibold">
                  <span className="text-zinc-700 dark:text-zinc-300">Chi phí lần gia hạn này:</span>
                  {!isExtendQuotaFull ? (
                    <span className="text-emerald-600 dark:text-emerald-400 flex items-center gap-1">
                      <Sparkles className="h-4 w-4" /> 0 Coin (Miễn phí từ gói)
                    </span>
                  ) : (
                    <span className="text-amber-600 dark:text-amber-400 flex items-center gap-1">
                      <Coins className="h-4 w-4" /> 10,000 Coin (Ví pay-as-you-go)
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
                      <p className="font-semibold">Số dư Coin không đủ để thanh toán</p>
                      <p className="text-amber-700 dark:text-amber-300/80 mt-0.5">
                        Bạn đã hết lượt gia hạn miễn phí trong gói và số dư hiện tại (<strong>{(coinBalance).toLocaleString()} Coin</strong>) không đủ 10,000 Coin.
                      </p>
                    </div>
                  </div>
                  <div className="flex gap-2 justify-end">
                    <Link href="/recruiter/billing">
                      <Button size="sm" variant="outline" className="h-7 text-xs border-amber-300 dark:border-amber-700 bg-white dark:bg-zinc-900">
                        Nâng cấp gói
                      </Button>
                    </Link>
                    <Link href="/recruiter/billing">
                      <Button size="sm" className="h-7 text-xs bg-amber-600 hover:bg-amber-700 text-white">
                        Nạp Coin ngay
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
                Hủy bỏ
              </Button>
              <Button
                disabled={extendSubmitting || (isExtendQuotaFull && !canPayWithCoins)}
                onClick={handleConfirmExtend}
                className="bg-emerald-600 hover:bg-emerald-700 text-white gap-2 px-5 shadow-sm"
              >
                {extendSubmitting && <Loader2 className="h-4 w-4 animate-spin" />}
                {!isExtendQuotaFull ? "✨ Gia hạn ngay (Miễn phí)" : "🪙 Xác nhận (10,000 Coin)"}
              </Button>
            </div>
          </div>
        </div>
      )}

    </div>
  )
}
