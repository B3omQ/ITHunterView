import React from "react";
import { parseWorkLocationText } from "@/lib/job-posting-text";
import { JobTextContent } from "./JobTextContent";
import { cn } from "@/lib/utils";

export interface WorkLocationScheduleContentProps {
  workLocationText: string | null | undefined;
  className?: string;
  subheadingClassName?: string;
}

export const WorkLocationScheduleContent: React.FC<WorkLocationScheduleContentProps> = ({
  workLocationText,
  className,
  subheadingClassName,
}) => {
  const details = parseWorkLocationText(workLocationText);

  if (!details.workLocation && !details.workingHours && !details.howToApply) {
    return null;
  }

  const defaultSubheadingClass = "text-sm font-semibold text-zinc-900 dark:text-zinc-100 mb-1";

  return (
    <div className={cn("space-y-4", className)}>
      {/* 1. Work Location */}
      {details.workLocation ? (
        <div>
          <h4 className={cn(defaultSubheadingClass, subheadingClassName)}>Work Location</h4>
          <JobTextContent
            value={details.workLocation}
            variant="bullet"
          />
        </div>
      ) : null}

      {/* 2. Working Hours */}
      {details.workingHours ? (
        <div>
          <h4 className={cn(defaultSubheadingClass, subheadingClassName)}>Working Hours</h4>
          <JobTextContent value={details.workingHours} variant="bullet" />
        </div>
      ) : null}

      {/* 3. How to Apply */}
      {details.howToApply ? (
        <div>
          <h4 className={cn(defaultSubheadingClass, subheadingClassName)}>How to Apply</h4>
          <JobTextContent value={details.howToApply} variant="bullet" allowInlineStrong={true} />
        </div>
      ) : null}
    </div>
  );
};
