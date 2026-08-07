const fs = require('fs');
const en = JSON.parse(fs.readFileSync('messages/en.json', 'utf8'));
const vi = JSON.parse(fs.readFileSync('messages/vi.json', 'utf8'));

const recruiterEn = {
  // Common
  addBtn: 'Add',
  editBtn: 'Edit',
  deleteBtn: 'Delete',
  cancelBtn: 'Cancel',
  saveBtn: 'Save',
  searchPlaceholder: 'Search...',
  
  // Layout & Nav
  dashboard: 'Dashboard',
  applicants: 'Applicants',
  jobs: 'Jobs',
  billing: 'Billing History',
  settings: 'Settings',
  profile: 'Profile',
};

const recruiterBillingHistoryEn = {
  pageTitle: 'Transaction History',
  pageDesc: 'View and manage all your subscription and top-up transactions.',
};

const recruiterBillingHistoryTableEn = {
  badgeSuccess: 'Paid / Success',
  badgePending: 'Pending',
  badgeCancelled: 'Cancelled',
  badgeFailed: 'Failed',
  planName: '{name}',
  planSub: 'Subscription',
  planDesc: 'Recruiter Subscription',
  typeCoin: 'Wallet Top-up',
  topupDesc: 'Added ITH Coins to your balance',
  searchPlaceholder: 'Search by Code, Type, Gateway...',
  clearSearch: 'Clear search',
  statusPlaceholder: 'Status Filter',
  statusAll: 'All Statuses',
  statusSuccess: 'Success / Paid',
  statusPending: 'Pending',
  statusFailed: 'Failed / Cancelled',
  typePlaceholder: 'Transaction Type',
  typeAll: 'All Types',
  typeSubscription: 'Subscription',
  clearFilters: 'Clear Filters',
  clearAllFilters: 'Clear All Filters',
  colOrderCode: 'ORDER CODE',
  colDate: 'DATE',
  colDesc: 'DESCRIPTION',
  colGateway: 'GATEWAY',
  colAmount: 'AMOUNT',
  colStatus: 'STATUS',
  errLoadFailed: 'Failed to load billing history',
  errLoadDesc: 'Please check your connection and try again.',
  noRecords: 'No transactions found',
  noRecordsFilter: 'Try clearing some filters.',
  noRecordsEmpty: 'You do not have any transactions yet.',
  showingText: 'Showing <span>{start} - {end}</span> of <span>{total}</span> transactions',
  pageSizePlaceholder: 'Page size',
  perPage: '{size} / page',
  na: 'N/A',
};

const recruiterCandidateProfileEn = {
  loading: 'Loading candidate profile...',
  notFoundTitle: 'Profile Not Found',
  notFoundDesc: 'The candidate profile could not be found or you do not have permission to view it.',
  pageTitle: 'Candidate Profile',
  noLocation: 'Location not provided',
  noEmail: 'Email not provided',
  noPhone: 'Phone not provided',
  socialLinks: 'Social Links',
  skills: 'Skills',
  noSkills: 'No skills added yet.',
  aboutMe: 'About Me',
  workExp: 'Work Experience',
  present: 'Present',
  noExp: 'No work experience added.',
  education: 'Education',
  noEdu: 'No education history added.',
  certifications: 'Certifications',
  issued: 'Issued: {date}',
  viewCredential: 'View Credential',
};

