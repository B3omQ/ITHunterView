const fs = require('fs');
const en = JSON.parse(fs.readFileSync('messages/en.json', 'utf8'));
const vi = JSON.parse(fs.readFileSync('messages/vi.json', 'utf8'));

const auditLogsEn = {
  // Staff page header
  staffTitle: 'Audit & Surveillance Logs',
  staffDesc: 'Track system activities, user authentication, data mutations, and security events across the platform.',
  // Admin page header
  adminTitle: 'Platform Safety & Audit Logs',
  adminDesc: 'Record and monitor data mutation (CUD) behaviors and core security events across the platform.',

  // Toolbar
  searchPlaceholder: 'Search by actor email, action, IP...',
  adminSearchPlaceholder: 'Search email, action, table, IP...',
  clearSearch: 'Clear search',
  clearFilters: 'Clear Filters',
  clearAllFilters: 'Clear All Filters',
  operationPlaceholder: 'Operation',
  categoryPlaceholder: 'Category',
  statusPlaceholder: 'Status',
  allOperations: 'All Operations',
  allCategories: 'All Categories',
  allStatuses: 'All Statuses',
  catAuthentication: 'Authentication',
  catDataMutation: 'Data Mutation',
  catSecurity: 'Security',
  catSystem: 'System',
  statusSuccess: 'Success',
  statusFail: 'Fail',
  fromLabel: 'From:',
  toLabel: 'To:',
  purgeLogsBtn: 'Purge logs',

  // Date error
  dateErrorStart: 'Start date cannot be after end date.',
  dateErrorRange: 'Time range too large. Please limit search range within 30 days to ensure performance.',

  // Table headers
  colTimestamp: 'TIMESTAMP',
  colActor: 'ACTOR',
  colActionCategory: 'ACTION & CATEGORY',
  colOperation: 'OPERATION',
  colTarget: 'TARGET (TABLE)',
  colStatus: 'STATUS',
  colIpAddress: 'IP ADDRESS',
  colActions: 'ACTIONS',

  // Error / empty states
  loadFailTitle: 'Failed to load audit logs',
  loadFailDesc: 'An error occurred while fetching system surveillance data. Please try again.',
  adminLoadFailDesc: 'An error occurred while fetching audit log records. Please try again.',
  retryBtn: 'Retry Loading',
  noLogsTitle: 'No audit logs found',
  noLogsFilterDesc: 'No audit log entries match the current filters. Try clearing or adjusting your search criteria.',
  noLogsEmptyDesc: 'No system audit logs recorded yet.',
  adminNoLogsFilterDesc: 'No audit logs match the current filters. Try clearing or adjusting your search criteria.',
  adminNoLogsEmptyDesc: 'No audit log activities recorded yet.',

  // Row actions
  viewDetailsTitle: 'View Log Details',
  viewLogTitle: 'View log details',

  // Pagination
  showingText: 'Showing <span class="font-semibold text-[#050505] dark:text-zinc-200">{start} - {end}</span> of <span class="font-semibold text-[#050505] dark:text-zinc-200">{total}</span> audit logs',
  pageSize: 'Page size',

  // Purge modal
  purgeModal: {
    title: 'Purge Logs Confirmation',
    warning: 'IMPORTANT WARNING:',
    warningMsg: 'This action will permanently delete all audit log records older than the specified number of days. Once deleted, this log data cannot be recovered.',
    keepLogsLabel: 'Keep logs within (days):',
    willDeleteMsg: 'Logs created before {date} will be permanently deleted.',
    cancelBtn: 'Cancel',
    purgeBtn: 'Purge',
    minDayError: 'Minimum retention period is 1 day.',
  },

  // Log details modal
  detailsModal: {
    title: 'Audit Log Record Details',
    actorLabel: 'Actor',
    networkLabel: 'Network Environment',
    recordedTimeLabel: 'Recorded Time',
    ipLabel: 'IP:',
    uaLabel: 'UA:',
    actionLabel: 'Action',
    tableLabel: 'Table:',
    payloadDiffLabel: 'Payload Diff',
    closeBtn: 'Close',
  },

  // Snapshot diff
  snapshotDiff: {
    noChanges: 'No structural/data changes recorded or no payload diff.',
    jsonError: 'Invalid JSON payload.',
    noFieldsModified: 'No fields had modified values.',
    colField: 'Field',
    colOldValue: 'Old Value',
    colNewValue: 'New Value',
    colRecordedValue: 'Recorded Value',
    valNull: 'null',
  }
};

