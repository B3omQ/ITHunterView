using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.FeatureUsage;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICandidateFeatureUsageUseCase
    {
        /// <summary>
        /// Thử tiêu thụ hạn mức quota Subscription hoặc trừ Coin trong ví của Candidate.
        /// </summary>
        /// <param name="userId">Id của Candidate</param>
        /// <param name="featureKey">Tên tính năng ("CvJdMatching", "MockInterview", "CvOptimize")</param>
        /// <returns>Thông tin phần quyền lợi đã tiêu thụ; caller dùng để hoàn lại nếu tác vụ thất bại.</returns>
        Task<FeatureConsumptionResult> TryConsumeFeatureAsync(Guid userId, string featureKey, string? referenceId = null);
        Task RefundFeatureUsageAsync(Guid userId, FeatureConsumptionResult consumption, string description);
        Task RefundFeatureUsageByReferenceAsync(Guid userId, Guid referenceId, string description);
    }
}
