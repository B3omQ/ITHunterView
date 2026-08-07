const fs = require('fs');

const en = JSON.parse(fs.readFileSync('messages/en.json', 'utf8'));
const vi = JSON.parse(fs.readFileSync('messages/vi.json', 'utf8'));

const cvMatchingEn = {
  // Critical Gaps Panel
  criticalGapsTitle: 'Critical Gaps',
  suggestionLabel: 'Suggestion:',

  // Penalties Warning Panel
  penaltiesTitle: 'Critical Factors Affecting Your Match Score',
  penaltiesDesc: 'The following issues are severely impacting your compatibility with this position. These must be addressed before applying.',
  penaltySuggestionLabel: 'How to fix:',
  penaltyApplied: 'Penalty Applied',
  mustHaveMissing: 'Must-have Missing Asset (Critical Gap)',
  multipleCriticalMissing: 'Multiple Critical Assets Missing (Pool A Capped)',
  coreSkillGap: 'Core Skill Credibility Gap (-{points} points)',
  killSwitchActivated: 'Kill-Switch Activated',
  
  // CV Selection Panel
  cvSelectionTitle: 'Select Your Resume (CV)',
  cvSelectionUploadTab: 'Upload File',
  cvSelectionPasteTab: 'Paste Text',
  cvSelectionSavedTab: 'My Saved',
  cvSelectionDragDrop: 'Drag & drop your file here, or <span class="text-primary underline">browse</span>',
  cvSelectionFileSupport: 'Supports PDF, DOCX, TXT up to 5MB',
  cvSelectionExtracting: 'Extracting text from resume...',
  cvSelectionUploadSuccess: 'Uploaded successfully',
  cvSelectionPastePlaceholder: 'Paste the raw text of your resume here...',
  cvSelectionSavedLabel: 'Choose a resume saved in your profile',
  cvSelectionNoSaved: 'No saved resumes found.',
  cvSelectionSelectPlaceholder: 'Select a resume',
  uploadNewCvBtn: 'Upload New CV',
  cvUploaded: 'Uploaded {date}',
  
  // JD Selection Panel
  jdSelectionTitle: 'Select Job Description (JD)',
  jdSelectionPasteTab: 'Paste JD Text',
  jdSelectionSavedTab: 'From Saved Jobs',
  jdSelectionPastePlaceholder: 'Paste the Job Description requirements here...',
  jdSelectionSavedLabel: 'Select one of your bookmarked job postings',
  jdSelectionNoSaved: 'No saved jobs found.',
  jdSelectionSelectPlaceholder: 'Select a job',
  
  // Result Overview Card
  overallMatchLabel: 'Overall Match',
  strengthsLabel: 'Strengths',
  gapsLabel: 'Gaps',
  highlySuitable: 'Highly Suitable',
  suitable: 'Suitable',
  partiallySuitable: 'Partially Suitable',
  notSuitable: 'Not Suitable',
  criticalGapsFound: 'Critical gaps found',
  matchScoreDesc: 'Based on required skills, experience, and qualifications',
  killSwitchTitle: 'Critical Warning: Complete Lack of Core Skills (Kill-Switch)',
  killSwitchDesc: 'The system has stopped scoring the rest of your CV because it <strong>lacks any evidence</strong> of mandatory core technical skills. The score has been capped at 15/100. Please ensure you update all critical skills before applying.',
  analysisResultTitle: 'Analysis Result',
  poolA: 'Pool A: Core Technical Skills',
  poolB: 'Pool B: Additional Assets',
  poolC: 'Pool C: Experience & Education',
  
  // Requirement Breakdown
  reqBreakdownTitle: 'JD Fit Requirement Breakdown',
  reqBreakdownDesc: 'Detailed mapping and scores of specific job requirements against your resume.',
  reqTypeTechSkill: 'Technical Skills',
  reqTypeExperience: 'Experience',
  reqTypeSeniority: 'Seniority Fit',
  reqTypeDomain: 'Domain Knowledge',
  reqTypeLanguage: 'Language',
  reqTypeEducation: 'Education',
  reqTypeSoftSkill: 'Soft Skills',
  mustHave: 'Must Have',
  niceToHave: 'Nice To Have',
  criticalGap: 'Critical Gap',
  noReasoning: 'No detailed reasoning provided.',
  
  // Improvement Suggestions
  suggestionsTitle: 'Actionable Improvements',
  priorityHigh: 'High Priority',
  priorityMedium: 'Medium Priority',
  priorityLow: 'Low Priority',
  priorityLabel: '{priority} Priority',
  actionLabel: 'Action:',
  insteadOf: 'Instead of:',
  tryThis: 'Try this:',
  
  // Optimizer Header
  optimizerTitle: 'Resume Optimization',
  optimizerDesc: 'Review the AI-generated suggestions to improve your resume match rate.',
  applyAllBtn: 'Apply All Suggestions',
  exportBtn: 'Export Optimized CV',
  
  // Matching Loading State
  analyzingSuitability: 'Analyzing Suitability',
  loadingDesc: 'This might take around 15–30 seconds. Do not close this window.',
  progressLabel: 'Progress',
  step1: 'Reading and normalizing CV data...',
  step2: 'Extracting key skills and experiences...',
  step3: 'Analyzing Job Description requirements...',
  step4: 'Executing vector search and similarity matching...',
  step5: 'Evaluating match relevance via AI Judge...',
  step6: 'Applying credibility and penalty scoring...',
  step7: 'Generating final feedback report...',
  
  // Suggestion Card
  suggestionCardTitle: 'Suggestion',
  acceptBtn: 'Accept',
  rejectBtn: 'Reject',
  originalText: 'Original Text',
  suggestedText: 'Suggested Update',
  
  // History Page
  completed: 'Completed',
  failed: 'Failed',
  title: 'CV Matching History',
  loading: 'Loading your matching history...',
  description: 'View all your past resume matches against job descriptions.',
  noMatchesFound: 'No Matches Found',
  noMatchesDesc: 'You haven\'t analyzed any CVs yet. Match your first CV to see the results here.',
  matchCvNow: 'Match CV Now',
  bypassJd: 'Unknown Job',
  bypassCv: 'Unknown CV',
  matchScore: '{score}% Match',
  naMatch: 'N/A',
  viewReport: 'View Report',
  deleteHistory: 'Delete Record',
  deleteHistoryTitle: 'Delete Match Record',
  deleteHistoryConfirm: 'Are you sure you want to delete this match record? This action cannot be undone.',
  cancel: 'Cancel',
  delete: 'Delete',
  deleting: 'Deleting...',
  
  // Loading State
  analyzingCvTitle: 'Analyzing your resume...',
  analyzingCvDesc: 'Our AI is comparing your qualifications against the job requirements.',
  
  // Match New Page
  newTitle: 'AI CV Matching',
  newDesc: 'Upload your CV and paste a Job Description to let AI evaluate your suitability and uncover hidden gaps.',
  viewHistory: 'View History',
  serviceFee: 'Service Fee',
  freeSub: 'Free ({subName})',
  unlimitedMatches: 'Unlimited Matches',
  remainingMatches: '{remaining}/{limit} Matches Left',
  coinPerMatch: '{coin} Coin per match',
  subExpired: 'Your {subName} subscription has expired.',
  currentBalance: 'Current Balance:',
  topUpCoin: 'Top-up Coin',
  uploadingResume: 'Uploading Resume...',
  notEnoughCoinBtn: 'Not enough Coin (Need {cost})',
  startAnalysisFree: 'Start Analysis (Free)',
  startAnalysisCoin: 'Start Analysis ({cost} Coin)',
  matchResultInfo: 'Here is your matching result. You can optimize your CV or analyze another one.',
  analyzeAnother: 'Analyze Another',
  cannotOptimizeError: 'Cannot optimize: CV ID not found.',
  optimizeCv: 'Optimize CV',
  
  // Optimizer Completion
  optimizationComplete: 'Optimization Complete!',
  optimizationCompleteDesc: 'Your resume has been updated. You can now download the optimized version or return to the dashboard.',
  downloadPdfBtn: 'Download PDF',
  backToDashBtn: 'Back to Dashboard',
  
  // Optimizer Page
  loadingOptimizer: 'Loading CV constraints and suggestions...',
  noOptimizationsTitle: 'No Optimizations Available',
  noOptimizationsDesc: 'Your CV is already well-optimized or there are no valid AI suggestions available for this job description.',
  backToMatchResult: 'Back to Match Result',
  sessionNotInit: 'Session not initialized.',
  savedToMyCv: 'Saved to My CVs successfully!',
  cannotDownload: 'Cannot download: Session not initialized.',
  generatingOptimizedCv: 'Generating your optimized CV...',
  cvDownloaded: 'CV Downloaded successfully!',
  failedGenerateCv: 'Failed to generate CV file.',
  previewPdfOnly: 'Real-time preview is only available for PDF files. For Word Documents (.docx), please use the Download button to view changes.',
  failedLoadPreview: 'Failed to load preview image.',
  cvPreviewTitle: 'CV Preview',
  cvPreviewDesc: 'This is a real-time preview of your optimized CV.',
  generatingPreviewImg: 'Generating preview image...',
};