const auditLogsVi = {
  // Staff page header
  staffTitle: 'Nhật Ký Kiểm Tra & Giám Sát',
  staffDesc: 'Theo dõi các hoạt động hệ thống, xác thực người dùng, thay đổi dữ liệu và sự kiện bảo mật trên toàn nền tảng.',
  // Admin page header
  adminTitle: 'An Toàn Nền Tảng & Nhật Ký Kiểm Tra',
  adminDesc: 'Ghi lại và giám sát các hành vi thay đổi dữ liệu (CUD) và các sự kiện bảo mật cốt lõi trên toàn nền tảng.',

  // Toolbar
  searchPlaceholder: 'Tìm kiếm theo email, hành động, IP...',
  adminSearchPlaceholder: 'Tìm kiếm email, hành động, bảng, IP...',
  clearSearch: 'Xóa tìm kiếm',
  clearFilters: 'Xóa Bộ Lọc',
  clearAllFilters: 'Xóa Tất Cả Bộ Lọc',
  operationPlaceholder: 'Loại Thao Tác',
  categoryPlaceholder: 'Danh Mục',
  statusPlaceholder: 'Trạng Thái',
  allOperations: 'Tất Cả Thao Tác',
  allCategories: 'Tất Cả Danh Mục',
  allStatuses: 'Tất Cả Trạng Thái',
  catAuthentication: 'Xác Thực',
  catDataMutation: 'Thay Đổi Dữ Liệu',
  catSecurity: 'Bảo Mật',
  catSystem: 'Hệ Thống',
  statusSuccess: 'Thành Công',
  statusFail: 'Thất Bại',
  fromLabel: 'Từ:',
  toLabel: 'Đến:',
  purgeLogsBtn: 'Xóa nhật ký',

  // Date error
  dateErrorStart: 'Ngày bắt đầu không thể sau ngày kết thúc.',
  dateErrorRange: 'Khoảng thời gian quá lớn. Vui lòng giới hạn phạm vi tìm kiếm trong vòng 30 ngày để đảm bảo hiệu suất.',

  // Table headers
  colTimestamp: 'THỜI GIAN',
  colActor: 'NGƯỜI THỰC HIỆN',
  colActionCategory: 'HÀNH ĐỘNG & DANH MỤC',
  colOperation: 'LOẠI THAO TÁC',
  colTarget: 'MỤC TIÊU (BẢNG)',
  colStatus: 'TRẠNG THÁI',
  colIpAddress: 'ĐỊA CHỈ IP',
  colActions: 'THAO TÁC',

  // Error / empty states
  loadFailTitle: 'Tải nhật ký kiểm tra thất bại',
  loadFailDesc: 'Đã xảy ra lỗi khi lấy dữ liệu giám sát hệ thống. Vui lòng thử lại.',
  adminLoadFailDesc: 'Đã xảy ra lỗi khi lấy hồ sơ nhật ký kiểm tra. Vui lòng thử lại.',
  retryBtn: 'Thử Lại',
  noLogsTitle: 'Không tìm thấy nhật ký kiểm tra',
  noLogsFilterDesc: 'Không có mục nhật ký nào khớp với bộ lọc hiện tại. Hãy thử xóa hoặc điều chỉnh tiêu chí tìm kiếm.',
  noLogsEmptyDesc: 'Chưa có nhật ký kiểm tra hệ thống nào được ghi lại.',
  adminNoLogsFilterDesc: 'Không có nhật ký kiểm tra nào khớp với bộ lọc hiện tại. Hãy thử xóa hoặc điều chỉnh tiêu chí tìm kiếm.',
  adminNoLogsEmptyDesc: 'Chưa có hoạt động nhật ký kiểm tra nào được ghi lại.',

  // Row actions
  viewDetailsTitle: 'Xem Chi Tiết Nhật Ký',
  viewLogTitle: 'Xem chi tiết nhật ký',

  // Pagination
  showingText: 'Hiển thị <span class="font-semibold text-[#050505] dark:text-zinc-200">{start} - {end}</span> trên tổng số <span class="font-semibold text-[#050505] dark:text-zinc-200">{total}</span> nhật ký',
  pageSize: 'Số dòng',

  // Purge modal
  purgeModal: {
    title: 'Xác Nhận Xóa Nhật Ký',
    warning: 'CẢNH BÁO QUAN TRỌNG:',
    warningMsg: 'Hành động này sẽ xóa vĩnh viễn tất cả hồ sơ nhật ký kiểm tra cũ hơn số ngày được chỉ định. Sau khi xóa, dữ liệu nhật ký này không thể khôi phục.',
    keepLogsLabel: 'Giữ nhật ký trong (ngày):',
    willDeleteMsg: 'Nhật ký được tạo trước {date} sẽ bị xóa vĩnh viễn.',
    cancelBtn: 'Hủy',
    purgeBtn: 'Xóa',
    minDayError: 'Thời gian lưu giữ tối thiểu là 1 ngày.',
  },

  // Log details modal
  detailsModal: {
    title: 'Chi Tiết Hồ Sơ Nhật Ký Kiểm Tra',
    actorLabel: 'Người Thực Hiện',
    networkLabel: 'Môi Trường Mạng',
    recordedTimeLabel: 'Thời Gian Ghi',
    ipLabel: 'IP:',
    uaLabel: 'UA:',
    actionLabel: 'Hành Động',
    tableLabel: 'Bảng:',
    payloadDiffLabel: 'Chênh Lệch Dữ Liệu',
    closeBtn: 'Đóng',
  },

  // Snapshot diff
  snapshotDiff: {
    noChanges: 'Không có thay đổi cấu trúc/dữ liệu nào được ghi nhận hoặc không có chênh lệch dữ liệu.',
    jsonError: 'Dữ liệu JSON không hợp lệ.',
    noFieldsModified: 'Không có trường nào bị sửa đổi giá trị.',
    colField: 'Trường Dữ Liệu',
    colOldValue: 'Giá Trị Cũ',
    colNewValue: 'Giá Trị Mới',
    colRecordedValue: 'Giá Trị Được Ghi Nhận',
    valNull: 'null',
  }
};

en['AdminAuditLogs'] = auditLogsEn;
en['StaffAuditLogs'] = auditLogsEn;
vi['AdminAuditLogs'] = auditLogsVi;
vi['StaffAuditLogs'] = auditLogsVi;

fs.writeFileSync('messages/en.json', JSON.stringify(en, null, 2));
fs.writeFileSync('messages/vi.json', JSON.stringify(vi, null, 2));
console.log('Audit log messages updated successfully!');
