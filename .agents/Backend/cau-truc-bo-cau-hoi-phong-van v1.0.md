# Cấu trúc Bộ câu hỏi Phỏng vấn

## Mục đích
Tài liệu này định nghĩa cấu trúc chuẩn để agent sinh bộ câu hỏi phỏng vấn theo từng cấp độ (level) ứng viên. Agent cần tuân thủ đúng số lượng câu hỏi cho từng phần theo bảng bên dưới.

## Các phần câu hỏi (Sections)

| Mã phần | Tên phần | Mô tả |
|---|---|---|
| Phần 1 | Giới thiệu bản thân | Câu hỏi mở đầu, giúp ứng viên giới thiệu tổng quan về bản thân |
| Phần 2 | Câu hỏi kiến thức | Kiểm tra kiến thức chuyên môn, lý thuyết liên quan đến vị trí ứng tuyển |
| Phần 3 | Câu hỏi kinh nghiệm & dự án | Đào sâu vào kinh nghiệm thực tế, các dự án ứng viên đã tham gia/triển khai |
| Phần 4 | Kỹ năng mềm / Xử lý tình huống | Đánh giá khả năng giao tiếp, làm việc nhóm, ra quyết định, xử lý vấn đề |
| Phần 5 | Hiểu biết về công ty | Đánh giá mức độ tìm hiểu và sự phù hợp của ứng viên với công ty |

## Phân loại dạng câu hỏi

Ngoài việc phân theo **nội dung** (5 phần ở trên), agent cần phân biệt thêm chiều **dạng câu hỏi** — vì cùng một nội dung nhưng đặt câu hỏi khác dạng sẽ khai thác được thông tin khác nhau.

| Dạng câu hỏi | Mô tả | Mục đích |
|---|---|---|
| Câu hỏi đóng (Closed-ended) | Câu trả lời ngắn gọn, dạng Có/Không hoặc chọn 1 trong các phương án cho sẵn | Xác nhận thông tin cơ bản, kiểm tra nhanh có/không biết |
| Câu hỏi mở (Open-ended) | Không có câu trả lời đơn giản, yêu cầu ứng viên trình bày, phân tích | Khai thác sâu kinh nghiệm, tư duy, quan điểm cá nhân |
| Câu hỏi "Outside-the-box" (sáng tạo) | Câu hỏi phi truyền thống, đòi hỏi tư duy sáng tạo, đưa ra ý tưởng/giải pháp mới | Đánh giá khả năng sáng tạo, tư duy linh hoạt, giải quyết vấn đề chưa từng gặp |

### Ma trận dạng câu hỏi theo Phần & Level

| Phần | Dạng ưu tiên | Ghi chú theo level |
|---|---|---|
| Phần 1 - Giới thiệu | Mở | Áp dụng như nhau ở mọi level |
| Phần 2 - Kiến thức | Đóng (Intern/Junior) → Mở (Middle/Senior) | Senior nên hỏi dạng mở kiểu so sánh/đánh đổi (trade-off) thay vì đúng/sai đơn thuần |
| Phần 3 - Kinh nghiệm & dự án | Mở | Giữ nguyên dạng ở mọi level, chỉ tăng độ sâu/độ phức tạp theo level |
| Phần 4 - Kỹ năng mềm / Xử lý tình huống | Mở (Intern → Middle) / Mở + Outside-the-box (Senior) | Chỉ Senior mới nên có câu hỏi sáng tạo/tình huống hoàn toàn mới |
| Phần 5 - Hiểu biết công ty | Đóng hoặc Mở nhẹ | Đóng nếu chỉ xác nhận đã tìm hiểu; Mở nếu muốn đánh giá mức độ phù hợp văn hóa |

### Nguyên tắc quan trọng
Câu hỏi dạng **Outside-the-box** không được dùng để leo thang độ khó tự động dựa trên chất lượng câu trả lời trước đó của ứng viên. Dạng câu hỏi này chỉ được kích hoạt có chủ đích khi level = Middle/Senior, không phải như một phản ứng "thưởng" khi ứng viên trả lời tốt các câu trước — tránh tình trạng câu hỏi vượt quá kỳ vọng năng lực của level đã chọn.

