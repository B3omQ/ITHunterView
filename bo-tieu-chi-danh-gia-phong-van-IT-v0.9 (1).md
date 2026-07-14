# BỘ TIÊU CHÍ ĐÁNH GIÁ PHỎNG VẤN THỬ NGÀNH IT — v0.9

> Dùng cho AI chấm điểm câu trả lời phỏng vấn và tổng hợp nhận xét cuối phiên.
> Gồm 3 lớp: (1) Chấm từng câu trả lời, (2) Feedback tổng thể 1 phiên, (3) So sánh tiến bộ giữa các phiên.

---

## 0. NGUYÊN TẮC THIẾT KẾ

- **Thang điểm thống nhất:** 1–5 cho mọi tiêu chí (1 = Rất yếu, 5 = Xuất sắc) để dễ tính trung bình và so sánh giữa các phiên.
- **Tách 2 góc nhìn:** Kỹ thuật (Technical) và Kỹ năng mềm (Soft Skills) được chấm **độc lập**, không cộng dồn vào 1 điểm duy nhất, để tránh một câu trả lời "nói hay nhưng sai kiến thức" bị đánh giá quá cao.
- **Trọng số theo loại câu hỏi:** Câu hỏi lý thuyết → thiên về Technical; câu hỏi hành vi (behavioral) → thiên về Soft Skills; câu hỏi coding/system design → cả hai, có thêm tiêu chí phụ.
- **Bằng chứng cụ thể (evidence-based):** Mỗi điểm số phải kèm 1 câu trích/diễn giải ngắn từ câu trả lời của ứng viên làm căn cứ — tránh nhận xét chung chung ("trả lời tốt", "cần cải thiện").
- **Không suy diễn quá mức:** Nếu câu trả lời quá ngắn/không đủ dữ liệu để chấm 1 tiêu chí, AI nên gắn nhãn "Không đủ dữ liệu" thay vì đoán điểm.

---

## 1. TIÊU CHÍ CHẤM TỪNG CÂU TRẢ LỜI

### 1.1 Góc nhìn KỸ THUẬT (Technical) — áp dụng cho câu hỏi chuyên môn/coding/system design

| # | Tiêu chí | Mô tả | Thang 1–5 |
|---|----------|-------|-----------|
| T1 | **Độ chính xác kiến thức** | Câu trả lời có đúng về mặt kỹ thuật không? Có khái niệm sai, nhầm lẫn không? | 1: Sai hoàn toàn – 5: Chính xác, không có lỗ hổng |
| T2 | **Độ sâu / hiểu bản chất** | Ứng viên chỉ thuộc định nghĩa hay hiểu "tại sao", "khi nào dùng", đánh đổi (trade-off)? | 1: Học vẹt – 5: Hiểu sâu, giải thích được nguyên lý |
| T3 | **Khả năng giải quyết vấn đề** | Cách tiếp cận bài toán: có phân tích, chia nhỏ vấn đề, xét edge case không? | 1: Không có hướng tiếp cận – 5: Tiếp cận có hệ thống, xét nhiều case |
| T4 | **Chất lượng giải pháp/code** | (Nếu là câu hỏi code) Tính đúng đắn, độ phức tạp thời gian/không gian, code sạch, đặt tên biến, khả năng test | 1: Không chạy được / sai logic – 5: Tối ưu, sạch, có test |
| T5 | **Ứng dụng thực tế** | Có liên hệ được với dự án/kinh nghiệm thực tế, hay chỉ nói lý thuyết suông? | 1: Thuần lý thuyết, không có ví dụ – 5: Có ví dụ thực tế rõ ràng, hợp lý |
| T6 | **Nhận biết giới hạn bản thân** | Khi không biết, có thừa nhận + đưa hướng suy luận hợp lý, hay bịa đặt (hallucination)? | 1: Bịa kiến thức sai – 5: Thừa nhận trung thực, vẫn thử suy luận logic |

**Điểm Technical của câu hỏi** = trung bình các tiêu chí áp dụng được (bỏ qua tiêu chí không liên quan, ví dụ câu hỏi lý thuyết thuần thì bỏ T4).

### 1.2 Góc nhìn KỸ NĂNG MỀM (Soft Skills) — áp dụng cho mọi câu hỏi

