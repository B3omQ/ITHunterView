"use client"

import { Button } from "@/components/ui/button"
import { X, Briefcase, MapPin, DollarSign, Calendar, FileText } from "lucide-react"

interface JobDetailModalProps {
  isOpen: boolean
  onClose: () => void
  job: any
}

export default function JobDetailModal({ isOpen, onClose, job }: JobDetailModalProps) {
  if (!isOpen || !job) return null

  // Function to format Date string
  const formatDate = (dateStr: string) => {
    if (!dateStr) return "N/A"
    return new Date(dateStr).toLocaleDateString("en-US", {
      year: "numeric",
      month: "long",
      day: "numeric",
    })
  }

  // Format Status Badge
  const getStatusBadge = (status: string) => {
    switch (status) {
      case "PUBLISHED":
        return <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-emerald-100 text-emerald-800 dark:bg-emerald-950/50 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-900">Active</span>
      case "DRAFT":
        return <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-zinc-100 text-zinc-800 dark:bg-zinc-800/50 dark:text-zinc-400 border border-zinc-200 dark:border-zinc-800">Draft</span>
      case "CLOSED":
        return <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-rose-100 text-rose-800 dark:bg-rose-950/50 dark:text-rose-400 border border-rose-200 dark:border-rose-900">Closed</span>
      default:
        return <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-zinc-100 text-zinc-800 dark:bg-zinc-800/50 dark:text-zinc-400 border border-zinc-200 dark:border-zinc-800">{status}</span>
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-xs">
      <div className="relative w-full max-w-2xl max-h-[90vh] overflow-y-auto rounded-xl bg-white p-6 shadow-2xl dark:bg-zinc-900 border border-zinc-200 dark:border-zinc-800">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-200 transition-colors"
        >
          <X className="h-5 w-5" />
        </button>

        {/* Header */}
        <div className="mb-6 border-b border-zinc-200 dark:border-zinc-800 pb-4 pr-6">
          <div className="flex items-center gap-3 mb-2">
            <h2 className="text-2xl font-bold text-zinc-900 dark:text-zinc-50">{job.title}</h2>
            {getStatusBadge(job.status)}
          </div>
          <p className="text-sm text-zinc-500 dark:text-zinc-400 font-mono">Job Code: {job.jobCode}</p>
        </div>

        {/* Quick Info Grid */}
        <div className="grid grid-cols-2 gap-4 mb-6 bg-zinc-50 dark:bg-zinc-950/50 p-4 rounded-xl border border-zinc-100 dark:border-zinc-900/50">
          <div className="flex items-center gap-2 text-sm text-zinc-600 dark:text-zinc-300">
            <Briefcase className="h-4 w-4 text-zinc-400" />
            <span><strong>Type:</strong> {job.jobType.replace("_", " ")}</span>
          </div>
          <div className="flex items-center gap-2 text-sm text-zinc-600 dark:text-zinc-300">
            <MapPin className="h-4 w-4 text-zinc-400" />
            <span><strong>Location:</strong> {job.location}</span>
          </div>
          <div className="flex items-center gap-2 text-sm text-zinc-600 dark:text-zinc-300">
            <DollarSign className="h-4 w-4 text-zinc-400" />
            <span>
              <strong>Salary:</strong>{" "}
              {job.minSalary || job.maxSalary
                ? `${job.minSalary?.toLocaleString() || "0"} - ${job.maxSalary?.toLocaleString() || "∞"} ${job.currency}`
                : "Negotiable"}
            </span>
          </div>
          <div className="flex items-center gap-2 text-sm text-zinc-600 dark:text-zinc-300">
            <Calendar className="h-4 w-4 text-zinc-400" />
            <span><strong>Posted:</strong> {formatDate(job.publishedAt || job.createdAt)}</span>
          </div>
        </div>

        {/* Sections */}
        <div className="space-y-6">
          <div>
            <h3 className="text-sm font-bold uppercase tracking-wider text-zinc-400 mb-2 flex items-center gap-1.5">
              <FileText className="h-4 w-4" /> Description
            </h3>
            <p className="text-sm text-zinc-700 dark:text-zinc-300 whitespace-pre-wrap leading-relaxed">
              {job.description || "No description provided."}
            </p>
          </div>

          {job.responsibilities && (
            <div>
              <h3 className="text-sm font-bold uppercase tracking-wider text-zinc-400 mb-2">Responsibilities</h3>
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

        <div className="flex justify-end mt-8 pt-4 border-t border-zinc-200 dark:border-zinc-800">
          <Button variant="outline" onClick={onClose}>
            Close
          </Button>
        </div>
      </div>
    </div>
  )
}
