export type CompanyVerificationMethod = 'BUSINESS_REGISTRATION' | 'POA_AND_ID';
export type CompanyStatus = 'DRAFT' | 'PENDING' | 'VERIFIED' | 'REJECTED';

export interface Company {
  id: string;
  name: string;
  taxCode: string;
  headquartersAddress: string;
  industry: string;
  companySize: string;
  description: string;
  companyType?: string;
  website: string;
  logoUrl: string;
  verificationMethod: CompanyVerificationMethod;
  verificationDocumentUrl: string;
  status: CompanyStatus;
  createdAt: string;
  updatedAt: string;
  createdByEmail?: string;
  createdByName?: string;
  pendingName?: string;
  pendingTaxCode?: string;
  pendingHeadquartersAddress?: string;
  pendingVerificationMethod?: CompanyVerificationMethod;
  pendingVerificationDocumentUrl?: string;
  pendingCompanyType?: string;
  rejectReason?: string;
  hasPendingChange?: boolean;

  // New fields
  tradeName?: string;
  targetCustomers?: string[];
  companyEmail?: string;
  contactPhone?: string;
  companyImages?: string[];
  mainField?: string;
  operatingMarkets?: string[];
  employeeBenefits?: string;
}

export interface CreateCompanyDto {
  name: string;
  industry: string;
  companySize: string;
  description: string;
  companyType?: string;
  website?: string;
  logoUrl?: string;
  taxCode?: string; // Optional during profile creation, required in legal
  headquartersAddress?: string; // Optional during profile creation

  // New fields
  tradeName?: string;
  targetCustomers?: string[];
  companyEmail?: string;
  contactPhone?: string;
  companyImages?: string[];
  mainField?: string;
  operatingMarkets?: string[];
  employeeBenefits?: string;
}

export interface UpdateCompanyDto {
  website?: string;
  logoUrl?: string;
  companySize?: string;
  description?: string;
  companyType?: string;
  industry?: string;

  // New fields
  tradeName?: string;
  targetCustomers?: string[];
  companyEmail?: string;
  contactPhone?: string;
  companyImages?: string[];
  mainField?: string;
  operatingMarkets?: string[];
  employeeBenefits?: string;
}

export interface VerifyCompanyDto {
  verificationMethod: string;
  verificationDocumentUrl: string;
  taxCode: string;
  companyName: string;
  headquartersAddress: string;
  companyType?: string;
}

export interface UpdateCompanyStatusDto {
  status: CompanyStatus;
  rejectReason?: string;
}
