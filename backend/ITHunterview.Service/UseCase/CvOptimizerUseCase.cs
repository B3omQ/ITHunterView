using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.CvOptimizer;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class CvOptimizerUseCase : ICvOptimizerUseCase
    {
        private readonly ICvOptimizationRepository _optimizationRepository;
        private readonly ICvRepository _cvRepository;
        private readonly IAiService _aiService;
        private readonly ICvTextExtractorService _cvTextExtractorService;

        public CvOptimizerUseCase(
            ICvOptimizationRepository optimizationRepository,
            ICvRepository cvRepository,
            IAiService aiService,
            ICvTextExtractorService cvTextExtractorService)
        {
            _optimizationRepository = optimizationRepository;
            _cvRepository = cvRepository;
            _aiService = aiService;
            _cvTextExtractorService = cvTextExtractorService;
        }

        public async Task<CvOptimizationResponseDto> OptimizeCvAsync(Guid candidateId, OptimizeCvRequestDto request)
        {
            var cv = await _cvRepository.GetByIdAsync(request.CvId);
            if (cv == null || cv.UserId != candidateId)
            {
                throw new KeyNotFoundException("CV không tồn tại hoặc không thuộc quyền sở hữu của bạn.");
            }

            string systemPrompt = @"You are an expert ATS system and a senior Technical Recruiter.
Analyze the candidate's CV. If a Job Description (JD) is provided, optimize the CV to better match the JD.
If no JD is provided, optimize the CV for general ATS best practices and strong impact.

You MUST return a valid JSON object strictly following this structure:
{
  ""strengths"": [""Strength 1"", ""Strength 2""],
  ""weaknesses"": [""Weakness 1"", ""Weakness 2""],
  ""missingKeywords"": [""Keyword 1"", ""Keyword 2""],
  ""suggestedEdits"": [
    {
      ""section"": ""Experience"",
      ""originalText"": ""Did some coding in React."",
      ""suggestedText"": ""Developed scalable frontend applications using React, improving render performance by 20%."",
      ""reason"": ""Action verbs and metrics improve ATS scoring.""
    }
  ],
  ""overallScore"": 85
}

Do NOT include any markdown blocks like ```json, just return the raw JSON string.";

            string cvText = cv.ParsedData;
            if (string.IsNullOrWhiteSpace(cvText))
            {
                cvText = await _cvTextExtractorService.ExtractTextFromUrlAsync(cv.FileUrl);
                if (string.IsNullOrWhiteSpace(cvText))
                {
                    throw new Exception("Không thể trích xuất nội dung từ CV. Vui lòng đảm bảo file CV hợp lệ.");
                }
                
                // Cache it back to DB
                cv.ParsedData = cvText;
                await _cvRepository.UpdateAsync(cv);
            }

            string userPrompt = $"CV Content:\n{cvText}\n\n";
            if (!string.IsNullOrWhiteSpace(request.TargetJdText))
            {
                userPrompt += $"Target Job Description:\n{request.TargetJdText}\n\n";
            }
            userPrompt += "Please optimize this CV.";

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

            JsonDocument jsonDoc;
            try
            {
                jsonDoc = JsonDocument.Parse(aiResponseText);
            }
            catch (Exception)
            {
                throw new Exception("AI generated an invalid JSON response. Please try again.");
            }

            var optimization = new CvOptimizations
            {
                Id = Guid.NewGuid(),
                CandidateId = candidateId,
                CvId = request.CvId,
                TargetJdText = request.TargetJdText,
                FeedbackData = aiResponseText,
                CreatedAt = DateTime.UtcNow
            };

            await _optimizationRepository.AddAsync(optimization);

            return new CvOptimizationResponseDto
            {
                Id = optimization.Id,
                CandidateId = optimization.CandidateId,
                CvId = optimization.CvId,
                TargetJdText = optimization.TargetJdText,
                FeedbackData = jsonDoc,
                CreatedAt = optimization.CreatedAt
            };
        }

        public async Task<List<CvOptimizationResponseDto>> GetMyOptimizationHistoryAsync(Guid candidateId)
        {
            var optimizations = await _optimizationRepository.GetByCandidateIdAsync(candidateId);
            return optimizations.Select(o => new CvOptimizationResponseDto
            {
                Id = o.Id,
                CandidateId = o.CandidateId,
                CvId = o.CvId,
                TargetJdText = o.TargetJdText,
                FeedbackData = JsonDocument.Parse(o.FeedbackData),
                CreatedAt = o.CreatedAt
            }).ToList();
        }

        public async Task<CvOptimizationResponseDto> GetOptimizationByIdAsync(Guid candidateId, Guid id)
        {
            var o = await _optimizationRepository.GetByIdAsync(id);
            if (o == null || o.CandidateId != candidateId)
            {
                throw new KeyNotFoundException("Optimization report not found.");
            }

            return new CvOptimizationResponseDto
            {
                Id = o.Id,
                CandidateId = o.CandidateId,
                CvId = o.CvId,
                TargetJdText = o.TargetJdText,
                FeedbackData = JsonDocument.Parse(o.FeedbackData),
                CreatedAt = o.CreatedAt
            };
        }
    }
}
