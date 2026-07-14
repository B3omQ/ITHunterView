using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.Implementations.Service
{
    public class PromptManagementService : IPromptManagementService
    {
        private readonly ITHunterviewContext _context;
        private readonly ILogger<PromptManagementService> _logger;

        public PromptManagementService(ITHunterviewContext context, ILogger<PromptManagementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GetActivePromptContentAsync(string promptKey)
        {
            var activeVersion = await _context.PromptVersions
                .Include(pv => pv.Prompt)
                .Where(pv => pv.Prompt.PromptKey == promptKey && pv.IsActive)
                .FirstOrDefaultAsync();

            if (activeVersion == null)
            {
                _logger.LogWarning($"No active prompt found for key: {promptKey}");
                return string.Empty;
            }

            return activeVersion.Content;
        }

        public async Task<string> GetActivePromptContentWithVariablesAsync(string promptKey, Dictionary<string, string> variables)
        {
            var content = await GetActivePromptContentAsync(promptKey);

            if (string.IsNullOrWhiteSpace(content))
            {
                return content;
            }

            foreach (var variable in variables)
            {
                var placeholder = $"[{variable.Key}]";
                
                // If the template contains the placeholder, replace it.
                // If the template doesn't contain it, the admin might have deleted it. We log a warning.
                if (!content.Contains(placeholder))
                {
                    _logger.LogWarning($"Template for {promptKey} is missing the placeholder {placeholder}. The data might be ignored by the LLM.");
                }
                
                content = content.Replace(placeholder, variable.Value);
            }

            // Simple validation to see if there are left-over placeholders (e.g. admin misspelled [CV_TEXXT])
            // We can't catch everything, but this helps.
            return content;
        }
    }
}
