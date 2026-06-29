"use client"

import { useRouter, useParams } from "next/navigation"
import { useJobDetails } from "@/hooks/useJobs"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { 
  ArrowLeft, 
  Briefcase, 
  MapPin, 
  DollarSign, 
  Calendar, 
  FileText, 
  CheckCircle,
  Pencil,
  Loader2,
  ListTodo,
  Users,
  Award,
  Monitor,
  Layers,
  Target
} from "lucide-react"

export default function JobDetailPage() {
  const router = useRouter()
  const params = useParams()
  const id = params.jobId as string

  const { job, loading, error } = useJobDetails(id)

  const formatDate = (dateStr: string) => {
    if (!dateStr) return "N/A"
    return new Date(dateStr).toLocaleDateString("en-US", {
      year: "numeric",
      month: "long",
      day: "numeric",
    })
  }

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "PUBLISHED":
        return <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-900/50">Active</span>
      case "DRAFT":
        return <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-zinc-50 text-zinc-600 dark:bg-zinc-800/40 dark:text-zinc-400 border border-zinc-200 dark:border-zinc-700/50">Draft</span>
      case "CLOSED":
        return <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-rose-50 text-rose-600 dark:bg-rose-950/40 dark:text-rose-400 border border-rose-200 dark:border-rose-900/50">Closed</span>
      default:
        return <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-zinc-50 text-zinc-600 dark:bg-zinc-800/40 dark:text-zinc-400 border border-zinc-200 dark:border-zinc-700/50">{status}</span>
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center space-y-2">
          <Loader2 className="h-8 w-8 text-blue-500 animate-spin mx-auto" />
          <p className="text-sm text-zinc-500 dark:text-zinc-400">Loading Job Details...</p>
        </div>
      </div>
    )
  }

  if (error || !job) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background py-10 px-4">
        <div className="text-center max-w-md bg-white dark:bg-zinc-900 p-6 rounded-2xl border border-zinc-200 dark:border-zinc-800 shadow-sm">
          <p className="text-red-500 font-semibold mb-4">{error || "Job Posting not found."}</p>
          <Button onClick={() => router.push("/recruiter/jobs")} className="bg-blue-600 hover:bg-blue-700 text-white">
            Go back to Jobs List
          </Button>
        </div>
      </div>
    )
  }

  const mustHaveSkills = job.skills?.filter((s: any) => s.isMandatory) || []
  const niceToHaveSkills = job.skills?.filter((s: any) => !s.isMandatory) || []

  return (
    <div className="min-h-screen bg-background py-10 px-4 sm:px-6 lg:px-8 transition-colors duration-200">
      <div className="max-w-4xl mx-auto space-y-6">
        
        {/* Back Button & Action Toolbar */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <Button 
              variant="ghost" 
              size="icon" 
              onClick={() => router.push("/recruiter/jobs")}
              className="rounded-full bg-white dark:bg-zinc-900 border border-zinc-200/80 dark:border-zinc-800/80 shadow-xs"
            >
              <ArrowLeft className="h-5 w-5 text-zinc-600 dark:text-zinc-400" />
            </Button>
            <div>
              <span className="text-xs font-mono text-zinc-400">Job Code: {job.jobCode}</span>
              <h1 className="text-xl font-extrabold text-zinc-900 dark:text-zinc-50 tracking-tight">Job Posting Details</h1>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <Button
              onClick={() => router.push(`/recruiter/jobs/${job.id}/applicants`)}
              variant="outline"
              className="bg-white hover:bg-zinc-100 text-zinc-700 font-medium shadow-sm border-zinc-200 gap-1.5"
            >
              <Users className="h-4 w-4" />
              View Applicants
            </Button>
            <Button
              onClick={() => router.push(`/recruiter/jobs/${job.id}/edit`)}
              className="bg-blue-600 hover:bg-blue-700 text-white font-medium shadow-md shadow-blue-500/10 gap-1.5"
            >
              <Pencil className="h-4 w-4" />
              Edit Posting
            </Button>
          </div>
        </div>

        {/* Main Details Card with Premium Blue Accent Top Border */}
        <Card className="border-zinc-200/80 dark:border-zinc-800/80 shadow-xs overflow-hidden relative border-t-4 border-t-blue-600">
          <CardContent className="p-8 space-y-8">
            
            {/* Title Block */}
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-zinc-200/60 dark:border-zinc-800/60 pb-6">
              <div>
                <h2 className="text-2xl font-black text-zinc-900 dark:text-zinc-50 leading-tight">{job.title}</h2>
                <div className="flex items-center gap-2 mt-2">
                  {getStatusBadge(job.status)}

                </div>
              </div>
            </div>

            {/* Quick Stats Grid */}
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4 bg-zinc-50 dark:bg-zinc-900/30 p-5 rounded-2xl border border-zinc-200/60 dark:border-zinc-800/60">


              <div className="flex items-start gap-2.5">
                <MapPin className="h-5 w-5 text-emerald-500 mt-0.5 shrink-0" />
                <div>
                  <span className="text-[10px] uppercase font-bold text-zinc-400 block tracking-wider">Location</span>
                  <span className="text-sm font-semibold text-zinc-700 dark:text-zinc-300">{job.detailedLocation || job.provinceCode}</span>
                </div>
              </div>

              <div className="flex items-start gap-2.5">
                <DollarSign className="h-5 w-5 text-amber-500 mt-0.5 shrink-0" />
                <div>
                  <span className="text-[10px] uppercase font-bold text-zinc-400 block tracking-wider">Salary Range</span>
                  <span className="text-sm font-semibold text-zinc-700 dark:text-zinc-300">
                    {job.minSalary || job.maxSalary
                      ? `${job.minSalary?.toLocaleString() || "0"} - ${job.maxSalary?.toLocaleString() || "∞"} ${job.currency}`
                      : "Negotiable"}
                  </span>
                </div>
              </div>

              <div className="flex items-start gap-2.5">
                <Calendar className="h-5 w-5 text-purple-500 mt-0.5 shrink-0" />
                <div>
                  <span className="text-[10px] uppercase font-bold text-zinc-400 block tracking-wider">Published Date</span>
                  <span className="text-sm font-semibold text-zinc-700 dark:text-zinc-300">{formatDate(job.publishedAt || job.createdAt)}</span>
                </div>
              </div>

              {job.level && (
                <div className="flex items-start gap-2.5">
                  <Award className="h-5 w-5 text-indigo-500 mt-0.5 shrink-0" />
                  <div>
                    <span className="text-[10px] uppercase font-bold text-zinc-400 block tracking-wider">Level</span>
                    <span className="text-sm font-semibold text-zinc-700 dark:text-zinc-300">{job.level}</span>
                  </div>
                </div>
              )}

              {job.workingModel && (
                <div className="flex items-start gap-2.5">
                  <Monitor className="h-5 w-5 text-cyan-500 mt-0.5 shrink-0" />
                  <div>
                    <span className="text-[10px] uppercase font-bold text-zinc-400 block tracking-wider">Working Model</span>
                    <span className="text-sm font-semibold text-zinc-700 dark:text-zinc-300">{job.workingModel}</span>
                  </div>
                </div>
              )}

              {job.jobExpertise && (
                <div className="flex items-start gap-2.5">
                  <Target className="h-5 w-5 text-rose-500 mt-0.5 shrink-0" />
                  <div>
                    <span className="text-[10px] uppercase font-bold text-zinc-400 block tracking-wider">Expertise</span>
                    <span className="text-sm font-semibold text-zinc-700 dark:text-zinc-300">{job.jobExpertise}</span>
                  </div>
                </div>
              )}

              {job.jobDomain && job.jobDomain.length > 0 && (
                <div className="flex items-start gap-2.5">
                  <Layers className="h-5 w-5 text-fuchsia-500 mt-0.5 shrink-0" />
                  <div>
                    <span className="text-[10px] uppercase font-bold text-zinc-400 block tracking-wider">Domain</span>
                    <div className="flex flex-wrap gap-1 mt-1">
                      {job.jobDomain.map((domain, index) => (
                        <span key={index} className="inline-flex px-2 py-0.5 rounded text-xs font-medium bg-zinc-100 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300 border border-zinc-200 dark:border-zinc-700">
                          {domain}
                        </span>
                      ))}
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* Standardized Skill Requirements */}
            <div className="space-y-3">
              <h3 className="text-sm font-bold uppercase tracking-wider text-zinc-400 flex items-center gap-1.5">
                <ListTodo className="h-4 w-4 text-blue-500" />
                Standardized Skill Requirements
              </h3>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* Must-have skills list */}
                <div className="border border-zinc-200/80 dark:border-zinc-800/80 rounded-xl p-4 bg-zinc-50/30 dark:bg-zinc-900/10">
                  <span className="block text-xs font-bold text-blue-600 dark:text-blue-400 mb-2 uppercase tracking-wide">Must-have Skills</span>
                  <div className="flex flex-wrap gap-1.5">
                    {mustHaveSkills.length > 0 ? (
                      mustHaveSkills.map((s: any) => (
                        <span key={s.skillId} className="inline-flex items-center bg-blue-50 text-blue-700 border border-blue-200 dark:bg-blue-950/40 dark:text-blue-400 dark:border-blue-900/50 px-2.5 py-1 rounded-md text-xs font-semibold">
                          {s.skillName}
                        </span>
                      ))
                    ) : (
                      <span className="text-xs text-zinc-400 italic">No Must-have skills specified.</span>
                    )}
                  </div>
                </div>

                {/* Nice-to-have skills list */}
                <div className="border border-zinc-200/80 dark:border-zinc-800/80 rounded-xl p-4 bg-zinc-50/30 dark:bg-zinc-900/10">
                  <span className="block text-xs font-bold text-emerald-600 dark:text-emerald-400 mb-2 uppercase tracking-wide">Nice-to-have Skills</span>
                  <div className="flex flex-wrap gap-1.5">
                    {niceToHaveSkills.length > 0 ? (
                      niceToHaveSkills.map((s: any) => (
                        <span key={s.skillId} className="inline-flex items-center bg-emerald-50 text-emerald-700 border border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-400 dark:border-emerald-900/50 px-2.5 py-1 rounded-md text-xs font-semibold">
                          {s.skillName}
                        </span>
                      ))
                    ) : (
                      <span className="text-xs text-zinc-400 italic">No Nice-to-have skills specified.</span>
                    )}
                  </div>
                </div>
              </div>
            </div>

            {/* Markdown/Sections content */}
            <div className="space-y-6">
              <div>
                <h3 className="text-sm font-bold uppercase tracking-wider text-zinc-400 mb-2 flex items-center gap-1.5">
                  <FileText className="h-4 w-4 text-blue-500" /> Job Description
                </h3>
                <p className="text-sm text-zinc-700 dark:text-zinc-300 whitespace-pre-wrap leading-relaxed">
                  {job.description || "No description provided."}
                </p>
              </div>

              {job.responsibilities && (
                <div>
                  <h3 className="text-sm font-bold uppercase tracking-wider text-zinc-400 mb-2">Key Responsibilities</h3>
                  <p className="text-sm text-zinc-700 dark:text-zinc-300 whitespace-pre-wrap leading-relaxed">
                    {job.responsibilities}
                  </p>
                </div>
              )}

              {job.requirements && (
                <div>
                  <h3 className="text-sm font-bold uppercase tracking-wider text-zinc-400 mb-2">Requirements</h3>
                  <p className="text-sm text-zinc-700 dark:text-zinc-300 whitespace-pre-wrap leading-relaxed">
                    {job.requirements}
                  </p>
                </div>
              )}

              {job.benefits && (
                <div>
                  <h3 className="text-sm font-bold uppercase tracking-wider text-zinc-400 mb-2">Benefits & Perks</h3>
                  <p className="text-sm text-zinc-700 dark:text-zinc-300 whitespace-pre-wrap leading-relaxed">
                    {job.benefits}
                  </p>
                </div>
              )}
            </div>

          </CardContent>
        </Card>
      </div>
    </div>
  )
}
