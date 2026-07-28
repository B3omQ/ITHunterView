import {
  containsRawHtmlTag,
  hasJobPostingMarkdownVisibleText,
  JOB_POSTING_RICH_TEXT_LIMITS,
  normalizeJobPostingMarkdown,
  type JobPostingRichTextField,
} from "@/lib/job-posting-markdown"

export interface RecruiterJobPostingRichTextFields {
  description: string
  requirements: string
  benefits: string
  incomeText: string
}

export interface RichTextValidationError {
  field: JobPostingRichTextField
  message: string
}

const fieldLabels: Record<JobPostingRichTextField, string> = {
  description: "Job Description",
  requirements: "Requirements",
  benefits: "Benefits",
  incomeText: "Income Details",
}

export function normalizeRecruiterJobPostingRichTextFields(
  fields: RecruiterJobPostingRichTextFields,
): RecruiterJobPostingRichTextFields {
  return {
    description: normalizeJobPostingMarkdown(fields.description),
    requirements: normalizeJobPostingMarkdown(fields.requirements),
    benefits: normalizeJobPostingMarkdown(fields.benefits),
    incomeText: normalizeJobPostingMarkdown(fields.incomeText),
  }
}

export function validateRecruiterJobPostingRichTextFields(
  fields: RecruiterJobPostingRichTextFields,
): RichTextValidationError | null {
  const normalized = normalizeRecruiterJobPostingRichTextFields(fields)

  for (const field of Object.keys(JOB_POSTING_RICH_TEXT_LIMITS) as JobPostingRichTextField[]) {
    const value = normalized[field]
    const label = fieldLabels[field]
    if (!hasJobPostingMarkdownVisibleText(value)) {
      return { field, message: `${label} is required` }
    }
    if (containsRawHtmlTag(value)) {
      return { field, message: `${label} must not contain HTML tags` }
    }
    if (value.length > JOB_POSTING_RICH_TEXT_LIMITS[field]) {
      return {
        field,
        message: `${label} must not exceed ${JOB_POSTING_RICH_TEXT_LIMITS[field].toLocaleString()} characters`,
      }
    }
  }

  return null
}
