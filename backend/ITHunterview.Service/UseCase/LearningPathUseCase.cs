using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.LearningPath;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class LearningPathUseCase : ILearningPathUseCase
    {
        private readonly ILearningPathRepository _learningPathRepository;
        private readonly IAiService _aiService;

        public LearningPathUseCase(
            ILearningPathRepository learningPathRepository,
            IAiService aiService)
        {
            _learningPathRepository = learningPathRepository;
            _aiService = aiService;
        }

        public async Task<LearningPathResponseDto> GenerateLearningPathAsync(Guid candidateId, GeneratePathRequestDto request)
        {
            string systemPrompt = @"You are an expert IT career coach. 
Generate a comprehensive, step-by-step learning path based on the user's current skills and target role.
The result MUST be a valid JSON array of objects, where each object represents a learning module.
Example output format:
[
  {
    ""title"": ""Module 1: Introduction"",
    ""description"": ""Basic concepts."",
    ""durationWeeks"": 2,
    ""skills"": [""Skill A"", ""Skill B""]
  }
]
Do NOT include any markdown blocks like ```json, just return the raw JSON array.";

            string userPrompt = $@"
Target Role: {request.TargetRole}
Current Skills: {request.CurrentSkills}
Target Skills: {request.TargetSkills}
Desired Timeframe: {request.TimeframeInWeeks} weeks.

Please generate a structured learning path.";

            var aiResponseText = await _aiService.GenerateTextAsync(userPrompt, systemPrompt);

            // Clean up the text in case AI still adds markdown
            aiResponseText = aiResponseText.Trim();
            if (aiResponseText.StartsWith("```json"))
            {
                aiResponseText = aiResponseText.Substring(7);
            }
            if (aiResponseText.StartsWith("```"))
            {
                aiResponseText = aiResponseText.Substring(3);
            }
            if (aiResponseText.EndsWith("```"))
            {
                aiResponseText = aiResponseText.Substring(0, aiResponseText.Length - 3);
            }
            aiResponseText = aiResponseText.Trim();

            // Validate if it is valid JSON
            JsonDocument jsonDoc;
            try
            {
                jsonDoc = JsonDocument.Parse(aiResponseText);
            }
            catch (Exception)
            {
                throw new Exception("AI generated an invalid JSON response. Please try again.");
            }

            var learningPath = new LearningPaths
            {
                Id = Guid.NewGuid(),
                CandidateId = candidateId,
                PathData = aiResponseText, // store raw json
                CreatedAt = DateTime.UtcNow
            };

            await _learningPathRepository.AddAsync(learningPath);

            return new LearningPathResponseDto
            {
                Id = learningPath.Id,
                CandidateId = learningPath.CandidateId,
                PathData = jsonDoc,
                CreatedAt = learningPath.CreatedAt
            };
        }

        public async Task<List<LearningPathResponseDto>> GetMyLearningPathsAsync(Guid candidateId)
        {
            var paths = await _learningPathRepository.GetByCandidateIdAsync(candidateId);
            return paths.Select(p => new LearningPathResponseDto
            {
                Id = p.Id,
                CandidateId = p.CandidateId,
                PathData = JsonDocument.Parse(p.PathData),
                CreatedAt = p.CreatedAt
            }).ToList();
        }

        public async Task<LearningPathResponseDto> GetLearningPathByIdAsync(Guid candidateId, Guid id)
        {
            var path = await _learningPathRepository.GetByIdAsync(id);
            if (path == null || path.CandidateId != candidateId)
            {
                throw new KeyNotFoundException("Learning path not found.");
            }

            return new LearningPathResponseDto
            {
                Id = path.Id,
                CandidateId = path.CandidateId,
                PathData = JsonDocument.Parse(path.PathData),
                CreatedAt = path.CreatedAt
            };
        }
    }
}
