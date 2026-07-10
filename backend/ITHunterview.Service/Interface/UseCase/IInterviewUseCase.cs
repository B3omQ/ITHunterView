using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Interview;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IInterviewUseCase
    {
        Task<List<InterviewSessionDto>> GetCandidateSessionsAsync(Guid candidateId);
        Task<InterviewSessionDetailDto> GetSessionDetailAsync(Guid sessionId, Guid candidateId);
        Task<InterviewSessionDto> CreateSessionAsync(Guid candidateId, CreateInterviewSessionDto dto);
        Task<InterviewAnswerDto> SubmitReplyAsync(Guid sessionId, Guid candidateId, SubmitReplyDto dto);
        Task SwitchModelAsync(Guid sessionId, Guid candidateId, SwitchModelDto dto);
        Task CompleteSessionAsync(Guid sessionId, Guid candidateId);
        Task DeleteSessionAsync(Guid sessionId, Guid candidateId);
    }
}
