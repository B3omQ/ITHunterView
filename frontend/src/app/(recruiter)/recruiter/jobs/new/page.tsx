"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { useJobMetadata, useJobDetails } from "@/hooks/useJobs"
import { useGetMyCompany } from "@/hooks/useCompany"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { ArrowLeft, X, Sparkles, AlertCircle } from "lucide-react"
import { LEVELS, WORKING_MODELS, JOB_DOMAINS, VIETNAM_PROVINCES } from "@/lib/job-constants"
import { MajorCombobox } from "@/components/shared/MajorCombobox"
import { recruiterService } from "@/services/recruiter.service"
import {
  DEFAULT_HOW_TO_APPLY,
  serializeWorkLocationText,
  getSerializedWorkLocationLength,
  normalizeMultilineText,
} from "@/lib/job-posting-text"

export default function CreateJobPage() {
  const router = useRouter()
  const { data: company, isLoading: companyLoading } = useGetMyCompany()

  const [formData, setFormData] = useState({
    jobCode: "",
    title: "",
    location: "",
    status: "DRAFT",
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

  const { availableSkills, majors, loading: metadataLoading, error: metadataError } = useJobMetadata()
  const { createJob, saving, error: saveError } = useJobDetails()

  const [selectedSkills, setSelectedSkills] = useState<Array<{ skillId: number; name: string; isMandatory: boolean }>>([])
  const [searchSkill, setSearchSkill] = useState("")
  const [creatingSkill, setCreatingSkill] = useState(false)

  const [searchDomain, setSearchDomain] = useState("")

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

  const addSkill = (skill: any, isMandatory: boolean) => {
    if (selectedSkills.some(s => s.skillId === skill.id)) {
      setSelectedSkills(prev =>
        prev.map(s => s.skillId === skill.id ? { ...s, isMandatory } : s)
      )
    } else {
      setSelectedSkills(prev => [...prev, { skillId: skill.id, name: skill.name, isMandatory }])
    }
    setSearchSkill("")
  }

  const removeSkill = (skillId: number) => {
    setSelectedSkills(prev => prev.filter(s => s.skillId !== skillId))
  }

  const handleCreateCustomSkill = async (isMandatory: boolean) => {
    if (!searchSkill.trim()) return
    setCreatingSkill(true)
    try {
      const res = await recruiterService.createSkill(searchSkill.trim(), 1)
      if (res.success && res.data?.success && res.data.data) {
        addSkill(res.data.data, isMandatory)
      } else {
        alert("Failed to create skill. " + (res.message || ""))
      }
    } catch {
      alert("Error creating skill.")
    } finally {
      setCreatingSkill(false)
    }
  }

  const validateForm = () => {
    if (!formData.title.trim()) return "Job Title is required"
    if (!formData.location.trim()) return "Location is required"
    if (!formData.level) return "Level is required"
    if (!formData.workingModel) return "Working Model is required"
    if (!formData.jobExpertise) return "Specialization (Expertise) is required"

    if (mustHaveSkills.length === 0) return "At least one Must-have Skill is required"
    if (niceToHaveSkills.length === 0) return "At least one Nice-to-have Skill is required"

    if (!formData.description.trim()) return "Job Description is required"
    if (!formData.incomeText.trim()) return "Income is required"
    if (!formData.workLocation.trim()) return "Work location is required"
    if (!formData.workingHours.trim()) return "Working hours are required"
    if (!formData.howToApply.trim()) return "How to apply is required"
    if (!formData.requirements.trim()) return "Requirements are required"
    if (!formData.benefits.trim()) return "Benefits are required"

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

  const handleSubmit = async (statusVal: "DRAFT" | "PUBLISHED") => {
    const validationError = validateForm()
    if (validationError) {
      alert(validationError)
      return
    }

    const serializedWorkLocation = serializeWorkLocationText({
      workLocation: formData.workLocation,
      workingHours: formData.workingHours,
      howToApply: formData.howToApply,
    })

    const payload = {
      jobCode: formData.jobCode,
      title: formData.title,
      location: formData.location,
      status: statusVal,
      minSalary: formData.minSalary ? Number(formData.minSalary) : null,
      maxSalary: formData.maxSalary ? Number(formData.maxSalary) : null,
      currency: formData.currency,
      expiresAt: formData.expiresAt ? new Date(formData.expiresAt).toISOString() : null,
      description: normalizeMultilineText(formData.description),
      incomeText: normalizeMultilineText(formData.incomeText),
      workLocationText: serializedWorkLocation,
      requirements: normalizeMultilineText(formData.requirements),
      benefits: normalizeMultilineText(formData.benefits),
      level: formData.level,
      workingModel: formData.workingModel,
      jobExpertise: formData.jobExpertise,
      jobDomain: formData.jobDomain,
      skills: selectedSkills.map(s => ({ skillId: s.skillId, isMandatory: s.isMandatory }))
    }

    const res = await createJob(payload)
    if (res.success) {
      router.push("/recruiter/jobs")
    }
  }

  const filteredAvailableSkills = availableSkills.filter(
    skill =>
      skill.name.toLowerCase().includes(searchSkill.toLowerCase()) &&
      !selectedSkills.some(s => s.skillId === skill.id)
  )

  const mustHaveSkills = selectedSkills.filter(s => s.isMandatory)
  const niceToHaveSkills = selectedSkills.filter(s => !s.isMandatory)

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
            className="rounded-full bg-white dark:bg-zinc-900 border border-zinc-200/80 dark:border-zinc-800/80 shadow-xs"
          >
            <ArrowLeft className="h-5 w-5 text-zinc-600 dark:text-zinc-400" />
          </Button>
          <div>
            <h1 className="text-2xl font-extrabold text-zinc-900 dark:text-zinc-50 tracking-tight">Create New IT Job Position</h1>
            <p className="text-zinc-500 dark:text-zinc-400 text-sm">Post a new opening to recruit candidates</p>
          </div>
        </div>

        {error && (
          <div className="flex items-center gap-3 text-sm font-medium text-red-500 bg-red-50 dark:bg-red-950/30 p-4 rounded-xl border border-red-200 dark:border-red-900">
            <AlertCircle className="h-5 w-5 shrink-0" />
            <span>{error}</span>
          </div>
        )}

        <Card className="border-zinc-200/80 dark:border-zinc-800/80 shadow-xs">
          <CardHeader className="border-b border-zinc-200/60 dark:border-zinc-800/60 pb-6">
            <CardTitle className="text-lg font-bold text-zinc-900 dark:text-zinc-50">Job Details Form</CardTitle>
            <CardDescription>Fill in the required information for the job listing.</CardDescription>
          </CardHeader>
          <CardContent className="p-6 space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="space-y-2">
                <Label htmlFor="title" className="font-semibold text-zinc-700 dark:text-zinc-300">Job Title *</Label>
                <Input
                  id="title"
                  name="title"
                  placeholder="e.g. Senior Frontend Developer (React)"
                  required
                  value={formData.title}
                  onChange={handleChange}
                  className="focus-visible:ring-blue-500"
                />
              </div>
              <div className="space-y-2 col-span-1 md:col-span-1">
                <Label htmlFor="location" className="font-semibold text-zinc-700 dark:text-zinc-300">City / Province *</Label>
                <select
                  id="location"
                  name="location"
                  required
                  className="w-full h-9 rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-1 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500 transition-all"
                  value={formData.location}
                  onChange={handleChange}
                >
                  <option value="">Select City/Province</option>
                  {VIETNAM_PROVINCES.map((loc) => (
                    <option key={loc} value={loc}>{loc}</option>
                  ))}
                </select>
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="space-y-2">
                <Label htmlFor="level" className="font-semibold text-zinc-700 dark:text-zinc-300">Level *</Label>
                <select
                  id="level"
                  name="level"
                  className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500 transition-all"
                  value={formData.level}
                  onChange={handleChange}
                >
                  <option value="">Select Level</option>
                  {LEVELS.map((lvl) => (
                    <option key={lvl} value={lvl}>{lvl}</option>
                  ))}
                </select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="workingModel" className="font-semibold text-zinc-700 dark:text-zinc-300">Working Model *</Label>
                <select
                  id="workingModel"
                  name="workingModel"
                  className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500 transition-all"
                  value={formData.workingModel}
                  onChange={handleChange}
                >
                  <option value="">Select Working Model</option>
                  {WORKING_MODELS.map((wm) => (
                    <option key={wm} value={wm}>{wm}</option>
                  ))}
                </select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="jobExpertise" className="font-semibold text-zinc-700 dark:text-zinc-300">Specialization (Expertise) *</Label>
                <MajorCombobox
                  majors={majors}
                  value={formData.jobExpertise}
                  onChange={(val) => setFormData(prev => ({ ...prev, jobExpertise: val }))}
                  className="w-full h-10 mt-1"
                  placeholder="Select specialization..."
                />
              </div>
            </div>

            <div className="space-y-2">
              <div className="flex justify-between items-center">
                <Label className="font-semibold text-zinc-700 dark:text-zinc-300">Job Domains</Label>
                <Input
                  placeholder="Search domains..."
                  className="w-48 h-8 text-xs"
                  value={searchDomain}
                  onChange={(e) => setSearchDomain(e.target.value)}
                />
              </div>
              <div className="flex flex-wrap gap-2 p-3 border rounded-md border-zinc-200 dark:border-zinc-800 bg-zinc-50/50 dark:bg-zinc-950/50 max-h-48 overflow-y-auto">
                {filteredDomains.map(domain => (
                  <label key={domain} className="flex items-center gap-2 text-sm cursor-pointer hover:bg-zinc-100 dark:hover:bg-zinc-900 p-1.5 rounded pr-3 border border-transparent hover:border-zinc-200 dark:hover:border-zinc-800 transition-colors">
                    <input
                      type="checkbox"
                      checked={formData.jobDomain.includes(domain)}
                      onChange={() => handleDomainChange(domain)}
                      className="rounded border-zinc-300 text-blue-600 focus:ring-blue-500 bg-white dark:bg-zinc-900"
                    />
                    <span className="text-zinc-700 dark:text-zinc-300">{domain}</span>
                  </label>
                ))}
                {filteredDomains.length === 0 && (
                  <div className="text-sm text-zinc-500 italic p-2">No domains found</div>
                )}
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <div className="space-y-2">
                <Label htmlFor="minSalary" className="font-semibold text-zinc-700 dark:text-zinc-300">Min Salary</Label>
                <Input
                  id="minSalary"
                  name="minSalary"
                  type="number"
                  placeholder="e.g. 1000"
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
                  placeholder="e.g. 2500"
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

            <hr className="border-zinc-200/60 dark:border-zinc-800/60" />

            {/* Standardized Skill Dictionary Section */}
            <div className="space-y-4">
              <div>
                <h3 className="text-base font-bold text-zinc-900 dark:text-zinc-50 flex items-center gap-1.5">
                  <Sparkles className="h-4.5 w-4.5 text-blue-500" />
                  Standardized Skill Dictionary
                </h3>
                <p className="text-xs text-zinc-500 dark:text-zinc-400 mt-0.5">Specify Must-have and Nice-to-have technical skills from standard list.</p>
              </div>

              {/* Skill Selector Input & Dropdown */}
              <div className="space-y-2">
                <Label htmlFor="searchSkill" className="text-sm font-semibold text-zinc-700 dark:text-zinc-300">Add Skills from dictionary</Label>
                <Input
                  id="searchSkill"
                  placeholder="Type to search e.g. React, Docker, Python..."
                  value={searchSkill}
                  onChange={(e) => setSearchSkill(e.target.value)}
                  className="focus-visible:ring-blue-500"
                />

                <div className="w-full max-h-48 overflow-y-auto rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 p-2 space-y-1">
                  {filteredAvailableSkills.length > 0 ? (
                    filteredAvailableSkills.map((skill) => (
                      <div key={skill.id} className="flex items-center justify-between p-2 hover:bg-zinc-50 dark:hover:bg-zinc-900 rounded-md transition-all">
                        <span className="text-sm font-medium text-zinc-800 dark:text-zinc-200">{skill.name} <span className="text-xs text-zinc-400">({skill.categoryName || "Other"})</span></span>
                        <div className="flex gap-2">
                          <Button
                            type="button"
                            size="sm"
                            variant="outline"
                            onClick={() => addSkill(skill, true)}
                            className="h-7 text-xs bg-blue-50 text-blue-600 hover:bg-blue-100 border-blue-200 dark:bg-blue-950/30 dark:text-blue-400"
                          >
                            + Must-have
                          </Button>
                          <Button
                            type="button"
                            size="sm"
                            variant="outline"
                            onClick={() => addSkill(skill, false)}
                            className="h-7 text-xs bg-emerald-50 text-emerald-600 hover:bg-emerald-100 border-emerald-200 dark:bg-emerald-950/30 dark:text-emerald-400"
                          >
                            + Nice-to-have
                          </Button>
                        </div>
                      </div>
                    ))
                  ) : (
                    <div className="flex flex-col items-center justify-center py-4 space-y-3">
                      <span className="text-sm text-zinc-500">"{searchSkill}" is not in the dictionary.</span>
                      <div className="flex gap-2">
                        <Button
                          type="button"
                          size="sm"
                          variant="default"
                          disabled={creatingSkill}
                          onClick={() => handleCreateCustomSkill(true)}
                          className="text-xs bg-blue-600 hover:bg-blue-700"
                        >
                          Create as Must-have
                        </Button>
                        <Button
                          type="button"
                          size="sm"
                          variant="default"
                          disabled={creatingSkill}
                          onClick={() => handleCreateCustomSkill(false)}
                          className="text-xs bg-emerald-600 hover:bg-emerald-700"
                        >
                          Create as Nice-to-have
                        </Button>
                      </div>
                    </div>
                  )}
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6 bg-zinc-50/50 dark:bg-zinc-900/30 p-4 rounded-xl border border-zinc-200/60 dark:border-zinc-800/60">
                {/* Must-have skills list */}
                <div className="space-y-2">
                  <Label className="font-bold text-xs uppercase tracking-wider text-blue-600 dark:text-blue-400">Must-have Skills *</Label>
                  <div className="min-h-[100px] border border-dashed border-zinc-200 dark:border-zinc-800 rounded-lg p-2 bg-white dark:bg-zinc-950 flex flex-wrap gap-1.5 items-start content-start">
                    {mustHaveSkills.length > 0 ? (
                      mustHaveSkills.map(s => (
                        <span key={s.skillId} className="inline-flex items-center gap-1 bg-blue-50 text-blue-700 border border-blue-200 dark:bg-blue-950/40 dark:text-blue-400 dark:border-blue-900/50 px-2 py-1 rounded text-xs font-semibold">
                          {s.name}
                          <button type="button" onClick={() => removeSkill(s.skillId)} className="hover:text-blue-900 dark:hover:text-blue-200">
                            <X className="h-3 w-3" />
                          </button>
                        </span>
                      ))
                    ) : (
                      <span className="text-xs text-zinc-400 p-2">Select skills as Must-have from above</span>
                    )}
                  </div>
                </div>

                {/* Nice-to-have skills list */}
                <div className="space-y-2">
                  <Label className="font-bold text-xs uppercase tracking-wider text-emerald-600 dark:text-emerald-400">Nice-to-have Skills *</Label>
                  <div className="min-h-[100px] border border-dashed border-zinc-200 dark:border-zinc-800 rounded-lg p-2 bg-white dark:bg-zinc-950 flex flex-wrap gap-1.5 items-start content-start">
                    {niceToHaveSkills.length > 0 ? (
                      niceToHaveSkills.map(s => (
                        <span key={s.skillId} className="inline-flex items-center gap-1 bg-emerald-50 text-emerald-700 border border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-400 dark:border-emerald-900/50 px-2 py-1 rounded text-xs font-semibold">
                          {s.name}
                          <button type="button" onClick={() => removeSkill(s.skillId)} className="hover:text-emerald-900 dark:hover:text-emerald-200">
                            <X className="h-3 w-3" />
                          </button>
                        </span>
                      ))
                    ) : (
                      <span className="text-xs text-zinc-400 p-2">Select skills as Nice-to-have from above</span>
                    )}
                  </div>
                </div>
              </div>
            </div>

            <hr className="border-zinc-200/60 dark:border-zinc-800/60" />

            <div className="space-y-2">
              <div>
                <Label htmlFor="description" className="font-semibold text-zinc-700 dark:text-zinc-300">Job Description *</Label>
                <p className="text-xs text-zinc-500 dark:text-zinc-400 mt-0.5">Each new line will be displayed as a bullet point.</p>
              </div>
              <textarea
                id="description"
                name="description"
                rows={4}
                required
                placeholder="Describe the job role, duties, and typical day-to-day work..."
                className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
                value={formData.description}
                onChange={handleChange}
              />
            </div>

            <div className="space-y-2">
              <div>
                <Label htmlFor="incomeText" className="font-semibold text-zinc-700 dark:text-zinc-300">Income *</Label>
                <p className="text-xs text-zinc-500 dark:text-zinc-400 mt-0.5">Each new line will be displayed on a separate line.</p>
              </div>
              <textarea
                id="incomeText"
                name="incomeText"
                rows={2}
                required
                placeholder="e.g. $1,000 - $2,000, Negotiable, 13th month bonus..."
                className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
                value={formData.incomeText}
                onChange={handleChange}
              />
            </div>

            {/* Work Location & Schedule 3-field section */}
            <div className="space-y-4 pt-2 border-t border-zinc-100 dark:border-zinc-900">
              <div>
                <h3 className="text-base font-bold text-zinc-900 dark:text-zinc-50">Work Location & Schedule</h3>
                <p className="text-xs text-zinc-500 dark:text-zinc-400 mt-0.5">Specify location details, working hours, and application instructions.</p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="workLocation" className="font-semibold text-zinc-700 dark:text-zinc-300">Work Location *</Label>
                <textarea
                  id="workLocation"
                  name="workLocation"
                  rows={2}
                  required
                  placeholder={`Ha Noi: 125 Hoang Ngan, Yen Hoa...\nHo Chi Minh City: ...`}
                  className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
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
                  className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
                  value={formData.workingHours}
                  onChange={handleChange}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="howToApply" className="font-semibold text-zinc-700 dark:text-zinc-300">How to Apply *</Label>
                <textarea
                  id="howToApply"
                  name="howToApply"
                  rows={2}
                  required
                  placeholder="Application instructions..."
                  className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
                  value={formData.howToApply}
                  onChange={handleChange}
                />
              </div>
            </div>

            <div className="space-y-2">
              <div>
                <Label htmlFor="requirements" className="font-semibold text-zinc-700 dark:text-zinc-300">Requirements *</Label>
                <p className="text-xs text-zinc-500 dark:text-zinc-400 mt-0.5">Each new line will be displayed as a bullet point.</p>
              </div>
              <textarea
                id="requirements"
                name="requirements"
                rows={3}
                required
                placeholder="List specific qualifications, experience level, degree, or other requirements..."
                className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
                value={formData.requirements}
                onChange={handleChange}
              />
            </div>

            <div className="space-y-2">
              <div>
                <Label htmlFor="benefits" className="font-semibold text-zinc-700 dark:text-zinc-300">Benefits *</Label>
                <p className="text-xs text-zinc-500 dark:text-zinc-400 mt-0.5">Each new line will be displayed as a bullet point.</p>
              </div>
              <textarea
                id="benefits"
                name="benefits"
                rows={3}
                required
                placeholder="List key benefits (e.g. 13th month salary, health insurance, flexible working hours)..."
                className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
                value={formData.benefits}
                onChange={handleChange}
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
            className="border-zinc-200/80 dark:border-zinc-800/80 hover:bg-zinc-100"
          >
            Cancel
          </Button>
          <Button
            type="button"
            variant="secondary"
            onClick={() => handleSubmit("DRAFT")}
            disabled={loading}
            className="bg-zinc-200 hover:bg-zinc-300 text-zinc-800 dark:bg-zinc-800 dark:hover:bg-zinc-700 dark:text-zinc-200"
          >
            {loading ? "Saving..." : "Save as Draft"}
          </Button>
          <Button
            type="button"
            onClick={() => handleSubmit("PUBLISHED")}
            disabled={loading}
            className="bg-blue-600 hover:bg-blue-700 text-white shadow-md shadow-blue-500/10"
          >
            {loading ? "Publishing..." : "Publish Job"}
          </Button>
        </div>
      </div>
    </div>
  )
}
