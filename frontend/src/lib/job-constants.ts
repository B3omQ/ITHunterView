export const LEVELS = ["Internship", "Fresher", "Junior", "Senior", "Manager"];
export const WORKING_MODELS = ["At office", "Remote", "Hybrid"];

export const VIETNAM_PROVINCES = [
  "Hà Nội", "TP Hồ Chí Minh", "Đà Nẵng", "Cần Thơ", "Hải Phòng",
  "An Giang", "Bà Rịa - Vũng Tàu", "Bắc Giang", "Bắc Kạn", "Bạc Liêu",
  "Bắc Ninh", "Bến Tre", "Bình Định", "Bình Dương", "Bình Phước",
  "Bình Thuận", "Cà Mau", "Cao Bằng", "Đắk Lắk", "Đắk Nông",
  "Điện Biên", "Đồng Nai", "Đồng Tháp", "Gia Lai", "Hà Giang",
  "Hà Nam", "Hà Tĩnh", "Hải Dương", "Hậu Giang", "Hòa Bình",
  "Hưng Yên", "Khánh Hòa", "Kiên Giang", "Kon Tum"
];

export const PROVINCE_OPTIONS = [
  { label: "Hà Nội", value: "HN" },
  { label: "TP. Hồ Chí Minh", value: "HCM" },
  { label: "Đà Nẵng", value: "DN" },
  { label: "Cần Thơ", value: "CT" },
  { label: "Hải Phòng", value: "HP" },
  { label: "Bình Dương", value: "BD" },
  { label: "Đồng Nai", value: "DNA" },
  { label: "Bắc Ninh", value: "BN" },
  { label: "Quảng Ninh", value: "QN" },
  { label: "Khánh Hòa", value: "KH" },
  { label: "Thừa Thiên Huế", value: "TTH" },
  { label: "Bà Rịa - Vũng Tàu", value: "VT" },
  { label: "Quảng Nam", value: "QNA" },
  { label: "Hải Dương", value: "HD" },
  { label: "Thanh Hóa", value: "TH" },
  { label: "Nghệ An", value: "NA" },
  { label: "Lâm Đồng", value: "LD" },
  { label: "Hưng Yên", value: "HY" },
  { label: "Vĩnh Phúc", value: "VP" },
  { label: "Thái Nguyên", value: "TN" },
  { label: "Long An", value: "LA" },
  { label: "International / Khác", value: "OTHER" }
];

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
