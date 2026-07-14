using System.Collections.Generic;

namespace ITHunterview.Service.Utils
{
    public static class InterviewRubricHelper
    {
        public static readonly Dictionary<string, Dictionary<string, List<string>>> RubricQuestions = new()
        {
            {
                "BA", new Dictionary<string, List<string>>
                {
                    {
                        "Senior", new List<string>
                        {
                            "Business Case & ROI: Trước khi dự án bắt đầu, Giám đốc hỏi bạn: \"Làm sao chứng minh dự án phần mềm này sẽ mang lại lợi nhuận?\". Khung phân tích (Framework) bạn sử dụng để trả lời là gì?",
                            "Enterprise Architecture: Sự thay đổi quy trình nghiệp vụ ở phòng ban A ảnh hưởng ngầm tới hệ thống của phòng ban B. Kỹ thuật nào giúp BA nhìn ra bức tranh tổng thể (Big Picture) để ngăn chặn lỗi hệ thống?",
                            "Tái cấu trúc quy trình (BPR): Trình bày sự khác biệt giữa Business Process Improvement (Cải tiến) và Business Process Reengineering (Tái cấu trúc). Rủi ro của BPR là gì?",
                            "Giải quyết xung đột cấp cao: Khách hàng (Sponsor) đưa ra một quy trình nghiệp vụ cực kỳ vô lý và kém hiệu quả, nhưng họ ép phải làm theo ý họ vì họ là người trả tiền. Chiến lược của Lead BA là gì?",
                            "Chuyển đổi số (Digital Transformation): Trào lưu ứng dụng AI/Machine Learning vào sản phẩm. BA làm thế nào để phân biệt đâu là \"nhu cầu thực sự cần AI\" và đâu chỉ là \"đua đòi công nghệ\"?",
                            "Xây dựng team: Bạn xây dựng khung năng lực (Competency Framework) cho team BA trong công ty như thế nào?",
                            "Change Management (Quản lý sự thay đổi): Hệ thống mới triển khai rất tốt, nhưng User từ chối sử dụng và quay lại dùng Excel/Giấy tờ. Lỗi ở đâu và BA làm gì để khắc phục?",
                            "Data-driven Decision Making: Làm thế nào để định nghĩa bộ chỉ số OKR (Objectives and Key Results) cho thành công của một sản phẩm phần mềm?",
                            "Sản phẩm vs Dự án: Tư duy của BA khi làm Product (Sản phẩm in-house dài hạn) khác biệt cốt lõi như thế nào so với làm Outsourcing (Dự án theo hợp đồng)?",
                            "Pháp lý & Tuân thủ: Khi phân tích hệ thống thu thập thông tin người dùng, BA cần đưa những ràng buộc lý thuyết nào vào tài liệu để đảm bảo tuân thủ quyền riêng tư (Privacy-by-design)?",
                        }
                    },
                    {
                        "Middle", new List<string>
                        {
                            "Kiểm soát Scope (Scope Creep): Dự án bị phình to yêu cầu liên tục do khách hàng \"nghĩ ra thêm\". Kỹ năng đàm phán của bạn để nói \"Không\" nhưng khách hàng vẫn vui vẻ là gì?",
                            "BPMN: Business Process Model and Notation (BPMN) giải quyết bài toán gì tốt hơn so với Flowchart thông thường trong các luồng nghiệp vụ phức tạp?",
                            "Reverse Engineering: Công ty yêu cầu bạn đập bỏ một phần mềm đã viết từ 10 năm trước (không còn ai lưu tài liệu) để làm app mới. Bạn lấy yêu cầu từ đâu và bằng cách nào?",
                            "Data Mapping: Trình bày kinh nghiệm lý thuyết của bạn khi làm tài liệu Data Mapping (Ánh xạ dữ liệu) giữa hệ thống cũ và hệ thống mới (Database migration).",
                            "Stakeholder Management: Trong buổi họp có Giám đốc (người trả tiền, ít thời gian) và Nhân viên (người trực tiếp dùng app, rất nhiều yêu cầu chi tiết). Bạn điều phối buổi họp lấy yêu cầu như thế nào?",
                            "Giải pháp kỹ thuật: Bạn có bao giờ phải đọc/hiểu JSON payload hoặc Swagger API để phân tích xem dữ liệu trả về có đáp ứng đủ cho UI không? Mô tả quy trình đó.",
                            "Domain Knowledge: Nhận một dự án thuộc ngành hoàn toàn xa lạ (VD: Quản trị kho bãi Logistics), các bước để bạn tự \"onboard\" kiến thức ngành một cách nhanh nhất là gì?",
                            "Định giá (Estimation): BA có tham gia vào quá trình Estimate dự án không? Kỹ thuật Story Pointing trong Agile hoạt động ra sao?",
                            "A/B Testing: Khi nào thì bạn đề xuất Product Manager sử dụng A/B Testing để ra quyết định thay vì dựa vào ý kiến chủ quan?",
                            "Rủi ro nghiệp vụ: Khi thiết kế tính năng \"Hoàn tiền tự động\" cho app Thương mại điện tử, rủi ro lớn nhất về mặt nghiệp vụ bạn phải chặn lại trong tài liệu là gì?",
                        }
                    },
                    {
                        "Junior", new List<string>
                        {
                            "Nghiệm thu: Làm thế nào để viết Acceptance Criteria (Tiêu chí nghiệm thu) cho tính năng \"Đăng nhập bằng Google\"? Đọc thử 3 tiêu chí bạn nghĩ ra.",
                            "Khai thác yêu cầu: Kể tên 3 kỹ thuật Elicitation (ví dụ: Phỏng vấn, Quan sát...). Khi nào thì nên dùng khảo sát (Survey) thay vì phỏng vấn trực tiếp?",
                            "Mô hình hóa: Giải thích sơ đồ Use Case. Tại sao Use Case Diagram lại hữu ích khi chốt scope với khách hàng không hiểu biết về IT?",
                            "Xử lý thay đổi: Khách hàng thay đổi yêu cầu vào phút chót khi code đã sắp xong. Quy trình chuẩn để xử lý Change Request này là gì?",
                            "Quản lý tồn đọng: Product Backlog là gì? Kỹ thuật MoSCoW được áp dụng để ưu tiên các backlog item như thế nào?",
                            "Tình huống mâu thuẫn: Khách hàng muốn tính năng A, nhưng Leader Dev nói tính năng A làm quá khó và tốn thời gian. Bạn đứng ở giữa xử lý thế nào?",
                            "Phân rã (Decomposition): Một tính năng lớn là \"Hệ thống Quản lý đơn hàng\", bạn phân rã (Breakdown) nó thành các chức năng nhỏ hơn bằng phương pháp nào?",
                            "UML: Phân biệt Sequence Diagram (Sơ đồ tuần tự) và Activity Diagram (Sơ đồ hoạt động).",
                            "Hiểu hệ thống: Khi BA phải làm việc với các hệ thống kết nối API với bên thứ 3 (ví dụ: Tích hợp cổng thanh toán), tài liệu của BA cần mô tả những gì để Dev code được?",
                            "Phân tích dữ liệu cơ bản: Bạn đánh giá sự thành công của một tính năng \"Gợi ý mua hàng\" dựa vào những chỉ số nào trên hệ thống?",
                        }
                    },
                    {
                        "Intern/Fresher", new List<string>
                        {
                            "Quy trình: Theo bạn, BA sẽ tham gia vào những giai đoạn nào trong vòng đời phát triển phần mềm (SDLC)?",
                            "Công cụ: Bạn hay sử dụng sơ đồ (Diagram) nào nhất để trình bày luồng đi của một người dùng trong hệ thống? Tại sao?",
                            "Phân tích yêu cầu: Phân biệt Yêu cầu nghiệp vụ (Business Requirement), Yêu cầu người dùng (User Requirement) và Yêu cầu hệ thống (System Requirement).",
                            "Tư duy: Khách hàng nói: \"Tôi muốn một cái nút màu đỏ thật to ở giữa màn hình\". Là BA, câu hỏi tiếp theo của bạn là gì?",
                            "Giao tiếp: Trong buổi họp lấy yêu cầu (Elicitation), bạn làm gì khi khách hàng nói quá lan man, không đúng trọng tâm chức năng đang bàn?",
                            "Agile/Scrum: Trong mô hình Scrum, BA thường đóng vai trò là ai (Product Owner hay Development Team) và nhiệm vụ chính trong Sprint Planning là gì?",
                            "Ghi chép: Thế nào là một User Story chuẩn chỉnh? (Cấu trúc cơ bản).",
                            "Tình huống: Dev phàn nàn rằng tài liệu của bạn viết thiếu trường hợp ngoại lệ (Edge cases). Lần sau bạn sẽ làm gì để khắc phục?",
                            "Giao diện: Phân biệt giữa Wireframe, Mockup và Prototype.",
                            "Phi chức năng: Tại sao khách hàng ít khi tự đưa ra yêu cầu phi chức năng (Non-functional), và BA làm sao để \"gợi ý\" cho họ?",
                        }
                    },
                }
            },
            {
                "Dev", new Dictionary<string, List<string>>
                {
                    {
                        "Senior", new List<string>
                        {
                            "Thiết kế hệ thống phân tán: Định lý CAP (Consistency, Availability, Partition tolerance) ứng dụng thế nào? Trong hệ thống đấu giá thời gian thực, bạn hy sinh yếu tố nào?",
                            "Database: Trình bày chiến lược Database Migration (thêm cột, sửa kiểu dữ liệu) trên môi trường Production với \"Zero Downtime\" (không ngừng hệ thống).",
                            "Resilience: Circuit Breaker pattern là gì? Nó giúp ích gì khi một Microservice phụ thuộc bị sập, tránh hiệu ứng domino ra toàn hệ thống?",
                            "Monitoring & Logging: Trong một hệ thống hàng trăm services, làm sao để bạn \"Trace\" (truy vết) vòng đời của một Request bị lỗi qua log?",
                            "Kiến trúc nâng cao: CQRS (Command Query Responsibility Segregation) và Event Sourcing kết hợp với nhau giải quyết bài toán gì? Rủi ro lớn nhất của nó là gì?",
                            "Quản lý Tech Debt: Làm thế nào để bạn thuyết phục Product Manager / CEO cho phép team dành 2 tuần của Sprint chỉ để giải quyết Technical Debt thay vì làm tính năng mới?",
                            "Code Review Culture: Triết lý của bạn khi review code của cấp dưới là gì? Làm sao để chê code xấu mà không gây tự ái?",
                            "Bảo mật hệ thống: Giải thích mô hình OAuth 2.0. Authorization Code Flow diễn ra như thế nào?",
                            "High Traffic: Hệ thống đột ngột nhận lưu lượng truy cập gấp 100 lần bình thường do một sự kiện Viral. Chiến lược \"Graceful Degradation\" (giảm cấp duy trì) của bạn là gì?",
                            "Lựa chọn công nghệ: Những tiêu chí nào bạn sẽ đặt lên bàn cân khi quyết định đưa một công nghệ/framework hoàn toàn mới vào stack hiện tại của công ty?",
                        }
                    },
                    {
                        "Middle", new List<string>
                        {
                            "System Architecture: Phân tích ưu nhược điểm giữa Monolithic và Microservices. Nếu một dự án có ngân sách eo hẹp và cần ra mắt trong 2 tháng, bạn chọn kiến trúc nào?",
                            "Message Queue: RabbitMQ hoặc Kafka giải quyết bài toán gì trong hệ thống? Cho một ví dụ cụ thể về việc dùng Message Queue trong một app Thương mại điện tử.",
                            "Tối ưu Database: Đánh Index (Indexing) giúp câu lệnh SELECT nhanh hơn, nhưng tại sao ta không đánh Index cho tất cả các cột?",
                            "Design Pattern: Áp dụng Strategy Pattern để giải quyết bài toán tính giá tiền vận chuyển phức tạp (thay đổi theo khoảng cách, cân nặng, mã giảm giá) như thế nào?",
                            "Nguyên lý SOLID: \"Dependency Inversion Principle\" (Chữ D trong SOLID) giúp ích gì cho việc bảo trì dự án sau 3 năm phát triển?",
                            "API Design: Làm sao để thiết kế một API \"Idempotent\" (Gọi 1 lần hay 100 lần đều cho ra cùng một trạng thái hệ thống)? Tính chất này quan trọng thế nào trong thanh toán?",
                            "Scale hệ thống: Load Balancer hoạt động dựa trên nguyên lý nào? Sự khác biệt giữa Vertical Scaling và Horizontal Scaling?",
                            "Bảo mật: Trình bày cơ chế tấn công SQL Injection và Cross-Site Scripting (XSS). Framework bạn đang dùng có cơ chế nào tự động phòng chống chúng?",
                            "Tái cấu trúc (Refactoring): Bạn nhận một hệ thống \"Legacy Code\" (code cũ, không có doc, logic rối rắm). Bạn sẽ tiếp cận việc refactor nó như thế nào để không làm hỏng tính năng đang chạy?",
                            "Xử lý bất đồng bộ (Asynchronous): Phân biệt Asynchronous Programming và Multi-threading. Khi nào I/O bound và CPU bound operation áp dụng hiệu quả nhất?",
                        }
                    },
                    {
                        "Junior", new List<string>
                        {
                            "Xử lý sự cố: API trả về kết quả quá chậm (mất 5 giây). Kể tên 3 nguyên nhân lý thuyết phổ biến nhất từ phía Backend và Database.",
                            "Database: Lỗi \"N+1 Query\" trong các ORM (như Entity Framework) là gì? Giải pháp lý thuyết để khắc phục nó?",
                            "Bảo mật: JWT (JSON Web Token) hoạt động như thế nào? Chuyện gì xảy ra nếu token bị đánh cắp và làm sao để vô hiệu hóa một JWT trước khi nó hết hạn?",
                            "Luồng dữ liệu (Concurrency): Hai người dùng cùng lúc bấm mua món hàng cuối cùng trong kho. Hệ thống của bạn làm sao để không bán ra 2 món trong khi kho chỉ có 1?",
                            "Caching: Bạn hiểu thế nào về cơ chế Caching (vd: Redis)? Những dữ liệu nào thì nên cache và những dữ liệu nào tuyệt đối không nên?",
                            "Testing: Giải thích khái niệm Mock và Stub trong Unit Test. Tại sao khi viết Unit Test cho một Service gọi API bên thứ 3, ta lại cần Mocking?",
                            "Git Collaboration: \"Merge conflict\" xảy ra khi nào? Bạn làm các bước gì  để resolve một conflict phức tạp với đồng nghiệp?",
                            "Kiến trúc API: Một RESTful API chuẩn cần đáp ứng những tiêu chí nào về mặt định danh URL và sử dụng HTTP Methods?",
                            "Tối ưu UI/UX (Dành cho Dev Mobile/FE): Xử lý luồng (Thread) như thế nào để giao diện (UI) không bị \"đơ\" khi ứng dụng đang tải một file nặng hoặc fetch data từ mạng?",
                            "Phát triển ứng dụng: Sự khác biệt giữa môi trường Staging và Production là gì? Tại sao phải test trên Staging trước?",
                        }
                    },
                    {
                        "Intern/Fresher", new List<string>
                        {
                            "Kiến trúc: Bạn hiểu thế nào về mô hình MVC (hoặc MVVM)? Nếu để toàn bộ logic kết nối database vào Controller thì hệ lụy là gì?",
                            "Database: Phân biệt INNER JOIN và LEFT JOIN. Trong thực tế, khi nào bạn dùng LEFT JOIN?",
                            "Mạng & API: Khi gọi một API trả về lỗi 401 và 403, ý nghĩa của chúng khác nhau như thế nào? Client nên xử lý ra sao trong từng trường hợp?",
                            "Git: Khi bạn đang làm dở một tính năng (code đang lỗi) nhưng sếp yêu cầu chuyển nhánh khẩn cấp để fix một bug khác, bạn thao tác trên Git như thế nào?",
                            "Cấu trúc dữ liệu: Sự khác biệt giữa Array và List (hoặc ArrayList) về mặt cấp phát bộ nhớ là gì? Khi nào ưu tiên dùng cái nào?",
                            "OOP: Giải thích \"Tính đa hình\" (Polymorphism) bằng một ví dụ thực tế liên quan đến thanh toán (vd: Momo, VNPay, Thẻ tín dụng).",
                            "Clean Code: Tại sao việc đặt tên biến là data1, flag2 lại bị coi là \"bad smell\" trong lập trình?",
                            "Debug: Nếu ứng dụng của bạn chạy mượt trên máy local nhưng báo lỗi \"Null Reference\" khi đưa lên server, bạn sẽ kiểm tra những nguyên nhân nào đầu tiên?",
                            "Performance cơ bản: Tại sao đọc/ghi dữ liệu vào RAM lại nhanh hơn đọc/ghi vào Ổ cứng?",
                            "Tư duy: Tham chiếu (Reference) và Tham trị (Value) khác nhau như thế nào? Chuyện gì xảy ra nếu truyền một Object vào một function và sửa đổi nó trong function đó?",
                        }
                    },
                }
            },
            {
                "Test", new Dictionary<string, List<string>>
                {
                    {
                        "Senior", new List<string>
                        {
                            "Shift-Left Testing: Áp dụng tư duy Shift-Left như thế nào trong quy trình phát triển? QA có thể tham gia \"test\" cái gì khi Dev còn chưa viết dòng code nào?",
                            "Chiến lược vĩ mô: Thiết kế chiến lược kiểm thử cho một hệ thống ngân hàng Core Banking, nơi một sai sót nhỏ có thể gây thiệt hại hàng tỷ đồng. Bạn ưu tiên loại hình test nào?",
                            "Chỉ số đo lường (Metrics): Bạn dùng những KPI/Chỉ số nào để báo cáo với Giám đốc rằng \"Chất lượng của team đang tăng lên/giảm đi\"? (Không tính việc đếm số lượng bug).",
                            "Post-mortem: Một lỗi nghiêm trọng (Blocker) đã lọt lên môi trường thật. Các bước thực hiện Root Cause Analysis (Phân tích nguyên nhân gốc) của bạn là gì?",
                            "Automation ROI: Làm thế nào để thuyết phục Ban Giám đốc cấp ngân sách mua tool bản quyền và thuê thêm kỹ sư Automation? Lập luận dựa trên ROI (Return on Investment) thế nào?",
                            "Quản lý team: Trong team có 2 Tester luôn cãi nhau về việc định nghĩa mức độ nghiêm trọng (Severity) của bug. Với vai trò Lead, bạn xây dựng bộ quy tắc nào để thống nhất?",
                            "Security & Compliance: Trình bày sự hiểu biết của bạn về việc kiểm thử tính tuân thủ bảo vệ dữ liệu (GDPR/PDPA). QA kiểm tra điều này như thế nào?",
                            "Microservices Testing: Vấn đề lớn nhất khi test End-to-End trong hệ thống hàng chục Microservices là gì và bạn giải quyết bài toán môi trường test như thế nào?",
                            "BDD (Behavior-Driven Development): Cucumber/Gherkin mang lại lợi ích thực tế gì so với viết test case bằng Excel truyền thống? Nhược điểm của nó là gì?",
                            "Văn hóa chất lượng: Làm thế nào để bạn thay đổi tư duy của team từ \"QA là người tìm lỗi\" sang \"Chất lượng là trách nhiệm của toàn team (kể cả Dev và PO)\"?",
                        }
                    },
                    {
                        "Middle", new List<string>
                        {
                            "Chiến lược Automation: Những trường hợp nào BẮT BUỘC nên chạy Automation Test, và những trường hợp nào nên giữ lại Manual Test?",
                            "Automation Framework: Giải thích cơ chế Page Object Model (POM) trong Automation. Nó giải quyết vấn đề gì khi UI của hệ thống thay đổi liên tục?",
                            "Data-driven Testing: Trong Automation test, làm thế nào để bạn tái sử dụng 1 kịch bản test đăng nhập cho 100 tài khoản khác nhau mà không phải copy/paste code?",
                            "CI/CD: Automation Test nên được trigger (chạy tự động) ở giai đoạn nào trong đường ống CI/CD Pipeline? Tại sao?",
                            "Hiệu suất: Sự khác biệt giữa Load Testing, Stress Testing và Spike Testing là gì?",
                            "Quản lý rủi ro: Sắp đến ngày release nhưng bạn phát hiện ra còn 5% Test Case chưa kịp chạy. Bạn sẽ quyết định \"Go\" hay \"No Go\" dựa vào tiêu chí nào?",
                            "Bảo trì test: \"Flaky Test\" (Test lúc pass lúc fail không rõ nguyên nhân) là nỗi ám ảnh của Automation. Nguyên nhân lý thuyết phổ biến gây ra nó là gì?",
                            "Mobile Automation: Sự khác biệt về kiến trúc (cách tương tác với app) giữa Appium và Cypress/Selenium là gì?",
                            "Chất lượng code: Là QA, bạn có quyền yêu cầu Developer viết Unit Test trước khi chuyển code cho bạn không? Tại sao Unit Test lại quan trọng với QA?",
                            "Test Data Management: Làm thế nào để tự động hóa việc tạo ra \"Fake Data\" có tính logic cao (ví dụ tài khoản phải có số dư > 0, trạng thái đã kích hoạt) để phục vụ chạy test ban đêm?",
                        }
                    },
                    {
                        "Junior", new List<string>
                        {
                            "API Testing: Khi test API bằng Postman/Swagger, bạn kiểm tra những gì ngoài việc nhìn Status Code trả về là 200 OK?",
                            "Database Testing: Bạn dùng câu lệnh SQL nào (lý thuyết) để verify xem dữ liệu tạo từ giao diện Web đã thực sự lưu đúng định dạng vào Database hay chưa?",
                            "Data Test: Tại sao việc lấy thẳng dữ liệu từ môi trường Production đổ về môi trường Test lại được coi là một rủi ro bảo mật lớn? Giải pháp là gì?",
                            "Mobile Testing: Nêu những rủi ro đặc thù khi test ứng dụng Mobile so với Web (vd: mạng chập chờn, bị cuộc gọi xen ngang, v.v.).",
                            "Bảo mật cơ bản: Bạn có thể dùng cách gì (không cần tool chuyên dụng) để test xem hệ thống có dính lỗi bảo mật khi user thao tác thanh toán 2 lần cực nhanh hay không?",
                            "Xử lý Requirement: Bạn nhận được một tính năng không có tài liệu BA viết, chỉ có lời dặn miệng từ PM. Bạn sẽ viết Test Case từ đâu?",
                            "Tình huống: Code đẩy lên Staging bị vỡ layout toàn bộ. Bạn sẽ làm gì tiếp theo: Log 100 cái bug hay làm cách khác?",
                            "Integration Test: Kiểm thử tích hợp là gì? Lấy ví dụ kiểm thử tích hợp giữa Hệ thống Giỏ hàng và Cổng thanh toán bên thứ ba.",
                            "Kế hoạch: Test Plan và Test Strategy khác nhau như thế nào?",
                            "Giao tiếp: Làm sao để thúc giục Dev fix những con bug Low Priority đã tồn đọng trong backlog từ nhiều tháng trước?",
                        }
                    },
                    {
                        "Intern/Fresher", new List<string>
                        {
                            "Kiểm thử giao diện (UI): Kể tên 5 yếu tố cơ bản bạn luôn phải check khi nhìn thấy một Form đăng ký tài khoản (Input, Button, Label...).",
                            "Test Case: Phân biệt \"Happy Path\" và \"Unhappy Path\". Cái nào dễ sinh ra bug nghiêm trọng hơn?",
                            "Báo cáo lỗi: Một thẻ Bug hoàn chỉnh trên Jira (hoặc Trello) cần bắt buộc phải có những trường thông tin nào để Dev có thể hiểu và tái hiện lỗi?",
                            "Kỹ thuật: Giải thích \"Phân tích giá trị biên\". Nếu một ô text yêu cầu độ dài từ 6 đến 12 ký tự, bạn sẽ test những giá trị độ dài nào?",
                            "Độ ưu tiên: Sự khác biệt giữa \"High Severity - Low Priority\" và \"Low Severity - High Priority\" là gì? Cho ví dụ mỗi loại.",
                            "Môi trường: Tại sao bạn vừa test trên Chrome trên Windows lại phải test thêm Safari trên macOS?",
                            "Khái niệm: Sự khác biệt giữa Smoke Test và Sanity Test là gì?",
                            "Tình huống: Nếu bạn test ra lỗi, nhưng Dev bảo \"Lỗi này do dữ liệu anh nhập linh tinh thôi, người dùng thật không ai làm thế\". Bạn xử lý thế nào?",
                            "Testing Type: Exploratory Testing (Kiểm thử thăm dò) là gì và khi nào nên áp dụng nó?",
                            "Quy trình: Bug Life Cycle diễn ra qua những trạng thái nào? Khi nào thì một bug được chuyển thành \"Re-open\"?",
                        }
                    },
                }
            },
        };
    }
}