| # | Tiêu chí | Mô tả | Thang 1–5 |
|---|----------|-------|-----------|
| S1 | **Cấu trúc trình bày** | Trả lời có mở đầu – nội dung – kết luận rõ ràng không? Với câu hỏi hành vi: có theo cấu trúc STAR (Situation–Task–Action–Result) không? | 1: Lộn xộn, không có mạch – 5: Rất mạch lạc, dễ theo dõi |
| S2 | **Sự rõ ràng & súc tích** | Có lan man, lặp ý, dùng từ mơ hồ không? Hay đi thẳng trọng tâm? | 1: Dài dòng, lạc đề – 5: Ngắn gọn, đúng trọng tâm |
| S3 | **Sự tự tin & thái độ** | Giọng điệu tự tin nhưng không tự cao; thể hiện chủ động, không né tránh câu hỏi | 1: Né tránh/rất thiếu tự tin – 5: Tự tin, chủ động, chuyên nghiệp |
| S4 | **Khả năng giao tiếp kỹ thuật** | Giải thích khái niệm khó cho người khác hiểu được (kỹ năng thường bị bỏ qua nhưng rất quan trọng khi làm việc nhóm) | 1: Dùng thuật ngữ tràn lan, khó hiểu – 5: Giải thích dễ hiểu, có ví dụ minh hoạ |
| S5 | **Tư duy phản biện/tự nhận thức** | Khi được hỏi về thất bại/hạn chế, có tự nhận thức, rút bài học không, hay đổ lỗi/né tránh? | 1: Đổ lỗi, phòng thủ – 5: Tự nhận thức tốt, có bài học cụ thể |
| S6 | **Khả năng xử lý tình huống bất ngờ/áp lực** | Khi gặp câu hỏi khó/bẫy, có bình tĩnh xử lý, hỏi lại làm rõ đề bài không? | 1: Bối rối, im lặng/bỏ cuộc – 5: Bình tĩnh, hỏi lại hợp lý, xử lý tốt |

**Điểm Soft Skills của câu hỏi** = trung bình các tiêu chí áp dụng được.

### 1.3 Output mẫu cho 1 câu trả lời (schema gợi ý cho AI)

```json
{
  "question_id": "Q3",
  "question_type": "technical | behavioral | coding | system_design",
  "technical_score": {
    "T1": 4, "T2": 3, "T3": 4, "T4": null, "T5": 3, "T6": 5,
    "average": 3.8
  },
  "soft_skill_score": {
    "S1": 4, "S2": 3, "S3": 4, "S4": 3, "S5": null, "S6": null,
    "average": 3.5
  },
  "evidence": "Ứng viên giải thích đúng khái niệm closure nhưng chưa nêu được use-case thực tế cụ thể.",
  "strengths": ["Hiểu đúng khái niệm cốt lõi", "Trình bày mạch lạc"],
  "improvements": ["Nên minh hoạ bằng ví dụ code/dự án thực tế", "Có thể rút gọn phần mở đầu"]
}
```

---

## 2. TIÊU CHÍ FEEDBACK TỔNG THỂ SAU 1 PHIÊN PHỎNG VẤN

Tổng hợp từ điểm từng câu, nhưng **không chỉ là trung bình cộng** — cần thêm phân tích xu hướng và mức độ sẵn sàng.

### 2.1 Điểm tổng hợp

| Nhóm | Cách tính | Ý nghĩa |
|------|-----------|---------|
| Điểm Technical trung bình | Trung bình `technical_score.average` của tất cả câu | Năng lực chuyên môn tổng thể |
| Điểm Soft Skills trung bình | Trung bình `soft_skill_score.average` của tất cả câu | Kỹ năng trình bày/giao tiếp tổng thể |
| Độ ổn định (consistency) | Độ lệch chuẩn điểm giữa các câu | Ứng viên có đều tay hay "câu tốt câu tệ" |
| Tỷ lệ câu "không đủ dữ liệu" | % câu bị thiếu thông tin để chấm | Ứng viên trả lời quá ngắn/né tránh |

### 2.2 Các mục nhận xét định tính bắt buộc

1. **Điểm mạnh nổi bật (Top 3)** — kèm dẫn chứng từ câu trả lời cụ thể.
2. **Điểm cần cải thiện (Top 3)** — ưu tiên theo mức độ ảnh hưởng đến kết quả phỏng vấn thật, không liệt kê dàn trải.
3. **Mô hình lỗi lặp lại (pattern)** — ví dụ: "liên tục trả lời thiếu ví dụ thực tế", "hay trả lời dài dòng ở câu behavioral". AI nên phát hiện lỗi *lặp lại ≥2 lần* thay vì lỗi đơn lẻ.
4. **Mức độ sẵn sàng (Readiness Level)** — phân loại: `Chưa sẵn sàng / Cần luyện thêm / Sẵn sàng ở mức junior / Sẵn sàng ở mức mid / Sẵn sàng phỏng vấn thật`.
5. **Gợi ý hành động tiếp theo (Action items)** — 2–3 việc cụ thể nên luyện tập trước phiên sau (VD: "luyện trả lời theo STAR cho câu hành vi", "ôn lại Big-O và trade-off cấu trúc dữ liệu").

