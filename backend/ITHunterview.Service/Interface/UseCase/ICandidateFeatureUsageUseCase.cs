using System;
using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICandidateFeatureUsageUseCase
    {
        /// <summary>
        /// Thử tiêu thụ hạn mức quota Subscription hoặc trừ Coin trong ví của Candidate.
        /// </summary>
        /// <param name="userId">Id của Candidate</param>
        /// <param name="featureKey">Tên tính năng ("CvJdMatching", "MockInterview", "CvOptimize")</param>
        /// <returns>True nếu cho phép thực hiện (đã trừ quota hoặc trừ coin thành công), ngược lại quăng Exception</returns>
        Task<bool> TryConsumeFeatureAsync(Guid userId, string featureKey);
    }
}
