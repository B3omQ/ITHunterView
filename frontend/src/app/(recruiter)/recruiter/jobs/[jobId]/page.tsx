"use client"

import { useRouter, useParams } from "next/navigation"
import { useJobDetails } from "@/hooks/useJobs"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { 
  ArrowLeft, 
  MapPin, 
  DollarSign, 
  Calendar, 
  FileText, 
  Pencil,
  Loader2,
  ListTodo,
  Award,
  Monitor,
  Layers,
  Target
} from "lucide-react"
import { MatchCvsSection } from "@/components/recruiter/MatchCvsSection"
import { JobPostingMarkdownContent } from "@/components/jobs/JobPostingMarkdownContent"
import { WorkLocationScheduleContent } from "@/components/jobs/WorkLocationScheduleContent"
import type { JobSkillRequirement } from "@/services/recruiter.service"
import { useTranslations } from "next-intl"

export default function JobDetailPage() {
  const router = useRouter()
  const params = useParams()
  const id = params.jobId as string
  const t = useTranslations("RecruiterJobDetail")

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
        return <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-900/50">{t("statusActive")}</span>
      case "DRAFT":
        return <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-zinc-50 text-zinc-600 dark:bg-zinc-800/40 dark:text-zinc-400 border border-zinc-200 dark:border-zinc-700/50">{t("statusDraft")}</span>
      case "CLOSED":
        return <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-rose-50 text-rose-600 dark:bg-rose-950/40 dark:text-rose-400 border border-rose-200 dark:border-rose-900/50">{t("statusClosed")}</span>
      default:
        return <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-zinc-50 text-zinc-600 dark:bg-zinc-800/40 dark:text-zinc-400 border border-zinc-200 dark:border-zinc-700/50">{status}</span>
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center space-y-2">
          <Loader2 className="h-8 w-8 text-blue-500 animate-spin mx-auto" />
          <p className="text-sm text-zinc-500 dark:text-zinc-400">{t("loading")}</p>
        </div>
      </div>
    )
  }

  if (error || !job) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background py-10 px-4">
        <div className="text-center max-w-md bg-white dark:bg-zinc-900 p-6 rounded-2xl border border-zinc-200 dark:border-zinc-800 shadow-sm">
          <p className="text-red-500 font-semibold mb-4">{error || t("notFound")}</p>
          <Button onClick={() => router.push("/recruiter/jobs")} className="bg-blue-600 hover:bg-blue-700 text-white">
            {t("goBack")}
          </Button>
        </div>
      </div>
    )
  }

  const mustHaveSkills = job.skills?.filter((skill: JobSkillRequirement) => skill.isMandatory) || []
  const niceToHaveSkills = job.skills?.filter((skill: JobSkillRequirement) => !skill.isMandatory) || []

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-8 space-y-6">
        
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
              <span className="text-xs font-mono text-zinc-400">{t("jobCode", { code: job.jobCode })}</span>
              <h1 className="text-xl font-extrabold text-zinc-900 dark:text-zinc-50 tracking-tight">{t("title")}</h1>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <Button
              onClick={() => router.push(`/recruiter/jobs/${job.id}/edit`)}
              className="bg-blue-600 hover:bg-blue-700 text-white font-medium shadow-md shadow-blue-500/10 gap-1.5"
            >
              <Pencil className="h-4 w-4" />
              {t("edit")}
            </Button>
          </div>
        </div>

        {/* Main Details Card */}
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
                  <span className="text-[10px] uppercase font-bold text-zinc-400 block tracking-wider">{t("locTitle")}</span>
                  <span className="text-sm font-semibold text-zinc-700 dark:text-zinc-300">{job.location}</span>
                </div>
              </div>

              <div className="flex items-start gap-2.5">
                <DollarSign className="h-5 w-5 text-amber-500 mt-0.5 shrink-0" />
                <div>
                  <span className="text-[10px] uppercase font-bold text-zinc-400 block tracking-wider">{t("salaryTitle")}</span>
                  <span className="text-sm font-semibold text-zinc-700 dark:text-zinc-300">
                    {job.minSalary || job.maxSalary
                      ? `${job.minSalary?.toLocaleString() || "0"} - ${job.maxSalary?.toLocaleString() || "∞"} ${job.currency}`
                      : t("salaryNego")}
                  </span>
                </div>
              </div>

              <div className="flex items-start gap-2.5">
                <Calendar className="h-5 w-5 text-purple-500 mt-0.5 shrink-0" />
                <div>
                  <span className="text-[10px] uppercase font-bold text-zinc-400 block tracking-wider">{t("pubDateTitle")}</span>
                  <span className="text-sm font-semibold text-zinc-700 dark:text-zinc-300">{formatDate(job.publishedAt || job.createdAt)}</span>
                </div>
              </div>

              {job.level && (
                <div className="flex items-start gap-2.5">
                  <Award className="h-5 w-5 text-indigo-500 mt-0.5 shrink-0" />
                  <div>
                    <span className="text-[10px] uppercase font-bold text-zinc-400 block tracking-wider">{t("levelTitle")}</span>
                    <span className="text-sm font-semibold text-zinc-700 dark:text-zinc-300">{job.level}</span>
                  </div>
                </div>
              )}

              {job.workingModel && (
                <div className="flex items-start gap-2.5">
                  <Monitor className="h-5 w-5 text-cyan-500 mt-0.5 shrink-0" />
                  <div>
                    <span className="text-[10px] uppercase font-bold text-zinc-400 block tracking-wider">{t("modelTitle")}</span>
                    <span className="text-sm font-semibold text-zinc-700 dark:text-zinc-300">{job.workingModel}</span>
                  </div>
                </div>
              )}

              {job.jobExpertise && (
                <div className="flex items-start gap-2.5">
                  <Target className="h-5 w-5 text-rose-500 mt-0.5 shrink-0" />
                  <div>
                    <span className="text-[10px] uppercase font-bold text-zinc-400 block tracking-wider">{t("expTitle")}</span>
                    <span className="text-sm font-semibold text-zinc-700 dark:text-zinc-300">{job.jobExpertise}</span>
                  </div>
                </div>
              )}

              {job.jobDomain && job.jobDomain.length > 0 && (
                <div className="flex items-start gap-2.5">
                  <Layers className="h-5 w-5 text-fuchsia-500 mt-0.5 shrink-0" />
                  <div>
                    <span className="text-[10px] uppercase font-bold text-zinc-400 block tracking-wider">{t("domainTitle")}</span>
                    <div className="flex flex-wrap gap-1 mt-1">
                      {job.jobDomain.map((domain: string, index: number) => (
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
                {t("stdSkills")}
              </h3>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* Must-have skills list */}
                <div className="border border-zinc-200/80 dark:border-zinc-800/80 rounded-xl p-4 bg-zinc-50/30 dark:bg-zinc-900/10">
                  <span className="block text-xs font-bold text-blue-600 dark:text-blue-400 mb-2 uppercase tracking-wide">{t("mustHave")}</span>
                  <div className="flex flex-wrap gap-1.5">
                    {mustHaveSkills.length > 0 ? (
                      mustHaveSkills.map((s: JobSkillRequirement) => (
                        <span key={s.skillId} className="inline-flex items-center bg-blue-50 text-blue-700 border border-blue-200 dark:bg-blue-950/40 dark:text-blue-400 dark:border-blue-900/50 px-2.5 py-1 rounded-md text-xs font-semibold">
                          {s.skillName}
                        </span>
                      ))
                    ) : (
                      <span className="text-xs text-zinc-400 italic">{t("noMustHave")}</span>
                    )}
                  </div>
                </div>

                {/* Nice-to-have skills list */}
                <div className="border border-zinc-200/80 dark:border-zinc-800/80 rounded-xl p-4 bg-zinc-50/30 dark:bg-zinc-900/10">
                  <span className="block text-xs font-bold text-emerald-600 dark:text-emerald-400 mb-2 uppercase tracking-wide">{t("niceHave")}</span>
                  <div className="flex flex-wrap gap-1.5">
                    {niceToHaveSkills.length > 0 ? (
                      niceToHaveSkills.map((s: JobSkillRequirement) => (
                        <span key={s.skillId} className="inline-flex items-center bg-emerald-50 text-emerald-700 border border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-400 dark:border-emerald-900/50 px-2.5 py-1 rounded-md text-xs font-semibold">
                          {s.skillName}
                        </span>
                      ))
                    ) : (
                      <span className="text-xs text-zinc-400 italic">{t("noNiceHave")}</span>
                    )}
                  </div>
                </div>
              </div>
            </div>

            {/* Markdown/Sections content */}
            <div className="space-y-6">
              {job.description && (
                <div>
                  <h3 className="text-sm font-bold uppercase tracking-wider text-zinc-400 mb-2 flex items-center gap-1.5">
                    <FileText className="h-4 w-4 text-blue-500" /> {t("descTitle")}
                  </h3>
                  <JobPostingMarkdownContent value={job.description} legacyMode="bullet" />
                </div>
              )}

              {job.incomeText && (
                <div>
                  <h3 className="text-sm font-bold uppercase tracking-wider text-zinc-400 mb-2">{t("incomeTitle")}</h3>
                  <JobPostingMarkdownContent value={job.incomeText} legacyMode="lines" />
                </div>
              )}

              {job.workLocationText && (
                <div>
                  <h3 className="text-sm font-bold uppercase tracking-wider text-zinc-400 mb-2">{t("workLocTitle")}</h3>
                  <WorkLocationScheduleContent workLocationText={job.workLocationText} />
                </div>
              )}

              {job.requirements && (
                <div>
                  <h3 className="text-sm font-bold uppercase tracking-wider text-zinc-400 mb-2">{t("reqTitle")}</h3>
                  <JobPostingMarkdownContent value={job.requirements} legacyMode="bullet" />
                </div>
              )}

              {job.benefits && (
                <div>
                  <h3 className="text-sm font-bold uppercase tracking-wider text-zinc-400 mb-2">{t("benTitle")}</h3>
                  <JobPostingMarkdownContent value={job.benefits} legacyMode="bullet" />
                </div>
              )}
            </div>

          </CardContent>
        </Card>

        {/* Suggested Candidates Match Section */}
        <MatchCvsSection jobId={job.id} jobStatus={job.status} jobParseStatus={job.parseStatus} />

      </div>
    </div>
  )
}
