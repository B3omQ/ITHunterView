"use client"

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import type { JobPosting } from "@/services/recruiter.service"
import { JobTextContent } from "@/components/jobs/JobTextContent"
import { WorkLocationScheduleContent } from "@/components/jobs/WorkLocationScheduleContent"

interface CandidateJobPreviewProps {
  job: JobPosting
}

export function CandidateJobPreview({ job }: CandidateJobPreviewProps) {
  return (
    <Card>
      <CardHeader className="border-b">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <CardTitle className="text-2xl">{job.title}</CardTitle>
            <div className="mt-2 flex flex-wrap gap-2 text-sm text-muted-foreground">
              {job.level && <Badge variant="secondary">{job.level}</Badge>}
              {job.workingModel && <Badge variant="outline">{job.workingModel}</Badge>}
              {job.location && <span>{job.location}</span>}
            </div>
          </div>
          <Badge variant="secondary">Candidate preview</Badge>
        </div>
      </CardHeader>

      <CardContent className="space-y-8 pt-6">
        <div>
          <h3 className="mb-3 text-base font-semibold">Job Description</h3>
          <JobTextContent value={job.description} variant="bullet" emptyFallback="No job description provided." />
        </div>

        {job.incomeText && (
          <div>
            <h3 className="mb-3 text-base font-semibold">Income</h3>
            <JobTextContent value={job.incomeText} variant="bullet" />
          </div>
        )}

        {job.workLocationText && (
          <div>
            <h3 className="mb-3 text-base font-semibold">Work Location & Schedule</h3>
            <WorkLocationScheduleContent workLocationText={job.workLocationText} />
          </div>
        )}

        <div>
          <h3 className="mb-3 text-base font-semibold">Requirements</h3>
          <JobTextContent value={job.requirements} variant="bullet" emptyFallback="No requirements provided." />
        </div>

        {job.benefits && (
          <div>
            <h3 className="mb-3 text-base font-semibold">Benefits</h3>
            <JobTextContent value={job.benefits} variant="bullet" />
          </div>
        )}
      </CardContent>
    </Card>
  )
}
