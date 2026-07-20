# Cấu trúc Bộ câu hỏi Phỏng vấn

## Mục đích
Tài liệu này định nghĩa cấu trúc chuẩn để agent sinh bộ câu hỏi phỏng vấn theo từng cấp độ (level) ứng viên mà jd cần tuyển hoặc cv được ghi. model cần tuân thủ đúng số lượng câu hỏi cho từng phần theo bảng bên dưới.

Tài liệu chỉ bổ xung thêm cấu trúc cho các câu hỏi phỏng vấn, vẫn giữ nguyên input là data của cv, jd, bộ câu hỏi mẫu để model sinh câu hỏi một cách chính xác.

## Các phần câu hỏi (Sections)

| Mã phần | Tên phần | Mô tả |
|---|---|---|
| Phần 1 | Giới thiệu bản thân | Câu hỏi mở đầu, giúp ứng viên giới thiệu tổng quan về bản thân |
| Phần 2 | Câu hỏi kiến thức | Kiểm tra kiến thức chuyên môn, lý thuyết liên quan đến vị trí ứng tuyển |
| Phần 3 | Câu hỏi kinh nghiệm & dự án | Đào sâu vào kinh nghiệm thực tế, các dự án ứng viên đã tham gia/triển khai |
| Phần 4 | Kỹ năng mềm / Xử lý tình huống | Đánh giá khả năng làm việc nhóm, ra quyết định, xử lý vấn đề |
| Phần 5 | Hiểu biết về công ty | Đánh giá mức độ tìm hiểu và sự phù hợp của ứng viên với công ty |

## Ma trận số lượng câu hỏi theo Level

| Level | Phần 1 | Phần 2 (Kiến thức) | Phần 3 (Kinh nghiệm) | Phần 4 (Kỹ năng mềm) | Phần 5 (Công ty) | Tổng |
|---|---|---|---|---|---|---|
| Intern / Fresher | 1 | 3 | 1 | 1 | 1 | 7 |
| Junior | 1 | 3 | 1 | 1 | 1 | 7 |
| Middle | 1 | 2 | 2 | 1 | 1 | 7 |
| Senior | 1 | 1 (hoặc bỏ qua kiến thức cơ bản) | 3–4 | 1 | 1 | 7–8 |

- có thể  thêm 1 câu hỏi follow up với câu hỏi trước nếu ứng viên chưa trả lời rõ (lưu ý chỉ 1 câu, nếu ứng viên trả lời vẫn chưa rõ thì hãy bỏ qua).
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

## Định dạng Output mong muốn (gợi ý cho model)

Khi sinh bộ câu hỏi, model nên trả về theo cấu trúc:

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

## Quy tắc ràng buộc
1. Luôn tuân thủ đúng số lượng câu hỏi theo bảng ma trận ở trên, trừ khi người dùng yêu cầu điều chỉnh cụ thể.
2. Câu hỏi Phần 2 (kiến thức) phải phù hợp với vị trí/ngành nghề được chỉ định (VD: Backend, Frontend, Data, Marketing...).
3. Câu hỏi Phần 3 nên được cá nhân hóa dựa trên CV/thông tin ứng viên nếu có, thay vì dùng câu hỏi chung chung.
4. Câu hỏi Phần 5 cần dựa trên thông tin thực tế về công ty (sản phẩm, văn hóa, giá trị cốt lõi...) nếu được cung cấp.
5. Không lặp lại nội dung câu hỏi giữa các phần.
6. Phần mở đầu buổi phỏng vấn hầu như sẽ là lời mở đầu chào "tên ứng viên" ứng tuyển vào vị trí "tiêu đề jd" rồi nối tiếp vào phần 1 mời ứng viên giới thiệu bản thân.
