export const WORK_LOCATION_TEXT_VERSION = 1;

export const DEFAULT_HOW_TO_APPLY =
  "Ứng viên nộp hồ sơ trực tuyến bằng cách bấm **Ứng tuyển** ngay dưới đây.";

export interface WorkLocationDetails {
  workLocation: string;
  workingHours: string;
  howToApply: string;
}

export interface ParsedWorkLocationDetails extends WorkLocationDetails {
  source: "structured" | "legacy" | "empty";
  version: number;
  unsupportedVersion: boolean;
}

/**
 * Normalizes multiline text by converting CRLF/CR to LF,
 * trimming trailing whitespace on each line, and trimming the overall string.
 */
export function normalizeMultilineText(value: string | null | undefined): string {
  if (!value) return "";
  const lfNormalized = value.replace(/\r\n/g, "\n").replace(/\r/g, "\n");
  const trimmedLines = lfNormalized
    .split("\n")
    .map((line) => line.trimEnd());
  return trimmedLines.join("\n").trim();
}

/**
 * Splits text into non-empty lines and strips leading bullet markers.
 */
export function splitTextLines(value: string | null | undefined): string[] {
  if (!value) return [];
  const normalized = normalizeMultilineText(value);
  if (!normalized) return [];

  return normalized
    .split("\n")
    .map((line) => line.trim())
    .filter((line) => line.length > 0)
    .map((line) => {
      // Strip leading bullet markers: '-', '*', '•', '1.', '1)', etc.
      return line.replace(/^([-*•]|\d+[\.\)])\s*/, "").trim();
    })
    .filter((line) => line.length > 0);
}

function parseLegacyHeaders(text: string): WorkLocationDetails | null {
  const lines = text.split("\n").map((l) => l.trim()).filter((l) => l.length > 0);

  let currentSection: "location" | "hours" | "apply" = "location";
  const locationLines: string[] = [];
  const hoursLines: string[] = [];
  const applyLines: string[] = [];
  let foundHeaders = false;

  for (const line of lines) {
    const lower = line.toLowerCase().replace(/[:：]/g, "").trim();

    if (
      lower === "địa điểm và thời gian" ||
      lower === "địa điểm & thời gian" ||
      lower === "thông tin làm việc"
    ) {
      foundHeaders = true;
      continue;
    }
    if (
      lower === "địa điểm làm việc" ||
      lower === "địa điểm" ||
      lower === "work location"
    ) {
      currentSection = "location";
      foundHeaders = true;
      continue;
    }
    if (
      lower === "thời gian làm việc" ||
      lower === "thời gian" ||
      lower === "working hours" ||
      lower === "working hour"
    ) {
      currentSection = "hours";
      foundHeaders = true;
      continue;
    }
    if (
      lower === "cách thức ứng tuyển" ||
      lower === "hướng dẫn ứng tuyển" ||
      lower === "how to apply"
    ) {
      currentSection = "apply";
      foundHeaders = true;
      continue;
    }

    if (currentSection === "location") {
      locationLines.push(line);
    } else if (currentSection === "hours") {
      hoursLines.push(line);
    } else if (currentSection === "apply") {
      applyLines.push(line);
    }
  }

  if (!foundHeaders) {
    return null;
  }

  const rawApply = applyLines.join("\n").trim();
  let finalApply = DEFAULT_HOW_TO_APPLY;
  if (rawApply) {
    finalApply = rawApply.replace(/\*\*Ứng tuyển\*\*/g, "Ứng tuyển").replace(/Ứng tuyển/g, "**Ứng tuyển**");
  }

  return {
    workLocation: locationLines.join("\n").trim(),
    workingHours: hoursLines.join("\n").trim(),
    howToApply: finalApply,
  };
}

/**
 * Parses WorkLocationText string into structured details.
 */
export function parseWorkLocationText(
  value: string | null | undefined
): ParsedWorkLocationDetails {
  if (!value || !value.trim()) {
    return {
      source: "empty",
      version: WORK_LOCATION_TEXT_VERSION,
      workLocation: "",
      workingHours: "",
      howToApply: DEFAULT_HOW_TO_APPLY,
      unsupportedVersion: false,
    };
  }

  const trimmed = value.trim();

  if (trimmed.startsWith("{") && trimmed.endsWith("}")) {
    try {
      const parsed = JSON.parse(trimmed);
      if (parsed && typeof parsed === "object" && !Array.isArray(parsed)) {
        if (parsed.version === WORK_LOCATION_TEXT_VERSION) {
          return {
            source: "structured",
            version: WORK_LOCATION_TEXT_VERSION,
            workLocation: typeof parsed.workLocation === "string" ? parsed.workLocation : "",
            workingHours: typeof parsed.workingHours === "string" ? parsed.workingHours : "",
            howToApply:
              typeof parsed.howToApply === "string" && parsed.howToApply.trim()
                ? parsed.howToApply
                : DEFAULT_HOW_TO_APPLY,
            unsupportedVersion: false,
          };
        } else if (typeof parsed.version === "number" && parsed.version > WORK_LOCATION_TEXT_VERSION) {
          return {
            source: "structured",
            version: parsed.version,
            workLocation: typeof parsed.workLocation === "string" ? parsed.workLocation : trimmed,
            workingHours: typeof parsed.workingHours === "string" ? parsed.workingHours : "",
            howToApply:
              typeof parsed.howToApply === "string" && parsed.howToApply.trim()
                ? parsed.howToApply
                : DEFAULT_HOW_TO_APPLY,
            unsupportedVersion: true,
          };
        }
      }
    } catch {
      // Fallback to legacy plain text handling if JSON parsing fails
    }
  }

  // Try parsing legacy plain text with Vietnamese/English headers
  const legacyHeaderParsed = parseLegacyHeaders(trimmed);
  if (legacyHeaderParsed) {
    return {
      source: "legacy",
      version: 0,
      workLocation: legacyHeaderParsed.workLocation,
      workingHours: legacyHeaderParsed.workingHours,
      howToApply: legacyHeaderParsed.howToApply,
      unsupportedVersion: false,
    };
  }

  return {
    source: "legacy",
    version: 0,
    workLocation: value,
    workingHours: "",
    howToApply: DEFAULT_HOW_TO_APPLY,
    unsupportedVersion: false,
  };
}

/**
 * Serializes WorkLocationDetails into JSON V1 string format.
 */
export function serializeWorkLocationText(details: WorkLocationDetails): string {
  const payload = {
    version: WORK_LOCATION_TEXT_VERSION,
    workLocation: normalizeMultilineText(details.workLocation),
    workingHours: normalizeMultilineText(details.workingHours),
    howToApply: details.howToApply.trim()
      ? normalizeMultilineText(details.howToApply)
      : DEFAULT_HOW_TO_APPLY,
  };
  return JSON.stringify(payload);
}

/**
 * Calculates the exact serialized length of WorkLocationDetails.
 */
export function getSerializedWorkLocationLength(details: WorkLocationDetails): number {
  return serializeWorkLocationText(details).length;
}