const recruiterApplicantsEn = {
  toastStatusUpdateSuccess: 'Status updated successfully',
  toastStatusUpdateFail: 'Failed to update status',
  toastLoadDetailFail: 'Failed to load application details.',
  toastExportSuccess: 'Successfully exported applicants to Excel!',
  toastExportFail: 'An error occurred while exporting to Excel.',
  toastNoDataExcel: 'No applicant data to export.',
  breadcrumbDashboard: 'Dashboard',
  breadcrumbActiveJobs: 'Active Jobs',
  breadcrumbApplicants: 'Applicants',
  pageTitle: 'Applicants for ',
  totalCandidates: 'Total: {total} candidates',
  searchPlaceholder: 'Search by name...',
  exportExcel: 'Export Excel',
  exportExcelTitle: 'Export applicant list to Excel',
  filter: 'Filter',
  backToJobs: 'Back to Jobs',
  colName: 'Candidate Name',
  colContact: 'Contact Details',
  colDate: 'Apply Date',
  colStage: 'Current Stage',
  colAction: 'Action',
  loading: 'Loading applicants...',
  noApplicants: 'No applicants found',
  noApplicantsDesc: 'Try adjusting your search or filters.',
  unknownCandidate: 'Unknown Candidate',
  downloadCv: 'Download CV',
  viewProfile: 'View Profile',
  showingText: 'Showing <span>{start}</span> to <span>{end}</span> of <span>{total}</span> applicants',
  modalTitle: 'Application Details',
  loadingDetails: 'Loading details...',
  coverLetter: 'Cover Letter',
  noCoverLetter: 'No cover letter provided.',
  resume: 'Resume / CV',
  livePreview: 'Live Preview',
  noCv: 'No CV attached.',
  failedToLoadDetails: 'Failed to load details.',
};

const recruiterVi = {
  // Common
  addBtn: 'Thêm',
  editBtn: 'Sửa',
  deleteBtn: 'Xóa',
  cancelBtn: 'Hủy',
  saveBtn: 'Lưu',
  searchPlaceholder: 'Tìm kiếm...',
  
  // Layout & Nav
  dashboard: 'Bảng điều khiển',
  applicants: 'Ứng viên',
  jobs: 'Công việc',
  billing: 'Lịch sử giao dịch',
  settings: 'Cài đặt',
  profile: 'Hồ sơ',
};

const recruiterBillingHistoryVi = {
  pageTitle: 'Lịch sử giao dịch',
  pageDesc: 'Xem và quản lý tất cả các giao dịch đăng ký và nạp tiền của bạn.',
};

const recruiterBillingHistoryTableVi = {
  badgeSuccess: 'Thành công',
  badgePending: 'Đang chờ',
  badgeCancelled: 'Đã hủy',
  badgeFailed: 'Thất bại',
  planName: '{name}',
  planSub: 'Gói đăng ký',
  planDesc: 'Đăng ký Recruiter',
  typeCoin: 'Nạp tiền ví',
  topupDesc: 'Thêm xu ITH vào số dư của bạn',
  searchPlaceholder: 'Tìm theo Mã, Loại, Cổng thanh toán...',
  clearSearch: 'Xóa tìm kiếm',
  statusPlaceholder: 'Lọc trạng thái',
  statusAll: 'Tất cả trạng thái',
  statusSuccess: 'Thành công',
  statusPending: 'Đang chờ',
  statusFailed: 'Thất bại / Hủy',
  typePlaceholder: 'Loại giao dịch',
  typeAll: 'Tất cả loại',
  typeSubscription: 'Đăng ký gói',
  clearFilters: 'Xóa bộ lọc',
  clearAllFilters: 'Xóa tất cả bộ lọc',
  colOrderCode: 'MÃ ĐƠN HÀNG',
  colDate: 'NGÀY GIAO DỊCH',
  colDesc: 'MÔ TẢ',
  colGateway: 'CỔNG THANH TOÁN',
  colAmount: 'SỐ TIỀN',
  colStatus: 'TRẠNG THÁI',
  errLoadFailed: 'Tải lịch sử giao dịch thất bại',
  errLoadDesc: 'Vui lòng kiểm tra kết nối và thử lại.',
  noRecords: 'Không tìm thấy giao dịch nào',
  noRecordsFilter: 'Thử xóa một số bộ lọc.',
  noRecordsEmpty: 'Bạn chưa có giao dịch nào.',
  showingText: 'Đang xem <span>{start} - {end}</span> trong số <span>{total}</span> giao dịch',
  pageSizePlaceholder: 'Số dòng / trang',
  perPage: '{size} / trang',
  na: 'N/A',
};

