using System;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching;

public interface IRecruiterUnlockedCvSnapshotStore
{
    Task<RetainedCvSnapshot> CaptureAsync(Guid unlockId, Cvs cv, CancellationToken ct);
    Task<string> CreateAuthorizedReadUrlAsync(string storageKey, CancellationToken ct);
}
