const fs = require('fs');
const en = JSON.parse(fs.readFileSync('messages/en.json', 'utf8'));
const vi = JSON.parse(fs.readFileSync('messages/vi.json', 'utf8'));

const accountsEn = {
  // Accounts page
  pageTitle: 'User Governance',
  pageDesc: 'Manage user accounts, review access status, and suspend policy-violating users across the platform.',
  createStaffBtn: 'Create Staff Account',
  
  // Filters
  searchPlaceholder: 'Search by email, name, company...',
  clearSearch: 'Clear search',
  roleFilterLabel: 'Role Filter',
  statusFilterLabel: 'Status Filter',
  allRoles: 'All Roles',
  allStatuses: 'All Statuses',
  roleAdmin: 'Admin',
  roleStaff: 'Staff',
  roleRecruiter: 'Recruiter',
  roleCandidate: 'Candidate',
  statusActive: 'Active',
  statusInactive: 'Inactive',
  statusBanned: 'Banned',
  statusPending: 'Pending Verification',
  clearFilters: 'Clear Filters',
  clearAllFilters: 'Clear All Filters',

  // Table
  colName: 'FULL NAME',
  colEmail: 'EMAIL',
  colRole: 'ROLE',
  colStatus: 'STATUS',
  colCreatedDate: 'CREATED DATE',
  colActions: 'ACTIONS',

  // States
  loadFailTitle: 'Failed to load user accounts',
  loadFailDesc: 'An error occurred while fetching user accounts data. Please try again.',
  noAccountsTitle: 'No user accounts found',
  noAccountsFilterDesc: 'No user accounts match the current filters. Try clearing or adjusting your search criteria.',
  noAccountsEmptyDesc: 'No user accounts recorded yet.',

  // Pagination
  showingText: 'Showing <span class="font-semibold text-[#050505] dark:text-zinc-200">{start} - {end}</span> of <span class="font-semibold text-[#050505] dark:text-zinc-200">{total}</span> user accounts',
  pageSize: 'Page size',

  // Row badges / actions
  notUpdated: 'Not updated',
  viewAccountTitle: 'View Account Details',
  changeStatusTitle: 'Change User Status',

  // Create Staff Modal
  createStaffModal: {
    title: 'Create Staff Account',
    emailLabel: 'Email Address',
    emailPlaceholder: 'Enter staff email address...',
    passwordLabel: 'Initial Password',
    passwordPlaceholder: 'Enter initial password (min 6 chars)...',
    hideBtn: 'Hide',
    showBtn: 'Show',
    cancelBtn: 'Cancel',
    createBtn: 'Create',
    emailRequired: 'Please enter an email address.',
    passwordRequired: 'Please enter an initial password.',
    passwordLength: 'Password must be at least 6 characters.',
    successMsg: 'Staff account created successfully!',
    failMsg: 'Failed to create staff account.',
    defaultErrorMsg: 'An error occurred while creating staff account.',
  },

  // Update Status Modal
  updateStatusModal: {
    title: 'Update Account Status',
    accountLabel: 'Account',
    newStatusLabel: 'New Status',
    activeOpt: 'Active (ACTIVE)',
    inactiveOpt: 'Inactive (INACTIVE)',
    bannedOpt: 'Banned (BANNED)',
    pendingOpt: 'Pending Verification (PENDING_VERIFICATION)',
    reasonLabel: 'Reason for Change',
    reasonPlaceholder: 'Enter detailed reason for the audit log (minimum 5 characters)...',
    reasonNote: 'This reason will be recorded in the audit history and cannot be edited.',
    cancelBtn: 'Cancel',
    confirmBtn: 'Confirm',
    reasonRequired: 'Please enter the reason for updating the status.',
    reasonLength: 'Reason must be at least 5 characters.',
    successMsg: 'User status updated successfully!',
    failMsg: 'Failed to update status.',
    defaultErrorMsg: 'An error occurred while updating.',
  },

  // User details page
  detailPage: {
    backToList: 'Back to list',
    error404: 'Account does not exist or an error occurred (404).',
    loading: 'Loading user profile details...',
    notFoundDetails: 'Detailed profile info for this user account could not be found.',

    systemAccountLabel: 'System Account',
    emailLabel: 'Email',
    roleLabel: 'Role',
    statusLabel: 'Status',
    joinedDateLabel: 'Joined Date',
    deactivatedDateLabel: 'Deactivated Date',
    changeStatusBtn: 'Change Status',

    candidateProfileLabel: 'Candidate Profile',
    aboutMeLabel: 'About Me',
    noIntro: 'No introduction provided.',
    phoneLabel: 'Phone',
    locationLabel: 'Location',
    socialLinksLabel: 'Social Links',
    noSocialLinks: 'No social links.',

    recruiterProfileLabel: 'Professional Recruiter',
    contactPhoneLabel: 'Contact Phone',
    affiliatedCompanyLabel: 'Affiliated Company',
    headquartersLabel: 'Headquarters',
    websiteLabel: 'Website',
    noCompanyMsg: 'This recruiter is not yet affiliated with any company.',

    adminProfileLabel: 'System Admin Profile',
    staffProfileLabel: 'Staff Profile',
    descriptionLabel: 'Description',
    staffDesc: 'This is an internal system Staff account. Staff members can monitor candidate and recruiter activities, inspect applications, review job postings, and handle standard support operations.',
    adminDesc: 'This is the master Administrator account. The administrator has full permissions over system configurations, user governance, audit log tracking, and security parameters.',
  }
};

