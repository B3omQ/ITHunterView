"use client"

import { X } from "lucide-react"
import { JobDetailPanel } from "./JobDetailPanel"

interface JobDetailModalProps {
  isOpen: boolean
  onClose: () => void
  jobId?: string
  isCandidateMode?: boolean
}

export default function JobDetailModal({ isOpen, onClose, jobId, isCandidateMode = false }: JobDetailModalProps) {
  if (!isOpen || !jobId) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 sm:p-6 backdrop-blur-sm">
      <div className="relative w-full max-w-3xl h-[90vh] md:h-[85vh] rounded-xl bg-white shadow-2xl overflow-hidden flex flex-col">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 z-20 bg-white/50 backdrop-blur-md rounded-full p-2 text-slate-500 hover:text-slate-900 hover:bg-slate-100 transition-colors"
        >
          <X className="h-5 w-5" />
        </button>

        <div className="flex-1 overflow-hidden relative">
          <JobDetailPanel jobId={jobId} isCandidateMode={isCandidateMode} />
        </div>
      </div>
    </div>
  )
}
