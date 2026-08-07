const fs = require('fs');
const en = JSON.parse(fs.readFileSync('messages/en.json', 'utf8'));
const vi = JSON.parse(fs.readFileSync('messages/vi.json', 'utf8'));

const financeEn = {
  // Page headers
  pageTitle: 'Financial Management & Transactions',
  pageDesc: 'Monitor, track, and audit payment transactions and financial records across the platform.',

  // Toolbar
  searchPlaceholder: 'Search by user name, email...',
  clearSearch: 'Clear search',
  statusFilterLabel: 'Status Filter',
  allStatuses: 'All Statuses',
  statusSuccess: 'Success',
  statusPending: 'Pending',
  statusFailed: 'Failed',
  clearFilters: 'Clear Filters',
  clearAllFilters: 'Clear All Filters',

  // Table
  colUserName: 'USER NAME',
  colEmail: 'EMAIL',
  colAmount: 'AMOUNT',
  colStatus: 'STATUS',
  colTime: 'TRANSACTION TIME',

  // States
  loadFailTitle: 'Failed to load transaction history',
  loadFailDesc: 'An error occurred while fetching payment records. Please try again.',
  retryBtn: 'Retry Loading',
  noDataTitle: 'No transactions found',
  noDataFilterDesc: 'No transactions match the current filters. Try clearing or adjusting your search criteria.',
  noDataEmptyDesc: 'No payment transactions recorded yet.',

  // Row badges
  unknownUser: 'Unknown',
  notAvailable: 'N/A',

  // Pagination
  showingText: 'Showing <span class="font-semibold text-[#050505] dark:text-zinc-200">{start} - {end}</span> of <span class="font-semibold text-[#050505] dark:text-zinc-200">{total}</span> transactions',
  pageSize: 'Page size'
};

const financeVi = {
  // Page headers
  pageTitle: 'Quản Lý Tài Chính & Giao Dịch',
  pageDesc: 'Theo dõi, giám sát và kiểm tra các giao dịch thanh toán và hồ sơ tài chính trên toàn bộ nền tảng.',

  // Toolbar
  searchPlaceholder: 'Tìm kiếm theo tên người dùng, email...',
  clearSearch: 'Xóa tìm kiếm',
  statusFilterLabel: 'Lọc Trạng Thái',
  allStatuses: 'Tất Cả Trạng Thái',
  statusSuccess: 'Thành Công',
  statusPending: 'Đang Xử Lý',
  statusFailed: 'Thất Bại',
  clearFilters: 'Xóa Bộ Lọc',
  clearAllFilters: 'Xóa Tất Cả Bộ Lọc',

  // Table
  colUserName: 'TÊN NGƯỜI DÙNG',
  colEmail: 'EMAIL',
  colAmount: 'SỐ TIỀN',
  colStatus: 'TRẠNG THÁI',
  colTime: 'THỜI GIAN GIAO DỊCH',

  // States
  loadFailTitle: 'Tải lịch sử giao dịch thất bại',
  loadFailDesc: 'Đã xảy ra lỗi khi lấy hồ sơ thanh toán. Vui lòng thử lại.',
  retryBtn: 'Thử Lại',
  noDataTitle: 'Không tìm thấy giao dịch',
  noDataFilterDesc: 'Không có giao dịch nào khớp với bộ lọc hiện tại. Hãy thử xóa hoặc điều chỉnh tiêu chí tìm kiếm.',
  noDataEmptyDesc: 'Chưa có giao dịch thanh toán nào được ghi lại.',

  // Row badges
  unknownUser: 'Không Rõ',
  notAvailable: 'N/A',

  // Pagination
  showingText: 'Hiển thị <span class="font-semibold text-[#050505] dark:text-zinc-200">{start} - {end}</span> trên tổng số <span class="font-semibold text-[#050505] dark:text-zinc-200">{total}</span> giao dịch',
  pageSize: 'Số lượng'
};

en['AdminFinance'] = financeEn;
vi['AdminFinance'] = financeVi;

fs.writeFileSync('messages/en.json', JSON.stringify(en, null, 2));
fs.writeFileSync('messages/vi.json', JSON.stringify(vi, null, 2));
console.log('Finance translations updated successfully!');
