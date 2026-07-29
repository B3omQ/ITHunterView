using System;

namespace ITHunterview.Service.Exceptions
{
    public class InterviewSessionCreationException : Exception
    {
        public bool SessionPersisted { get; }

        public InterviewSessionCreationException(string message, bool sessionPersisted, Exception innerException)
            : base(message, innerException)
        {
            SessionPersisted = sessionPersisted;
        }
    }
}
