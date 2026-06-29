"use client"

import { useState, useEffect } from "react"
import { useRouter, useParams } from "next/navigation"
import { useJobMetadata, useJobDetails } from "@/hooks/useJobs"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { ArrowLeft, Plus, X, Sparkles, AlertCircle, Loader2 } from "lucide-react"
import { LEVELS, WORKING_MODELS, JOB_DOMAINS, JOB_EXPERTISES, VIETNAM_PROVINCES } from "@/lib/job-constants"
import { LocationPicker, LocationData } from "@/components/shared/LocationPicker"
import { ProvinceSelect } from "@/components/shared/ProvinceSelect"
import { MajorCombobox } from "@/components/shared/MajorCombobox"

export default function EditJobPage() {
  const router = useRouter()
  const params = useParams()
  const id = params.jobId as string

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
  const { job, loading: detailLoading, saving, error: detailError, setError, updateJob } = useJobDetails(id)
  
  const [selectedSkills, setSelectedSkills] = useState<Array<{ skillId: number; name: string; isMandatory: boolean }>>([])
  const [searchSkill, setSearchSkill] = useState("")


  const [searchDomain, setSearchDomain] = useState("")

  const loading = metadataLoading || detailLoading
  const error = metadataError || detailError

  // Load existing job details into form fields
  useEffect(() => {
    if (job) {
      setFormData({
        jobCode: job.jobCode || "",
        title: job.title || "",

        provinceCode: job.provinceCode || "",
        detailedLocation: job.detailedLocation || "",
        latitude: job.latitude || null,
        longitude: job.longitude || null,

        status: job.status || "DRAFT",
        minSalary: job.minSalary ? job.minSalary.toString() : "",
        maxSalary: job.maxSalary ? job.maxSalary.toString() : "",
        description: job.description || "",
        responsibilities: job.responsibilities || "",
        requirements: job.requirements || "",
        benefits: job.benefits || "",
        level: job.level || "",
        workingModel: job.workingModel || "",
        jobExpertise: job.jobExpertise || "",
        jobDomain: job.jobDomain || [],
      })
      
      if (job.skills) {
        setSelectedSkills(job.skills.map((s: any) => ({
          skillId: s.skillId,
          name: s.skillName || s.name || "",
          isMandatory: s.isMandatory
        })))
      }
      
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

  const handleUpdate = async (statusVal?: string) => {
    const payload = {
      ...formData,
      status: statusVal || formData.status,

      minSalary: formData.minSalary ? Number(formData.minSalary) : null,
      maxSalary: formData.maxSalary ? Number(formData.maxSalary) : null,
      currency: job?.currency || "USD",
      skills: selectedSkills.map(s => ({ skillId: s.skillId, isMandatory: s.isMandatory }))
    }

    const res = await updateJob(payload)
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
            <h1 className="text-2xl font-extrabold text-zinc-900 dark:text-zinc-50 tracking-tight">Edit Job Position</h1>
            <p className="text-zinc-500 dark:text-zinc-400 text-sm">Update job description, parameters and skills</p>
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
            <CardDescription>Make changes to update the job posting.</CardDescription>
          </CardHeader>
          <CardContent className="p-6 space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="space-y-2 md:col-span-2">
                <Label htmlFor="title" className="font-semibold text-zinc-700 dark:text-zinc-300">Job Title *</Label>
                <Input
                  id="title"
                  name="title"
                  placeholder="e.g. Senior Frontend Developer"
                  required
                  value={formData.title}
                  onChange={handleChange}
                  className="focus-visible:ring-blue-500"
                />
              </div>

              <div className="space-y-2">
                <Label className="font-semibold text-zinc-700 dark:text-zinc-300">Province/City *</Label>
                <ProvinceSelect 
                  value={formData.provinceCode}
                  onChange={(val) => setFormData(prev => ({ ...prev, provinceCode: val }))}
                />
              </div>

              <div className="space-y-2 md:col-span-3">
                <Label className="font-semibold text-zinc-700 dark:text-zinc-300">Detailed Location Map *</Label>
                <LocationPicker 
                  value={{
                    provinceCode: formData.provinceCode,
                    detailedLocation: formData.detailedLocation,
                    latitude: formData.latitude,
                    longitude: formData.longitude
                  }}
                  onChange={(val: LocationData) => setFormData(prev => ({
                    ...prev,
                    // Ignore LocationPicker's provinceCode to avoid overriding ProvinceSelect
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

            <div className="space-y-2">
              <Label htmlFor="status" className="font-semibold text-zinc-700 dark:text-zinc-300">Status</Label>
              <select
                id="status"
                name="status"
                className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-955 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500 transition-all"
                value={formData.status}
                onChange={handleChange}
              >
                <option value="DRAFT">Draft</option>
                <option value="PUBLISHED">Active / Published</option>
                <option value="CLOSED">Closed</option>
              </select>
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
                  <div className="min-h-[100px] border border-dashed border-zinc-200 dark:border-zinc-800 rounded-lg p-2 bg-white dark:bg-zinc-955 flex flex-wrap gap-1.5 items-start content-start">
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
                      <span className="text-xs text-zinc-400 p-2">No Must-have skills selected</span>
                    )}
                  </div>
                </div>

                {/* Nice-to-have skills list */}
                <div className="space-y-2">
                  <Label className="font-bold text-xs uppercase tracking-wider text-emerald-600 dark:text-emerald-400">Nice-to-have Skills</Label>
                  <div className="min-h-[100px] border border-dashed border-zinc-200 dark:border-zinc-800 rounded-lg p-2 bg-white dark:bg-zinc-955 flex flex-wrap gap-1.5 items-start content-start">
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
                      <span className="text-xs text-zinc-400 p-2">No Nice-to-have skills selected</span>
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
                              className="h-7 text-xs bg-blue-50 text-blue-600 hover:bg-blue-100 border-blue-200 dark:bg-blue-955/30 dark:text-blue-400"
                            >
                              + Must-have
                            </Button>
                            <Button
                              type="button"
                              size="sm"
                              variant="outline"
                              onClick={() => addSkill(skill, false)}
                              className="h-7 text-xs bg-emerald-50 text-emerald-600 hover:bg-emerald-100 border-emerald-200 dark:bg-emerald-955/30 dark:text-emerald-400"
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
                placeholder="List major responsibilities..."
                className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-955 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
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
                placeholder="List requirements..."
                className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-955 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
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
                placeholder="List benefits..."
                className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-955 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
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
            disabled={saving}
            className="border-zinc-200/80 dark:border-zinc-800/80 hover:bg-zinc-100"
          >
            Cancel
          </Button>
          <Button 
            type="button" 
            onClick={() => handleUpdate()} 
            disabled={saving}
            className="bg-blue-600 hover:bg-blue-700 text-white shadow-md shadow-blue-500/10"
          >
            {saving ? "Saving Changes..." : "Save Change"}
          </Button>
        </div>
      </div>
    </div>
  )
}
