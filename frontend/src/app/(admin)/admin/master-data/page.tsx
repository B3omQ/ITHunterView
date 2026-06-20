'use client';

import React, { useState, useEffect, useMemo } from 'react';
import {
  Search,
  Plus,
  Edit2,
  Trash2,
  AlertTriangle,
  RotateCcw,
  CheckCircle,
  X,
  ChevronLeft,
  ChevronRight,
  Loader2,
  Database,
  Layers,
  Settings,
  XCircle,
} from 'lucide-react';
import {
  useSkills,
  useSkillCategories,
  useCreateSkill,
  useUpdateSkill,
  useUpdateSkillStatus,
  useDeleteSkill,
} from '@/hooks/useSkill';
import {
  useMajors,
  useCreateMajor,
  useUpdateMajor,
  useDeleteMajor,
  useRestoreMajor,
} from '@/hooks/useMajor';
import type { SkillDto, MajorDto, SkillStatus } from '@/types/master-data.types';

function TableSkeleton({ columns }: { columns: number }) {
  return (
    <div className="w-full">
      <div className="overflow-x-auto">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="border-b border-border bg-muted/30">
              {Array.from({ length: columns }).map((_, i) => (
                <th key={i} className="px-6 py-4">
                  <div className="h-4 bg-muted animate-pulse rounded-md w-24" />
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-border/60">
            {Array.from({ length: 5 }).map((_, rowIndex) => (
              <tr key={rowIndex}>
                {Array.from({ length: columns }).map((_, colIndex) => (
                  <td key={colIndex} className="px-6 py-4">
                    <div
                      className={`h-5 bg-muted/70 animate-pulse rounded-md ${
                        colIndex === columns - 1 ? 'ml-auto w-16' : 'w-3/4'
                      }`}
                    />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

const validateSkillForm = (name: string, categoryId: string): string | null => {
  if (!name.trim()) return 'Tên kỹ năng không được để trống.';
  if (name.trim().length < 2) return 'Tên kỹ năng phải có ít nhất 2 ký tự.';
  if (!categoryId) return 'Danh mục kỹ năng không được để trống.';
  return null;
};

const validateMajorForm = (code: string, name: string): string | null => {
  if (!code.trim()) return 'Mã chuyên ngành không được để trống.';
  if (!/^[A-Za-z0-9\-]+$/.test(code.trim())) {
    return 'Mã chuyên ngành chỉ được chứa chữ cái, chữ số và dấu gạch ngang.';
  }
  if (!name.trim()) return 'Tên chuyên ngành không được để trống.';
  if (name.trim().length < 3) return 'Tên chuyên ngành phải có ít nhất 3 ký tự.';
  return null;
};

export default function MasterDataPage() {
  const [activeTab, setActiveTab] = useState<'skills' | 'majors'>('skills');

  // Search State with local Debounce
  const [skillSearch, setSkillSearch] = useState('');
  const [debouncedSkillSearch, setDebouncedSkillSearch] = useState('');
  const [majorSearch, setMajorSearch] = useState('');
  const [debouncedMajorSearch, setDebouncedMajorSearch] = useState('');

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSkillSearch(skillSearch), 300);
    return () => clearTimeout(timer);
  }, [skillSearch]);

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedMajorSearch(majorSearch), 300);
    return () => clearTimeout(timer);
  }, [majorSearch]);

  // Skill Filters & Pagination State
  const [selectedCategoryId, setSelectedCategoryId] = useState<number | null>(null);
  const [selectedStatus, setSelectedStatus] = useState<SkillStatus | null>(null);
  const [skillPage, setSkillPage] = useState(1);
  const skillPageSize = 10;

  // Major Pagination State
  const [majorPage, setMajorPage] = useState(1);
  const majorPageSize = 10;

  // Toast State
  const [toast, setToast] = useState<{
    message: string;
    type: 'success' | 'error' | 'warning';
    undoAction?: () => void;
  } | null>(null);

  const showToast = (
    message: string,
    type: 'success' | 'error' | 'warning' = 'success',
    undoAction?: () => void
  ) => {
    setToast({ message, type, undoAction });
  };

  useEffect(() => {
    if (toast) {
      const timer = setTimeout(() => setToast(null), 6000);
      return () => clearTimeout(timer);
    }
  }, [toast]);

  // Fetch Data - Skills
  const {
    data: skillsData,
    isLoading: isSkillsLoading,
    isError: isSkillsError,
    refetch: refetchSkills,
  } = useSkills({
    page: skillPage,
    pageSize: skillPageSize,
    search: debouncedSkillSearch,
    categoryId: selectedCategoryId || undefined,
    status: selectedStatus || undefined,
  });

  const { data: categoriesData, isLoading: isCategoriesLoading } = useSkillCategories();

  // Fetch Data - Majors
  const {
    data: majorsData,
    isLoading: isMajorsLoading,
    isError: isMajorsError,
    refetch: refetchMajors,
  } = useMajors({
    page: majorPage,
    pageSize: majorPageSize,
    search: debouncedMajorSearch,
  });

  // Mutations
  const createSkillMutation = useCreateSkill();
  const updateSkillMutation = useUpdateSkill();
  const updateSkillStatusMutation = useUpdateSkillStatus();
  const deleteSkillMutation = useDeleteSkill();

  const createMajorMutation = useCreateMajor();
  const updateMajorMutation = useUpdateMajor();
  const deleteMajorMutation = useDeleteMajor();
  const restoreMajorMutation = useRestoreMajor();

  // Modals / Dialogs State - Skills
  const [isSkillModalOpen, setIsSkillModalOpen] = useState(false);
  const [skillModalMode, setSkillModalMode] = useState<'create' | 'edit'>('create');
  const [selectedSkill, setSelectedSkill] = useState<SkillDto | null>(null);
  const [skillForm, setSkillForm] = useState({ name: '', categoryId: '', status: 'ACTIVE' as SkillStatus });
  const [skillFormError, setSkillFormError] = useState('');

  const [isSkillDeleteOpen, setIsSkillDeleteOpen] = useState(false);
  const [skillToDelete, setSkillToDelete] = useState<SkillDto | null>(null);

  // Force Change Status Warning Dialog State
  const [isForceStatusOpen, setIsForceStatusOpen] = useState(false);
  const [forceStatusData, setForceStatusData] = useState<{ id: number; status: SkillStatus; message: string } | null>(
    null
  );

  // Modals / Dialogs State - Majors
  const [isMajorModalOpen, setIsMajorModalOpen] = useState(false);
  const [majorModalMode, setMajorModalMode] = useState<'create' | 'edit'>('create');
  const [selectedMajor, setSelectedMajor] = useState<MajorDto | null>(null);
  const [majorForm, setMajorForm] = useState({ name: '', code: '' });
  const [majorFormError, setMajorFormError] = useState('');

  const [isMajorDeleteOpen, setIsMajorDeleteOpen] = useState(false);
  const [majorToDelete, setMajorToDelete] = useState<MajorDto | null>(null);

  // Reset pagination when filter/search changes
  useEffect(() => {
    setSkillPage(1);
  }, [debouncedSkillSearch, selectedCategoryId, selectedStatus]);

  useEffect(() => {
    setMajorPage(1);
  }, [debouncedMajorSearch]);

  // Skill Form Actions
  const handleOpenSkillCreate = () => {
    setSkillModalMode('create');
    setSelectedSkill(null);
    setSkillForm({
      name: '',
      categoryId: categoriesData?.data?.[0]?.id?.toString() || '',
      status: 'ACTIVE',
    });
    setSkillFormError('');
    setIsSkillModalOpen(true);
  };

  const handleOpenSkillEdit = (skill: SkillDto) => {
    setSkillModalMode('edit');
    setSelectedSkill(skill);
    setSkillForm({
      name: skill.name,
      categoryId: skill.categoryId?.toString() || '',
      status: skill.status,
    });
    setSkillFormError('');
    setIsSkillModalOpen(true);
  };

  const handleSkillSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSkillFormError('');

    const validationError = validateSkillForm(skillForm.name, skillForm.categoryId);
    if (validationError) {
      setSkillFormError(validationError);
      return;
    }

    const categoryIdNum = parseInt(skillForm.categoryId, 10);

    if (skillModalMode === 'create') {
      createSkillMutation.mutate(
        {
          name: skillForm.name.trim(),
          categoryId: categoryIdNum,
          status: skillForm.status,
        },
        {
          onSuccess: (res) => {
            if (res.success) {
              showToast('Thêm kỹ năng mới thành công!', 'success');
              setIsSkillModalOpen(false);
            } else {
              setSkillFormError(res.message || 'Có lỗi xảy ra.');
            }
          },
          onError: (err: any) => {
            setSkillFormError(err.response?.data?.message || 'Có lỗi xảy ra khi tạo kỹ năng.');
          },
        }
      );
    } else if (skillModalMode === 'edit' && selectedSkill) {
      updateSkillMutation.mutate(
        {
          id: selectedSkill.id,
          dto: {
            name: skillForm.name.trim(),
            categoryId: categoryIdNum,
            status: skillForm.status,
          },
        },
        {
          onSuccess: (res) => {
            if (res.success) {
              showToast('Cập nhật kỹ năng thành công!', 'success');
              setIsSkillModalOpen(false);
            } else {
              setSkillFormError(res.message || 'Có lỗi xảy ra.');
            }
          },
          onError: (err: any) => {
            setSkillFormError(err.response?.data?.message || 'Có lỗi xảy ra khi cập nhật.');
          },
        }
      );
    }
  };

  const handleSkillStatusToggle = (skill: SkillDto) => {
    const newStatus: SkillStatus = skill.status === 'ACTIVE' ? 'DEACTIVE' : 'ACTIVE';

    updateSkillStatusMutation.mutate(
      {
        id: skill.id,
        dto: {
          status: newStatus,
          force: false,
        },
      },
      {
        onSuccess: (res) => {
          if (res.success) {
            showToast(`Cập nhật trạng thái kỹ năng sang ${newStatus} thành công!`, 'success');
          }
        },
        onError: (err: any) => {
          const apiMessage = err.response?.data?.message;
          if (apiMessage && (apiMessage.includes('đang được sử dụng') || apiMessage.includes('vô hiệu hóa'))) {
            // Mở Dialog ép buộc đổi trạng thái
            setForceStatusData({
              id: skill.id,
              status: newStatus,
              message: apiMessage,
            });
            setIsForceStatusOpen(true);
          } else {
            showToast(apiMessage || 'Lỗi cập nhật trạng thái kỹ năng.', 'error');
          }
        },
      }
    );
  };

  const handleForceSkillStatusSubmit = () => {
    if (!forceStatusData) return;

    updateSkillStatusMutation.mutate(
      {
        id: forceStatusData.id,
        dto: {
          status: forceStatusData.status,
          force: true,
        },
      },
      {
        onSuccess: (res) => {
          if (res.success) {
            showToast(`Buộc cập nhật trạng thái kỹ năng sang ${forceStatusData.status} thành công!`, 'success');
            setIsForceStatusOpen(false);
            setForceStatusData(null);
          }
        },
        onError: (err: any) => {
          showToast(err.response?.data?.message || 'Lỗi buộc cập nhật trạng thái.', 'error');
          setIsForceStatusOpen(false);
          setForceStatusData(null);
        },
      }
    );
  };

  const handleSkillDeleteClick = (skill: SkillDto) => {
    setSkillToDelete(skill);
    setIsSkillDeleteOpen(true);
  };

  const handleSkillDeleteConfirm = () => {
    if (!skillToDelete) return;

    deleteSkillMutation.mutate(skillToDelete.id, {
      onSuccess: (res) => {
        if (res.success) {
          showToast('Xóa kỹ năng thành công!', 'success');
          setIsSkillDeleteOpen(false);
          setSkillToDelete(null);
        } else {
          showToast(res.message || 'Lỗi khi xóa kỹ năng.', 'error');
        }
      },
      onError: (err: any) => {
        showToast(
          err.response?.data?.message || 'Không thể xóa kỹ năng này (có thể do ràng buộc khóa ngoại).',
          'error'
        );
        setIsSkillDeleteOpen(false);
      },
    });
  };

  // Major Form Actions
  const handleOpenMajorCreate = () => {
    setMajorModalMode('create');
    setSelectedMajor(null);
    setMajorForm({ name: '', code: '' });
    setMajorFormError('');
    setIsMajorModalOpen(true);
  };

  const handleOpenMajorEdit = (major: MajorDto) => {
    setMajorModalMode('edit');
    setSelectedMajor(major);
    setMajorForm({ name: major.name, code: major.code });
    setMajorFormError('');
    setIsMajorModalOpen(true);
  };

  const handleMajorSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setMajorFormError('');

    const validationError = validateMajorForm(majorForm.code, majorForm.name);
    if (validationError) {
      setMajorFormError(validationError);
      return;
    }

    if (majorModalMode === 'create') {
      createMajorMutation.mutate(
        {
          code: majorForm.code.trim().toUpperCase(),
          name: majorForm.name.trim(),
        },
        {
          onSuccess: (res) => {
            if (res.success) {
              showToast('Thêm chuyên ngành mới thành công!', 'success');
              setIsMajorModalOpen(false);
            } else {
              setMajorFormError(res.message || 'Có lỗi xảy ra.');
            }
          },
          onError: (err: any) => {
            setMajorFormError(err.response?.data?.message || 'Có lỗi xảy ra khi tạo chuyên ngành.');
          },
        }
      );
    } else if (majorModalMode === 'edit' && selectedMajor) {
      updateMajorMutation.mutate(
        {
          id: selectedMajor.id,
          dto: {
            code: majorForm.code.trim().toUpperCase(),
            name: majorForm.name.trim(),
          },
        },
        {
          onSuccess: (res) => {
            if (res.success) {
              showToast('Cập nhật chuyên ngành thành công!', 'success');
              setIsMajorModalOpen(false);
            } else {
              setMajorFormError(res.message || 'Có lỗi xảy ra.');
            }
          },
          onError: (err: any) => {
            setMajorFormError(err.response?.data?.message || 'Có lỗi xảy ra khi cập nhật.');
          },
        }
      );
    }
  };

  const handleMajorDeleteClick = (major: MajorDto) => {
    setMajorToDelete(major);
    setIsMajorDeleteOpen(true);
  };

  const handleMajorDeleteConfirm = () => {
    if (!majorToDelete) return;

    deleteMajorMutation.mutate(majorToDelete.id, {
      onSuccess: (res) => {
        if (res.success) {
          const deletedId = majorToDelete.id;
          const deletedName = majorToDelete.name;
          showToast(
            `Đã xóa mềm chuyên ngành "${deletedName}".`,
            'warning',
            () => {
              // Action khôi phục (Undo)
              restoreMajorMutation.mutate(deletedId, {
                onSuccess: (restoreRes) => {
                  if (restoreRes.success) {
                    showToast(`Khôi phục chuyên ngành "${deletedName}" thành công!`, 'success');
                  }
                },
                onError: (restoreErr: any) => {
                  showToast(
                    restoreErr.response?.data?.message || 'Khôi phục chuyên ngành thất bại.',
                    'error'
                  );
                },
              });
            }
          );
          setIsMajorDeleteOpen(false);
          setMajorToDelete(null);
        } else {
          showToast(res.message || 'Lỗi khi xóa chuyên ngành.', 'error');
        }
      },
      onError: (err: any) => {
        showToast(
          err.response?.data?.message || 'Không thể xóa chuyên ngành này do ràng buộc khóa ngoại.',
          'error'
        );
        setIsMajorDeleteOpen(false);
      },
    });
  };

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Quản lý Master Data</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Quản trị danh mục Kỹ năng (Skills Library) và Chuyên ngành học (Major List) trong hệ thống.
          </p>
        </div>
        <button
          onClick={activeTab === 'skills' ? handleOpenSkillCreate : handleOpenMajorCreate}
          className="inline-flex items-center gap-1.5 px-4 py-2 bg-primary hover:bg-primary/95 text-primary-foreground font-medium text-sm rounded-xl shadow-xs transition-colors"
        >
          <Plus size={16} />
          <span>Thêm {activeTab === 'skills' ? 'kỹ năng' : 'chuyên ngành'} mới</span>
        </button>
      </div>

      {/* Tabs Menu */}
      <div className="flex border-b border-border">
        <button
          onClick={() => setActiveTab('skills')}
          className={`flex items-center gap-2 px-6 py-3 border-b-2 text-sm font-medium transition-all ${
            activeTab === 'skills'
              ? 'border-primary text-primary bg-primary/5 rounded-t-lg'
              : 'border-transparent text-muted-foreground hover:text-foreground hover:bg-muted/30'
          }`}
        >
          <Layers size={16} />
          <span>Skills Library (Kỹ năng)</span>
        </button>
        <button
          onClick={() => setActiveTab('majors')}
          className={`flex items-center gap-2 px-6 py-3 border-b-2 text-sm font-medium transition-all ${
            activeTab === 'majors'
              ? 'border-primary text-primary bg-primary/5 rounded-t-lg'
              : 'border-transparent text-muted-foreground hover:text-foreground hover:bg-muted/30'
          }`}
        >
          <Database size={16} />
          <span>Major List (Chuyên ngành)</span>
        </button>
      </div>

      {/* TAB CONTENT: SKILLS */}
      {activeTab === 'skills' && (
        <div className="space-y-4">
          {/* Filters Bar */}
          <div className="bg-card border border-border rounded-2xl p-4 space-y-4 shadow-2xs">
            <div className="flex flex-col md:flex-row gap-4 items-stretch md:items-center">
              {/* Search */}
              <div className="relative flex-1">
                <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
                <input
                  type="text"
                  placeholder="Tìm kiếm kỹ năng theo tên..."
                  value={skillSearch}
                  onChange={(e) => setSkillSearch(e.target.value)}
                  className="pl-9 pr-4 py-2 w-full rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground"
                />
                {skillSearch && (
                  <button
                    onClick={() => setSkillSearch('')}
                    className="absolute right-3 top-2.5 text-muted-foreground hover:text-foreground"
                  >
                    <X size={16} />
                  </button>
                )}
              </div>

              {/* Status Select */}
              <div className="w-full md:w-48">
                <select
                  value={selectedStatus || ''}
                  onChange={(e) => setSelectedStatus((e.target.value as SkillStatus) || null)}
                  className="w-full px-3 py-2 rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
                >
                  <option value="">Tất cả trạng thái</option>
                  <option value="ACTIVE">Hoạt động</option>
                  <option value="DEACTIVE">Vô hiệu hóa</option>
                </select>
              </div>
            </div>

            {/* Category Pills */}
            <div className="space-y-2">
              <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wider block">
                Phân loại danh mục
              </span>
              <div className="flex flex-wrap gap-2 max-h-32 overflow-y-auto pr-2">
                <button
                  onClick={() => setSelectedCategoryId(null)}
                  className={`px-3 py-1.5 rounded-full text-xs font-medium transition-colors ${
                    selectedCategoryId === null
                      ? 'bg-primary text-primary-foreground'
                      : 'bg-muted text-muted-foreground hover:bg-muted/80 hover:text-foreground'
                  }`}
                >
                  Tất cả
                </button>
                {categoriesData?.data?.map((cat) => (
                  <button
                    key={cat.id}
                    onClick={() => setSelectedCategoryId(cat.id)}
                    className={`px-3 py-1.5 rounded-full text-xs font-medium transition-colors ${
                      selectedCategoryId === cat.id
                        ? 'bg-primary text-primary-foreground'
                        : 'bg-muted text-muted-foreground hover:bg-muted/80 hover:text-foreground'
                    }`}
                  >
                    {cat.name}
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* Table Container */}
          <div className="bg-card border border-border rounded-2xl overflow-hidden shadow-2xs">
            {isSkillsLoading ? (
              <TableSkeleton columns={4} />
            ) : isSkillsError ? (
              <div className="text-center py-20 space-y-3">
                <p className="text-sm text-destructive font-medium">Lỗi khi tải dữ liệu từ máy chủ.</p>
                <button
                  onClick={() => refetchSkills()}
                  className="px-4 py-2 text-xs font-medium border border-border rounded-xl hover:bg-muted"
                >
                  Tải lại
                </button>
              </div>
            ) : !skillsData?.data?.items || skillsData.data.items.length === 0 ? (
              <div className="text-center py-20 text-muted-foreground">
                <p className="text-sm">Không tìm thấy kỹ năng nào phù hợp.</p>
              </div>
            ) : (
              <>
                <div className="overflow-x-auto">
                  <table className="w-full text-left border-collapse">
                    <thead>
                      <tr className="border-b border-border bg-muted/30">
                        <th className="px-6 py-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                          Kỹ năng
                        </th>
                        <th className="px-6 py-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                          Danh mục
                        </th>
                        <th className="px-6 py-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider text-center">
                          Trạng thái
                        </th>
                        <th className="px-6 py-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider text-right">
                          Hành động
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border/60">
                      {skillsData.data.items.map((skill) => (
                        <tr key={skill.id} className="hover:bg-muted/20 transition-colors">
                          <td className="px-6 py-4">
                            <span className="text-sm font-medium text-foreground">{skill.name}</span>
                          </td>
                          <td className="px-6 py-4">
                            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-secondary text-secondary-foreground">
                              {skill.categoryName || 'Không phân loại'}
                            </span>
                          </td>
                          <td className="px-6 py-4">
                            <div className="flex items-center justify-center">
                              <label className="relative inline-flex items-center cursor-pointer">
                                <input
                                  type="checkbox"
                                  checked={skill.status === 'ACTIVE'}
                                  onChange={() => handleSkillStatusToggle(skill)}
                                  className="sr-only peer"
                                />
                                <div className="w-9 h-5 bg-muted peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-border after:border after:rounded-full after:height-4 after:h-4 after:w-4 after:transition-all peer-checked:bg-primary"></div>
                              </label>
                            </div>
                          </td>
                          <td className="px-6 py-4 text-right">
                            <div className="flex items-center justify-end gap-2">
                              <button
                                onClick={() => handleOpenSkillEdit(skill)}
                                className="p-1.5 text-muted-foreground hover:text-foreground rounded-lg hover:bg-muted transition-colors"
                                title="Chỉnh sửa"
                              >
                                <Edit2 size={15} />
                              </button>
                              <button
                                onClick={() => handleSkillDeleteClick(skill)}
                                className="p-1.5 text-muted-foreground hover:text-destructive rounded-lg hover:bg-destructive/10 transition-colors"
                                title="Xóa"
                              >
                                <Trash2 size={15} />
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Pagination */}
                <div className="flex flex-col sm:flex-row items-center justify-between px-6 py-4 border-t border-border gap-4 bg-muted/10">
                  <span className="text-xs text-muted-foreground">
                    Hiển thị {(skillPage - 1) * skillPageSize + 1} -{' '}
                    {Math.min(skillPage * skillPageSize, skillsData?.data?.total || 0)} trong tổng số{' '}
                    {skillsData?.data?.total || 0} kỹ năng
                  </span>
                  <div className="flex items-center gap-1.5">
                    <button
                      onClick={() => setSkillPage((p) => Math.max(1, p - 1))}
                      disabled={skillPage === 1}
                      className="p-1.5 rounded-lg border border-border bg-card text-muted-foreground hover:text-foreground disabled:opacity-50 disabled:pointer-events-none transition-colors"
                    >
                      <ChevronLeft size={16} />
                    </button>
                    {Array.from({ length: skillsData?.data?.totalPages || 0 }, (_, i) => i + 1).map((p) => (
                      <button
                        key={p}
                        onClick={() => setSkillPage(p)}
                        className={`px-3 py-1 rounded-lg text-xs font-medium border transition-colors ${
                          skillPage === p
                            ? 'bg-primary text-primary-foreground border-primary'
                            : 'border-border bg-card text-muted-foreground hover:text-foreground'
                        }`}
                      >
                        {p}
                      </button>
                    ))}
                    <button
                      onClick={() => setSkillPage((p) => Math.min(skillsData?.data?.totalPages || 1, p + 1))}
                      disabled={skillPage === (skillsData?.data?.totalPages || 1)}
                      className="p-1.5 rounded-lg border border-border bg-card text-muted-foreground hover:text-foreground disabled:opacity-50 disabled:pointer-events-none transition-colors"
                    >
                      <ChevronRight size={16} />
                    </button>
                  </div>
                </div>
              </>
            )}
          </div>
        </div>
      )}

      {/* TAB CONTENT: MAJORS */}
      {activeTab === 'majors' && (
        <div className="space-y-4">
          {/* Filters Bar */}
          <div className="bg-card border border-border rounded-2xl p-4 shadow-2xs flex gap-4">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-2.5 h-4 w-4 text-muted-foreground" />
              <input
                type="text"
                placeholder="Tìm kiếm chuyên ngành theo tên hoặc mã chuyên ngành..."
                value={majorSearch}
                onChange={(e) => setMajorSearch(e.target.value)}
                className="pl-9 pr-4 py-2 w-full rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground"
              />
              {majorSearch && (
                <button
                  onClick={() => setMajorSearch('')}
                  className="absolute right-3 top-2.5 text-muted-foreground hover:text-foreground"
                >
                  <X size={16} />
                </button>
              )}
            </div>
          </div>

          {/* Table Container */}
          <div className="bg-card border border-border rounded-2xl overflow-hidden shadow-2xs">
            {isMajorsLoading ? (
              <TableSkeleton columns={3} />
            ) : isMajorsError ? (
              <div className="text-center py-20 space-y-3">
                <p className="text-sm text-destructive font-medium">Lỗi khi tải dữ liệu từ máy chủ.</p>
                <button
                  onClick={() => refetchMajors()}
                  className="px-4 py-2 text-xs font-medium border border-border rounded-xl hover:bg-muted"
                >
                  Tải lại
                </button>
              </div>
            ) : !majorsData?.data?.items || majorsData.data.items.length === 0 ? (
              <div className="text-center py-20 text-muted-foreground">
                <p className="text-sm">Không tìm thấy chuyên ngành nào.</p>
              </div>
            ) : (
              <>
                <div className="overflow-x-auto">
                  <table className="w-full text-left border-collapse">
                    <thead>
                      <tr className="border-b border-border bg-muted/30">
                        <th className="px-6 py-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider w-1/4">
                          Mã Chuyên ngành
                        </th>
                        <th className="px-6 py-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                          Tên chuyên ngành
                        </th>
                        <th className="px-6 py-4 text-xs font-semibold text-muted-foreground uppercase tracking-wider text-right">
                          Hành động
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border/60">
                      {majorsData.data.items.map((major) => (
                        <tr key={major.id} className="hover:bg-muted/20 transition-colors">
                          <td className="px-6 py-4">
                            <span className="text-sm font-mono font-semibold bg-neutral-200/60 dark:bg-neutral-800 text-neutral-800 dark:text-neutral-200 px-2 py-1 rounded-md">
                              {major.code}
                            </span>
                          </td>
                          <td className="px-6 py-4">
                            <span className="text-sm font-medium text-foreground">{major.name}</span>
                          </td>
                          <td className="px-6 py-4 text-right">
                            <div className="flex items-center justify-end gap-2">
                              <button
                                onClick={() => handleOpenMajorEdit(major)}
                                className="p-1.5 text-muted-foreground hover:text-foreground rounded-lg hover:bg-muted transition-colors"
                                title="Chỉnh sửa"
                              >
                                <Edit2 size={15} />
                              </button>
                              <button
                                onClick={() => handleMajorDeleteClick(major)}
                                className="p-1.5 text-muted-foreground hover:text-destructive rounded-lg hover:bg-destructive/10 transition-colors"
                                title="Xóa"
                              >
                                <Trash2 size={15} />
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Pagination */}
                <div className="flex flex-col sm:flex-row items-center justify-between px-6 py-4 border-t border-border gap-4 bg-muted/10">
                  <span className="text-xs text-muted-foreground">
                    Hiển thị {(majorPage - 1) * majorPageSize + 1} -{' '}
                    {Math.min(majorPage * majorPageSize, majorsData?.data?.total || 0)} trong tổng số{' '}
                    {majorsData?.data?.total || 0} chuyên ngành
                  </span>
                  <div className="flex items-center gap-1.5">
                    <button
                      onClick={() => setMajorPage((p) => Math.max(1, p - 1))}
                      disabled={majorPage === 1}
                      className="p-1.5 rounded-lg border border-border bg-card text-muted-foreground hover:text-foreground disabled:opacity-50 disabled:pointer-events-none transition-colors"
                    >
                      <ChevronLeft size={16} />
                    </button>
                    {Array.from({ length: majorsData?.data?.totalPages || 0 }, (_, i) => i + 1).map((p) => (
                      <button
                        key={p}
                        onClick={() => setMajorPage(p)}
                        className={`px-3 py-1 rounded-lg text-xs font-medium border transition-colors ${
                          majorPage === p
                            ? 'bg-primary text-primary-foreground border-primary'
                            : 'border-border bg-card text-muted-foreground hover:text-foreground'
                        }`}
                      >
                        {p}
                      </button>
                    ))}
                    <button
                      onClick={() => setMajorPage((p) => Math.min(majorsData?.data?.totalPages || 1, p + 1))}
                      disabled={majorPage === (majorsData?.data?.totalPages || 1)}
                      className="p-1.5 rounded-lg border border-border bg-card text-muted-foreground hover:text-foreground disabled:opacity-50 disabled:pointer-events-none transition-colors"
                    >
                      <ChevronRight size={16} />
                    </button>
                  </div>
                </div>
              </>
            )}
          </div>
        </div>
      )}

      {/* SKILL ADD / EDIT DIALOG */}
      {isSkillModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 backdrop-blur-xs animate-in fade-in duration-200">
          <div className="bg-card border border-border w-full max-w-md rounded-2xl p-6 shadow-xl space-y-4 animate-in zoom-in-95 duration-200">
            <div className="flex justify-between items-center border-b border-border/80 pb-3">
              <h3 className="text-lg font-bold text-foreground">
                {skillModalMode === 'create' ? 'Thêm kỹ năng mới' : 'Chỉnh sửa kỹ năng'}
              </h3>
              <button
                onClick={() => setIsSkillModalOpen(false)}
                className="text-muted-foreground hover:text-foreground p-1 rounded-lg hover:bg-muted"
              >
                <X size={18} />
              </button>
            </div>

            <form onSubmit={handleSkillSubmit} className="space-y-4">
              {skillFormError && (
                <div className="p-3 bg-destructive/10 text-destructive text-xs font-medium rounded-xl border border-destructive/20 flex items-center gap-2">
                  <XCircle size={16} className="shrink-0" />
                  <span>{skillFormError}</span>
                </div>
              )}

              <div className="space-y-1.5">
                <label className="text-xs font-bold text-foreground">Tên kỹ năng</label>
                <input
                  type="text"
                  placeholder="Nhập tên kỹ năng (ví dụ: React, .NET Core...)"
                  value={skillForm.name}
                  onChange={(e) => setSkillForm({ ...skillForm, name: e.target.value })}
                  className="w-full px-3.5 py-2 rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground/60"
                  required
                />
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-bold text-foreground">Danh mục</label>
                <select
                  value={skillForm.categoryId}
                  onChange={(e) => setSkillForm({ ...skillForm, categoryId: e.target.value })}
                  className="w-full px-3.5 py-2 rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
                  required
                >
                  <option value="" disabled>
                    Chọn danh mục kỹ năng
                  </option>
                  {categoriesData?.data?.map((cat) => (
                    <option key={cat.id} value={cat.id}>
                      {cat.name}
                    </option>
                  ))}
                </select>
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-bold text-foreground">Trạng thái mặc định</label>
                <div className="flex gap-4">
                  <label className="flex items-center gap-2 text-sm font-medium cursor-pointer">
                    <input
                      type="radio"
                      name="skill-status"
                      checked={skillForm.status === 'ACTIVE'}
                      onChange={() => setSkillForm({ ...skillForm, status: 'ACTIVE' })}
                      className="accent-primary"
                    />
                    <span>Hoạt động</span>
                  </label>
                  <label className="flex items-center gap-2 text-sm font-medium cursor-pointer">
                    <input
                      type="radio"
                      name="skill-status"
                      checked={skillForm.status === 'DEACTIVE'}
                      onChange={() => setSkillForm({ ...skillForm, status: 'DEACTIVE' })}
                      className="accent-primary"
                    />
                    <span>Vô hiệu hóa</span>
                  </label>
                </div>
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t border-border/80">
                <button
                  type="button"
                  onClick={() => setIsSkillModalOpen(false)}
                  className="px-4 py-2 border border-border hover:bg-muted text-foreground font-medium text-sm rounded-xl transition-colors"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={createSkillMutation.isPending || updateSkillMutation.isPending}
                  className="px-4 py-2 bg-primary hover:bg-primary/95 text-primary-foreground font-medium text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5 disabled:opacity-50"
                >
                  {(createSkillMutation.isPending || updateSkillMutation.isPending) && (
                    <Loader2 size={14} className="animate-spin" />
                  )}
                  <span>Lưu thay đổi</span>
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* SKILL DELETE CONFIRMATION DIALOG */}
      {isSkillDeleteOpen && skillToDelete && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 backdrop-blur-xs animate-in fade-in duration-200">
          <div className="bg-card border border-border w-full max-w-md rounded-2xl p-6 shadow-xl space-y-4 animate-in zoom-in-95 duration-200">
            <div className="flex items-start gap-3">
              <div className="p-2 rounded-full bg-destructive/10 text-destructive shrink-0">
                <AlertTriangle size={24} />
              </div>
              <div className="space-y-1.5">
                <h3 className="text-base font-bold text-foreground">Bạn muốn xóa kỹ năng này?</h3>
                <p className="text-sm text-muted-foreground">
                  Hành động này sẽ xóa hoàn toàn kỹ năng <strong className="text-foreground">"{skillToDelete.name}"</strong> ra khỏi cơ sở dữ liệu.
                  Nếu kỹ năng này đang liên kết với các thực thể khác, hệ thống sẽ chặn hành động này.
                </p>
              </div>
            </div>

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setIsSkillDeleteOpen(false)}
                className="px-4 py-2 border border-border hover:bg-muted text-foreground font-medium text-sm rounded-xl transition-colors"
              >
                Hủy
              </button>
              <button
                type="button"
                onClick={handleSkillDeleteConfirm}
                disabled={deleteSkillMutation.isPending}
                className="px-4 py-2 bg-destructive text-destructive-foreground hover:bg-destructive/90 font-medium text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5"
              >
                {deleteSkillMutation.isPending && <Loader2 size={14} className="animate-spin" />}
                <span>Xác nhận xóa</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* SKILL FORCE STATUS CONFIRMATION DIALOG */}
      {isForceStatusOpen && forceStatusData && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 backdrop-blur-xs animate-in fade-in duration-200">
          <div className="bg-card border border-border w-full max-w-md rounded-2xl p-6 shadow-xl space-y-4 animate-in zoom-in-95 duration-200">
            <div className="flex items-start gap-3">
              <div className="p-2 rounded-full bg-amber-500/10 text-amber-500 shrink-0">
                <AlertTriangle size={24} />
              </div>
              <div className="space-y-1.5">
                <h3 className="text-base font-bold text-foreground">Yêu cầu buộc đổi trạng thái</h3>
                <p className="text-sm text-muted-foreground leading-relaxed">
                  {forceStatusData.message}
                </p>
                <p className="text-xs text-muted-foreground italic">
                  * Khi vô hiệu hóa, kỹ năng này sẽ ẩn đi và không thể tìm thấy bởi các ứng viên hoặc tin tuyển dụng mới.
                </p>
              </div>
            </div>

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => {
                  setIsForceStatusOpen(false);
                  setForceStatusData(null);
                  refetchSkills(); // Reload state to reset switch
                }}
                className="px-4 py-2 border border-border hover:bg-muted text-foreground font-medium text-sm rounded-xl transition-colors"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                onClick={handleForceSkillStatusSubmit}
                disabled={updateSkillStatusMutation.isPending}
                className="px-4 py-2 bg-amber-500 hover:bg-amber-600 text-white font-medium text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5"
              >
                {updateSkillStatusMutation.isPending && <Loader2 size={14} className="animate-spin" />}
                <span>Đồng ý vô hiệu hóa</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* MAJOR ADD / EDIT DIALOG */}
      {isMajorModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 backdrop-blur-xs animate-in fade-in duration-200">
          <div className="bg-card border border-border w-full max-w-md rounded-2xl p-6 shadow-xl space-y-4 animate-in zoom-in-95 duration-200">
            <div className="flex justify-between items-center border-b border-border/80 pb-3">
              <h3 className="text-lg font-bold text-foreground">
                {majorModalMode === 'create' ? 'Thêm chuyên ngành mới' : 'Chỉnh sửa chuyên ngành'}
              </h3>
              <button
                onClick={() => setIsMajorModalOpen(false)}
                className="text-muted-foreground hover:text-foreground p-1 rounded-lg hover:bg-muted"
              >
                <X size={18} />
              </button>
            </div>

            <form onSubmit={handleMajorSubmit} className="space-y-4">
              {majorFormError && (
                <div className="p-3 bg-destructive/10 text-destructive text-xs font-medium rounded-xl border border-destructive/20 flex flex-col gap-2">
                  <div className="flex items-center gap-2">
                    <XCircle size={16} className="shrink-0" />
                    <span>{majorFormError}</span>
                  </div>
                  {/* Option khôi phục chuyên ngành đã xóa nếu lỗi báo trùng */}
                  {(majorFormError.includes('đã tồn tại') || majorFormError.includes('trùng')) && (
                    <p className="text-xs text-muted-foreground leading-relaxed mt-1">
                      Mẹo: Hãy kiểm tra xem mã hoặc tên chuyên ngành này có bị xóa mềm trước đó hay không. Khôi phục từ hành động Xóa Undo nếu cần.
                    </p>
                  )}
                </div>
              )}

              <div className="space-y-1.5">
                <label className="text-xs font-bold text-foreground">Mã chuyên ngành</label>
                <input
                  type="text"
                  placeholder="Nhập mã chuyên ngành (ví dụ: CNTT, MKT...)"
                  value={majorForm.code}
                  onChange={(e) => setMajorForm({ ...majorForm, code: e.target.value })}
                  className="w-full px-3.5 py-2 rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground/60 uppercase"
                  required
                  disabled={majorModalMode === 'edit'} // Code unique identifier
                />
              </div>

              <div className="space-y-1.5">
                <label className="text-xs font-bold text-foreground">Tên chuyên ngành</label>
                <input
                  type="text"
                  placeholder="Nhập tên chuyên ngành đầy đủ..."
                  value={majorForm.name}
                  onChange={(e) => setMajorForm({ ...majorForm, name: e.target.value })}
                  className="w-full px-3.5 py-2 rounded-xl border border-input bg-background/50 text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all placeholder:text-muted-foreground/60"
                  required
                />
              </div>

              <div className="flex justify-end gap-3 pt-3 border-t border-border/80">
                <button
                  type="button"
                  onClick={() => setIsMajorModalOpen(false)}
                  className="px-4 py-2 border border-border hover:bg-muted text-foreground font-medium text-sm rounded-xl transition-colors"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={createMajorMutation.isPending || updateMajorMutation.isPending}
                  className="px-4 py-2 bg-primary hover:bg-primary/95 text-primary-foreground font-medium text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5 disabled:opacity-50"
                >
                  {(createMajorMutation.isPending || updateMajorMutation.isPending) && (
                    <Loader2 size={14} className="animate-spin" />
                  )}
                  <span>Lưu thay đổi</span>
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MAJOR DELETE CONFIRMATION DIALOG */}
      {isMajorDeleteOpen && majorToDelete && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/55 backdrop-blur-xs animate-in fade-in duration-200">
          <div className="bg-card border border-border w-full max-w-md rounded-2xl p-6 shadow-xl space-y-4 animate-in zoom-in-95 duration-200">
            <div className="flex items-start gap-3">
              <div className="p-2 rounded-full bg-destructive/10 text-destructive shrink-0">
                <AlertTriangle size={24} />
              </div>
              <div className="space-y-1.5">
                <h3 className="text-base font-bold text-foreground">Bạn muốn xóa chuyên ngành này?</h3>
                <p className="text-sm text-muted-foreground">
                  Hành động này sẽ thực hiện <strong className="text-foreground">xóa mềm</strong> chuyên ngành <strong className="text-foreground">"{majorToDelete.name}"</strong>.
                  Hệ thống vẫn giữ lại bản ghi để duy trì tính toàn vẹn dữ liệu cho các ứng viên đã đăng ký ngành này. Bạn có thể khôi phục lại chuyên ngành này sau.
                </p>
              </div>
            </div>

            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={() => setIsMajorDeleteOpen(false)}
                className="px-4 py-2 border border-border hover:bg-muted text-foreground font-medium text-sm rounded-xl transition-colors"
              >
                Hủy
              </button>
              <button
                type="button"
                onClick={handleMajorDeleteConfirm}
                disabled={deleteMajorMutation.isPending}
                className="px-4 py-2 bg-destructive text-destructive-foreground hover:bg-destructive/90 font-medium text-sm rounded-xl shadow-xs transition-colors flex items-center gap-1.5"
              >
                {deleteMajorMutation.isPending && <Loader2 size={14} className="animate-spin" />}
                <span>Xác nhận xóa</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* TOAST SYSTEM W/ UNDO */}
      {toast && (
        <div className="fixed bottom-6 right-6 z-50 animate-in slide-in-from-bottom-5 fade-in duration-300">
          <div className={`flex items-center gap-3 px-4 py-3 rounded-2xl shadow-lg border text-sm font-medium ${
            toast.type === 'success' ? 'bg-emerald-500/10 text-emerald-500 border-emerald-500/25' :
            toast.type === 'warning' ? 'bg-amber-500/10 text-amber-500 border-amber-500/25' :
            'bg-destructive/10 text-destructive border-destructive/25'
          }`}>
            <CheckCircle size={18} className="shrink-0" />
            <div className="flex-1">
              <span>{toast.message}</span>
              {toast.undoAction && (
                <button
                  onClick={() => {
                    toast.undoAction?.();
                    setToast(null);
                  }}
                  className="ml-3 inline-flex items-center gap-1 text-xs font-bold underline hover:no-underline hover:opacity-90"
                >
                  <RotateCcw size={12} />
                  <span>Khôi phục</span>
                </button>
              )}
            </div>
            <button
              onClick={() => setToast(null)}
              className="text-muted-foreground hover:text-foreground shrink-0 p-0.5 rounded-lg hover:bg-black/5"
            >
              <X size={14} />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
