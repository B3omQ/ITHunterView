"use client"

import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { X } from "lucide-react"

interface JobFormModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: any) => Promise<void>
  initialData?: any
  title: string
}

export default function JobFormModal({
  isOpen,
  onClose,
  onSubmit,
  initialData,
  title,
}: JobFormModalProps) {
  const [formData, setFormData] = useState({
    jobCode: "",
    title: "",
    categoryId: 1, // Default category
    location: "",
    jobType: "FULL_TIME",
    status: "DRAFT",
    minSalary: "",
    maxSalary: "",
    currency: "USD",
    description: "",
    responsibilities: "",
    requirements: "",
    benefits: "",
  })
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState("")

  useEffect(() => {
    if (initialData) {
      setFormData({
        jobCode: initialData.jobCode || "",
        title: initialData.title || "",
        categoryId: initialData.categoryId || 1,
        location: initialData.location || "",
        jobType: initialData.jobType || "FULL_TIME",
        status: initialData.status || "DRAFT",
        minSalary: initialData.minSalary ? initialData.minSalary.toString() : "",
        maxSalary: initialData.maxSalary ? initialData.maxSalary.toString() : "",
        currency: initialData.currency || "USD",
        description: initialData.description || "",
        responsibilities: initialData.responsibilities || "",
        requirements: initialData.requirements || "",
        benefits: initialData.benefits || "",
      })
    } else {
      setFormData({
        jobCode: "",
        title: "",
        categoryId: 1,
        location: "",
        jobType: "FULL_TIME",
        status: "DRAFT",
        minSalary: "",
        maxSalary: "",
        currency: "USD",
        description: "",
        responsibilities: "",
        requirements: "",
        benefits: "",
      })
    }
    setError("")
  }, [initialData, isOpen])

  if (!isOpen) return null

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
  ) => {
    const { name, value } = e.target
    setFormData((prev) => ({ ...prev, [name]: value }))
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError("")
    setLoading(true)

    try {
      const payload = {
        ...formData,
        categoryId: Number(formData.categoryId),
        minSalary: formData.minSalary ? Number(formData.minSalary) : null,
        maxSalary: formData.maxSalary ? Number(formData.maxSalary) : null,
      }
      await onSubmit(payload)
      onClose()
    } catch (err: any) {
      setError(err.message || "Failed to submit job posting.")
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-xs">
      <div className="relative w-full max-w-2xl max-h-[90vh] overflow-y-auto rounded-xl bg-white p-6 shadow-2xl dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800 transition-all duration-200">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-200 transition-colors"
        >
          <X className="h-5 w-5" />
        </button>

        <h2 className="text-xl font-bold mb-4 text-zinc-900 dark:text-zinc-50">{title}</h2>
        
        {error && (
          <div className="mb-4 text-sm font-medium text-red-500 bg-red-50 dark:bg-red-950/30 p-3 rounded-lg border border-red-200 dark:border-red-900">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="title">Job Title</Label>
              <Input
                id="title"
                name="title"
                placeholder="e.g. Senior Backend Engineer"
                required
                value={formData.title}
                onChange={handleChange}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="jobCode">Job Code</Label>
              <Input
                id="jobCode"
                name="jobCode"
                placeholder="e.g. BE-3005"
                required
                value={formData.jobCode}
                onChange={handleChange}
              />
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="space-y-2">
              <Label htmlFor="jobType">Job Type</Label>
              <select
                id="jobType"
                name="jobType"
                className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
                value={formData.jobType}
                onChange={handleChange}
              >
                <option value="FULL_TIME">Full Time</option>
                <option value="PART_TIME">Part Time</option>
                <option value="CONTRACT">Contract</option>
                <option value="FREELANCE">Freelance</option>
                <option value="INTERNSHIP">Internship</option>
              </select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="status">Status</Label>
              <select
                id="status"
                name="status"
                className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
                value={formData.status}
                onChange={handleChange}
              >
                <option value="DRAFT">Draft</option>
                <option value="PUBLISHED">Active / Published</option>
                <option value="CLOSED">Closed</option>
              </select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="location">Location</Label>
              <Input
                id="location"
                name="location"
                placeholder="e.g. Remote, Hanoi, Hybrid"
                required
                value={formData.location}
                onChange={handleChange}
              />
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="space-y-2">
              <Label htmlFor="minSalary">Min Salary</Label>
              <Input
                id="minSalary"
                name="minSalary"
                type="number"
                placeholder="Min salary"
                value={formData.minSalary}
                onChange={handleChange}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="maxSalary">Max Salary</Label>
              <Input
                id="maxSalary"
                name="maxSalary"
                type="number"
                placeholder="Max salary"
                value={formData.maxSalary}
                onChange={handleChange}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="currency">Currency</Label>
              <Input
                id="currency"
                name="currency"
                placeholder="USD"
                value={formData.currency}
                onChange={handleChange}
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="description">Job Description</Label>
            <textarea
              id="description"
              name="description"
              rows={3}
              placeholder="Describe the job position..."
              className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
              value={formData.description}
              onChange={handleChange}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="responsibilities">Responsibilities</Label>
            <textarea
              id="responsibilities"
              name="responsibilities"
              rows={2}
              placeholder="What are the key responsibilities..."
              className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
              value={formData.responsibilities}
              onChange={handleChange}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="requirements">Requirements</Label>
            <textarea
              id="requirements"
              name="requirements"
              rows={2}
              placeholder="Candidate requirements..."
              className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
              value={formData.requirements}
              onChange={handleChange}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="benefits">Benefits</Label>
            <textarea
              id="benefits"
              name="benefits"
              rows={2}
              placeholder="Benefits, perks, environment..."
              className="w-full rounded-md border border-zinc-200 dark:border-zinc-800 bg-white dark:bg-zinc-950 px-3 py-2 text-sm text-zinc-950 dark:text-zinc-50 focus:outline-hidden focus:ring-2 focus:ring-blue-500"
              value={formData.benefits}
              onChange={handleChange}
            />
          </div>

          <div className="flex justify-end gap-3 mt-6 pt-4 border-t border-zinc-200 dark:border-zinc-800">
            <Button type="button" variant="outline" onClick={onClose} disabled={loading}>
              Cancel
            </Button>
            <Button type="submit" className="bg-blue-600 hover:bg-blue-700 text-white" disabled={loading}>
              {loading ? "Saving..." : "Save Job Posting"}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
