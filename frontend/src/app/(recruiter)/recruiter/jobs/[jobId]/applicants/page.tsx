"use client"

import { useState, useEffect } from "react"
import { useRouter, useParams } from "next/navigation"
import { useJobDetails } from "@/hooks/useJobs"
import { JobApplicationService } from "@/services/job-application.service"
import { ApplicantDto, ApplicationStatus } from "@/types/job-application.types"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { 
  ArrowLeft,
  Loader2,
  Mail,
  Calendar,
  Eye,
  Filter,
  Users,
  FileText,
  Download
} from "lucide-react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { toast } from "sonner"
import { JobApplicationDetailDto } from "@/types/job-application.types"

export default function JobApplicantsPage() {
  const router = useRouter()
  const params = useParams()
  const jobId = params.jobId as string

  const { job, loading: jobLoading } = useJobDetails(jobId)
  
  const [applicants, setApplicants] = useState<ApplicantDto[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize] = useState(10)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Detail Modal State
  const [detailModalOpen, setDetailModalOpen] = useState(false)
  const [selectedDetail, setSelectedDetail] = useState<JobApplicationDetailDto | null>(null)
  const [detailLoading, setDetailLoading] = useState(false)

  useEffect(() => {
    const fetchApplicants = async () => {
      setLoading(true)
      try {
        const response = await JobApplicationService.getApplicantsByJobId(jobId, page, pageSize)
        setApplicants(response.items || [])
        setTotalCount(response.total || 0)
        setError(null)
      } catch (err: any) {
        console.error("Failed to fetch applicants:", err)
        setError("Failed to load applicants. Please try again.")
      } finally {
        setLoading(false)
      }
    }

    if (jobId) {
      fetchApplicants()
    }
  }, [jobId, page, pageSize])

  const formatDate = (dateStr: string) => {
    if (!dateStr) return "N/A"
    return new Date(dateStr).toLocaleDateString("en-GB", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric"
    })
  }

  const handleStatusChange = async (applicantId: string, newStatus: ApplicationStatus) => {
    try {
      await JobApplicationService.updateApplicationStatus(applicantId, newStatus)
      toast.success("Status updated successfully")
      // Update local state
      setApplicants(prev => prev.map(a => a.id === applicantId ? { ...a, status: newStatus } : a))
    } catch (err) {
      toast.error("Failed to update status")
    }
  }

  const handleViewProfile = async (applicantId: string) => {
    setDetailModalOpen(true)
    setDetailLoading(true)
    setSelectedDetail(null)
    try {
      const detail = await JobApplicationService.getApplicationDetail(applicantId)
      setSelectedDetail(detail)
    } catch (err) {
      toast.error("Failed to load application details.")
      setDetailModalOpen(false)
    } finally {
      setDetailLoading(false)
    }
  }

  const isFinalStatus = (status: ApplicationStatus) => {
    return ["HIRED", "REJECTED", "WITHDRAWN"].includes(status as string);
  }

  const formatStatusText = (status: string) => {
    return status.charAt(0).toUpperCase() + status.slice(1).toLowerCase();
  }

  const handleDownloadCv = async (url: string, filename: string) => {
    try {
      const response = await fetch(url);
      const blob = await response.blob();
      const blobUrl = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = blobUrl;
      a.download = filename || 'candidate_cv';
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      window.URL.revokeObjectURL(blobUrl);
    } catch (err) {
      console.error("Failed to download directly, falling back to new tab", err);
      window.open(url, '_blank');
    }
  }

  const getStatusColorClasses = (status: ApplicationStatus) => {
    switch (status) {
      case ApplicationStatus.APPLIED:
        return "bg-blue-50 text-blue-700 border-blue-200 focus:ring-blue-500 focus:border-blue-500"
      case ApplicationStatus.VIEWED:
        return "bg-indigo-50 text-indigo-700 border-indigo-200 focus:ring-indigo-500 focus:border-indigo-500"
      case ApplicationStatus.SHORTLISTED:
        return "bg-amber-50 text-amber-700 border-amber-200 focus:ring-amber-500 focus:border-amber-500"
      case ApplicationStatus.INTERVIEWING:
        return "bg-purple-50 text-purple-700 border-purple-200 focus:ring-purple-500 focus:border-purple-500"
      case ApplicationStatus.OFFERED:
        return "bg-emerald-50 text-emerald-700 border-emerald-200 focus:ring-emerald-500 focus:border-emerald-500"
      case ApplicationStatus.HIRED:
        return "bg-green-50 text-green-700 border-green-200 cursor-not-allowed"
      case ApplicationStatus.REJECTED:
        return "bg-rose-50 text-rose-700 border-rose-200 cursor-not-allowed"
      case ApplicationStatus.WITHDRAWN:
        return "bg-zinc-100 text-zinc-600 border-zinc-200 cursor-not-allowed"
      default:
        return "bg-zinc-50 text-zinc-700 border-zinc-200 focus:ring-zinc-500 focus:border-zinc-500"
    }
  }

  const renderStatusSelect = (applicant: ApplicantDto) => {
    const disabled = isFinalStatus(applicant.status);
    return (
      <select 
        value={applicant.status}
        disabled={disabled}
        onChange={(e) => handleStatusChange(applicant.id, e.target.value as ApplicationStatus)}
        className={`text-xs font-semibold border rounded-md py-1.5 px-2 outline-none transition-colors ${getStatusColorClasses(applicant.status)} ${!disabled && 'cursor-pointer shadow-sm hover:brightness-95'}`}
      >
        {Array.from(new Set([applicant.status, ApplicationStatus.APPLIED, ApplicationStatus.VIEWED])).map(status => (
          <option key={status} value={status} className="bg-white text-zinc-900">{formatStatusText(status as string)}</option>
        ))}
      </select>
    )
  }

  const totalPages = Math.ceil(totalCount / pageSize)

  return (
    <div className="min-h-screen bg-zinc-50 dark:bg-zinc-950 py-10 px-4 sm:px-6 lg:px-8">
      <div className="max-w-6xl mx-auto space-y-6">
        
        {/* Header Breadcrumb & Actions */}
        <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4">
          <div className="space-y-1">
            <div className="flex items-center text-sm text-zinc-500 font-medium mb-2">
              <span className="hover:text-zinc-900 cursor-pointer" onClick={() => router.push("/recruiter/dashboard")}>Dashboard</span>
              <span className="mx-2">›</span>
              <span className="hover:text-zinc-900 cursor-pointer" onClick={() => router.push("/recruiter/jobs")}>Active Jobs</span>
              <span className="mx-2">›</span>
              <span className="text-zinc-900 font-semibold">Applicants</span>
            </div>
            <h1 className="text-2xl font-black text-zinc-900 dark:text-zinc-50 tracking-tight flex items-center gap-2">
              Applicants for {jobLoading ? <span className="h-6 w-48 bg-zinc-200 rounded animate-pulse"></span> : job?.title}
            </h1>
            <p className="text-sm text-zinc-500">
              <span className="font-semibold text-zinc-700">Total: {totalCount} candidates</span>
            </p>
          </div>

          <div className="flex items-center gap-3">
            <Button variant="outline" className="bg-white border-zinc-200 text-zinc-700 font-medium gap-2">
              <Filter className="h-4 w-4" /> Filter
            </Button>
            <Button onClick={() => router.push(`/recruiter/jobs/${jobId}`)} className="bg-slate-900 hover:bg-slate-800 text-white gap-2 shadow-sm">
              <ArrowLeft className="h-4 w-4" /> Back to Job
            </Button>
          </div>
        </div>

        {/* Applicants List */}
        <Card className="border-zinc-200/80 shadow-sm overflow-hidden bg-white">
          <div className="overflow-x-auto">
            <table className="w-full text-sm text-left">
              <thead className="bg-zinc-50/80 border-b border-zinc-200/80 text-zinc-500 font-semibold text-xs uppercase tracking-wider">
                <tr>
                  <th className="px-6 py-4">Candidate Name</th>
                  <th className="px-6 py-4">Email</th>
                  <th className="px-6 py-4">Apply Date</th>
                  <th className="px-6 py-4">Current Stage</th>
                  <th className="px-6 py-4 text-right">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-zinc-100">
                {loading ? (
                  <tr>
                    <td colSpan={5} className="px-6 py-12 text-center">
                      <Loader2 className="h-6 w-6 text-blue-500 animate-spin mx-auto mb-2" />
                      <p className="text-zinc-500">Loading applicants...</p>
                    </td>
                  </tr>
                ) : error ? (
                  <tr>
                    <td colSpan={5} className="px-6 py-12 text-center text-red-500 font-medium">
                      {error}
                    </td>
                  </tr>
                ) : applicants.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="px-6 py-12 text-center">
                      <div className="flex flex-col items-center justify-center text-zinc-400 space-y-3">
                        <div className="bg-zinc-50 p-4 rounded-full">
                          <Users className="h-8 w-8 text-zinc-300" />
                        </div>
                        <p className="text-sm font-medium text-zinc-600">No applicants yet</p>
                        <p className="text-xs">Candidates who apply for this job will appear here.</p>
                      </div>
                    </td>
                  </tr>
                ) : (
                  applicants.map((applicant) => (
                    <tr key={applicant.id} className="hover:bg-zinc-50/50 transition-colors group">
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="flex items-center gap-3">
                          <div className="h-10 w-10 rounded-full bg-zinc-200 border border-zinc-300 overflow-hidden shrink-0 flex items-center justify-center">
                            {applicant.avatarUrl ? (
                              <img src={applicant.avatarUrl} alt={applicant.candidateName} className="h-full w-full object-cover" />
                            ) : (
                              <span className="font-bold text-zinc-500">{applicant.candidateName?.charAt(0) || "U"}</span>
                            )}
                          </div>
                          <span className="font-bold text-zinc-900 group-hover:text-blue-600 transition-colors">
                            {applicant.candidateName || "Unknown Candidate"}
                          </span>
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="flex items-center gap-1.5 text-zinc-500">
                          <Mail className="h-3.5 w-3.5" />
                          <span>{applicant.email}</span>
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="flex items-center gap-1.5 text-zinc-500">
                          <Calendar className="h-3.5 w-3.5" />
                          <span>{formatDate(applicant.applyDate)}</span>
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap">
                        {renderStatusSelect(applicant)}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right">
                        <Button 
                          onClick={() => handleViewProfile(applicant.id)}
                          className="bg-slate-900 hover:bg-slate-800 text-white h-8 text-xs px-3 font-semibold shadow-sm"
                        >
                          <Eye className="h-3.5 w-3.5 mr-1.5" />
                          View Profile
                        </Button>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
          
          {/* Pagination */}
          {!loading && applicants.length > 0 && totalPages > 1 && (
            <div className="border-t border-zinc-100 px-6 py-4 flex items-center justify-between">
              <div className="text-sm text-zinc-500">
                Showing <span className="font-semibold text-zinc-700">{(page - 1) * pageSize + 1}</span> to <span className="font-semibold text-zinc-700">{Math.min(page * pageSize, totalCount)}</span> of <span className="font-semibold text-zinc-700">{totalCount}</span> applicants
              </div>
              <div className="flex gap-1">
                {Array.from({ length: totalPages }).map((_, i) => (
                  <button
                    key={i}
                    onClick={() => setPage(i + 1)}
                    className={`h-8 w-8 rounded flex items-center justify-center text-sm font-medium transition-colors ${
                      page === i + 1 
                        ? "bg-zinc-900 text-white" 
                        : "text-zinc-500 hover:bg-zinc-100"
                    }`}
                  >
                    {i + 1}
                  </button>
                ))}
              </div>
            </div>
          )}
        </Card>

        {/* Application Details Modal */}
        <Dialog open={detailModalOpen} onOpenChange={setDetailModalOpen}>
          <DialogContent className="sm:max-w-[600px] bg-white">
            <DialogHeader>
              <DialogTitle className="text-xl font-bold text-zinc-900">Application Details</DialogTitle>
            </DialogHeader>
            
            <div className="py-4 space-y-6">
              {detailLoading ? (
                <div className="flex flex-col items-center justify-center py-10 space-y-3">
                  <Loader2 className="h-8 w-8 text-blue-500 animate-spin" />
                  <p className="text-sm text-zinc-500">Loading details...</p>
                </div>
              ) : selectedDetail ? (
                <>
                  <div className="flex items-center gap-4 border-b border-zinc-100 pb-4">
                    <div className="h-16 w-16 rounded-full bg-zinc-200 border border-zinc-300 overflow-hidden shrink-0 flex items-center justify-center">
                      {selectedDetail.avatarUrl ? (
                        <img src={selectedDetail.avatarUrl} alt={selectedDetail.candidateName} className="h-full w-full object-cover" />
                      ) : (
                        <span className="text-2xl font-bold text-zinc-500">{selectedDetail.candidateName?.charAt(0) || "U"}</span>
                      )}
                    </div>
                    <div>
                      <h3 className="text-lg font-bold text-zinc-900">{selectedDetail.candidateName}</h3>
                      <p className="text-sm text-zinc-500 flex items-center gap-1.5 mt-1">
                        <Mail className="h-4 w-4" /> {selectedDetail.email}
                      </p>
                    </div>
                  </div>

                  <div className="space-y-3">
                    <h4 className="text-sm font-semibold text-zinc-900">Cover Letter</h4>
                    <div className="bg-zinc-50 p-4 rounded-lg border border-zinc-200/60 text-sm text-zinc-700 whitespace-pre-wrap min-h-[100px]">
                      {selectedDetail.coverLetter || <span className="text-zinc-400 italic">No cover letter provided.</span>}
                    </div>
                  </div>

                  <div className="space-y-3">
                    <h4 className="text-sm font-semibold text-zinc-900">Resume / CV</h4>
                    {selectedDetail.cvUrl ? (
                      <div className="flex items-center justify-between bg-blue-50/50 border border-blue-100 p-4 rounded-lg">
                        <div className="flex items-center gap-3">
                          <FileText className="h-8 w-8 text-blue-600" />
                          <div>
                            <p className="text-sm font-medium text-zinc-900">{selectedDetail.cvFileName || "candidate_cv.pdf"}</p>
                            <p className="text-xs text-zinc-500">View or download attached document</p>
                          </div>
                        </div>
                        <Button variant="outline" size="sm" className="bg-white" onClick={() => handleDownloadCv(selectedDetail.cvUrl!, selectedDetail.cvFileName!)}>
                          <Download className="h-4 w-4 mr-2" /> Download
                        </Button>
                      </div>
                    ) : (
                      <div className="bg-zinc-50 p-4 rounded-lg border border-zinc-200/60 flex items-center gap-3">
                        <FileText className="h-5 w-5 text-zinc-400" />
                        <span className="text-sm text-zinc-500 italic">No CV attached.</span>
                      </div>
                    )}
                  </div>
                </>
              ) : (
                <div className="py-10 text-center text-zinc-500">Failed to load details.</div>
              )}
            </div>
          </DialogContent>
        </Dialog>
      </div>
    </div>
  )
}
