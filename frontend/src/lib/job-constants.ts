export const LEVELS = ["Internship", "Fresher", "Junior", "Senior", "Manager"];
export const WORKING_MODELS = ["At office", "Remote", "Hybrid"];

export const PROVINCE_OPTIONS = [
  { label: "Hà Nội", value: "HN" },
  { label: "TP. Hồ Chí Minh", value: "HCM" },
  { label: "Đà Nẵng", value: "DN" },
  { label: "Cần Thơ", value: "CT" },
  { label: "Hải Phòng", value: "HP" },
  { label: "An Giang", value: "AG" },
  { label: "Bà Rịa - Vũng Tàu", value: "VT" },
  { label: "Bắc Giang", value: "BG" },
  { label: "Bắc Kạn", value: "BK" },
  { label: "Bạc Liêu", value: "BL" },
  { label: "Bắc Ninh", value: "BN" },
  { label: "Bến Tre", value: "BTE" },
  { label: "Bình Định", value: "BDI" },
  { label: "Bình Dương", value: "BD" },
  { label: "Bình Phước", value: "BP" },
  { label: "Bình Thuận", value: "BTH" },
  { label: "Cà Mau", value: "CM" },
  { label: "Cao Bằng", value: "CB" },
  { label: "Đắk Lắk", value: "DL" },
  { label: "Đắk Nông", value: "DNO" },
  { label: "Điện Biên", value: "DB" },
  { label: "Đồng Nai", value: "DNA" },
  { label: "Đồng Tháp", value: "DT" },
  { label: "Gia Lai", value: "GL" },
  { label: "Hà Giang", value: "HG" },
  { label: "Hà Nam", value: "HNA" },
  { label: "Hà Tĩnh", value: "HT" },
  { label: "Hải Dương", value: "HD" },
  { label: "Hậu Giang", value: "HGI" },
  { label: "Hòa Bình", value: "HB" },
  { label: "Hưng Yên", value: "HY" },
  { label: "Khánh Hòa", value: "KH" },
  { label: "Kiên Giang", value: "KG" },
  { label: "Kon Tum", value: "KT" },
  { label: "Lai Châu", value: "LC" },
  { label: "Lâm Đồng", value: "LD" },
  { label: "Lạng Sơn", value: "LS" },
  { label: "Lào Cai", value: "LCA" },
  { label: "Long An", value: "LA" },
  { label: "Nam Định", value: "ND" },
  { label: "Nghệ An", value: "NA" },
  { label: "Ninh Bình", value: "NB" },
  { label: "Ninh Thuận", value: "NT" },
  { label: "Phú Thọ", value: "PT" },
  { label: "Phú Yên", value: "PY" },
  { label: "Quảng Bình", value: "QB" },
  { label: "Quảng Nam", value: "QNA" },
  { label: "Quảng Ngãi", value: "QNG" },
  { label: "Quảng Ninh", value: "QN" },
  { label: "Quảng Trị", value: "QT" },
  { label: "Sóc Trăng", value: "ST" },
  { label: "Sơn La", value: "SL" },
  { label: "Tây Ninh", value: "TNI" },
  { label: "Thái Bình", value: "TB" },
  { label: "Thái Nguyên", value: "TN" },
  { label: "Thanh Hóa", value: "TH" },
  { label: "Thừa Thiên Huế", value: "TTH" },
  { label: "Tiền Giang", value: "TG" },
  { label: "Trà Vinh", value: "TV" },
  { label: "Tuyên Quang", value: "TQ" },
  { label: "Vĩnh Long", value: "VL" },
  { label: "Vĩnh Phúc", value: "VP" },
  { label: "Yên Bái", value: "YB" },
  { label: "International / Khác", value: "OTHER" }
];

export const VIETNAM_PROVINCES = PROVINCE_OPTIONS.filter(p => p.value !== "OTHER").map(p => p.label);

export const getProvinceLabel = (value: string | undefined | null) => {
  if (!value) return "Unknown";
  const found = PROVINCE_OPTIONS.find(p => p.value === value);
  return found ? found.label : value;
};

export const JOB_EXPERTISES = [
  "Software Engineer", "Frontend Engineer", "Backend Engineer", "Fullstack Engineer", 
  "Mobile Engineer", "DevOps Engineer", "Data Engineer", "Data Scientist", 
  "AI / Machine Learning Engineer", "UI/UX Designer", "Product Manager", 
  "Project Manager", "Business Analyst", "QA/Tester", "System Administrator", 
  "Database Administrator", "Security Engineer", "Cloud Engineer"
];

export const JOB_DOMAINS = [
  "Blockchain & Web3 Services", "Food and Beverage", "Tourism and Hospitality Services",
  "Insurance", "Consumer Goods", "E-commerce", "Education and Training", "Banking",
  "Game", "Government", "IT Hardware and Computing", "Non-Profit and Social Services",
  "Manufacturing and Engineering", "Media, Advertising and Entertainment", "Environment",
  "Pharmaceuticals", "Real Estate, Property and Construction", "Retail and Wholesale",
  "IT Services and IT Consulting", "Telecommunication", "Transportation, Logistics and Warehouse",
  "Cyber Security", "Trading and Commercial", "Network and Infrastructure",
  "Software Development Outsourcing", "Software Products and Web Services", "Agriculture",
  "Sports and Fitness", "Apparel and Fashion", "Creative and Design", "Staffing and Recruiting",
  "Publishing and Printing", "Facility Management", "Research Services", "Healthcare",
  "Materials and Mining", "Utilities", "Professional Services", "Securities & Investment",
  "Financial Services", "Emerging Tech R&D", "AI Software & Services"
];

export const COMPANY_INDUSTRIES = [
  "Blockchain & Web3 Services", "Food and Beverage", "Tourism and Hospitality Services",
  "Insurance", "Consumer Goods", "E-commerce", "Education and Training", "Banking",
  "Game", "Government", "IT Hardware and Computing", "Non-Profit and Social Services",
  "Manufacturing and Engineering", "Media, Advertising and Entertainment", "Environment",
  "Pharmaceuticals", "Real Estate, Property and Construction", "Retail and Wholesale",
  "IT Services and IT Consulting", "Telecommunication", "Transportation, Logistics and Warehouse",
  "Cyber Security", "Trading and Commercial", "Network and Infrastructure",
  "Software Development Outsourcing", "Software Products and Web Services", "Agriculture",
  "Sports and Fitness", "Apparel and Fashion", "Creative and Design", "Staffing and Recruiting",
  "Publishing and Printing", "Facility Management", "Research Services", "Healthcare",
  "Materials and Mining", "Utilities", "Professional Services", "Securities & Investment",
  "Financial Services", "Emerging Tech R&D", "AI Software & Services"
];

export const COMPANY_TYPES = ["IT Outsourcing", "IT Product", "Headhunt", "IT Service and IT Consulting", "Non-IT"];
