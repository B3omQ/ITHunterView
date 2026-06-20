Output 1 — API Specification

A. Profile Header (dùng chung cho cả 4 tab)

1. GET /api/v1/candidate/profile/summary


Mô tả: Lấy dữ liệu hiển thị ở header — tên, avatar, vị trí hiện tại (suy ra từ experience đang là is_current), location, trạng thái visible, % hoàn thiện hồ sơ.
Request: không có body (userId lấy từ token).
Response:


json{
  "data": {
    "id": "uuid",
    "fullName": "Alex Johnson",
    "avatarUrl": "string|null",
    "currentTitle": "Senior Frontend Developer",
    "location": "San Francisco, CA",
    "isVisibleToRecruiters": true,
    "profileCompletionPercentage": 72,
    "completionHint": "Add 2 more sections to reach 80%",
    "lastSavedAt": "2026-06-20T10:42:00Z"
  }
}

2. PATCH /api/v1/candidate/profile/visibility


Mô tả: Bật/tắt toggle "Visible to Recruiters".
Request: { "isVisibleToRecruiters": true }
Response: { "data": true }


3. POST /api/v1/candidate/profile/avatar (multipart/form-data)


Mô tả: Upload/đổi ảnh đại diện.
Request: form field file
Response: { "data": { "avatarUrl": "https://..." } }



B. Tab Personal Info

4. GET /api/v1/candidate/profile/personal-info


Response:


json{
  "data": {
    "firstName": "Alex",
    "lastName": "Johnson",
    "email": "alex.johnson@email.com",
    "phone": "+1 (415) 555-0198",
    "location": "San Francisco, CA",
    "aboutMe": "Experienced frontend developer with 7+ years...",
    "portfolioUrl": "https://yourportfolio.com",
    "linkedInUrl": "linkedin.com/in/username",
    "githubUrl": "github.com/username",
    "updatedAt": "2026-06-20T10:42:00Z"
  }
}

5. PUT /api/v1/candidate/profile/personal-info


Mô tả: Lưu toàn bộ Personal Info (nút "Save Changes").
Request:


json{
  "firstName": "Alex",
  "lastName": "Johnson",
  "phone": "+1 (415) 555-0198",
  "location": "San Francisco, CA",
  "aboutMe": "max 500 ký tự",
  "portfolioUrl": "string|null",
  "linkedInUrl": "string|null",
  "githubUrl": "string|null"
}


Response: bản ghi đã cập nhật + updatedAt.



C. Tab Skills

6. GET /api/v1/candidate/profile/skills


Response: { "data": [{ "skillId": 1, "name": "React" }, ...], "totalCount": 12 }


7. GET /api/v1/skills/search?keyword=Doc&excludeOwned=true


Mô tả: Autocomplete cho input "Type a skill..." (tìm trong master skills, loại bỏ skill user đã có).
Response: { "data": [{ "id": 1, "name": "Docker", "categoryId": 3 }] }


8. GET /api/v1/candidate/profile/skills/suggestions


Mô tả: Gợi ý kỹ năng theo xu hướng thị trường ("Suggested for You").
Response: { "data": [{ "skillId": 9, "name": "Vue.js" }, ...] }


9. POST /api/v1/candidate/profile/skills


Mô tả: Thêm 1 skill (từ ô input hoặc click "+" ở phần Suggested).
Request: { "skillId": 9, "proficiencyLevel": 3 } (nếu skill chưa tồn tại trong master data, FE gọi skills create trước hoặc BE tự find-or-create theo skillName)
Response: skill vừa thêm.


10. DELETE /api/v1/candidate/profile/skills/{skillId}


Mô tả: Xoá skill khỏi profile (nút "x" trên chip).
Response: { "data": true }



D. Tab Experience

11. GET /api/v1/candidate/profile/experiences


Response:


json{
  "data": [
    {
      "id": "uuid",
      "title": "Senior Frontend Developer",
      "companyName": "Stripe",
      "companyId": "uuid|null",
      "location": "San Francisco, CA",
      "employmentType": "FULL_TIME",
      "startDate": "2022-01-01",
      "endDate": null,
      "isCurrent": true,
      "description": "Led the redesign of Stripe's merchant dashboard..."
    }
  ]
}

12. POST /api/v1/candidate/profile/experiences


Request: như object trên (không có id).
Response: bản ghi đã tạo.


13. PUT /api/v1/candidate/profile/experiences/{id}


Request: cùng cấu trúc với POST.
Response: bản ghi đã cập nhật.


14. DELETE /api/v1/candidate/profile/experiences/{id}


Response: { "data": true }



E. Tab Education

15. GET /api/v1/majors


Mô tả: Master data cho dropdown ngành học khi thêm/sửa Education.
Response: { "data": [{ "id": 1, "name": "Computer Science", "code": "CS" }] }


16. GET /api/v1/candidate/profile/educations


Response:


json{
  "data": [
    {
      "id": "uuid",
      "degree": "B.S. Computer Science",
      "majorId": 1,
      "institutionName": "UC Berkeley",
      "gpa": 3.8,
      "maxGpa": 4.0,
      "startDate": "2013-09-01",
      "endDate": "2017-06-01",
      "description": "Focus on Human-Computer Interaction..."
    }
  ]
}

17. POST /api/v1/candidate/profile/educations — Request giống object trên (không id).

18. PUT /api/v1/candidate/profile/educations/{id} — cùng cấu trúc.

19. DELETE /api/v1/candidate/profile/educations/{id} — { "data": true }


F. Tab Education → Certifications

20. GET /api/v1/candidate/profile/certifications


Response:


json{
  "data": [
    {
      "id": "uuid",
      "name": "AWS Certified Developer – Associate",
      "issuingOrganization": "Amazon Web Services",
      "issueDate": "2023-03-01",
      "expirationDate": "2026-03-01",
      "credentialUrl": "string|null"
    }
  ]
}

21. POST /api/v1/candidate/profile/certifications — Request giống trên (không id), expirationDate có thể null ("No expiry").

22. PUT /api/v1/candidate/profile/certifications/{id} — cùng cấu trúc.

23. DELETE /api/v1/candidate/profile/certifications/{id} — { "data": true }