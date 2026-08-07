const fs = require('fs');

const path = 'src/app/(candidate)/candidate/optimize-cv/page.tsx';
let content = fs.readFileSync(path, 'utf8');

const replacements = [
  ["toast.error('Không thể tải chi tiết báo cáo.');", "toast.error(t('toastLoadDetailFail'));"],
  ["toast.error('Chỉ hỗ trợ các định dạng file PDF (.pdf) hoặc Word (.docx)');", "toast.error(t('toastInvalidFormat'));"],
  ["toast.error(`Bạn không đủ Coin. Cần ${optimizeCost.toLocaleString()} Coin để tiếp tục.`);", "toast.error(t('toastNotEnoughCoin', { cost: optimizeCost.toLocaleString() }));"],
  ["toast.error('Vui lòng chọn một CV từ danh sách của bạn');", "toast.error(t('toastSelectSavedCv'));"],
  ["toast.success('Phân tích CV hoàn tất!');", "toast.success(t('toastAnalysisComplete'));"],
  ["toast.error('Vui lòng chọn file CV để tải lên');", "toast.error(t('toastSelectFileToUpload'));"],
  ["toast.error('Tải CV lên không thành công');", "toast.error(t('toastUploadFail'));"],

  // Status badges
  ["Đạt chuẩn", "{t('statusGood')}"],
  ["Cần chú ý", "{t('statusWarning')}"],
  ["Còn thiếu", "{t('statusMissing')}"],
  ["Ưu tiên cao", "{t('priorityHigh')}"],
  ["Ưu tiên vừa", "{t('priorityMedium')}"],
  ["Khuyến nghị", "{t('priorityRecommended')}"],

  // Header
  ["Tối ưu hóa Bố cục & Cấu trúc CV", "{t('pageTitle')}"],
  ["Đánh giá độ đầy đủ của các phần chuẩn trong CV và phân tích thứ tự ưu tiên bố cục (dành cho Sinh viên/Fresher hoặc Người đã đi làm) mà không chỉnh sửa văn bản của bạn.", "{t('pageDesc')}"],
  ["Đánh giá CV khác", "{t('evaluateAnotherBtn')}"],

  // Loading
  ["Hệ thống AI đang phân tích CV của bạn...", "{t('analyzingTitle')}"],
  ["Đang kiểm tra các Section tiêu chuẩn, đánh giá thứ tự ưu tiên theo kinh nghiệm làm việc và tổng hợp giải pháp cải thiện.", "{t('analyzingDesc')}"],

  // Input
  ["Phí sử dụng:", "{t('costLabel')}"],
  ["Miễn phí (Gói {activeSubName} - Còn {isSubUnlimited ? 'Vô hạn' : subRemaining} lượt)", "{isSubUnlimited ? t('freeSubUnlimited', { subName: activeSubName }) : t('freeSubRemaining', { subName: activeSubName, remaining: subRemaining })}"],
  ["{optimizeCost.toLocaleString()} Coin / lượt", "{t('costPerTime', { cost: optimizeCost.toLocaleString() })}"],
  ["Số dư hiện tại:", "{t('currentBalanceLabel')}"],
  ["Nạp thêm", "{t('topUpBtn')}"],

  ["Chọn CV cần đánh giá", "{t('selectCvTitle')}"],
  ["Vui lòng chọn CV đã lưu trong tài khoản của bạn hoặc tải lên một file CV mới (.pdf, .docx).", "{t('selectCvDesc')}"],
  ["CV đã lưu ({myCvs.length})", "{t('tabSavedCv', { count: myCvs.length })}"],
  ["Tải lên CV mới", "{t('tabUploadCv')}"],
  ["Đang tải danh sách CV...", "{t('loadingCvList')}"],
  ["Bạn chưa có CV nào được lưu trong hệ thống.", "{t('noSavedCv')}"],
  ["Tải lên CV đầu tiên", "{t('uploadFirstCvBtn')}"],
  ["CV Chính", "{t('primaryCvBadge')}"],

  ["Nhấp để chọn file hoặc kéo thả vào đây", "{t('uploadDragDrop')}"],
  ["Hỗ trợ định dạng PDF hoặc DOCX (tối đa 10MB)", "{t('uploadFormatSupport')}"],
  ["Đang tải CV lên...", "{t('btnUploading')}"],
  ["Phân tích & Tối ưu CV", "{t('btnAnalyzeOptimize')}"],

  // Results
  ["Điểm Đánh giá Cấu trúc", "{t('scoreTitle')}"],
  ["analysisResult.overallScore >= 80 ? 'Cấu trúc rất tốt' : analysisResult.overallScore >= 60 ? 'Khá đầy đủ, cần hoàn thiện' : 'Cần bổ sung thêm section'", "analysisResult.overallScore >= 80 ? t('scoreExcellent') : analysisResult.overallScore >= 60 ? t('scoreGood') : t('scoreNeedsWork')"],
  ["Nhận xét Tổng quan của AI", "{t('overviewTitle')}"],
  ["File CV:", "{t('cvFileLabel')}"],
  ["CV đã chọn", "{t('defaultCvName')}"],

  ["1. Kiểm tra Độ đầy đủ của các Section chuẩn", "{t('section1Title')}"],
  ["Đánh giá sự hiện diện của các danh mục bắt buộc và bổ sung trong bố cục CV.", "{t('section1Desc')}"],
  ["2. Phân tích Thứ tự Ưu tiên Bố cục (Layout Order)", "{t('section2Title')}"],
  ["Đánh giá đối tượng: {analysisResult.priorityOrder.candidateLevel}", "{t('targetAudience', { level: analysisResult.priorityOrder.candidateLevel })}"],
  ["Quy tắc: Đối với Sinh viên/Mới đi làm ➔ Ưu tiên Học vấn & Kỹ năng lên trước; Đối với Người đã đi làm ➔ Ưu tiên Kinh nghiệm lên trước.", "{t('section2Rule')}"],
  ["'Bố cục thứ tự sắp xếp hiện tại là TỐI ƯU'", "t('orderOptimal')"],
  ["'Bố cục thứ tự sắp xếp CẦN ĐIỀU CHỈNH'", "t('orderNeedsAdjustment')"],
  ["Thứ tự hiện tại trong CV:", "{t('currentOrderLabel')}"],
  ["Thứ tự khuyến nghị tối ưu:", "{t('recommendedOrderLabel')}"],

  ["3. Danh sách Giải pháp & Khuyến nghị Cải thiện", "{t('section3Title')}"],
  ["Các đề xuất cụ thể giúp nâng cao tính chuyên nghiệp và thu hút nhà tuyển dụng mà không làm biến đổi nội dung gốc.", "{t('section3Desc')}"],
  ["Không có khuyến nghị bổ sung nào. CV của bạn đã tuân thủ rất tốt các chuẩn bố cục!", "{t('noRecommendations')}"],
  ["Ví dụ Trước (Hiện tại):", "{t('exampleBefore')}"],
  ["Ví dụ Sau (Khuyến nghị):", "{t('exampleAfter')}"],

  // History
  ["Lịch sử Tối ưu hóa CV", "{t('historyTitle')}"],
  ["Tổng cộng: {historyData.totalCount} lần đánh giá", "{t('historyTotal', { count: historyData.totalCount })}"],
  ["Xem lại các kết quả đánh giá và đề xuất tối ưu hóa cấu trúc CV trước đây của bạn.", "{t('historyDesc')}"],
  ["Đang tải lịch sử phân tích...", "{t('loadingHistory')}"],
  ["Chưa có lịch sử phân tích CV nào.", "{t('noHistoryTitle')}"],
  ["Các lần đánh giá CV mới sẽ tự động hiển thị tại đây.", "{t('noHistoryDesc')}"],
  ["'Hồ sơ CV'", "t('defaultHistoryName')"],
  ["Xem báo cáo", "{t('viewReportBtn')}"],
  ["Xóa lịch sử", "{t('deleteHistoryBtn')}"]
];

for (const [search, replace] of replacements) {
  content = content.replace(search, replace);
}

fs.writeFileSync(path, content, 'utf8');
console.log('Replacements completed.');
