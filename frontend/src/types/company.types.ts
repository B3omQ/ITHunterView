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
  rejectReason?: string;
  hasPendingChange?: boolean;
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
}

export interface VerifyCompanyDto {
  verificationMethod: string;
  verificationDocumentUrl: string;
  taxCode: string;
  companyName: string;
  headquartersAddress: string;
}

export interface UpdateCompanyStatusDto {
  status: CompanyStatus;
  rejectReason?: string;
}