## Ma trận số lượng câu hỏi theo Level

| Level | Phần 1 | Phần 2 (Kiến thức) | Phần 3 (Kinh nghiệm) | Phần 4 (Kỹ năng mềm) | Phần 5 (Công ty) | Tổng |
|---|---|---|---|---|---|---|
| Intern / Fresher | 1 | 3 | 1 | 1 | 1 | 7 |
| Junior | 1 | 3 | 1 | 1 | 1 | 7 |
| Middle | 1 | 2 | 2 | 1 | 1 | 7 |
| Senior | 1 | 1 (hoặc bỏ qua kiến thức cơ bản) | 3–4 | 1 | 1 | 7–8 |

## Ghi chú áp dụng theo Level

### Intern / Fresher
- Ưu tiên kiểm tra nền tảng kiến thức (Phần 2 nhiều nhất).
- Câu hỏi kinh nghiệm (Phần 3) có thể thay bằng câu hỏi về đồ án, bài tập lớn, hoặc quá trình tự học nếu ứng viên chưa có kinh nghiệm đi làm.

### Junior
- Tương tự Intern/Fresher về số lượng, nhưng độ sâu câu hỏi Phần 2 và Phần 3 cần cao hơn (đòi hỏi ứng viên đã áp dụng kiến thức vào công việc thực tế, dù ở mức cơ bản).

### Middle
- Giảm số câu kiến thức thuần túy, tăng số câu kinh nghiệm để đánh giá khả năng giải quyết vấn đề thực tế và ra quyết định kỹ thuật.

### Senior
- Có thể bỏ hoàn toàn câu hỏi kiến thức cơ bản, thay bằng 1 câu kiến thức chuyên sâu (ví dụ: system design, trade-off kỹ thuật) hoặc không hỏi kiến thức riêng lẻ mà lồng vào câu hỏi kinh nghiệm.
- Số câu hỏi kinh nghiệm là phần trọng tâm nhất (3–4 câu), tập trung vào: độ phức tạp dự án đã xử lý, vai trò lãnh đạo/mentor, khả năng ra quyết định chiến lược.

## Định dạng Output mong muốn (gợi ý cho agent)

Khi sinh bộ câu hỏi, agent nên trả về theo cấu trúc:

```
Level: <Intern/Fresher | Junior | Middle | Senior>

Phần 1 - Giới thiệu bản thân:
1. ...

Phần 2 - Câu hỏi kiến thức:
1. ...
2. ...

Phần 3 - Câu hỏi kinh nghiệm & dự án:
1. ...

Phần 4 - Kỹ năng mềm / Xử lý tình huống:
1. ...

Phần 5 - Hiểu biết về công ty:
1. ...
```

## Quy tắc ràng buộc cho Agent
1. Luôn tuân thủ đúng số lượng câu hỏi theo bảng ma trận ở trên, trừ khi người dùng yêu cầu điều chỉnh cụ thể.
2. Câu hỏi Phần 2 (kiến thức) phải phù hợp với vị trí/ngành nghề được chỉ định (VD: Backend, Frontend, Data, Marketing...).
3. Câu hỏi Phần 3 nên được cá nhân hóa dựa trên CV/thông tin ứng viên nếu có, thay vì dùng câu hỏi chung chung.
4. Câu hỏi Phần 5 cần dựa trên thông tin thực tế về công ty (sản phẩm, văn hóa, giá trị cốt lõi...) nếu được cung cấp.
5. Không lặp lại nội dung câu hỏi giữa các phần.
6. Dạng câu hỏi (đóng/mở/outside-the-box) phải bám theo Ma trận dạng câu hỏi theo Phần & Level ở trên; không tự ý đổi dạng câu hỏi dựa trên diễn biến hội thoại.
7. Không leo thang độ khó hoặc đổi dạng câu hỏi sang Outside-the-box chỉ vì ứng viên trả lời tốt các câu trước — độ khó và dạng câu hỏi phải neo theo level đã chọn từ đầu phiên phỏng vấn, không phải theo phản ứng tức thời với câu trả lời.