const recruiterCandidateProfileVi = {
  loading: 'Đang tải hồ sơ ứng viên...',
  notFoundTitle: 'Không tìm thấy hồ sơ',
  notFoundDesc: 'Không thể tìm thấy hồ sơ ứng viên hoặc bạn không có quyền xem.',
  pageTitle: 'Hồ sơ Ứng viên',
  noLocation: 'Chưa cập nhật vị trí',
  noEmail: 'Chưa cập nhật email',
  noPhone: 'Chưa cập nhật số điện thoại',
  socialLinks: 'Liên kết Mạng xã hội',
  skills: 'Kỹ năng',
  noSkills: 'Chưa thêm kỹ năng nào.',
  aboutMe: 'Giới thiệu Bản thân',
  workExp: 'Kinh nghiệm Làm việc',
  present: 'Hiện tại',
  noExp: 'Chưa thêm kinh nghiệm làm việc.',
  education: 'Học vấn',
  noEdu: 'Chưa thêm lịch sử học vấn.',
  certifications: 'Chứng chỉ',
  issued: 'Cấp ngày: {date}',
  viewCredential: 'Xem Chứng chỉ',
};

const recruiterApplicantsVi = {
  toastStatusUpdateSuccess: 'Cập nhật trạng thái thành công',
  toastStatusUpdateFail: 'Cập nhật trạng thái thất bại',
  toastLoadDetailFail: 'Tải thông tin ứng tuyển thất bại.',
  toastExportSuccess: 'Đã xuất danh sách ứng viên ra file Excel thành công!',
  toastExportFail: 'Có lỗi xảy ra khi xuất Excel.',
  toastNoDataExcel: 'Không có dữ liệu ứng viên để xuất Excel.',
  breadcrumbDashboard: 'Bảng điều khiển',
  breadcrumbActiveJobs: 'Việc làm đang mở',
  breadcrumbApplicants: 'Ứng viên',
  pageTitle: 'Ứng viên cho ',
  totalCandidates: 'Tổng số: {total} ứng viên',
  searchPlaceholder: 'Tìm kiếm theo tên...',
  exportExcel: 'Xuất Excel',
  exportExcelTitle: 'Xuất danh sách ứng viên ra Excel để nộp Cấp trên',
  filter: 'Bộ lọc',
  backToJobs: 'Quay lại Việc làm',
  colName: 'Tên Ứng viên',
  colContact: 'Thông tin Liên lạc',
  colDate: 'Ngày Ứng tuyển',
  colStage: 'Trạng thái Hiện tại',
  colAction: 'Hành động',
  loading: 'Đang tải danh sách ứng viên...',
  noApplicants: 'Không tìm thấy ứng viên nào',
  noApplicantsDesc: 'Thử điều chỉnh tìm kiếm hoặc bộ lọc của bạn.',
  unknownCandidate: 'Ứng viên Ẩn danh',
  downloadCv: 'Tải CV',
  viewProfile: 'Xem Hồ sơ',
  showingText: 'Đang xem <span>{start}</span> đến <span>{end}</span> trong số <span>{total}</span> ứng viên',
  modalTitle: 'Chi tiết Ứng tuyển',
  loadingDetails: 'Đang tải chi tiết...',
  coverLetter: 'Thư Ứng tuyển (Cover Letter)',
  noCoverLetter: 'Không có thư ứng tuyển.',
  resume: 'Sơ yếu Lý lịch / CV',
  livePreview: 'Xem Trực tiếp',
  noCv: 'Không có CV đính kèm.',
  failedToLoadDetails: 'Tải chi tiết thất bại.',
};

en['Recruiter'] = recruiterEn;
en['RecruiterBillingHistory'] = recruiterBillingHistoryEn;
en['RecruiterBillingHistoryTable'] = recruiterBillingHistoryTableEn;
en['RecruiterCandidateProfile'] = recruiterCandidateProfileEn;
en['RecruiterApplicants'] = recruiterApplicantsEn;
vi['Recruiter'] = recruiterVi;
vi['RecruiterBillingHistory'] = recruiterBillingHistoryVi;
vi['RecruiterBillingHistoryTable'] = recruiterBillingHistoryTableVi;
vi['RecruiterCandidateProfile'] = recruiterCandidateProfileVi;
vi['RecruiterApplicants'] = recruiterApplicantsVi;

fs.writeFileSync('messages/en.json', JSON.stringify(en, null, 2));
fs.writeFileSync('messages/vi.json', JSON.stringify(vi, null, 2));
console.log('Recruiter translations updated successfully!');
