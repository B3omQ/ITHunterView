using ITHunterview.Domain.Entities;
using System.Text;

namespace ITHunterview.Service.Helpers
{
    public static class JdTextHelper
    {
        public static string BuildRawText(JobPostings job)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(job.Title))
                sb.AppendLine($"Title: {job.Title}");

            if (!string.IsNullOrWhiteSpace(job.Description))
                sb.AppendLine($"Description: {job.Description}");

            if (!string.IsNullOrWhiteSpace(job.Requirements))
                sb.AppendLine($"Requirements: {job.Requirements}");

            if (!string.IsNullOrWhiteSpace(job.Benefits))
                sb.AppendLine($"Benefits: {job.Benefits}");

            if (!string.IsNullOrWhiteSpace(job.IncomeText))
                sb.AppendLine($"Income: {job.IncomeText}");

            if (!string.IsNullOrWhiteSpace(job.WorkLocationText))
                sb.AppendLine($"Work Location: {job.WorkLocationText}");

            return sb.ToString().TrimEnd();
        }
    }
}
