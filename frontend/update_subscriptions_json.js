const fs = require('fs');
const en = JSON.parse(fs.readFileSync('messages/en.json', 'utf8'));
const vi = JSON.parse(fs.readFileSync('messages/vi.json', 'utf8'));

const subscriptionsEn = {
  // Page headers
  pageTitle: 'Subscription & Coin Configuration',
  pageDesc: 'Manage subscription packages, AI limits, and coin wallet configurations.',

  // Tabs
  tabSubscriptions: 'Subscription Packages',
  tabCoinConfig: 'Coin Configuration',
  tabCustomCoinPrice: 'Custom Coin Price',

  // Actions
  addNewPackage: 'Add New Package',
  editPackageTitle: 'Edit Service Package',
  createPackageTitle: 'Create New Service Package',

  // Filters
  targetRoleFilter: 'Target Role',
  allTargetRoles: 'All Target Roles',
  roleCandidate: 'Candidate',
  roleRecruiter: 'Recruiter',
  statusFilter: 'Status Filter',
  allStatuses: 'All Statuses',
  statusActive: 'Active',
  statusInactive: 'Inactive',
  clearFilters: 'Clear Filters',
  clearAllFilters: 'Clear All Filters',

  // Table Columns
  colPackageName: 'PACKAGE NAME',
  colTargetRole: 'TARGET ROLE',
  colPrice: 'PRICE',
  colDuration: 'DURATION',
  colStatus: 'STATUS',
  colTransactions: 'TRANSACTIONS',
  colActions: 'ACTIONS',

  // Badges & States
  badgeSold: 'Sold',
  badgeNotSold: 'Not Sold',
  editPackage: 'Edit Package',
  duplicatePackage: 'Duplicate Package',
  loadFailTitle: 'Failed to load subscription packages',
  loadFailDesc: 'An error occurred while fetching package records. Please try again.',
  retryBtn: 'Retry Loading',
  noDataTitle: 'No service packages found',
  noDataFilterDesc: 'No subscription packages match the current filters. Try clearing or adjusting your filter.',
  noDataEmptyDesc: 'No service packages configured yet.',
  durationDays: 'days',
  
  // Pagination
  showingText: 'Showing <span class="font-semibold text-[#050505] dark:text-zinc-200">{start} - {end}</span> of <span class="font-semibold text-[#050505] dark:text-zinc-200">{total}</span> service packages',
  pageSize: 'Page size',

  // Toasts
  toastCreateSuccess: 'Subscription package created successfully in INACTIVE status',
  toastUpdateSuccess: 'Subscription package updated successfully',
  toastDuplicateSuccess: 'Package duplicated successfully (default copy in INACTIVE status)',
  toastStatusChangeSuccess: 'Package status changed to {status}',

  // Custom Coin Tab
  customCoinTitle: 'Custom Coin Top-up Price',
  customCoinDesc: 'This is the price Candidate pays for one individually purchased Coin. It does not affect Coin packages or feature costs.',
  customCoinLoading: 'Loading custom Coin price...',
  customCoinLabel: 'Price per Coin (VND)',
  customCoinSaveBtn: 'Save custom Coin price',
  customCoinSavingBtn: 'Saving...',
  customCoinToastValid: 'Enter a positive whole-number price in VND.',
  customCoinToastSuccess: 'Custom Coin price updated successfully',

  // Coin Config Tab
  coinConfigLoading: 'Loading coin configuration...',
  aiCostsTitle: 'AI Costs (Coins)',
  aiCostsDesc: 'Configure the number of coins consumed per AI service usage.',
  cvJdMatching: 'CV-JD Matching',
  mockInterview: 'Mock Interview',
  learningPath: 'Learning Path',
  unlockCv: 'Unlock CV',
  postJob: 'Post Job',
  extendJob: 'Extend Job',
  pushTop: 'Push Top',
  coinLabel: 'Coin',
  coinRateInfo: 'The default top-up rate is set at <strong>1 Coin = 2,000 VND</strong> when configuring wallet top-up packages.',
  coinPackagesTitle: 'Coin Top-up Packages',
  coinPackagesDesc: 'Manage the list of coin top-up amounts displayed to candidates.',
  addPackageBtn: 'Add Package',
  colPkgName: 'Package Name',
  colCoinsAmount: 'Coins Amount',
  colPriceVnd: 'Price (VND)',
  saveAllConfigBtn: 'Save All Configuration',
  savingChangesBtn: 'Saving changes...',
  btnDelete: 'Delete',
  placeholderPkgName: 'Example: Top-up 20 Coins',

  // Form
  formPackageName: 'Service Package Name',
  formPlaceholderPackageName: 'Example: Premium Candidate Monthly',
  formPrice: 'Price (VND)',
  formDuration: 'Duration (days)',
  formTargetAudience: 'Target Audience',
  formSelectRole: 'Select role',
  formFeatureLimitTitle: 'Feature Limit Configuration',
  formCvMatchLimit: 'CV-JD Match Limit per Month',
  formMockInterviewLimit: 'Mock Interview Limit per Month',
  formLearningPathLimit: 'Learning Path Generation Limit per Period',
  formLearningPathSlotLimit: 'Learning Path Slot Limit (-1 for unlimited)',
  formJobSlots: 'Job Slots',
  formJobExtendLimit: 'Job Extend Limit (per month)',
  formUnlockCvLimit: 'Unlock CV Limit',
  formPushTopLimit: 'Push Top Limit',
  formCoinCredit: 'Coin Credit Bonus',
  formUsedWarning: '* This package has active transactions. Only the package name can be edited. To change prices or limits, please duplicate this package to create a new one.',
  btnProcessing: 'Processing...',
  btnSaveChanges: 'Save Changes',
  btnCreatePackage: 'Create Package'
};

