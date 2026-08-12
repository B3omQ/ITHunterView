"use client"

import { useState, useEffect, useRef } from "react"
import { useRouter, useParams } from "next/navigation"
import { useJobMetadata, useJobDetails } from "@/hooks/useJobs"
import { useGetMyCompany } from "@/hooks/useCompany"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { ArrowLeft, Loader2, Ban, Save, Send } from "lucide-react"
import { LEVELS, WORKING_MODELS, JOB_DOMAINS, VIETNAM_PROVINCES } from "@/lib/job-constants"
// import { MajorCombobox } from "@/components/shared/MajorCombobox"
import { JobPostingRichTextEditor } from "@/components/forms/JobPostingRichTextEditor"
import {
  DEFAULT_HOW_TO_APPLY,
  parseWorkLocationText,
  serializeWorkLocationText,
  getSerializedWorkLocationLength,
} from "@/lib/job-posting-text"
import {
  normalizeRecruiterJobPostingRichTextFields,
  validateRecruiterJobPostingRichTextFields,
} from "@/lib/recruiter-job-posting-form"
import { JOB_POSTING_RICH_TEXT_LIMITS } from "@/lib/job-posting-markdown"
import { useTranslations } from "next-intl"

export default function EditJobPage() {
  const router = useRouter()
  const params = useParams()
  const id = params.jobId as string
  const t = useTranslations("RecruiterJobEdit")

  const [formData, setFormData] = useState({
    jobCode: "",
    title: "",
    location: "",
    minSalary: "",
    maxSalary: "",
    currency: "USD",
    applicationDeadline: "",
    description: "",
    incomeText: "",
    workLocation: "",
    workingHours: "",
    howToApply: DEFAULT_HOW_TO_APPLY,
    requirements: "",
    benefits: "",
    level: "",
    workingModel: "",
    jobExpertise: "",
    jobDomain: [] as string[],
  })

  const initializedRef = useRef(false)

  const { /* majors, */ loading: metadataLoading, error: metadataError } = useJobMetadata()
  const { job, loading: detailLoading, error: detailError, updateJob } = useJobDetails(id)
  const { data: company, isLoading: companyLoading } = useGetMyCompany()

  const [searchDomain, setSearchDomain] = useState("")
  const [submittingAction, setSubmittingAction] = useState<"DRAFT" | "PUBLISH" | null>(null)

  const loading = metadataLoading || detailLoading || companyLoading
  const error = metadataError || detailError

  const todayStr = new Date().toISOString().split('T')[0];

  useEffect(() => {
    if (job && !initializedRef.current) {
      initializedRef.current = true
      const parsedWorkLoc = parseWorkLocationText(job.workLocationText)

      setFormData({
        jobCode: job.jobCode || "",
        title: job.title || "",
        location: job.location || "",
        minSalary: job.minSalary ? job.minSalary.toString() : "",
        maxSalary: job.maxSalary ? job.maxSalary.toString() : "",
        currency: job.currency || "USD",
        applicationDeadline: job.applicationDeadline ? new Date(job.applicationDeadline).toISOString().split("T")[0] : "",
        description: job.description || "",
        incomeText: job.incomeText || "",
        workLocation: parsedWorkLoc.workLocation || "",
        workingHours: parsedWorkLoc.workingHours || "",
        howToApply: DEFAULT_HOW_TO_APPLY,
        requirements: job.requirements || "",
        benefits: job.benefits || "",
        level: job.level || "",
        workingModel: job.workingModel || "",
        jobExpertise: job.jobExpertise || "",
        jobDomain: job.jobDomain || [],
      })
    }
  }, [job])

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
  ) => {
    const { name, value } = e.target
    setFormData((prev) => ({ ...prev, [name]: value }))
  }

  const handleDomainChange = (domain: string) => {
    setFormData(prev => ({
      ...prev,
      jobDomain: prev.jobDomain.includes(domain)
        ? prev.jobDomain.filter(d => d !== domain)
        : [...prev.jobDomain, domain]
    }))
  }

  const validateForm = () => {
    if (!formData.title.trim()) return t("errTitleReq")
    if (!formData.location.trim()) return t("errLocReq")
    if (!formData.level) return t("errLevelReq")
    if (!formData.workingModel) return t("errModelReq")
    // if (!formData.jobExpertise) return t("errExpReq")

    const richTextError = validateRecruiterJobPostingRichTextFields(formData)
    if (richTextError) return richTextError.message

    if (!formData.workLocation.trim()) return t("errWorkLocReq")
    if (!formData.workingHours.trim()) return t("errWorkHourReq")

    const serializedLen = getSerializedWorkLocationLength({
      workLocation: formData.workLocation,
      workingHours: formData.workingHours,
      howToApply: DEFAULT_HOW_TO_APPLY,
    })
    if (serializedLen > 4000) {
      return t("errLimit")
    }

    if (!formData.applicationDeadline) return t("errExpDateReq")

    const today = new Date()
    today.setHours(0, 0, 0, 0)
    const expDate = new Date(formData.applicationDeadline)
    if (expDate <= today) {
      return t("errExpFuture")
    }

    return null
  }

  const handleSubmit = async (action: "DRAFT" | "PUBLISH") => {
    const validationError = validateForm()
    if (validationError) {
      alert(validationError)
      return
    }

    setSubmittingAction(action)
    try {
      const serializedWorkLocation = serializeWorkLocationText({
        workLocation: formData.workLocation,
        workingHours: formData.workingHours,
        howToApply: DEFAULT_HOW_TO_APPLY,
      })
      const richText = normalizeRecruiterJobPostingRichTextFields(formData)

      const payload = {
        jobCode: formData.jobCode,
        title: formData.title,
        location: formData.location,
        minSalary: formData.minSalary ? Number(formData.minSalary) : null,
        maxSalary: formData.maxSalary ? Number(formData.maxSalary) : null,
        currency: formData.currency,
        applicationDeadline: formData.applicationDeadline ? new Date(formData.applicationDeadline).toISOString() : null,
        description: richText.description,
        incomeText: richText.incomeText || "Thỏa thuận",
        workLocationText: serializedWorkLocation,
        requirements: richText.requirements,
        benefits: richText.benefits,
        level: formData.level,
        workingModel: formData.workingModel,
        jobExpertise: formData.jobExpertise,
        jobDomain: formData.jobDomain,
      }

      const res = await updateJob(payload)
      if (res.success && res.data) {
        if (action === "PUBLISH") {
          const needsAnalysis = res.data.parseStatus === "NOT_REQUESTED" || res.data.parseStatus === "STALE"
          router.push("/recruiter/jobs/" + id + "/preview" + (needsAnalysis ? "?publish=1" : ""))
        } else {
          router.push("/recruiter/jobs")
        }
      } else {
        alert(res.message || t("updateFail"))
      }
    } finally {
      setSubmittingAction(null)
    }
  }

  const filteredDomains = JOB_DOMAINS.filter(domain => domain.toLowerCase().includes(searchDomain.toLowerCase()))

  if (companyLoading || detailLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center text-muted-foreground">{t("loading")}</div>
      </div>
    )
  }

  if (!company || company.status !== 'VERIFIED') {
    return (
      <div className="max-w-2xl mx-auto mt-12 p-8 border rounded-xl bg-card text-center space-y-4">
        <h2 className="text-2xl font-bold">{t("verificationRequired")}</h2>
        <p className="text-muted-foreground">
          {t("verificationMsg")}
        </p>
        <Button onClick={() => router.push('/recruiter/company/legal')}>
          {t("completeVerification")}
        </Button>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background transition-colors duration-200">
      <div className="w-full pb-8 space-y-6">

        {/* Back Button & Header */}
        <div className="flex items-center gap-3">
          <Button
            variant="ghost"
            size="icon"
            onClick={() => router.push("/recruiter/jobs")}
            className="rounded-full hover:bg-zinc-100 dark:hover:bg-zinc-800"
          >
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-zinc-50">{t("pageTitle")}</h1>
            <p className="text-sm text-zinc-500 dark:text-zinc-400">{t("pageDesc")}</p>
          </div>
        </div>

        {error && (
          <div className="p-4 rounded-xl border border-red-200 bg-red-50 text-red-700 text-sm">
            {error}
          </div>
        )}

        {job?.isBanned && (
          <div className="flex flex-col gap-2 text-sm font-medium text-red-700 bg-red-50 dark:bg-red-950/40 p-4 rounded-xl border border-red-200 dark:border-red-900/50">
            <div className="flex items-center gap-2">
              <Ban className="h-5 w-5 shrink-0" />
              <span className="font-bold">{t("banned")}</span>
            </div>
            {job.banReason && <p className="text-red-600 dark:text-red-400 pl-7 text-xs">{t("banReason", { reason: job.banReason })}</p>}
          </div>
        )}

        <Card className="border-zinc-200/80 dark:border-zinc-800/80 shadow-xs">
          <CardHeader>
            <CardTitle className="text-lg font-bold">{t("generalInfoTitle")}</CardTitle>
            <CardDescription>{t("generalInfoDesc", { code: formData.jobCode || id })}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="space-y-2">
                <Label htmlFor="title" className="font-semibold text-zinc-700 dark:text-zinc-300">{t("jobTitle")}</Label>
                <Input
                  id="title"
                  name="title"
                  required
                  value={formData.title}
                  onChange={handleChange}
                  className="focus-visible:ring-blue-500"
                />
              </div>

              {/* 
              <div className="space-y-2">
                <Label htmlFor="jobCode" className="font-semibold text-zinc-700 dark:text-zinc-300">{t("jobCode")}</Label>
                <Input
                  id="jobCode"
                  name="jobCode"
                  value={formData.jobCode}
                  onChange={handleChange}
                  className="focus-visible:ring-blue-500"
                />
              </div>
              */}
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              <div className="space-y-2">
                <Label htmlFor="location" className="font-semibold text-zinc-700 dark:text-zinc-300">{t("location")}</Label>
                <select
                  id="location"
                  name="location"
                  required
                  className="w-full h-9 rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-1 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
                  value={formData.location}
                  onChange={handleChange}
                >
                  <option value="">{t("selectLocation")}</option>
                  {VIETNAM_PROVINCES.map((prov) => (
                    <option key={prov} value={prov}>
                      {prov}
                    </option>
                  ))}
                </select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="level" className="font-semibold text-zinc-700 dark:text-zinc-300">{t("jobLevel")}</Label>
                <select
                  id="level"
                  name="level"
                  required
                  className="w-full h-9 rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-1 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
                  value={formData.level}
                  onChange={handleChange}
                >
                  <option value="">{t("selectLevel")}</option>
                  {LEVELS.map((lvl) => (
                    <option key={lvl} value={lvl}>
                      {lvl}
                    </option>
                  ))}
                </select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="workingModel" className="font-semibold text-zinc-700 dark:text-zinc-300">{t("workingModel")}</Label>
                <select
                  id="workingModel"
                  name="workingModel"
                  required
                  className="w-full h-9 rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-1 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
                  value={formData.workingModel}
                  onChange={handleChange}
                >
                  <option value="">{t("selectModel")}</option>
                  {WORKING_MODELS.map((wm) => (
                    <option key={wm} value={wm}>
                      {wm}
                    </option>
                  ))}
                </select>
              </div>
            </div>



            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {/*
              <div className="space-y-2">
                <Label className="font-semibold text-zinc-700 dark:text-zinc-300">{t("specialization")}</Label>
                <MajorCombobox
                  majors={majors}
                  value={formData.jobExpertise}
                  onChange={(val) => setFormData(prev => ({ ...prev, jobExpertise: val }))}
                />
              </div>
              */}

              <div className="space-y-2">
                <Label className="font-semibold text-zinc-700 dark:text-zinc-300">{t("industry")}</Label>
                <Input
                  placeholder={t("filterDomain")}
                  value={searchDomain}
                  onChange={(e) => setSearchDomain(e.target.value)}
                  className="mb-2 text-xs h-8"
                />
                <div className="flex flex-wrap gap-1.5 max-h-32 overflow-y-auto p-2 border rounded-md bg-zinc-50/50 dark:bg-zinc-900/50">
                  {filteredDomains.map((dom) => {
                    const isSelected = formData.jobDomain.includes(dom)
                    return (
                      <button
                        type="button"
                        key={dom}
                        onClick={() => handleDomainChange(dom)}
                        className={`text-xs px-2.5 py-1 rounded-full border transition-all ${
                          isSelected
                            ? "bg-blue-600 text-white border-blue-600 font-medium"
                            : "bg-white dark:bg-zinc-950 text-zinc-600 dark:text-zinc-400 border-zinc-200 dark:border-zinc-800 hover:border-zinc-300"
                        }`}
                      >
                        {isSelected ? "✓ " : "+ "}{dom}
                      </button>
                    )
                  })}
                </div>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="description" className="font-semibold text-zinc-700 dark:text-zinc-300">{t("jobDesc")}</Label>
              <JobPostingRichTextEditor
                id="description"
                required
                value={formData.description}
                maxLength={JOB_POSTING_RICH_TEXT_LIMITS.description}
                disabled={loading || submittingAction !== null}
                onChange={(description) => setFormData((previous) => ({ ...previous, description }))}
              />
            </div>

            {/* Compensation & Additional Details */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              <div className="space-y-2">
                <Label htmlFor="minSalary" className="font-semibold text-zinc-700 dark:text-zinc-300">{t("minSalary")}</Label>
                <Input
                  id="minSalary"
                  name="minSalary"
                  type="number"
                  value={formData.minSalary}
                  onChange={handleChange}
                  className="focus-visible:ring-blue-500"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="maxSalary" className="font-semibold text-zinc-700 dark:text-zinc-300">{t("maxSalary")}</Label>
                <Input
                  id="maxSalary"
                  name="maxSalary"
                  type="number"
                  value={formData.maxSalary}
                  onChange={handleChange}
                  className="focus-visible:ring-blue-500"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="applicationDeadline" className="font-semibold text-zinc-700 dark:text-zinc-300">{t("applicationDeadline")}</Label>
                <Input
                  id="applicationDeadline"
                  name="applicationDeadline"
                  type="date"
                  min={todayStr}
                  value={formData.applicationDeadline}
                  onChange={handleChange}
                  className="focus-visible:ring-blue-500"
                />
                <p 
                  className="text-[11.5px] text-zinc-500 dark:text-zinc-400 mt-1 italic leading-relaxed"
                  dangerouslySetInnerHTML={{ __html: t("sysVisibilityNotice") }}
                />
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              <div className="space-y-2">
                <Label htmlFor="incomeText" className="font-semibold text-zinc-700 dark:text-zinc-300">{t("incomeDetails")}</Label>
                <JobPostingRichTextEditor
                  id="incomeText"
                  placeholder={t("incomePlaceholder")}
                  value={formData.incomeText}
                  maxLength={JOB_POSTING_RICH_TEXT_LIMITS.incomeText}
                  disabled={loading || submittingAction !== null}
                  onChange={(incomeText) => setFormData((previous) => ({ ...previous, incomeText }))}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="workLocation" className="font-semibold text-zinc-700 dark:text-zinc-300">{t("workLocation")}</Label>
                <textarea
                  id="workLocation"
                  name="workLocation"
                  rows={2}
                  required
                  className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={formData.workLocation}
                  onChange={handleChange}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="workingHours" className="font-semibold text-zinc-700 dark:text-zinc-300">{t("workingHours")}</Label>
                <textarea
                  id="workingHours"
                  name="workingHours"
                  rows={2}
                  required
                  className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={formData.workingHours}
                  onChange={handleChange}
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="requirements" className="font-semibold text-zinc-700 dark:text-zinc-300">{t("requirements")}</Label>
              <JobPostingRichTextEditor
                id="requirements"
                required
                value={formData.requirements}
                maxLength={JOB_POSTING_RICH_TEXT_LIMITS.requirements}
                disabled={loading || submittingAction !== null}
                onChange={(requirements) => setFormData((previous) => ({ ...previous, requirements }))}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="benefits" className="font-semibold text-zinc-700 dark:text-zinc-300">{t("benefits")}</Label>
              <JobPostingRichTextEditor
                id="benefits"
                required
                value={formData.benefits}
                maxLength={JOB_POSTING_RICH_TEXT_LIMITS.benefits}
                disabled={loading || submittingAction !== null}
                onChange={(benefits) => setFormData((previous) => ({ ...previous, benefits }))}
              />
            </div>
          </CardContent>
        </Card>

        {/* Action Buttons */}
        <div className="flex justify-end gap-3 pt-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => router.push("/recruiter/jobs")}
            disabled={loading}
          >
            {t("cancel")}
          </Button>

          <Button
            type="button"
            variant="secondary"
            onClick={() => handleSubmit("DRAFT")}
            disabled={loading || submittingAction !== null}
            className="bg-slate-200 hover:bg-slate-300 text-slate-800"
          >
            {submittingAction === "DRAFT" ? (
              <Loader2 className="w-4 h-4 mr-2 animate-spin" />
            ) : (
              <Save className="w-4 h-4 mr-2" />
            )}
            {t("saveDraft")}
          </Button>

          <Button
            type="button"
            onClick={() => handleSubmit("PUBLISH")}
            disabled={loading || submittingAction !== null}
            className="bg-indigo-600 hover:bg-indigo-700 text-white font-medium shadow-md"
          >
            {submittingAction === "PUBLISH" ? (
              <Loader2 className="w-4 h-4 mr-2 animate-spin" />
            ) : (
              <Send className="w-4 h-4 mr-2" />
            )}
            {t("publish")}
          </Button>
        </div>
      </div>
    </div>
  )
}
