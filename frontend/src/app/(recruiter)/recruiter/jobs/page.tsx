"use client"

import { useRouter } from "next/navigation"
import { useJobs } from "@/hooks/useJobs"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent } from "@/components/ui/card"
import { 
  Search, 
  Plus, 
  Users, 
  Pencil, 
  Eye, 
  XCircle, 
  ChevronLeft, 
  ChevronRight,
  Loader2
} from "lucide-react"

export default function JobPostingsPage() {
  const router = useRouter()
  const pageSize = 7 // Matches mockup showing 7 items
  
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
    closeJob
  } = useJobs(1, pageSize)

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
  const renderStatusBadge = (statusVal: string) => {
    switch (statusVal) {
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
            {statusVal}
          </span>
        )
    }
  }

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950 py-10 px-4 sm:px-6 lg:px-8 transition-colors duration-200">
      <div className="max-w-7xl mx-auto space-y-6">
        
        {/* Top Header Card */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-white dark:bg-zinc-900 p-6 rounded-2xl shadow-xs border border-zinc-200/80 dark:border-zinc-800/80">
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

        {/* Filter Card */}
        <Card className="border-zinc-200/80 dark:border-zinc-800/80 shadow-xs">
          <CardContent className="p-4 flex flex-col sm:flex-row items-center gap-4 justify-between">
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
          </CardContent>
        </Card>

        {/* Job Table View */}
        <div className="bg-white dark:bg-zinc-900 rounded-2xl shadow-xs border border-zinc-200/80 dark:border-zinc-800/80 overflow-hidden relative min-h-[400px]">
          {loading && (
            <div className="absolute inset-0 bg-white/70 dark:bg-zinc-900/70 z-10 flex items-center justify-center backdrop-blur-xs">
              <Loader2 className="h-8 w-8 text-blue-500 animate-spin" />
            </div>
          )}

          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-zinc-200 dark:border-zinc-800/80 bg-zinc-50/50 dark:bg-zinc-950/30">
                  <th className="px-6 py-4.5 text-xs font-bold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">Job Title</th>
                  <th className="px-6 py-4.5 text-xs font-bold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">Posted Date</th>
                  <th className="px-6 py-4.5 text-xs font-bold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">Status</th>
                  <th className="px-6 py-4.5 text-xs font-bold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider">Applications</th>
                  <th className="px-6 py-4.5 text-xs font-bold text-zinc-500 dark:text-zinc-400 uppercase tracking-wider text-center">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-zinc-200/60 dark:divide-zinc-800/60">
                {jobs.length > 0 ? (
                  jobs.map((job) => (
                    <tr 
                      key={job.id} 
                      className="group hover:bg-zinc-50/50 dark:hover:bg-zinc-800/20 transition-colors"
                    >
                      <td className="px-6 py-4.5">
                        <div className="flex items-center gap-2.5">
                          <span className="font-bold text-zinc-900 dark:text-zinc-100 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
                            {job.title}
                          </span>
                          <span className="inline-flex px-1.5 py-0.5 rounded text-[10px] font-bold bg-zinc-100 text-zinc-500 dark:bg-zinc-800 dark:text-zinc-400 border border-zinc-200/50 dark:border-zinc-700/50">
                            {job.jobCode}
                          </span>
                        </div>
                      </td>
                      <td className="px-6 py-4.5 text-sm text-zinc-500 dark:text-zinc-400">
                        {formatDate(job.publishedAt || job.createdAt)}
                      </td>
                      <td className="px-6 py-4.5">
                        {renderStatusBadge(job.status)}
                      </td>
                      <td className="px-6 py-4.5 text-sm font-semibold text-zinc-700 dark:text-zinc-300">
                        <div className="flex items-center gap-1.5">
                          <Users className="h-4 w-4 text-zinc-400 shrink-0" />
                          <span>{job.applicationCount}</span>
                        </div>
                      </td>
                      <td className="px-6 py-4.5">
                        <div className="flex items-center justify-center gap-1.5">
                          <Button 
                            variant="ghost" 
                            size="sm" 
                            onClick={() => openEditModal(job.id)}
                            className="h-8 text-blue-600 hover:text-blue-700 hover:bg-blue-50 dark:text-blue-400 dark:hover:text-blue-300 dark:hover:bg-blue-950/30 gap-1"
                          >
                            <Pencil className="h-3.5 w-3.5" />
                            Edit
                          </Button>
                          <Button 
                            variant="ghost" 
                            size="sm" 
                            onClick={() => openViewModal(job.id)}
                            className="h-8 text-zinc-600 hover:text-zinc-700 hover:bg-zinc-100 dark:text-zinc-400 dark:hover:text-zinc-300 dark:hover:bg-zinc-800/50 gap-1"
                          >
                            <Eye className="h-3.5 w-3.5" />
                            View
                          </Button>
                          {job.status !== "CLOSED" && (
                            <Button 
                              variant="ghost" 
                              size="sm" 
                              onClick={() => handleCloseJob(job.id)}
                              className="h-8 text-red-600 hover:text-red-700 hover:bg-red-50 dark:text-red-400 dark:hover:text-red-300 dark:hover:bg-red-950/30 gap-1"
                            >
                              <XCircle className="h-3.5 w-3.5" />
                              Close
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan={5} className="text-center py-16 text-zinc-400 dark:text-zinc-500">
                      No job postings found matching the filters.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          {/* Table Footer / Pagination */}
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

    </div>
  )
}
