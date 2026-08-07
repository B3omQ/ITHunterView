const fs = require('fs');
const en = JSON.parse(fs.readFileSync('messages/en.json', 'utf8'));
const vi = JSON.parse(fs.readFileSync('messages/vi.json', 'utf8'));

// Initialize Layout if it doesn't exist
if (!en['Layout']) en['Layout'] = {};
if (!en['Layout']['Search']) en['Layout']['Search'] = {};

if (!vi['Layout']) vi['Layout'] = {};
if (!vi['Layout']['Search']) vi['Layout']['Search'] = {};

// New keys for Global Search
const newKeysEn = {
  placeholder: 'Type a command or search...',
  noResults: 'No results found.',
  quickActions: 'Quick Actions',
  navigation: 'Navigation',
  jobs: 'Jobs & Candidates',
  searchJobs: 'Search Jobs',
  createJob: 'Create Job',
  searchCandidates: 'Search Candidates',
  mockInterview: 'Mock Interview',
  cvMatching: 'CV-JD Matching',
  billing: 'Billing',
  settings: 'Settings',
  profile: 'My Profile',
  theme: 'Theme',
  light: 'Light',
  dark: 'Dark',
  system: 'System'
};

const newKeysVi = {
  placeholder: 'Nhập lệnh hoặc tìm kiếm...',
  noResults: 'Không tìm thấy kết quả.',
  quickActions: 'Hành động nhanh',
  navigation: 'Điều hướng',
  jobs: 'Tuyển dụng',
  searchJobs: 'Tìm kiếm Việc làm',
  createJob: 'Tạo Tin Tuyển Dụng',
  searchCandidates: 'Tìm kiếm Ứng viên',
  mockInterview: 'Phỏng vấn AI',
  cvMatching: 'Đánh giá CV-JD',
  billing: 'Thanh toán',
  settings: 'Cài đặt',
  profile: 'Hồ sơ của tôi',
  theme: 'Giao diện',
  light: 'Sáng',
  dark: 'Tối',
  system: 'Hệ thống'
};

// Merge without overwriting existing
en['Layout']['Search'] = { ...newKeysEn, ...en['Layout']['Search'] };
vi['Layout']['Search'] = { ...newKeysVi, ...vi['Layout']['Search'] };

fs.writeFileSync('messages/en.json', JSON.stringify(en, null, 2));
fs.writeFileSync('messages/vi.json', JSON.stringify(vi, null, 2));

console.log('Search translations updated successfully!');
