import { AlertCircle, CheckCircle2, Clock3, Loader2, RefreshCw, Sparkles } from 'lucide-react'
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
    return <StatusBadge className={className} title="AI is analyzing the current draft" icon={<Loader2 className="size-3 animate-spin" />} tone="amber">AI analyzing</StatusBadge>
  }
  if (normalized === 'STALE') {
    return <StatusBadge className={className} title="Job requirements changed. Run analysis again before publishing." icon={<RefreshCw className="size-3" />} tone="amber">Analysis needs refresh</StatusBadge>
  }
  if (normalized === 'READY') {
    return <StatusBadge className={className} title="AI analysis is ready for the recruiter review step." icon={<Clock3 className="size-3" />} tone="blue">Ready for review</StatusBadge>
  }
  if (normalized === 'FAILED') {
    return <StatusBadge className={className} title={error || 'AI analysis failed'} icon={<AlertCircle className="size-3" />} tone="red">AI failed</StatusBadge>
  }
  if (normalized === 'SUCCESS') {
    return <StatusBadge className={className} title="AI analysis was finalized with this job." icon={<CheckCircle2 className="size-3" />} tone="green">AI finalized</StatusBadge>
  }
  if (normalized === 'NOT_REQUESTED') {
    return <StatusBadge className={className} title="This draft has not been analyzed yet." icon={<Sparkles className="size-3" />} tone="neutral">Not analyzed</StatusBadge>
  }

  return <StatusBadge className={className} title="AI analysis status is unavailable." icon={<AlertCircle className="size-3" />} tone="neutral">Status unavailable</StatusBadge>
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
    red: 'border-destructive/20 bg-destructive/10 text-destructive',
    green: 'border-emerald-500/20 bg-emerald-500/10 text-emerald-700 dark:text-emerald-400',
    neutral: 'border-border bg-muted text-muted-foreground',
  }
  return <span title={title} className={cn('inline-flex items-center gap-1.5 rounded-full border px-2.5 py-0.5 text-xs font-medium', tones[tone], className)}>{icon}{children}</span>
}
