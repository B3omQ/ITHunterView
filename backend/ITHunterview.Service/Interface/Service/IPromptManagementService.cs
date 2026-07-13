using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.Service
{
    public interface IPromptManagementService
    {
        Task<string> GetActivePromptContentAsync(string promptKey);
        Task<string> GetActivePromptContentWithVariablesAsync(string promptKey, Dictionary<string, string> variables);
    }
}
