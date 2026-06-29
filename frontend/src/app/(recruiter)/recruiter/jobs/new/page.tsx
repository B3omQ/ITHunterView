"use client"

import { useState, useEffect } from "react"
import { useRouter } from "next/navigation"
import { useJobMetadata, useJobDetails } from "@/hooks/useJobs"
import { useGetMyCompany } from "@/hooks/useCompany"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { ArrowLeft, Plus, X, Sparkles, AlertCircle } from "lucide-react"
import { LEVELS, WORKING_MODELS, JOB_DOMAINS, JOB_EXPERTISES, VIETNAM_PROVINCES } from "@/lib/job-constants"
import { LocationPicker, LocationData } from "@/components/shared/LocationPicker"
import { MajorCombobox } from "@/components/shared/MajorCombobox"

export default function CreateJobPage() {
  const router = useRouter()
  const { data: company, isLoading: companyLoading } = useGetMyCompany()

  const [formData, setFormData] = useState({
    jobCode: "",
    title: "",

    provinceCode: "",
    detailedLocation: "",
    latitude: null as number | null,
    longitude: null as number | null,

    status: "DRAFT",
    minSalary: "",
    maxSalary: "",
    description: "",
    responsibilities: "",
    requirements: "",
    benefits: "",
    level: "",
    workingModel: "",
    jobExpertise: "",
    jobDomain: [] as string[],
  })

  const { categories, availableSkills, majors, loading: metadataLoading, error: metadataError } = useJobMetadata()
  const { createJob, saving, error: saveError } = useJobDetails()
  
  const [selectedSkills, setSelectedSkills] = useState<Array<{ skillId: number; name: string; isMandatory: boolean }>>([])
  const [searchSkill, setSearchSkill] = useState("")
  

  const [searchDomain, setSearchDomain] = useState("")

  const loading = metadataLoading || saving || companyLoading
  const error = metadataError || saveError

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

  // Skill Selection Handlers
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

  const handleSubmit = async (statusVal: "DRAFT" | "PUBLISHED") => {
    const payload = {
      ...formData,
      status: statusVal,

      minSalary: formData.minSalary ? Number(formData.minSalary) : null,
      maxSalary: formData.maxSalary ? Number(formData.maxSalary) : null,
      currency: "USD",
      skills: selectedSkills.map(s => ({ skillId: s.skillId, isMandatory: s.isMandatory }))
    }

    const res = await createJob(payload)
    if (res.success) {
      router.push("/recruiter/jobs")
    }
  }

  // Filter skills based on search term
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
    <div className="min-h-screen bg-background py-10 px-4 sm:px-6 lg:px-8 transition-colors duration-200">
      <div className="max-w-4xl mx-auto space-y-6">

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
              <div className="space-y-2 col-span-1 md:col-span-2">
                <Label className="font-semibold text-zinc-700 dark:text-zinc-300">Location *</Label>
                <LocationPicker 
                  value={{
                    provinceCode: formData.provinceCode,
                    detailedLocation: formData.detailedLocation,
                    latitude: formData.latitude,
                    longitude: formData.longitude
                  }}
                  onChange={(val: LocationData) => setFormData(prev => ({
                    ...prev,
                    provinceCode: val.provinceCode,
                    detailedLocation: val.detailedLocation,
                    latitude: val.latitude,
                    longitude: val.longitude
                  }))}
                />
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="space-y-2">
                <Label htmlFor="level" className="font-semibold text-zinc-700 dark:text-zinc-300">Level</Label>
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
                <Label htmlFor="workingModel" className="font-semibold text-zinc-700 dark:text-zinc-300">Working Model</Label>
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
                <Label htmlFor="jobExpertise" className="font-semibold text-zinc-700 dark:text-zinc-300">Specialization (Expertise)</Label>
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

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
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

              <div className="grid grid-cols-1 md:grid-cols-2 gap-6 bg-zinc-50/50 dark:bg-zinc-900/30 p-4 rounded-xl border border-zinc-200/60 dark:border-zinc-800/60">
                {/* Must-have skills list */}
                <div className="space-y-2">
                  <Label className="font-bold text-xs uppercase tracking-wider text-blue-600 dark:text-blue-400">Must-have Skills</Label>
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
                      <span className="text-xs text-zinc-400 p-2">Drag or select skills as Must-have</span>
                    )}
                  </div>
                </div>

                {/* Nice-to-have skills list */}
                <div className="space-y-2">
                  <Label className="font-bold text-xs uppercase tracking-wider text-emerald-600 dark:text-emerald-400">Nice-to-have Skills</Label>
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
                      <span className="text-xs text-zinc-400 p-2">Drag or select skills as Nice-to-have</span>
                    )}
                  </div>
                </div>
              </div>

              {/* Skill Selector Input & Dropdown */}
              <div className="relative">
                <Label htmlFor="searchSkill" className="text-sm font-semibold text-zinc-700 dark:text-zinc-300">Add Skills from dictionary</Label>
                <Input
                  id="searchSkill"
                  placeholder="Type to search e.g. React, Docker, Python..."
                  value={searchSkill}
                  onChange={(e) => setSearchSkill(e.target.value)}
                  className="mt-1 focus-visible:ring-blue-500"
                />

                {searchSkill.trim() && (
                  <div className="absolute z-10 w-full mt-1 max-h-60 overflow-y-auto rounded-lg border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-900 shadow-lg p-2 space-y-1">
                    {filteredAvailableSkills.length > 0 ? (
                      filteredAvailableSkills.map((skill) => (
                        <div key={skill.id} className="flex items-center justify-between p-2 hover:bg-zinc-50 dark:hover:bg-zinc-800 rounded-md transition-all">
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
                      <div className="text-center text-xs text-zinc-400 py-3">No matching skills found in dictionary.</div>
                    )}
                  </div>
                )}
              </div>
            </div>

            <hr className="border-zinc-200/60 dark:border-zinc-800/60" />

            <div className="space-y-2">
              <Label htmlFor="description" className="font-semibold text-zinc-700 dark:text-zinc-300">Job Description *</Label>
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
              <Label htmlFor="responsibilities" className="font-semibold text-zinc-700 dark:text-zinc-300">Key Responsibilities</Label>
              <textarea
                id="responsibilities"
                name="responsibilities"
                rows={3}
                placeholder="List major responsibilities (e.g. design scalable microservices, manage CI/CD)..."
                className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
                value={formData.responsibilities}
                onChange={handleChange}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="requirements" className="font-semibold text-zinc-700 dark:text-zinc-300">Detailed Requirements</Label>
              <textarea
                id="requirements"
                name="requirements"
                rows={3}
                placeholder="List specific qualifications, experience level, degree, or other requirements..."
                className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
                value={formData.requirements}
                onChange={handleChange}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="benefits" className="font-semibold text-zinc-700 dark:text-zinc-300">Perks & Benefits</Label>
              <textarea
                id="benefits"
                name="benefits"
                rows={3}
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
            {loading ? "Publishing..." : "Publish & Submit Review"}
          </Button>
        </div>
      </div>
    </div>
  )
}
