const fs = require('fs');
const en = JSON.parse(fs.readFileSync('messages/en.json', 'utf8'));
const vi = JSON.parse(fs.readFileSync('messages/vi.json', 'utf8'));

const aiConfigEn = {
  title: 'AI Configuration',
  desc: 'Manage the active AI model, API keys, and rate limits for platform intelligence features.',
  saveSuccess: 'AI Configuration saved successfully!',
  saveError: 'Failed to save AI config',
  loadError: 'Failed to load AI config',
  testConnectionSuccessMsg: 'Test connection successful!',
  testConnectionFailedMsg: 'Test connection failed!',
  testConnectionError: 'Test connection failed due to an error.',
  testSuccess: 'Success',
  
  card1Title: 'Active Provider Selection',
  card1Desc: 'Choose the primary Large Language Model (LLM) engine powering AI features across the application.',
  modelLabel: 'Model: ',
  defaultModel: 'Default',
  configured: 'Configured ({apiKeyPreview})',
  notConfigured: 'Not Configured',

  card2Title: 'Provider Settings ({activeProvider})',
  card2Desc: 'Configure authentication keys and rate limits for {activeProvider}.',
  apiKeyLabel: 'API Key',
  apiKeyPlaceholder: 'Leave blank to keep existing configured key (sk-...)',
  apiKeyHelp: 'This key will be securely saved in the encrypted database and used for {activeProvider} requests.',
  rateLimitLabel: 'Global Rate Limit (Requests per minute per user)',
  rateLimitHelp: 'Limits the maximum number of AI requests a single IP or user account can issue in 1 minute to protect quota limits.',

  testConnectionBtn: 'Test Connection',
  connectedStatus: 'Connected ({ms}ms)',
  failedStatus: 'Connection Failed',
  saveConfigBtn: 'Save Configuration',

  errorDetailsTitle: 'Connection Error Details'
};

const aiConfigVi = {
  title: 'Cấu Hình AI',
  desc: 'Quản lý mô hình AI hoạt động, khóa API và giới hạn tốc độ cho các tính năng thông minh của nền tảng.',
  saveSuccess: 'Đã lưu cấu hình AI thành công!',
  saveError: 'Lưu cấu hình AI thất bại',
  loadError: 'Tải cấu hình AI thất bại',
  testConnectionSuccessMsg: 'Kiểm tra kết nối thành công!',
  testConnectionFailedMsg: 'Kiểm tra kết nối thất bại!',
  testConnectionError: 'Kiểm tra kết nối thất bại do lỗi.',
  testSuccess: 'Thành công',
  
  card1Title: 'Lựa Chọn Nhà Cung Cấp Hoạt Động',
  card1Desc: 'Chọn mô hình ngôn ngữ lớn (LLM) chính cung cấp sức mạnh cho các tính năng AI trên toàn ứng dụng.',
  modelLabel: 'Mô hình: ',
  defaultModel: 'Mặc định',
  configured: 'Đã cấu hình ({apiKeyPreview})',
  notConfigured: 'Chưa cấu hình',

  card2Title: 'Cài Đặt Nhà Cung Cấp ({activeProvider})',
  card2Desc: 'Định cấu hình khóa xác thực và giới hạn tốc độ cho {activeProvider}.',
  apiKeyLabel: 'Khóa API',
  apiKeyPlaceholder: 'Để trống để giữ nguyên khóa đã định cấu hình (sk-...)',
  apiKeyHelp: 'Khóa này sẽ được lưu trữ an toàn trong cơ sở dữ liệu mã hóa và dùng cho các yêu cầu {activeProvider}.',
  rateLimitLabel: 'Giới Hạn Tốc Độ Chung (Yêu cầu mỗi phút trên người dùng)',
  rateLimitHelp: 'Giới hạn số lượng yêu cầu AI tối đa mà một IP hoặc tài khoản người dùng có thể gửi trong 1 phút để bảo vệ hạn mức.',

  testConnectionBtn: 'Kiểm Tra Kết Nối',
  connectedStatus: 'Đã Kết Nối ({ms}ms)',
  failedStatus: 'Kết Nối Thất Bại',
  saveConfigBtn: 'Lưu Cấu Hình',

  errorDetailsTitle: 'Chi Tiết Lỗi Kết Nối'
};

en['AiConfig'] = aiConfigEn;
vi['AiConfig'] = aiConfigVi;

fs.writeFileSync('messages/en.json', JSON.stringify(en, null, 2));
fs.writeFileSync('messages/vi.json', JSON.stringify(vi, null, 2));
console.log('Messages updated successfully!');
