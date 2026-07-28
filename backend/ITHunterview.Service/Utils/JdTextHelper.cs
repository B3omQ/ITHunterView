using ITHunterview.Domain.Entities;
using System.Text;

namespace ITHunterview.Service.Utils
{
    public static class JdTextHelper
    {
        public static string BuildRawText(JobPostings job)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(job.Title))
                sb.AppendLine($"Title: {job.Title}");

            if (!string.IsNullOrWhiteSpace(job.Description))
                sb.AppendLine($"Description: {JobPostingRichText.ToPlainText(job.Description)}");

            if (!string.IsNullOrWhiteSpace(job.Requirements))
                sb.AppendLine($"Requirements: {JobPostingRichText.ToPlainText(job.Requirements)}");

            if (!string.IsNullOrWhiteSpace(job.Benefits))
                sb.AppendLine($"Benefits: {JobPostingRichText.ToPlainText(job.Benefits)}");

            if (!string.IsNullOrWhiteSpace(job.IncomeText))
                sb.AppendLine($"Income: {JobPostingRichText.ToPlainText(job.IncomeText)}");

            if (!string.IsNullOrWhiteSpace(job.WorkLocationText))
            {
                var formattedLocation = WorkLocationTextHelper.FormatForAi(job.WorkLocationText);
                if (!string.IsNullOrWhiteSpace(formattedLocation))
                    sb.AppendLine(formattedLocation);
            }

            return sb.ToString().TrimEnd();
        }
    }
}
