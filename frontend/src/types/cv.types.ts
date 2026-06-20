export interface Cv {
  id: string;
  userId: string;
  fileUrl: string;
  fileName: string;
  fileSize: number | null;
  fileType: string;
  isPrimary: boolean;
  parsedData: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCvRequest {
  fileUrl: string;
  fileName: string;
  fileSize: number | null;
  fileType: string;
  isPrimary: boolean;
  parsedData: string;
}
