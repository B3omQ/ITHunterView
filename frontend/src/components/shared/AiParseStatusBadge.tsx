import { AlertCircle, Clock3, Loader2, RefreshCw, Sparkles } from 'lucide-react'
import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

interface AiParseStatusBadgeProps {
  status?: 'NOT_REQUESTED' | 'PENDING' | 'PROCESSING' | 'READY' | 'SUCCESS' | 'FAILED' | 'STALE' | string
  error?: string | null
  className?: string
  forCandidate?: boolean
}

export function AiParseStatusBadge({ status, error, className, forCandidate = false }: AiParseStatusBadgeProps) {
  const normalized = status?.toUpperCase()

  if (forCandidate || !normalized) return null

  if (normalized === 'PENDING' || normalized === 'PROCESSING') {
    return <StatusBadge className={className} title="AI system is scanning job description (JD) to extract skills and requirements..." icon={<Loader2 className="size-3 animate-spin text-amber-500" />} tone="amber">AI Scanning JD...</StatusBadge>
  }
  if (normalized === 'STALE') {
    return <StatusBadge className={className} title="The job description content has changed. Please re-scan with AI to update skill tags." icon={<RefreshCw className="size-3 text-amber-500" />} tone="amber">Re-scan Needed</StatusBadge>
  }
  if (normalized === 'READY') {
    return <StatusBadge className={className} title="AI has finished scanning skill tags from JD, waiting for review and publication." icon={<Clock3 className="size-3 text-amber-500" />} tone="amber">AI Scanned • Pending Review</StatusBadge>
  }
  if (normalized === 'FAILED') {
    return <StatusBadge className={className} title={error || 'An error occurred while scanning skills with AI. Please check JD content or try again.'} icon={<AlertCircle className="size-3 text-rose-500" />} tone="red">AI Scan Error</StatusBadge>
  }
  if (normalized === 'SUCCESS') {
    return <StatusBadge className={className} title="AI completed scanning job description, extracted and tagged standard skills for candidate matching." icon={<Sparkles className="size-3 text-emerald-500 fill-emerald-500/20 shrink-0" />} tone="green">AI Scanned & Tagged</StatusBadge>
  }
  if (normalized === 'RAW_FALLBACK') {
    return <StatusBadge className={className} title="Structured AI analysis was unavailable. The original job description is retained for matching." icon={<AlertCircle className="size-3 text-amber-500" />} tone="amber">AI Partial - Raw JD Fallback</StatusBadge>
  }
  if (normalized === 'NOT_REQUESTED') {
    return <StatusBadge className={className} title="This job posting has not been scanned by AI for skills and tagging." icon={<Sparkles className="size-3 text-zinc-400" />} tone="neutral">AI Not Scanned</StatusBadge>
  }

  return <StatusBadge className={className} title="AI scan status unavailable." icon={<AlertCircle className="size-3" />} tone="neutral">Unknown AI Status</StatusBadge>
}

function StatusBadge({
  children,
  icon,
  title,
  tone,
  className,
}: {
  children: ReactNode
  icon: ReactNode
  title: string
  tone: 'amber' | 'blue' | 'red' | 'green' | 'neutral'
  className?: string
}) {
  const tones = {
    amber: 'border-amber-500/20 bg-amber-500/10 text-amber-700 dark:text-amber-400',
    blue: 'border-blue-500/20 bg-blue-500/10 text-blue-700 dark:text-blue-400',
    red: 'border-rose-500/20 bg-rose-500/10 text-rose-600 dark:text-rose-400',
    green: 'border-emerald-500/20 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400',
    neutral: 'border-zinc-200 dark:border-zinc-800 bg-zinc-100 dark:bg-zinc-800/60 text-zinc-600 dark:text-zinc-400',
  }
  return <span title={title} className={cn('inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-medium', tones[tone], className)}>{icon}{children}</span>
}
