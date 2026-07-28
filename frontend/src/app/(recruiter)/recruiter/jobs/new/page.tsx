"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { useJobMetadata, useJobDetails } from "@/hooks/useJobs"
import { useGetMyCompany } from "@/hooks/useCompany"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { AlertCircle, ArrowLeft, Loader2, Save, Send } from "lucide-react"
import { LEVELS, WORKING_MODELS, JOB_DOMAINS, VIETNAM_PROVINCES } from "@/lib/job-constants"
import { MajorCombobox } from "@/components/shared/MajorCombobox"
import { JobPostingRichTextEditor } from "@/components/forms/JobPostingRichTextEditor"
import {
  DEFAULT_HOW_TO_APPLY,
  serializeWorkLocationText,
  getSerializedWorkLocationLength,
} from "@/lib/job-posting-text"
import {
  normalizeRecruiterJobPostingRichTextFields,
  validateRecruiterJobPostingRichTextFields,
} from "@/lib/recruiter-job-posting-form"
import { JOB_POSTING_RICH_TEXT_LIMITS } from "@/lib/job-posting-markdown"
import { useWalletBalance } from "@/hooks/useWallet"

export default function CreateJobPage() {
  const router = useRouter()
  const { data: company, isLoading: companyLoading } = useGetMyCompany()
  const { data: walletRes } = useWalletBalance()
  const walletData = walletRes?.data
  const jobSlotsLimit = walletData?.jobSlotsLimit ?? 1
  const jobSlotsUsed = walletData?.jobSlotsUsed ?? 0
  const isSlotFull = jobSlotsLimit !== -1 && jobSlotsUsed >= jobSlotsLimit

  const [formData, setFormData] = useState({
    jobCode: "",
    title: "",
    location: "",
    minSalary: "",
    maxSalary: "",
    currency: "USD",
    expiresAt: "",
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

  const { majors, loading: metadataLoading, error: metadataError } = useJobMetadata()
  const { createJob, saving, error: saveError } = useJobDetails()

  const [searchDomain, setSearchDomain] = useState("")
  const [submittingAction, setSubmittingAction] = useState<"DRAFT" | "PUBLISH" | null>(null)

  const loading = metadataLoading || saving || companyLoading
  const error = metadataError || saveError

  const todayStr = new Date().toISOString().split('T')[0];
  const maxDateObj = new Date();
  maxDateObj.setDate(maxDateObj.getDate() + 30);
  const maxDateStr = maxDateObj.toISOString().split('T')[0];

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
    if (!formData.title.trim()) return "Job Title is required"
    if (!formData.location.trim()) return "Location is required"
    if (!formData.level) return "Level is required"
    if (!formData.workingModel) return "Working Model is required"
    if (!formData.jobExpertise) return "Specialization (Expertise) is required"

    const richTextError = validateRecruiterJobPostingRichTextFields(formData)
    if (richTextError) return richTextError.message

    if (!formData.workLocation.trim()) return "Work location is required"
    if (!formData.workingHours.trim()) return "Working hours are required"
    if (!formData.howToApply.trim()) return "How to apply is required"

    const serializedLen = getSerializedWorkLocationLength({
      workLocation: formData.workLocation,
      workingHours: formData.workingHours,
      howToApply: formData.howToApply,
    })
    if (serializedLen > 4000) {
      return "Work location details must not exceed 4,000 characters after formatting"
    }

    if (!formData.expiresAt) return "Expiration Date is required"

    const today = new Date()
    today.setHours(0, 0, 0, 0)
    const expDate = new Date(formData.expiresAt)
    if (expDate <= today) {
      return "Expiration Date must be in the future (after today)"
    }

    const maxExpDate = new Date(today)
    maxExpDate.setDate(maxExpDate.getDate() + 30)
    if (expDate > maxExpDate) {
      return "Expiration Date cannot exceed 30 days from today"
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
        howToApply: formData.howToApply,
      })
      const richText = normalizeRecruiterJobPostingRichTextFields(formData)

      const payload = {
        jobCode: formData.jobCode,
        title: formData.title,
        location: formData.location,
        minSalary: formData.minSalary ? Number(formData.minSalary) : null,
        maxSalary: formData.maxSalary ? Number(formData.maxSalary) : null,
        currency: formData.currency,
        expiresAt: formData.expiresAt ? new Date(formData.expiresAt).toISOString() : null,
        description: richText.description,
        incomeText: richText.incomeText,
        workLocationText: serializedWorkLocation,
        requirements: richText.requirements,
        benefits: richText.benefits,
        level: formData.level,
        workingModel: formData.workingModel,
        jobExpertise: formData.jobExpertise,
        jobDomain: formData.jobDomain,
      }

      const res = await createJob(payload)
      if (res.success && res.data) {
        const createdJobId = res.data.id
        if (action === "PUBLISH") {
          const needsAnalysis = res.data.parseStatus === "NOT_REQUESTED" || res.data.parseStatus === "STALE"
          router.push("/recruiter/jobs/" + createdJobId + "/preview" + (needsAnalysis ? "?publish=1" : ""))
        } else {
          router.push("/recruiter/jobs")
        }
      } else {
        alert(res.message || "Failed to create job draft")
      }
    } finally {
      setSubmittingAction(null)
    }
  }

  const filteredDomains = JOB_DOMAINS.filter(domain => domain.toLowerCase().includes(searchDomain.toLowerCase()))

  if (companyLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center text-muted-foreground">Checking company status...</div>
      </div>
    )
  }

  if (!company || company.status !== 'VERIFIED') {
    return (
      <div className="max-w-2xl mx-auto mt-12 p-8 border rounded-xl bg-card text-center space-y-4">
        <div className="mx-auto w-16 h-16 bg-amber-100 text-amber-600 rounded-full flex items-center justify-center mb-4">
          <svg xmlns="http://www.w3.org/2000/svg" className="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
        </div>
        <h2 className="text-2xl font-bold">Verification Required</h2>
        <p className="text-muted-foreground">
          Your company needs to be verified before you can post new jobs.
          Please complete your Legal Verification and wait for admin approval.
        </p>
        <div className="pt-4 flex justify-center gap-4">
          <Button variant="outline" onClick={() => router.push('/recruiter/dashboard')}>
            Return to Dashboard
          </Button>
          <Button onClick={() => router.push('/recruiter/company/legal')}>
            Complete Verification
          </Button>
        </div>
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
            <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-zinc-50">Tạo tin tuyển dụng mới (Bản thảo)</h1>
            <p className="text-sm text-zinc-500 dark:text-zinc-400">Nhập thông tin công việc, sau đó chuyển sang giao diện AI Preview để kiểm tra & trích xuất kỹ năng.</p>
          </div>
        </div>

        {error && (
          <div className="p-4 rounded-xl border border-red-200 bg-red-50 text-red-700 text-sm">
            {error}
          </div>
        )}

        {/* Main Job Information Card */}
        <Card className="border-zinc-200/80 dark:border-zinc-800/80 shadow-xs">
          <CardHeader>
            <CardTitle className="text-lg font-bold">Thông tin chung về vị trí tuyển dụng</CardTitle>
            <CardDescription>Cung cấp các thông tin cơ bản về công việc.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="space-y-2">
                <Label htmlFor="title" className="font-semibold text-zinc-700 dark:text-zinc-300">Job Title *</Label>
                <Input
                  id="title"
                  name="title"
                  required
                  placeholder="e.g. Senior Fullstack Engineer (React/Node.js)"
                  value={formData.title}
                  onChange={handleChange}
                  className="focus-visible:ring-blue-500"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="jobCode" className="font-semibold text-zinc-700 dark:text-zinc-300">Job Code (Optional)</Label>
                <Input
                  id="jobCode"
                  name="jobCode"
                  placeholder="Auto-generated if left empty"
                  value={formData.jobCode}
                  onChange={handleChange}
                  className="focus-visible:ring-blue-500"
                />
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              <div className="space-y-2">
                <Label htmlFor="location" className="font-semibold text-zinc-700 dark:text-zinc-300">Primary Location (City) *</Label>
                <select
                  id="location"
                  name="location"
                  required
                  className="w-full h-9 rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-1 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
                  value={formData.location}
                  onChange={handleChange}
                >
                  <option value="">Select City / Province</option>
                  {VIETNAM_PROVINCES.map((prov) => (
                    <option key={prov} value={prov}>
                      {prov}
                    </option>
                  ))}
                </select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="level" className="font-semibold text-zinc-700 dark:text-zinc-300">Job Level *</Label>
                <select
                  id="level"
                  name="level"
                  required
                  className="w-full h-9 rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-1 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
                  value={formData.level}
                  onChange={handleChange}
                >
                  <option value="">Select Job Level</option>
                  {LEVELS.map((lvl) => (
                    <option key={lvl} value={lvl}>
                      {lvl}
                    </option>
                  ))}
                </select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="workingModel" className="font-semibold text-zinc-700 dark:text-zinc-300">Working Model *</Label>
                <select
                  id="workingModel"
                  name="workingModel"
                  required
                  className="w-full h-9 rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-1 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
                  value={formData.workingModel}
                  onChange={handleChange}
                >
                  <option value="">Select Working Model</option>
                  {WORKING_MODELS.map((wm) => (
                    <option key={wm} value={wm}>
                      {wm}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="howToApply" className="font-semibold text-zinc-700 dark:text-zinc-300">How to Apply *</Label>
              <textarea
                id="howToApply"
                name="howToApply"
                rows={2}
                required
                className="w-full rounded-md border border-zinc-200 bg-white px-3 py-2 text-sm text-zinc-950 focus:outline-none focus:ring-2 focus:ring-ring dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50"
                value={formData.howToApply}
                onChange={handleChange}
              />
              <p className="text-xs text-muted-foreground">The default text guides candidates to the online application button; you may adapt it for this role.</p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="space-y-2">
                <Label className="font-semibold text-zinc-700 dark:text-zinc-300">Specialization (Job Expertise / Major) *</Label>
                <MajorCombobox
                  majors={majors}
                  value={formData.jobExpertise}
                  onChange={(val) => setFormData(prev => ({ ...prev, jobExpertise: val }))}
                />
              </div>

              <div className="space-y-2">
                <Label className="font-semibold text-zinc-700 dark:text-zinc-300">Industry / Domain</Label>
                <Input
                  placeholder="Filter domain options..."
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
              <Label htmlFor="description" className="font-semibold text-zinc-700 dark:text-zinc-300">Job Description *</Label>
              <JobPostingRichTextEditor
                id="description"
                required
                placeholder="Describe the job position, key responsibilities, and team culture..."
                value={formData.description}
                maxLength={JOB_POSTING_RICH_TEXT_LIMITS.description}
                disabled={loading || submittingAction !== null}
                onChange={(description) => setFormData((previous) => ({ ...previous, description }))}
              />
            </div>

            {/* Compensation & Additional Details */}
            <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
              <div className="space-y-2">
                <Label htmlFor="minSalary" className="font-semibold text-zinc-700 dark:text-zinc-300">Min Salary</Label>
                <Input
                  id="minSalary"
                  name="minSalary"
                  type="number"
                  placeholder="e.g. 1500"
                  value={formData.minSalary}
                  onChange={handleChange}
                  className="focus-visible:ring-blue-500"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="maxSalary" className="font-semibold text-zinc-700 dark:text-zinc-300">Max Salary</Label>
                <Input
                  id="maxSalary"
                  name="maxSalary"
                  type="number"
                  placeholder="e.g. 3000"
                  value={formData.maxSalary}
                  onChange={handleChange}
                  className="focus-visible:ring-blue-500"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="currency" className="font-semibold text-zinc-700 dark:text-zinc-300">Currency</Label>
                <select
                  id="currency"
                  name="currency"
                  className="w-full h-9 rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-1 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
                  value={formData.currency}
                  onChange={handleChange}
                >
                  <option value="USD">USD</option>
                  <option value="VND">VND</option>
                </select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="expiresAt" className="font-semibold text-zinc-700 dark:text-zinc-300">Expiration Date *</Label>
                <Input
                  id="expiresAt"
                  name="expiresAt"
                  type="date"
                  min={todayStr}
                  max={maxDateStr}
                  value={formData.expiresAt}
                  onChange={handleChange}
                  className="focus-visible:ring-blue-500"
                />
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              <div className="space-y-2">
                <Label htmlFor="incomeText" className="font-semibold text-zinc-700 dark:text-zinc-300">Income Details *</Label>
                <JobPostingRichTextEditor
                  id="incomeText"
                  required
                  placeholder="Up to $3,000 / month + performance bonus..."
                  value={formData.incomeText}
                  maxLength={JOB_POSTING_RICH_TEXT_LIMITS.incomeText}
                  disabled={loading || submittingAction !== null}
                  onChange={(incomeText) => setFormData((previous) => ({ ...previous, incomeText }))}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="workLocation" className="font-semibold text-zinc-700 dark:text-zinc-300">Work Location Address *</Label>
                <textarea
                  id="workLocation"
                  name="workLocation"
                  rows={2}
                  required
                  placeholder={`Ha Noi: 125 Hoang Ngan, Yen Hoa...\nHo Chi Minh City: ...`}
                  className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={formData.workLocation}
                  onChange={handleChange}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="workingHours" className="font-semibold text-zinc-700 dark:text-zinc-300">Working Hours *</Label>
                <textarea
                  id="workingHours"
                  name="workingHours"
                  rows={2}
                  required
                  placeholder="Monday - Friday (09:00 - 18:00)"
                  className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  value={formData.workingHours}
                  onChange={handleChange}
                />
              </div>
            </div>

            <div className="space-y-2">
              <div>
                <Label htmlFor="requirements" className="font-semibold text-zinc-700 dark:text-zinc-300">Requirements *</Label>
              </div>
              <JobPostingRichTextEditor
                id="requirements"
                required
                placeholder="List specific qualifications, experience level, degree, or other requirements..."
                value={formData.requirements}
                maxLength={JOB_POSTING_RICH_TEXT_LIMITS.requirements}
                disabled={loading || submittingAction !== null}
                onChange={(requirements) => setFormData((previous) => ({ ...previous, requirements }))}
              />
            </div>

            <div className="space-y-2">
              <div>
                <Label htmlFor="benefits" className="font-semibold text-zinc-700 dark:text-zinc-300">Benefits *</Label>
              </div>
              <JobPostingRichTextEditor
                id="benefits"
                required
                placeholder="List key benefits (e.g. 13th month salary, health insurance, flexible working hours)..."
                value={formData.benefits}
                maxLength={JOB_POSTING_RICH_TEXT_LIMITS.benefits}
                disabled={loading || submittingAction !== null}
                onChange={(benefits) => setFormData((previous) => ({ ...previous, benefits }))}
              />
            </div>
          </CardContent>
        </Card>

        {/* Slot Full Notice */}
        {isSlotFull && (
          <div className="p-4 rounded-xl border border-amber-200 dark:border-amber-800/50 bg-amber-50/80 dark:bg-amber-950/20 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 text-sm">
            <div className="flex items-center gap-3 text-amber-800 dark:text-amber-300">
              <AlertCircle className="h-5 w-5 shrink-0 text-amber-600 dark:text-amber-400" />
              <div>
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="font-semibold">Đã đạt giới hạn tin Active miễn phí của gói ({jobSlotsUsed} tin đang Active • Hạn mức theo gói: {jobSlotsLimit === -1 ? "Vô hạn" : jobSlotsLimit} tin).</span>
                </div>
                <p className="text-xs text-amber-700 dark:text-amber-400 mt-0.5 leading-relaxed">
                  Nhấn &quot;Publish Job&quot; (Xuất bản Active) cho tin này sẽ tự động khấu trừ <strong>20,000 Coin</strong> từ ví của bạn (Số dư hiện có: {(walletData?.balance || 0).toLocaleString()} Coin). Nếu chưa muốn đăng ngay hoặc chưa nạp đủ Coin, bạn có thể chọn <strong>&quot;Save as Draft&quot;</strong> (Lưu nháp) hoàn toàn miễn phí.
                </p>
              </div>
            </div>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => router.push("/recruiter/billing")}
              className="shrink-0 border-amber-300 dark:border-amber-700 hover:bg-amber-100/50 text-amber-800 dark:text-amber-300 text-xs font-medium self-end sm:self-center"
            >
              Nâng cấp gói
            </Button>
          </div>
        )}

        {/* Action Buttons */}
        <div className="flex justify-end gap-3 pt-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => router.push("/recruiter/jobs")}
            disabled={loading}
          >
            Hủy
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
            Lưu nháp
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
            Publish
          </Button>
        </div>
      </div>
    </div>
  )
}
