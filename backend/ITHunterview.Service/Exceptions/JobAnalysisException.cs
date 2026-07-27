using System;

namespace ITHunterview.Service.Exceptions
{
    public class JobAnalysisException : Exception
    {
        public string Code { get; }
        public int HttpStatus { get; }
        public string SafeMessage { get; }
        public Guid? CorrelationId { get; }

        public JobAnalysisException(
            string code,
            int httpStatus,
            string safeMessage,
            Guid? correlationId = null,
            Exception? innerException = null)
            : base(safeMessage, innerException)
        {
            Code = code;
            HttpStatus = httpStatus;
            SafeMessage = safeMessage;
            CorrelationId = correlationId;
        }

        public static JobAnalysisException AnalysisStale(string message = "Semantic input has changed. Please request a new analysis.")
        {
            return new JobAnalysisException("ANALYSIS_STALE", 409, message);
        }

        public static JobAnalysisException DecisionVersionConflict(string message = "Decisions have been updated by another session. Please refresh.")
        {
            return new JobAnalysisException("DECISION_VERSION_CONFLICT", 409, message);
        }

        public static JobAnalysisException RunNotActive(string message = "The specified analysis run is no longer active.")
        {
            return new JobAnalysisException("RUN_NOT_ACTIVE", 409, message);
        }

        public static JobAnalysisException InvalidPayload(string message)
        {
            return new JobAnalysisException("INVALID_PAYLOAD", 400, message);
        }

        public static JobAnalysisException QuotaExceeded(string message, int retryAfterSeconds = 60)
        {
            return new JobAnalysisException("QUOTA_EXCEEDED", 429, message);
        }

        public static JobAnalysisException IncompleteReview(string message)
        {
            return new JobAnalysisException("INCOMPLETE_REVIEW", 422, message);
        }
    }
}
