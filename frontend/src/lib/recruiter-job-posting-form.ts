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

/** Fields that must have visible text (required). */
const REQUIRED_RICH_TEXT_FIELDS: JobPostingRichTextField[] = [
  "description",
  "requirements",
  "benefits",
]

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

  // Validate required fields (description, requirements, benefits)
  for (const field of REQUIRED_RICH_TEXT_FIELDS) {
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

  // Validate incomeText only when it has content (optional field)
  const incomeValue = normalized.incomeText
  if (hasJobPostingMarkdownVisibleText(incomeValue)) {
    if (containsRawHtmlTag(incomeValue)) {
      return { field: "incomeText", message: `${fieldLabels.incomeText} must not contain HTML tags` }
    }
    if (incomeValue.length > JOB_POSTING_RICH_TEXT_LIMITS.incomeText) {
      return {
        field: "incomeText",
        message: `${fieldLabels.incomeText} must not exceed ${JOB_POSTING_RICH_TEXT_LIMITS.incomeText.toLocaleString()} characters`,
      }
    }
  }

  return null
}

