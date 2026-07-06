export type SkillStatus = 'ACTIVE' | 'DEACTIVE';

export interface SkillCategoryDto {
  id: number;
  name: string;
}

export interface SkillDto {
  id: number;
  categoryId?: number;
  categoryName: string;
  name: string;
  status: SkillStatus;
  createdBy?: string;
  updatedBy?: string;
}

export interface CreateSkillDto {
  name: string;
  categoryId: number;
  status?: SkillStatus;
}

export interface UpdateSkillDto {
  name: string;
  categoryId: number;
  status: SkillStatus;
}

export interface UpdateSkillStatusDto {
  status: SkillStatus;
  force: boolean;
}

/**
 * DTO đại diện cho một Chuyên ngành (Major) trong hệ thống dưới dạng cây phân cấp.
 */
export interface MajorDto {
  id: number;
  /** Tên chuyên ngành (Ví dụ: Kỹ thuật phần mềm). Tối thiểu 3 ký tự, tối đa 255 ký tự. */
  name: string;
  /** Mã chuyên ngành duy nhất (Ví dụ: SE, BA). Tối thiểu 2 ký tự, tối đa 50 ký tự, chỉ chứa chữ, số, gạch nối và gạch dưới. */
  code: string;
  /** ID của chuyên ngành cha. Nếu bằng null hoặc undefined tức là chuyên ngành gốc (Level 1). */
  parentId?: number;
  /** Tên của chuyên ngành cha. */
  parentName?: string;
  /** Danh sách các chuyên ngành con (Level kế tiếp). Cây phân cấp tối đa 3 levels. */
  children?: MajorDto[];
  createdBy?: string;
  updatedBy?: string;
}

/**
 * DTO dùng để gửi yêu cầu tạo mới Chuyên ngành.
 */
export interface CreateMajorDto {
  /** Tên chuyên ngành cần tạo. */
  name: string;
  /** Mã chuyên ngành (sẽ được tự động viết hoa và normalize). */
  code: string;
  /** ID của chuyên ngành cha (nếu muốn đặt làm con). Chỉ chấp nhận nút cha có Level 1 hoặc Level 2. */
  parentId?: number | null;
}

/**
 * DTO dùng để gửi yêu cầu cập nhật Chuyên ngành.
 * Lưu ý: Việc cập nhật parentId bị cấm ở mức API đối với chế độ sửa đổi để bảo vệ tính toàn vẹn của cây.
 */
export interface UpdateMajorDto {
  /** Tên mới của chuyên ngành. */
  name: string;
  /** Mã mới của chuyên ngành. */
  code: string;
  /** ID của chuyên ngành cha. Không được thay đổi so với giá trị ban đầu. */
  parentId?: number | null;
}