const accountsVi = {
  // Accounts page
  pageTitle: 'Quản Lý Người Dùng',
  pageDesc: 'Quản lý tài khoản người dùng, xem xét trạng thái truy cập và đình chỉ những người dùng vi phạm chính sách trên toàn nền tảng.',
  createStaffBtn: 'Tạo Tài Khoản Staff',
  
  // Filters
  searchPlaceholder: 'Tìm kiếm theo email, tên, công ty...',
  clearSearch: 'Xóa tìm kiếm',
  roleFilterLabel: 'Lọc Vai Trò',
  statusFilterLabel: 'Lọc Trạng Thái',
  allRoles: 'Tất Cả Vai Trò',
  allStatuses: 'Tất Cả Trạng Thái',
  roleAdmin: 'Quản Trị Viên (Admin)',
  roleStaff: 'Nhân Viên (Staff)',
  roleRecruiter: 'Nhà Tuyển Dụng',
  roleCandidate: 'Ứng Viên',
  statusActive: 'Hoạt Động',
  statusInactive: 'Không Hoạt Động',
  statusBanned: 'Bị Cấm',
  statusPending: 'Chờ Xác Thực',
  clearFilters: 'Xóa Bộ Lọc',
  clearAllFilters: 'Xóa Tất Cả Bộ Lọc',

  // Table
  colName: 'HỌ VÀ TÊN',
  colEmail: 'EMAIL',
  colRole: 'VAI TRÒ',
  colStatus: 'TRẠNG THÁI',
  colCreatedDate: 'NGÀY TẠO',
  colActions: 'THAO TÁC',

  // States
  loadFailTitle: 'Tải dữ liệu người dùng thất bại',
  loadFailDesc: 'Đã xảy ra lỗi khi lấy dữ liệu tài khoản người dùng. Vui lòng thử lại.',
  noAccountsTitle: 'Không tìm thấy tài khoản',
  noAccountsFilterDesc: 'Không có tài khoản nào khớp với bộ lọc. Hãy thử thay đổi tiêu chí tìm kiếm.',
  noAccountsEmptyDesc: 'Chưa có tài khoản nào được ghi nhận.',

  // Pagination
  showingText: 'Hiển thị <span class="font-semibold text-[#050505] dark:text-zinc-200">{start} - {end}</span> trên tổng số <span class="font-semibold text-[#050505] dark:text-zinc-200">{total}</span> tài khoản',
  pageSize: 'Số lượng',

  // Row badges / actions
  notUpdated: 'Chưa cập nhật',
  viewAccountTitle: 'Xem chi tiết tài khoản',
  changeStatusTitle: 'Đổi trạng thái tài khoản',

  // Create Staff Modal
  createStaffModal: {
    title: 'Tạo Tài Khoản Staff',
    emailLabel: 'Địa Chỉ Email',
    emailPlaceholder: 'Nhập email nhân viên...',
    passwordLabel: 'Mật Khẩu Ban Đầu',
    passwordPlaceholder: 'Nhập mật khẩu (tối thiểu 6 ký tự)...',
    hideBtn: 'Ẩn',
    showBtn: 'Hiện',
    cancelBtn: 'Hủy',
    createBtn: 'Tạo Mới',
    emailRequired: 'Vui lòng nhập địa chỉ email.',
    passwordRequired: 'Vui lòng nhập mật khẩu ban đầu.',
    passwordLength: 'Mật khẩu phải dài ít nhất 6 ký tự.',
    successMsg: 'Tạo tài khoản Staff thành công!',
    failMsg: 'Tạo tài khoản Staff thất bại.',
    defaultErrorMsg: 'Đã xảy ra lỗi khi tạo tài khoản Staff.',
  },

  // Update Status Modal
  updateStatusModal: {
    title: 'Cập Nhật Trạng Thái Tài Khoản',
    accountLabel: 'Tài Khoản',
    newStatusLabel: 'Trạng Thái Mới',
    activeOpt: 'Hoạt Động (ACTIVE)',
    inactiveOpt: 'Không Hoạt Động (INACTIVE)',
    bannedOpt: 'Bị Cấm (BANNED)',
    pendingOpt: 'Chờ Xác Thực (PENDING_VERIFICATION)',
    reasonLabel: 'Lý Do Thay Đổi',
    reasonPlaceholder: 'Nhập lý do chi tiết cho việc thay đổi (tối thiểu 5 ký tự)...',
    reasonNote: 'Lý do này sẽ được ghi vào lịch sử kiểm toán và không thể chỉnh sửa.',
    cancelBtn: 'Hủy',
    confirmBtn: 'Xác Nhận',
    reasonRequired: 'Vui lòng nhập lý do cập nhật trạng thái.',
    reasonLength: 'Lý do phải dài ít nhất 5 ký tự.',
    successMsg: 'Cập nhật trạng thái người dùng thành công!',
    failMsg: 'Cập nhật trạng thái thất bại.',
    defaultErrorMsg: 'Đã xảy ra lỗi khi cập nhật.',
  },

  // User details page
  detailPage: {
    backToList: 'Quay lại danh sách',
    error404: 'Tài khoản không tồn tại hoặc đã xảy ra lỗi (404).',
    loading: 'Đang tải chi tiết hồ sơ người dùng...',
    notFoundDetails: 'Không tìm thấy thông tin hồ sơ chi tiết cho tài khoản này.',

    systemAccountLabel: 'Tài Khoản Hệ Thống',
    emailLabel: 'Email',
    roleLabel: 'Vai Trò',
    statusLabel: 'Trạng Thái',
    joinedDateLabel: 'Ngày Tham Gia',
    deactivatedDateLabel: 'Ngày Vô Hiệu Hóa',
    changeStatusBtn: 'Thay Đổi Trạng Thái',

    candidateProfileLabel: 'Hồ Sơ Ứng Viên',
    aboutMeLabel: 'Giới Thiệu',
    noIntro: 'Chưa có phần giới thiệu.',
    phoneLabel: 'Điện Thoại',
    locationLabel: 'Vị Trí',
    socialLinksLabel: 'Mạng Xã Hội',
    noSocialLinks: 'Chưa có liên kết mạng xã hội.',

    recruiterProfileLabel: 'Nhà Tuyển Dụng Chuyên Nghiệp',
    contactPhoneLabel: 'Điện Thoại Liên Hệ',
    affiliatedCompanyLabel: 'Công Ty Trực Thuộc',
    headquartersLabel: 'Trụ Sở Chính',
    websiteLabel: 'Website',
    noCompanyMsg: 'Nhà tuyển dụng này chưa liên kết với công ty nào.',

    adminProfileLabel: 'Hồ Sơ Quản Trị Viên (Admin)',
    staffProfileLabel: 'Hồ Sơ Nhân Viên (Staff)',
    descriptionLabel: 'Mô Tả',
    staffDesc: 'Đây là tài khoản Nhân viên hệ thống nội bộ. Nhân viên có thể theo dõi hoạt động của ứng viên và nhà tuyển dụng, kiểm tra đơn đăng ký, xem xét tin tuyển dụng và xử lý các hoạt động hỗ trợ tiêu chuẩn.',
    adminDesc: 'Đây là tài khoản Quản trị viên cao nhất. Quản trị viên có toàn quyền cấu hình hệ thống, quản lý người dùng, theo dõi nhật ký kiểm toán và thiết lập các thông số bảo mật.',
  }
};

en['AdminAccounts'] = accountsEn;
vi['AdminAccounts'] = accountsVi;

fs.writeFileSync('messages/en.json', JSON.stringify(en, null, 2));
fs.writeFileSync('messages/vi.json', JSON.stringify(vi, null, 2));
console.log('Accounts translations updated successfully!');
