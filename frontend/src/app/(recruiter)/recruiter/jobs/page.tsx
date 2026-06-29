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
  Loader2,
  Briefcase,
  MapPin,
  Calendar,
  Layers,
  Target,
  Monitor
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
    <div className="min-h-screen bg-background py-10 px-4 sm:px-6 lg:px-8 transition-colors duration-200">
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

        {/* Job Card Grid View */}
        <div className="relative min-h-[400px]">
          {loading && (
            <div className="absolute inset-0 bg-white/70 dark:bg-zinc-950/70 z-10 flex items-center justify-center backdrop-blur-xs rounded-2xl">
              <Loader2 className="h-8 w-8 text-blue-500 animate-spin" />
            </div>
          )}

          {jobs.length > 0 ? (
            <div className="grid grid-cols-1 gap-4">
              {jobs.map((job) => (
                <div 
                  key={job.id} 
                  className="group bg-white dark:bg-zinc-900 border border-zinc-200/80 dark:border-zinc-800/80 rounded-xl p-5 hover:shadow-md transition-all flex flex-col md:flex-row md:items-start justify-between gap-6 relative overflow-hidden"
                >
                  <div className="absolute left-0 top-0 bottom-0 w-1 bg-zinc-200 dark:bg-zinc-800 group-hover:bg-blue-500 transition-colors"></div>
                  
                  <div className="flex-1 space-y-4">
                    {/* Header: Title and Badges */}
                    <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-4">
                      <div className="flex items-center gap-2">
                        <h3 className="font-bold text-lg text-zinc-900 dark:text-zinc-50 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
                          {job.title}
                        </h3>
                        <span className="inline-flex px-1.5 py-0.5 rounded text-[10px] font-bold bg-zinc-100 text-zinc-500 dark:bg-zinc-800 dark:text-zinc-400 border border-zinc-200/50 dark:border-zinc-700/50">
                          {job.jobCode}
                        </span>
                      </div>
                      <div className="flex items-center gap-2">
                        {renderStatusBadge(job.status)}
                      </div>
                    </div>

                    {/* Metadata line */}
                    <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-zinc-500 dark:text-zinc-400">
                      <div className="flex items-center gap-1">
                        <MapPin className="h-4 w-4 shrink-0" />
                        <span>{job.provinceCode}</span>
                      </div>
                      <div className="flex items-center gap-1">
                        <Calendar className="h-4 w-4 shrink-0" />
                        <span>Posted {formatDate(job.publishedAt || job.createdAt)}</span>
                      </div>
                      <div className="flex items-center gap-1">
                        <Users className="h-4 w-4 shrink-0" />
                        <span>{job.applicationCount} Applications</span>
                      </div>
                    </div>

                    {/* Secondary Attributes (Level, Working Model, Domain, Expertise) */}
                    <div className="flex flex-wrap gap-3">
                      {job.level && (
                        <div className="flex items-center gap-1.5 text-xs text-zinc-600 dark:text-zinc-300">
                          <Briefcase className="h-3.5 w-3.5 text-indigo-500" />
                          {job.level}
                        </div>
                      )}
                      {job.workingModel && (
                        <div className="flex items-center gap-1.5 text-xs text-zinc-600 dark:text-zinc-300">
                          <Monitor className="h-3.5 w-3.5 text-cyan-500" />
                          {job.workingModel}
                        </div>
                      )}
                      {job.jobExpertise && (
                        <div className="flex items-center gap-1.5 text-xs text-zinc-600 dark:text-zinc-300">
                          <Target className="h-3.5 w-3.5 text-rose-500" />
                          {job.jobExpertise}
                        </div>
                      )}
                      {job.jobDomain && job.jobDomain.length > 0 && (
                        <div className="flex items-center gap-1.5 text-xs text-zinc-600 dark:text-zinc-300">
                          <Layers className="h-3.5 w-3.5 text-fuchsia-500" />
                          <span className="truncate max-w-[200px]">{job.jobDomain.join(", ")}</span>
                        </div>
                      )}
                    </div>

                    {/* Skills Badges */}
                    {job.skills && job.skills.length > 0 && (
                      <div className="flex flex-wrap gap-1.5 pt-1">
                        {job.skills.map((skill, index) => (
                          <span key={index} className="inline-flex px-2 py-0.5 rounded text-xs font-medium bg-blue-50 text-blue-700 border border-blue-200 dark:bg-blue-950/40 dark:text-blue-400 dark:border-blue-900/50">
                            {skill}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>

                  {/* Actions Column */}
                  <div className="flex md:flex-col items-center justify-end md:justify-center gap-2 pt-4 md:pt-0 mt-4 md:mt-0 border-t md:border-t-0 md:border-l border-zinc-100 dark:border-zinc-800/80 md:pl-6">
                    <Button 
                      variant="ghost" 
                      size="sm" 
                      onClick={() => openEditModal(job.id)}
                      className="w-full justify-start text-blue-600 hover:text-blue-700 hover:bg-blue-50 dark:text-blue-400 dark:hover:text-blue-300 dark:hover:bg-blue-950/30 gap-2"
                    >
                      <Pencil className="h-4 w-4" />
                      Edit Job
                    </Button>
                    <Button 
                      variant="ghost" 
                      size="sm" 
                      onClick={() => openViewModal(job.id)}
                      className="w-full justify-start text-zinc-600 hover:text-zinc-700 hover:bg-zinc-100 dark:text-zinc-400 dark:hover:text-zinc-300 dark:hover:bg-zinc-800/50 gap-2"
                    >
                      <Eye className="h-4 w-4" />
                      View Details
                    </Button>
                    {job.status !== "CLOSED" && (
                      <Button 
                        variant="ghost" 
                        size="sm" 
                        onClick={() => handleCloseJob(job.id)}
                        className="w-full justify-start text-red-600 hover:text-red-700 hover:bg-red-50 dark:text-red-400 dark:hover:text-red-300 dark:hover:bg-red-950/30 gap-2"
                      >
                        <XCircle className="h-4 w-4" />
                        Close Job
                      </Button>
                    )}
                  </div>
                </div>
              ))}
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

    </div>
  )
}
