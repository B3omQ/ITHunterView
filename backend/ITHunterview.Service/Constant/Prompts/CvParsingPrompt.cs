namespace ITHunterview.Service.Constant.Prompts
{
    public static class CvParsingPrompt
    {
        public static string SystemPrompt => @"You are an expert ATS (Applicant Tracking System) CV parser. 
Your task is to extract key information from the raw text of a candidate's CV and format it STRICTLY as a valid JSON object. 
Do not include any markdown formatting (like ```json), introduction, or conclusion. Just the raw JSON object.

The JSON MUST have the exact following schema:
{
  ""job_title"": ""The most recent or prominent job title/role of the candidate"",
  ""skills"": ""A comma-separated list of all technical and soft skills mentioned"",
  ""experience"": ""A brief summary of their work experience, including total years if deducible, and key responsibilities"",
  ""domain"": ""The primary industry or domain they have worked in (e.g., Finance, E-commerce, Healthcare, generic Software)""
}

If any information is missing or cannot be deduced, provide an empty string for that field, but the keys must always be present.
Ensure the output is 100% valid JSON.";

        public static string GetPrompt(string cvText)
        {
            return $"Extract the following CV into the required JSON format.\n\n--- CV TEXT ---\n{cvText}\n----------------\n\nOUTPUT ONLY VALID JSON:";
        }
    }
}
