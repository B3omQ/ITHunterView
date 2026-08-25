using System;
using System.Threading;
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
        Task<FeatureConsumptionResult> TryConsumePushTopAsync(Guid userId, string? referenceId, FeatureConsumptionExpectation expectation);
        Task RefundFeatureUsageAsync(Guid userId, FeatureConsumptionResult consumption, string description);
        Task RefundFeatureUsageByReferenceAsync(Guid userId, Guid referenceId, string description);

        /// <summary>
        /// Reserves a subscription entitlement or pay-as-you-go coin amount
        /// for a durable matching job. The caller owns the surrounding
        /// transaction when it needs to commit the job and billing together.
        /// </summary>
        Task<FeatureReservationResult> ReserveFeatureAsync(
            Guid userId,
            string featureKey,
            Guid referenceId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Acquires the per-user wallet lock inside the caller's transaction.
        /// Matching submission uses it before the idempotency recheck.
        /// </summary>
        Task AcquireFeatureSubmissionLockAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        /// <summary>Captures a previously reserved entitlement exactly once.</summary>
        Task CaptureFeatureReservationAsync(
            Guid reservationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases an un-captured reservation or refunds a captured one.
        /// Repeated calls are idempotent.
        /// </summary>
        Task RefundFeatureReservationAsync(
            Guid userId,
            Guid referenceId,
            string reasonCode,
            CancellationToken cancellationToken = default);
    }
}