const cvMatchingVi = {
  // Critical Gaps Panel
  criticalGapsTitle: 'Lỗ Hổng Nghiêm Trọng',
  suggestionLabel: 'Đề xuất:',

  // Penalties Warning Panel
  penaltiesTitle: 'Các Yếu Tố Chính Ảnh Hưởng Đến Điểm Phù Hợp Của Bạn',
  penaltiesDesc: 'Các vấn đề sau đây đang tác động nghiêm trọng đến mức độ phù hợp của bạn với vị trí này. Bạn cần giải quyết chúng trước khi ứng tuyển.',
  penaltySuggestionLabel: 'Cách khắc phục:',
  penaltyApplied: 'Bị Trừ Điểm',
  mustHaveMissing: 'Thiếu Tài Sản Bắt Buộc (Lỗ Hổng Nghiêm Trọng)',
  multipleCriticalMissing: 'Thiếu Nhiều Tài Sản Bắt Buộc (Bị Giới Hạn Pool A)',
  coreSkillGap: 'Khoảng Cách Đáng Tin Cậy Của Kỹ Năng Cốt Lõi (-{points} điểm)',
  killSwitchActivated: 'Kích Hoạt Kill-Switch (Ngắt Mạch)',
  
  // CV Selection Panel
  cvSelectionTitle: 'Chọn Hồ Sơ (CV) Của Bạn',
  cvSelectionUploadTab: 'Tải File',
  cvSelectionPasteTab: 'Dán Văn Bản',
  cvSelectionSavedTab: 'Đã Lưu',
  cvSelectionDragDrop: 'Kéo thả file vào đây, hoặc <span class="text-primary underline">chọn file</span>',
  cvSelectionFileSupport: 'Hỗ trợ PDF, DOCX, TXT tối đa 5MB',
  cvSelectionExtracting: 'Đang trích xuất văn bản từ CV...',
  cvSelectionUploadSuccess: 'Tải lên thành công',
  cvSelectionPastePlaceholder: 'Dán văn bản thô của CV vào đây...',
  cvSelectionSavedLabel: 'Chọn một CV đã lưu trong hồ sơ của bạn',
  cvSelectionNoSaved: 'Không tìm thấy CV nào đã lưu.',
  cvSelectionSelectPlaceholder: 'Chọn CV',
  uploadNewCvBtn: 'Tải Lên CV Mới',
  cvUploaded: 'Đã tải lên {date}',
  
  // JD Selection Panel
  jdSelectionTitle: 'Chọn Yêu Cầu Công Việc (JD)',
  jdSelectionPasteTab: 'Dán Văn Bản JD',
  jdSelectionSavedTab: 'Từ Các Công Việc Đã Lưu',
  jdSelectionPastePlaceholder: 'Dán các yêu cầu công việc vào đây...',
  jdSelectionSavedLabel: 'Chọn một trong các công việc bạn đã lưu',
  jdSelectionNoSaved: 'Không tìm thấy công việc đã lưu nào.',
  jdSelectionSelectPlaceholder: 'Chọn công việc',
  
  // Result Overview Card
  overallMatchLabel: 'Độ Phù Hợp Tổng Thể',
  strengthsLabel: 'Điểm Mạnh',
  gapsLabel: 'Lỗ Hổng',
  highlySuitable: 'Rất Phù Hợp',
  suitable: 'Phù Hợp',
  partiallySuitable: 'Phù Hợp Một Phần',
  notSuitable: 'Không Phù Hợp',
  criticalGapsFound: 'Phát hiện lỗ hổng nghiêm trọng',
  matchScoreDesc: 'Dựa trên kỹ năng, kinh nghiệm và bằng cấp được yêu cầu',
  killSwitchTitle: 'Cảnh báo Nghiêm trọng: Thiếu hoàn toàn Kỹ năng cốt lõi (Kill-Switch)',
  killSwitchDesc: 'Hệ thống đã ngừng chấm điểm phần còn lại vì CV của bạn <strong>không có bất kỳ bằng chứng nào</strong> về các công nghệ cốt lõi bắt buộc (Core Tech Skills). Điểm số đã bị đóng băng ở mức 15/100. Hãy đảm bảo bạn có cập nhật đầy đủ các kỹ năng quan trọng nhất trước khi ứng tuyển.',
  analysisResultTitle: 'Kết Quả Phân Tích',
  poolA: 'Pool A: Kỹ Năng Kỹ Thuật Lõi',
  poolB: 'Pool B: Tài Sản Bổ Sung',
  poolC: 'Pool C: Kinh Nghiệm & Học Vấn',
  
  // Requirement Breakdown
  reqBreakdownTitle: 'Phân Tích Yêu Cầu JD Fit',
  reqBreakdownDesc: 'Ánh xạ chi tiết và điểm số của các yêu cầu công việc cụ thể so với hồ sơ của bạn.',
  reqTypeTechSkill: 'Kỹ Năng Kỹ Thuật',
  reqTypeExperience: 'Kinh Nghiệm',
  reqTypeSeniority: 'Độ Phù Hợp Cấp Bậc',
  reqTypeDomain: 'Kiến Thức Lĩnh Vực',
  reqTypeLanguage: 'Ngoại Ngữ',
  reqTypeEducation: 'Học Vấn',
  reqTypeSoftSkill: 'Kỹ Năng Mềm',
  mustHave: 'Bắt Buộc',
  niceToHave: 'Nên Có',
  criticalGap: 'Lỗ Hổng Nghiêm Trọng',
  noReasoning: 'Không có lý do chi tiết nào được cung cấp.',
  
  // Improvement Suggestions
  suggestionsTitle: 'Các Cải Thiện Khả Thi',
  priorityHigh: 'Ưu Tiên Cao',
  priorityMedium: 'Ưu Tiên Trung Bình',
  priorityLow: 'Ưu Tiên Thấp',
  priorityLabel: 'Ưu tiên {priority}',
  actionLabel: 'Hành động:',
  insteadOf: 'Thay vì:',
  tryThis: 'Hãy thử:',
  
  // Optimizer Header
  optimizerTitle: 'Tối Ưu Hóa Hồ Sơ',
  optimizerDesc: 'Xem xét các đề xuất từ AI để cải thiện tỷ lệ phù hợp của hồ sơ.',
  applyAllBtn: 'Áp Dụng Tất Cả',
  exportBtn: 'Xuất CV Đã Tối Ưu',
  
  // Matching Loading State
  analyzingSuitability: 'Đang Phân Tích Độ Phù Hợp',
  loadingDesc: 'Quá trình này có thể mất khoảng 15-30 giây. Vui lòng không đóng cửa sổ này.',
  progressLabel: 'Tiến độ',
  step1: 'Đang đọc và chuẩn hóa dữ liệu CV...',
  step2: 'Trích xuất các kỹ năng và kinh nghiệm chính...',
  step3: 'Phân tích yêu cầu Công việc (JD)...',
  step4: 'Đang thực hiện tìm kiếm vector và đo độ tương đồng...',
  step5: 'Đánh giá độ phù hợp thông qua AI Judge...',
  step6: 'Áp dụng điểm số đáng tin cậy và hình phạt...',
  step7: 'Tạo báo cáo phản hồi cuối cùng...',
  
  // Suggestion Card
  suggestionCardTitle: 'Đề Xuất',
  acceptBtn: 'Chấp Nhận',
  rejectBtn: 'Từ Chối',
  originalText: 'Văn Bản Gốc',
  suggestedText: 'Văn Bản Cập Nhật',
  
  // History Page
  completed: 'Đã Hoàn Thành',
  failed: 'Thất Bại',
  title: 'Lịch Sử Phân Tích CV',
  loading: 'Đang tải lịch sử phân tích của bạn...',
  description: 'Xem lại tất cả các bản phân tích CV so với yêu cầu công việc trước đây.',
  noMatchesFound: 'Không Có Lịch Sử Nào',
  noMatchesDesc: 'Bạn chưa thực hiện phân tích CV nào. Hãy bắt đầu để xem kết quả tại đây.',
  matchCvNow: 'Phân Tích CV Ngay',
  bypassJd: 'Công Việc Không Xác Định',
  bypassCv: 'CV Không Xác Định',
  matchScore: 'Phù hợp {score}%',
  naMatch: 'N/A',
  viewReport: 'Xem Báo Cáo',
  deleteHistory: 'Xóa Bản Ghi',
  deleteHistoryTitle: 'Xóa Bản Ghi Phân Tích',
  deleteHistoryConfirm: 'Bạn có chắc chắn muốn xóa bản ghi phân tích này không? Hành động này không thể hoàn tác.',
  cancel: 'Hủy',
  delete: 'Xóa',
  deleting: 'Đang xóa...',
  
  // Loading State
  analyzingCvTitle: 'Đang phân tích hồ sơ của bạn...',
  analyzingCvDesc: 'AI của chúng tôi đang so sánh trình độ của bạn với các yêu cầu công việc.',
  
  // Match New Page
  newTitle: 'Phân Tích CV bằng AI',
  newDesc: 'Tải CV của bạn lên và dán yêu cầu công việc để AI đánh giá mức độ phù hợp và chỉ ra những điểm cần cải thiện.',
  viewHistory: 'Xem Lịch Sử',
  serviceFee: 'Phí Dịch Vụ',
  freeSub: 'Miễn Phí ({subName})',
  unlimitedMatches: 'Không Giới Hạn Lượt Phân Tích',
  remainingMatches: 'Còn {remaining}/{limit} Lượt Phân Tích',
  coinPerMatch: '{coin} Coin / Lần phân tích',
  subExpired: 'Gói {subName} của bạn đã hết hạn.',
  currentBalance: 'Số dư hiện tại:',
  topUpCoin: 'Nạp Coin',
  uploadingResume: 'Đang Tải CV...',
  notEnoughCoinBtn: 'Không đủ Coin (Cần {cost})',
  startAnalysisFree: 'Bắt Đầu Phân Tích (Miễn Phí)',
  startAnalysisCoin: 'Bắt Đầu Phân Tích ({cost} Coin)',
  matchResultInfo: 'Đây là kết quả phân tích của bạn. Bạn có thể tối ưu hóa CV này hoặc phân tích một CV khác.',
  analyzeAnother: 'Phân Tích CV Khác',
  cannotOptimizeError: 'Không thể tối ưu hóa: Không tìm thấy CV ID.',
  optimizeCv: 'Tối Ưu CV',
  
  // Optimizer Completion
  optimizationComplete: 'Tối Ưu Hóa Hoàn Tất!',
  optimizationCompleteDesc: 'Hồ sơ của bạn đã được cập nhật. Bạn có thể tải xuống phiên bản đã được tối ưu hoặc quay lại bảng điều khiển.',
  downloadPdfBtn: 'Tải Xuống PDF',
  backToDashBtn: 'Quay Lại Bảng Điều Khiển',

  // Optimizer Page
  loadingOptimizer: 'Đang tải ràng buộc CV và các đề xuất...',
  noOptimizationsTitle: 'Không Có Tối Ưu Hóa Nào',
  noOptimizationsDesc: 'CV của bạn đã được tối ưu hoặc không có đề xuất AI nào hợp lệ cho công việc này.',
  backToMatchResult: 'Quay lại Kết Quả Phân Tích',
  sessionNotInit: 'Phiên chưa được khởi tạo.',
  savedToMyCv: 'Đã lưu vào CV Của Tôi thành công!',
  cannotDownload: 'Không thể tải xuống: Phiên chưa được khởi tạo.',
  generatingOptimizedCv: 'Đang tạo CV đã tối ưu của bạn...',
  cvDownloaded: 'Tải CV thành công!',
  failedGenerateCv: 'Không thể tạo file CV.',
  previewPdfOnly: 'Bản xem trước theo thời gian thực chỉ khả dụng cho file PDF. Đối với tài liệu Word (.docx), vui lòng sử dụng nút Tải Xuống để xem các thay đổi.',
  failedLoadPreview: 'Không thể tải ảnh xem trước.',
  cvPreviewTitle: 'Xem Trước CV',
  cvPreviewDesc: 'Đây là bản xem trước theo thời gian thực CV đã tối ưu của bạn.',
  generatingPreviewImg: 'Đang tạo ảnh xem trước...',
};

en['CandidateCVMatching'] = cvMatchingEn;
vi['CandidateCVMatching'] = cvMatchingVi;

fs.writeFileSync('messages/en.json', JSON.stringify(en, null, 2));
fs.writeFileSync('messages/vi.json', JSON.stringify(vi, null, 2));

console.log('Candidate translations updated successfully!');
