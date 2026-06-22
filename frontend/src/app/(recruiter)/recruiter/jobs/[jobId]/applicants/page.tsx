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
  Users
} from "lucide-react"

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

  const getStatusBadge = (status: ApplicationStatus) => {
    switch (status) {
      case ApplicationStatus.APPLIED:
        return <span className="inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-semibold bg-blue-50 text-blue-700 border border-blue-200">Applied</span>
      case ApplicationStatus.VIEWED:
        return <span className="inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-semibold bg-indigo-50 text-indigo-700 border border-indigo-200">Viewed</span>
      case ApplicationStatus.SHORTLISTED:
        return <span className="inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-semibold bg-amber-50 text-amber-700 border border-amber-200">Shortlisted</span>
      case ApplicationStatus.INTERVIEWING:
        return <span className="inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-semibold bg-purple-50 text-purple-700 border border-purple-200">Interviewing</span>
      case ApplicationStatus.OFFERED:
        return <span className="inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">Offered</span>
      case ApplicationStatus.HIRED:
        return <span className="inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-semibold bg-green-50 text-green-700 border border-green-200">Hired</span>
      case ApplicationStatus.REJECTED:
        return <span className="inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-semibold bg-rose-50 text-rose-700 border border-rose-200">Rejected</span>
      case ApplicationStatus.WITHDRAWN:
        return <span className="inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-semibold bg-zinc-100 text-zinc-600 border border-zinc-200">Withdrawn</span>
      default:
        return <span className="inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-semibold bg-zinc-100 text-zinc-600 border border-zinc-200">{status}</span>
    }
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
                        {getStatusBadge(applicant.status)}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-right">
                        <Button 
                          onClick={() => {
                            // Currently we don't have a specific candidate profile view implemented.
                            // You can navigate to CV view or a candidate profile view if it exists.
                            // router.push(`/recruiter/candidates/${applicant.candidateId}`);
                            alert("View profile feature coming soon!")
                          }}
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
      </div>
    </div>
  )
}
