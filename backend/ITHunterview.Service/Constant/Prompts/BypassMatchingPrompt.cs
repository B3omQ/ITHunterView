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
        ""categoryWeight"": 0.0,
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
        ""deduction"": 0-100,
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
1. Các mức điểm (HandlerScore): Bắt buộc chỉ dùng các giá trị: 0.0, 0.3, 0.5, 0.7, 1.0 (hoặc theo công thức của handler cụ thể).
2. Weight: tech_skill (1.0), experience (0.9), seniority_fit (0.9), domain_knowledge (0.7), language (0.6), education (0.5), soft_skill (0.4).

HANDLER SCORING RULES (MANDATORY — follow exactly):
[H_TECH] tech_skill — Code: H_TECH_0X:
  H_TECH_01 = 0.0  : Skill hoàn toàn vắng mặt trong CV
  H_TECH_02 = 0.3  : Alternative/transferable skill (vd: JD cần Node.js, CV có .NET)
  H_TECH_03 = 0.5  : Skill chỉ có trong Skills section, KHÔNG xuất hiện trong project/experience
  H_TECH_04 = 0.7  : Skill trong project/experience với action verb nhưng THIẾU outcome cụ thể
  H_TECH_05 = 1.0  : Skill trong project/experience với action verb + outcome rõ ràng (số liệu/scope)

[H_EXP] experience — Code: H_EXP_0X:
  ratio = years_found_in_cv / years_required_in_jd
  H_EXP_01 = 0.0  : Không có professional experience nào
  H_EXP_02 = 0.2  : ratio < 0.5
  H_EXP_03 = 0.5  : 0.5 <= ratio < 0.8
  H_EXP_04 = 0.8  : 0.8 <= ratio < 1.0
  H_EXP_05 = 0.3  : JD yêu cầu experience với skill cụ thể, CV không có timeline rõ ràng
  H_EXP_06 = 1.0  : ratio >= 1.0

[H_SENIOR] seniority_fit — Code: H_SENIOR_0X:
  H_SENIOR_01 = 0.0 : CV hoàn toàn trống về experience/project
  H_SENIOR_02 = 0.3 : Overqualified rõ ràng
  H_SENIOR_03 = 0.4 : Underqualified, không có scope signal bù đắp
  H_SENIOR_04 = 0.6 : Title/years gần đúng nhưng không có scope signal
  H_SENIOR_05 = 0.8 : Title + years khớp + >= 1 scope signal
  H_SENIOR_06 = 1.0 : Title + years + nhiều scope signals khớp rõ ràng

[H_EDU] education — Code: H_EDU_0X:
  degree_score: 1.0 (đủ/vượt), 0.8 (năm cuối chưa tốt nghiệp), 0.4 (thấp hơn 1 bậc), 0.0 (không có)
  major_score:  1.0 (CNTT/KHMT/SE), 0.6 (Toán/Vật lý/Điện tử), 0.2 (không liên quan)
  handler_score = degree_score * 0.7 + major_score * 0.3
  H_EDU_01 = 0.0 | H_EDU_02 = 0.4 | H_EDU_03 = 0.8 | H_EDU_04..06 dùng công thức

[H_LANG] language — Code: H_LANG_0X:
  H_LANG_01 = 0.0 : Không có signal
  H_LANG_02 = 0.2 : cert_score < 60% ngưỡng
  H_LANG_03 = 0.5 : 60-79% ngưỡng, hoặc CV viết bằng ngôn ngữ đó
  H_LANG_04 = 0.6 : Có cert nhưng không có điểm số
  H_LANG_05 = 0.8 : 80-99% ngưỡng
  H_LANG_06 = 1.0 : cert_score >= min_score

[H_SOFT] soft_skill — Code: H_SOFT_0X:
  H_SOFT_01 = 0.0 : Không có signal hành vi
  H_SOFT_02 = 0.3 : Signal yếu (mention gián tiếp, thiếu context)
  H_SOFT_03 = 0.6 : Một bằng chứng cụ thể rõ ràng
  H_SOFT_04 = 1.0 : Nhiều loại bằng chứng kết hợp

[H_DOMAIN] domain_knowledge — Code: H_DOMAIN_0X:
  H_DOMAIN_01 = 0.0 : Không có signal
  H_DOMAIN_02 = 0.4 : Mention gián tiếp, không có action
  H_DOMAIN_03 = 0.7 : Evidence áp dụng trong project
  H_DOMAIN_04 = 1.0 : Cert chuyên ngành hoặc professional experience trong domain

POOL SCORING FORMULA:
  Pool_A = Σ [ (70 * w_i / Σw_must_have) * handlerScore_i ]  for all must_have requirements
  Pool_B = Σ [ (30 * w_j / Σw_nice_to_have) * handlerScore_j ] for all nice_to_have requirements
  Total_raw = Pool_A + Pool_B

SOFT SKILL EVIDENCE TABLE:
  Tự học:        Strong(0.6)=cert/side project | Weak(0.3)=stack đa dạng hơn curriculum
  Teamwork:      Strong(0.6)=team project role rõ ràng | Weak(0.3)=chỉ ghi ""team"" chung
  Communication: Strong(0.6)=CV mô tả dự án chi tiết | Weak(0.3)=CV sơ sài rỗng thông tin
  Problem-solving: Strong(0.6)=technical challenge cụ thể | Weak(0.3)=chỉ CRUD cơ bản

HARD CAPS & PENALTIES:
  - RULE_TC1_01: Bất kỳ must-have requirement nào có handlerScore = 0.0 → set flag = ""CRITICAL_GAP"".
  - RULE_TC1_02: Nếu có >= 2 CRITICAL_GAP ở must-have → Pool A bị cap tối đa 40% (28/70 điểm), set poolACapped = true.
  - PNL_TC1_01: Nếu top 3 must-have tech skills khai báo rỗng nhưng không có trong project thực tế → trừ thẳng 15 điểm.
  - KSW_01 (Kill-Switch): Nếu 100% core must-have tech_skill có handlerScore = 0.0 → Đóng băng điểm ở 15/100, set killSwitchTriggered = true, result = ""Not Suitable"".

Xếp loại (result): >= 80 là Highly Suitable, >= 60 là Suitable, >= 40 là Partially Suitable, < 40 là Not Suitable (hoặc khi KSW_01 triggered).

Chỉ trả về JSON hợp lệ. Bắt đầu bằng {{ và kết thúc bằng }}.
";
        }
    }
}