const subscriptionsVi = {
  // Page headers
  pageTitle: 'Cấu Hình Gói Cước & Xu (Coin)',
  pageDesc: 'Quản lý các gói cước, giới hạn AI, và cấu hình ví xu.',

  // Tabs
  tabSubscriptions: 'Gói Cước Dịch Vụ',
  tabCoinConfig: 'Cấu Hình Xu',
  tabCustomCoinPrice: 'Giá Nạp Xu Tùy Chọn',

  // Actions
  addNewPackage: 'Thêm Gói Mới',
  editPackageTitle: 'Chỉnh Sửa Gói Dịch Vụ',
  createPackageTitle: 'Tạo Gói Dịch Vụ Mới',

  // Filters
  targetRoleFilter: 'Đối Tượng',
  allTargetRoles: 'Tất Cả Đối Tượng',
  roleCandidate: 'Ứng Viên',
  roleRecruiter: 'Nhà Tuyển Dụng',
  statusFilter: 'Lọc Trạng Thái',
  allStatuses: 'Tất Cả Trạng Thái',
  statusActive: 'Đang Hoạt Động',
  statusInactive: 'Ngừng Hoạt Động',
  clearFilters: 'Xóa Bộ Lọc',
  clearAllFilters: 'Xóa Tất Cả Bộ Lọc',

  // Table Columns
  colPackageName: 'TÊN GÓI',
  colTargetRole: 'ĐỐI TƯỢNG',
  colPrice: 'GIÁ TIỀN',
  colDuration: 'THỜI HẠN',
  colStatus: 'TRẠNG THÁI',
  colTransactions: 'GIAO DỊCH',
  colActions: 'THAO TÁC',

  // Badges & States
  badgeSold: 'Đã Bán',
  badgeNotSold: 'Chưa Bán',
  editPackage: 'Chỉnh Sửa Gói',
  duplicatePackage: 'Nhân Bản Gói',
  loadFailTitle: 'Tải danh sách gói cước thất bại',
  loadFailDesc: 'Đã xảy ra lỗi khi lấy danh sách gói. Vui lòng thử lại.',
  retryBtn: 'Thử Lại',
  noDataTitle: 'Không tìm thấy gói dịch vụ',
  noDataFilterDesc: 'Không có gói cước nào khớp với bộ lọc hiện tại. Hãy thử xóa hoặc điều chỉnh bộ lọc.',
  noDataEmptyDesc: 'Chưa có gói dịch vụ nào được cấu hình.',
  durationDays: 'ngày',
  
  // Pagination
  showingText: 'Hiển thị <span class="font-semibold text-[#050505] dark:text-zinc-200">{start} - {end}</span> trên tổng số <span class="font-semibold text-[#050505] dark:text-zinc-200">{total}</span> gói dịch vụ',
  pageSize: 'Số lượng',

  // Toasts
  toastCreateSuccess: 'Đã tạo thành công gói dịch vụ ở trạng thái NGỪNG HOẠT ĐỘNG',
  toastUpdateSuccess: 'Đã cập nhật thành công gói dịch vụ',
  toastDuplicateSuccess: 'Đã nhân bản gói thành công (bản sao mặc định ở trạng thái NGỪNG HOẠT ĐỘNG)',
  toastStatusChangeSuccess: 'Đã đổi trạng thái gói thành {status}',

  // Custom Coin Tab
  customCoinTitle: 'Giá Nạp Xu Tùy Chọn',
  customCoinDesc: 'Đây là giá mà Ứng Viên trả cho một Xu (Coin) khi mua lẻ. Nó không ảnh hưởng đến các gói Xu hoặc chi phí tính năng.',
  customCoinLoading: 'Đang tải giá Xu tùy chọn...',
  customCoinLabel: 'Giá mỗi Xu (VND)',
  customCoinSaveBtn: 'Lưu giá Xu tùy chọn',
  customCoinSavingBtn: 'Đang lưu...',
  customCoinToastValid: 'Vui lòng nhập giá nguyên dương tính bằng VND.',
  customCoinToastSuccess: 'Cập nhật giá Xu tùy chọn thành công',

  // Coin Config Tab
  coinConfigLoading: 'Đang tải cấu hình Xu...',
  aiCostsTitle: 'Chi phí AI (Xu)',
  aiCostsDesc: 'Cấu hình số lượng xu bị trừ khi sử dụng dịch vụ AI.',
  cvJdMatching: 'Phân tích CV-JD',
  mockInterview: 'Phỏng Vấn Thử (Mock)',
  learningPath: 'Lộ Trình Học Tập',
  unlockCv: 'Mở Khóa CV',
  postJob: 'Đăng Tin Tuyển Dụng',
  extendJob: 'Gia Hạn Tin Tuyển Dụng',
  pushTop: 'Đẩy Tin Lên Top',
  coinLabel: 'Xu',
  coinRateInfo: 'Tỷ giá nạp mặc định được thiết lập là <strong>1 Xu = 2,000 VND</strong> khi cấu hình các gói nạp ví.',
  coinPackagesTitle: 'Các Gói Nạp Xu',
  coinPackagesDesc: 'Quản lý danh sách các mức nạp Xu hiển thị cho ứng viên.',
  addPackageBtn: 'Thêm Gói',
  colPkgName: 'Tên Gói',
  colCoinsAmount: 'Số lượng Xu',
  colPriceVnd: 'Giá (VND)',
  saveAllConfigBtn: 'Lưu Tất Cả Cấu Hình',
  savingChangesBtn: 'Đang lưu thay đổi...',
  btnDelete: 'Xóa',
  placeholderPkgName: 'Ví dụ: Nạp 20 Xu',

  // Form
  formPackageName: 'Tên Gói Dịch Vụ',
  formPlaceholderPackageName: 'Ví dụ: Gói Ứng Viên Premium Tháng',
  formPrice: 'Giá Tiền (VND)',
  formDuration: 'Thời Hạn (ngày)',
  formTargetAudience: 'Đối Tượng Hướng Đến',
  formSelectRole: 'Chọn đối tượng',
  formFeatureLimitTitle: 'Cấu Hình Giới Hạn Tính Năng',
  formCvMatchLimit: 'Giới Hạn Phân Tích CV-JD Mỗi Tháng',
  formMockInterviewLimit: 'Giới Hạn Phỏng Vấn Thử Mỗi Tháng',
  formLearningPathLimit: 'Giới Hạn Tạo Lộ Trình Học Mỗi Kỳ',
  formLearningPathSlotLimit: 'Giới Hạn Số Slot Lộ Trình Học (-1 là vô hạn)',
  formJobSlots: 'Số Tin Tuyển Dụng',
  formJobExtendLimit: 'Giới Hạn Gia Hạn Tin (mỗi tháng)',
  formUnlockCvLimit: 'Giới Hạn Mở Khóa CV',
  formPushTopLimit: 'Giới Hạn Đẩy Tin Top',
  formCoinCredit: 'Thưởng Xu (Coin)',
  formUsedWarning: '* Gói này đã có giao dịch. Bạn chỉ có thể sửa tên gói. Để đổi giá hoặc giới hạn, vui lòng nhân bản gói này thành một gói mới.',
  btnProcessing: 'Đang xử lý...',
  btnSaveChanges: 'Lưu Thay Đổi',
  btnCreatePackage: 'Tạo Gói Mới'
};

en['AdminSubscriptions'] = subscriptionsEn;
vi['AdminSubscriptions'] = subscriptionsVi;

fs.writeFileSync('messages/en.json', JSON.stringify(en, null, 2));
fs.writeFileSync('messages/vi.json', JSON.stringify(vi, null, 2));
console.log('Subscriptions translations updated successfully!');
