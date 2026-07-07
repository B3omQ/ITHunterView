using System;

namespace ITHunterview.Service.Constant.Prompts
{
    public static class BypassMatchingPrompt
    {
        public static string GetPrompt(string cvText, string jdText)
        {
            return $@"
Bạn là một trợ lý AI chuyên nghiệp về tuyển dụng, có nhiệm vụ đánh giá mức độ phù hợp giữa Hồ sơ ứng viên (CV) và Yêu cầu công việc (JD).
MỌI TRƯỜNG VĂN BẢN (Text fields) NHƯ 'reasoning', 'narrative', 'gapDescription', 'suggestion', 'evidence', 'issue', 'action', 'example' TRONG JSON KẾT QUẢ ĐẦU RA PHẢI ĐƯỢC VIẾT BẰNG TIẾNG ANH (English). MẶC DÙ PROMPT NÀY BẰNG TIẾNG VIỆT, NHƯNG BẠN PHẢI TRẢ LỜI BẰNG TIẾNG ANH.

Dưới đây là Dữ liệu Đầu vào:
--- START CV ---
{cvText}
--- END CV ---

--- START JD ---
{jdText}
--- END JD ---

Hãy phân tích, đối chiếu CV với JD dựa trên BỘ TIÊU CHÍ JD FIT (bao gồm 7 Category và các quy tắc Hard Caps/Penalties). Sau đó xuất ra kết quả DƯỚI DẠNG JSON TUYỆT ĐỐI THEO SCHEMA ĐƯỢC YÊU CẦU SAU ĐÂY. KHÔNG TRẢ VỀ BẤT KỲ VĂN BẢN NÀO BÊN NGOÀI KHỐI JSON.

{{
  ""mode"": ""jd_fit"",
  ""jdFit"": {{
    ""score"": 0-100,
    ""result"": ""Highly Suitable"" | ""Suitable"" | ""Partially Suitable"" | ""Not Suitable"",
    ""killSwitchTriggered"": true/false,
    ""poolACapped"": true/false,
    ""poolA"": {{ ""score"": 0-70, ""max"": 70 }},
    ""poolB"": {{ ""score"": 0-30, ""max"": 30 }},
    ""requirementScores"": [
      {{
        ""reqId"": ""string (id ngẫu nhiên)"",
        ""normalizedText"": ""string (Tên kỹ năng/yêu cầu)"",
        ""importance"": ""must_have"" | ""nice_to_have"",
        ""category"": ""tech_skill"" | ""experience"" | ""seniority_fit"" | ""domain_knowledge"" | ""language"" | ""education"" | ""soft_skill"",
        ""categoryWeight"": 0.0, // 1.0(tech), 0.9(exp/seniority), 0.7(domain), 0.6(lang), 0.5(edu), 0.4(soft)
        ""entities"": {{}},
        ""handlerUsed"": ""string (Tên handler)"",
        ""handlerCode"": ""string (Mã code, vd: H_TECH_03, H_EXP_05...)"",
        ""handlerScore"": 0.0 | 0.3 | 0.5 | 0.7 | 1.0,
        ""reasoning"": ""string (Giải thích ngắn gọn tại sao được điểm này)"",
        ""confidence"": ""high"" | ""medium"" | ""low"",
        ""flag"": ""CRITICAL_GAP"" | null
      }}
    ],
    ""criticalGaps"": [
      {{
        ""requirement"": ""string"",
        ""gapDescription"": ""string"",
        ""severity"": ""high"" | ""medium"",
        ""suggestion"": ""string""
      }}
    ],
    ""penalties"": [
      {{
        ""code"": ""RULE_TC1_01"" | ""RULE_TC1_02"" | ""PNL_TC1_01"" | ""KSW_01"",
        ""triggered"": true/false,
        ""deduction"": 0-100, // Hoặc ghi 15 nếu PNL_TC1_01
        ""evidence"": ""string""
      }}
    ],
    ""narrative"": ""string (Tóm tắt tổng quan)""
  }},
  ""improvements"": [
    {{
      ""priority"": ""high"" | ""medium"" | ""low"",
      ""category"": ""tech_skill"" | ""experience"" | ""education"" | ""soft_skill"",
      ""issue"": ""string"",
      ""action"": ""string"",
      ""example"": {{
        ""before"": ""string"",
        ""after"": ""string""
      }}
    }}
  ],
  ""processingTime"": 1000
}}

Quy tắc tính điểm (BẮT BUỘC TUÂN THỦ):
1. Các mức điểm (HandlerScore): Bắt buộc chỉ dùng các giá trị: 0.0, 0.3, 0.5, 0.7, 1.0 (hoặc các giá trị cụ thể được quy định trong quy tắc).
2. Weight: tech_skill (1.0), experience (0.9), seniority_fit (0.9), domain_knowledge (0.7), language (0.6), education (0.5), soft_skill (0.4).
3. Pool A (Must-have): max 70. Pool B (Nice-to-have): max 30.
4. Hard Caps & Penalties:
   - RULE_TC1_01: Bất kỳ must-have requirement nào có handlerScore = 0.0 → set flag = ""CRITICAL_GAP"".
   - RULE_TC1_02: Nếu có >= 2 CRITICAL_GAP ở must-have → Pool A bị cap tối đa 40% (28/70 điểm), set poolACapped = true.
   - PNL_TC1_01: Nếu top 3 must-have tech skills khai báo rỗng nhưng không có trong project thực tế → trừ thẳng 15 điểm.
   - KSW_01 (Kill-Switch): Nếu 100% core must-have tech_skill có handlerScore = 0.0 → Đóng băng điểm ở 15/100, set killSwitchTriggered = true, result = ""Not Suitable"".
5. Xếp loại (result): >= 80 là Highly Suitable, >= 60 là Suitable, >= 40 là Partially Suitable, < 40 là Not Suitable (hoặc khi KSW_01 triggered).

Chỉ trả về JSON hợp lệ. Bắt đầu bằng {{ và kết thúc bằng }}.
";
        }
    }
}
