const fs = require('fs');
const enPath = 'messages/en.json';
const viPath = 'messages/vi.json';

const en = JSON.parse(fs.readFileSync(enPath, 'utf8'));
const vi = JSON.parse(fs.readFileSync(viPath, 'utf8'));

// Initialize namespaces if they don't exist
if (!en['RecruiterJobs']) en['RecruiterJobs'] = {};
if (!vi['RecruiterJobs']) vi['RecruiterJobs'] = {};
if (!en['RecruiterJobsNew']) en['RecruiterJobsNew'] = {};
if (!vi['RecruiterJobsNew']) vi['RecruiterJobsNew'] = {};
if (!en['RecruiterJobEdit']) en['RecruiterJobEdit'] = {};
if (!vi['RecruiterJobEdit']) vi['RecruiterJobEdit'] = {};

// RecruiterJobs
en['RecruiterJobs']['sysExpiryNote'] = "System Expiry:";
vi['RecruiterJobs']['sysExpiryNote'] = "Hạn hiển thị hệ thống:";

en['RecruiterJobs']['hiddenBadge'] = "HIDDEN";
vi['RecruiterJobs']['hiddenBadge'] = "ĐÃ ẨN";

en['RecruiterJobs']['daysLeft'] = "{days} DAYS LEFT";
vi['RecruiterJobs']['daysLeft'] = "CÒN {days} NGÀY";

en['RecruiterJobs']['extendWarningPrefix'] = "📌 Job posting ";
vi['RecruiterJobs']['extendWarningPrefix'] = "📌 Tin tuyển dụng ";

en['RecruiterJobs']['extendWarningSuffix'] = " will be hidden automatically by the system after 30 days of active display. You can use this feature to extend the display time by 15 days from the current system expiry date.";
vi['RecruiterJobs']['extendWarningSuffix'] = " sẽ bị hệ thống tự động ẩn sau 30 ngày hiển thị. Bạn có thể dùng tính năng này để gia hạn thời gian hiển thị thêm 15 ngày tính từ hạn hiện tại.";

en['RecruiterJobs']['currentExpirySys'] = "Current Expiry (System)";
vi['RecruiterJobs']['currentExpirySys'] = "Hạn hiển thị (Hệ thống)";

en['RecruiterJobs']['plus15DaysBtn'] = "+ 15 Days";
vi['RecruiterJobs']['plus15DaysBtn'] = "+ 15 Ngày";

// RecruiterJobsNew
en['RecruiterJobsNew']['sysVisibilityNotice'] = "* <strong>Note:</strong> Regardless of the application deadline, the system limits the default active display time to <strong>30 days</strong> from the publish date. After 30 days, you can extend (paid) to keep the job visible.";
vi['RecruiterJobsNew']['sysVisibilityNotice'] = "* <strong>Lưu ý:</strong> Bất kể hạn nộp hồ sơ là ngày nào, hệ thống mặc định chỉ hiển thị tin này trong vòng <strong>30 ngày</strong> kể từ lúc đăng bài. Sau 30 ngày, bạn có thể gia hạn (tốn phí) để tin tiếp tục hiển thị.";

// RecruiterJobEdit
en['RecruiterJobEdit']['sysVisibilityNotice'] = "* <strong>Note:</strong> Regardless of the application deadline, the system limits the default active display time to <strong>30 days</strong> from the publish date. After 30 days, you can extend (paid) to keep the job visible.";
vi['RecruiterJobEdit']['sysVisibilityNotice'] = "* <strong>Lưu ý:</strong> Bất kể hạn nộp hồ sơ là ngày nào, hệ thống mặc định chỉ hiển thị tin này trong vòng <strong>30 ngày</strong> kể từ lúc đăng bài. Sau 30 ngày, bạn có thể gia hạn (tốn phí) để tin tiếp tục hiển thị.";


fs.writeFileSync(enPath, JSON.stringify(en, null, 2));
fs.writeFileSync(viPath, JSON.stringify(vi, null, 2));

console.log('Translations updated successfully!');
