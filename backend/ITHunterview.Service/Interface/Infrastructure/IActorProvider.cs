using System;

namespace ITHunterview.Service.Interface.Infrastructure
{
    public interface IActorProvider
    {
        Guid? ActorUserId { get; }
        string ActorEmail { get; }
        string ActorRole { get; }
        string IpAddress { get; }
        string UserAgent { get; }
        void SetActor(Guid userId, string email, string role, string ipAddress = "system", string userAgent = "system");
    }
}
