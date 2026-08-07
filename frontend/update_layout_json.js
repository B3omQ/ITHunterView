const fs = require('fs');
const en = JSON.parse(fs.readFileSync('messages/en.json', 'utf8'));
const vi = JSON.parse(fs.readFileSync('messages/vi.json', 'utf8'));

// Initialize Layout if it doesn't exist
if (!en['Layout']) en['Layout'] = {};
if (!en['Layout']['Sidebar']) en['Layout']['Sidebar'] = {};

if (!vi['Layout']) vi['Layout'] = {};
if (!vi['Layout']['Sidebar']) vi['Layout']['Sidebar'] = {};

// New keys for Dashboard Header
const newKeysEn = {
  searchPlaceholder: 'Quick search...',
  language: 'Language',
  welcomeBack: 'Welcome back',
  headerSubtitle: 'Have a productive day',
  notifications: 'Notifications',
  profile: 'Profile',
  settings: 'Settings',
  changePassword: 'Change Password',
  darkMode: 'Dark Mode',
  walletBalance: 'Wallet Balance',
  topUp: 'Top Up',
  subscriptions: 'Subscriptions',
  topUpCoins: 'Top Up Coins',
  transactionHistory: 'Transaction History',
  billingPlans: 'Billing & Payment'
};

const newKeysVi = {
  searchPlaceholder: 'Tìm kiếm nhanh...',
  language: 'Ngôn ngữ',
  welcomeBack: 'Chào mừng trở lại',
  headerSubtitle: 'Chúc bạn một ngày làm việc hiệu quả',
  notifications: 'Thông báo',
  profile: 'Thông tin cá nhân',
  settings: 'Cài đặt',
  changePassword: 'Đổi mật khẩu',
  darkMode: 'Giao diện tối',
  walletBalance: 'Số dư ví',
  topUp: 'Nạp tiền',
  subscriptions: 'Gói đăng ký',
  topUpCoins: 'Nạp xu',
  transactionHistory: 'Lịch sử giao dịch',
  billingPlans: 'Gói & Thanh toán'
};

// Merge without overwriting existing
en['Layout']['Sidebar'] = { ...newKeysEn, ...en['Layout']['Sidebar'] };
vi['Layout']['Sidebar'] = { ...newKeysVi, ...vi['Layout']['Sidebar'] };

fs.writeFileSync('messages/en.json', JSON.stringify(en, null, 2));
fs.writeFileSync('messages/vi.json', JSON.stringify(vi, null, 2));

console.log('Layout translations updated successfully!');