---

## 3. TIÊU CHÍ SO SÁNH VỚI PHIÊN PHỎNG VẤN TRƯỚC (TIẾN BỘ / THỤT LÙI)

### 3.1 Điều kiện áp dụng
- Chỉ so sánh khi 2 phiên có **độ khó/loại câu hỏi tương đương** (hoặc AI cần ghi chú nếu độ khó khác nhau để tránh so sánh khập khiễng).
- Lưu lại lịch sử điểm theo từng tiêu chí (T1–T6, S1–S6) qua các phiên, không chỉ điểm tổng.

### 3.2 Bảng so sánh gợi ý

| Tiêu chí so sánh | Cách đánh giá |
|---|---|
| **Delta điểm tổng** | Điểm phiên này − điểm phiên trước (Technical & Soft Skills tách riêng) |
| **Xu hướng theo từng tiêu chí con** | Tiêu chí nào tăng, giảm, giữ nguyên (VD: T2 tăng nhưng S3 giảm) |
| **Lỗi cũ đã khắc phục chưa** | Đối chiếu "Action items" của phiên trước — có thấy cải thiện trong phiên này không |
| **Lỗi mới phát sinh** | Có xuất hiện điểm yếu mới không có ở phiên trước |
| **Độ ổn định qua thời gian** | So sánh độ lệch chuẩn điểm giữa các câu qua các phiên — càng giảm càng tốt (ứng viên đều tay hơn) |
| **Tốc độ tiến bộ** | Số điểm cải thiện trung bình / số phiên đã luyện — để gợi ý tần suất luyện tập phù hợp |

### 3.3 Nhãn phân loại xu hướng

- 📈 **Tiến bộ rõ rệt**: điểm tổng tăng ≥ 0.5 và không có lỗi mới nghiêm trọng.
- ➡️ **Ổn định**: điểm tổng thay đổi trong khoảng ±0.3.
- 📉 **Thụt lùi**: điểm tổng giảm ≥ 0.5, hoặc lỗi cũ lặp lại + phát sinh lỗi mới.
- ⚠️ **Tiến bộ lệch**: Technical tăng nhưng Soft Skills giảm (hoặc ngược lại) — cần cảnh báo riêng vì đây là dấu hiệu hay bị bỏ sót.

### 3.4 Output mẫu cho phần so sánh (schema gợi ý)

```json
{
  "session_id": "S5",
  "compared_with": "S4",
  "technical_delta": +0.4,
  "soft_skill_delta": -0.2,
  "trend_label": "Tiến bộ lệch",
  "resolved_issues": ["Đã khắc phục: thiếu ví dụ thực tế khi trả lời câu technical"],
  "new_issues": ["Bắt đầu trả lời dài dòng hơn ở câu behavioral"],
  "consistency_change": "Độ lệch chuẩn giảm từ 1.1 xuống 0.7 → đều tay hơn",
  "recommendation": "Giữ đà cải thiện kiến thức chuyên môn, tập trung luyện súc tích hoá câu trả lời hành vi (giới hạn 90 giây/câu)."
}
```

---

## 4. GHI CHÚ TRIỂN KHAI (dành cho đội xây dựng AI chấm điểm)

- Nên **cố định seed câu hỏi/độ khó** theo từng "bộ đề" để việc so sánh giữa phiên có ý nghĩa thống kê.
- Cân nhắc gắn **trọng số theo vị trí ứng tuyển** (VD: Backend → tăng trọng số T3/T4; BA/PM → tăng trọng số S1/S2/S4).
- Với câu hỏi coding, nên tách thêm 1 lớp chấm tự động (chạy test case) độc lập với AI chấm định tính, rồi kết hợp lại.
- Cân nhắc thêm cơ chế **calibration**: định kỳ cho con người review một mẫu ngẫu nhiên các câu AI đã chấm để hiệu chỉnh độ lệch của AI so với người thật (giống cách các công ty lớn calibrate rubric phỏng vấn).
- Version hiện tại là **v0.9** — khuyến nghị thử nghiệm trên ít nhất 20–30 phiên phỏng vấn thật rồi mới chốt v1.0, đặc biệt cần tinh chỉnh ngưỡng phân loại "Tiến bộ/Thụt lùi" (mục 3.3) theo dữ liệu thực tế.

---

*Nguồn tham khảo cách xây rubric: các mô hình chấm phỏng vấn kỹ thuật của Google, Medium Engineering, Karat, và các best practice về interview rubric (competency-based, evidence-based scoring).*
