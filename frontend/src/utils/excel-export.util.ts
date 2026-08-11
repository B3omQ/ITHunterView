import * as XLSX from 'xlsx';
import type { MatchHistoryDto } from '@/types/cv.types';
import type { ApplicantDto } from '@/types/job-application.types';
import { getMatchBandLabel, getMatchMethodLabel, getScorePercent } from '@/lib/matching-score';

/**
 * Export Candidate Match Results (from Scan DB / Matching History) to Excel (.xlsx)
 * Structured for HR to forward to Hiring Managers.
 */
export function exportMatchingResultsToExcel(jobTitle: string, matches: MatchHistoryDto[]) {
  if (!matches || matches.length === 0) {
    throw new Error('No matching candidate data to export.');
  }

  const exportData = buildMatchingExportRows(jobTitle, matches);

  const worksheet = XLSX.utils.json_to_sheet(exportData);

  // Set column widths for better readability
  const columnWidths = [
    { wch: 6 },  // STT
    { wch: 30 }, // Tên ứng viên / File CV
    { wch: 32 }, // Vị trí tuyển dụng
    { wch: 22 }, // Điểm phù hợp tổng thể
    { wch: 25 }, // Phương pháp
    { wch: 28 }, // Mức độ phù hợp
    { wch: 15 }, // Trạng thái
    { wch: 45 }, // Link File CV
    { wch: 22 }, // Thời gian
  ];
  worksheet['!cols'] = columnWidths;

  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, worksheet, 'Kết quả Match CV');

  const cleanTitle = (jobTitle || 'Job').replace(/[/\\?%*:|"<>]/g, '_');
  const fileName = `Danh_Sach_Ung_Vien_Phu_Hop_${cleanTitle}_${new Date().toISOString().slice(0, 10)}.xlsx`;

  XLSX.writeFile(workbook, fileName);
}

export function buildMatchingExportRows(jobTitle: string, matches: MatchHistoryDto[]) {
  return matches.map((match, index) => {
    const scorePercent = `${Math.round(getScorePercent(match))}%`;

    const formattedDate = match.updatedAt
      ? new Date(match.updatedAt).toLocaleDateString('vi-VN', {
          day: '2-digit',
          month: '2-digit',
          year: 'numeric',
          hour: '2-digit',
          minute: '2-digit',
        })
      : 'N/A';

    return {
      'STT': index + 1,
      'Tên ứng viên / File CV': match.cvFileName || 'Ứng viên ẩn danh',
      'Vị trí tuyển dụng (JD)': match.jdTitle || jobTitle,
      'Điểm phù hợp tổng thể': scorePercent,
      'Phương pháp Đánh giá': getMatchMethodLabel(match.matchMethod),
      'Mức độ phù hợp': getMatchBandLabel(getScorePercent(match)),
      'Trạng thái': match.status === 'Completed' ? 'Hoàn thành' : match.status,
      'Link File CV': match.fileUrl || 'Chưa có file',
      'Thời gian Đánh giá': formattedDate,
    };
  });
}

/**
 * Export Applicants List (Applied candidates for a Job) to Excel (.xlsx)
 */
export function exportApplicantsToExcel(jobTitle: string, applicants: ApplicantDto[]) {
  if (!applicants || applicants.length === 0) {
    throw new Error('No applicant data to export.');
  }

  const exportData = applicants.map((app, index) => {
    const formattedDate = app.applyDate
      ? new Date(app.applyDate).toLocaleDateString('vi-VN', {
          day: '2-digit',
          month: '2-digit',
          year: 'numeric',
          hour: '2-digit',
          minute: '2-digit',
        })
      : 'N/A';

    return {
      'STT': index + 1,
      'Họ và Tên': app.candidateName || 'Chưa cập nhật',
      'Email liên hệ': app.email || 'N/A',
      'Số điện thoại': app.phone || 'N/A',
      'Trạng thái tuyển dụng': formatApplicationStatus(app.status),
      'Ngày nộp hồ sơ': formattedDate,
      'File CV': app.cvFileName || 'N/A',
      'Link CV': app.cvUrl || 'Chưa đính kèm',
    };
  });

  const worksheet = XLSX.utils.json_to_sheet(exportData);

  const columnWidths = [
    { wch: 6 },  // STT
    { wch: 28 }, // Họ và Tên
    { wch: 28 }, // Email
    { wch: 18 }, // SĐT
    { wch: 22 }, // Trạng thái
    { wch: 20 }, // Ngày nộp
    { wch: 30 }, // File CV
    { wch: 45 }, // Link CV
  ];
  worksheet['!cols'] = columnWidths;

  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, worksheet, 'Danh sách ứng viên');

  const cleanTitle = (jobTitle || 'Job').replace(/[/\\?%*:|"<>]/g, '_');
  const fileName = `Danh_Sach_Ung_Tuyen_${cleanTitle}_${new Date().toISOString().slice(0, 10)}.xlsx`;

  XLSX.writeFile(workbook, fileName);
}

function formatApplicationStatus(status: string): string {
  switch (status?.toUpperCase()) {
    case 'APPLIED': return 'Mới ứng tuyển';
    case 'VIEWED': return 'Đã xem hồ sơ';
    case 'SHORTLISTED': return 'Đã chọn lọc';
    case 'INTERVIEWING': return 'Đang phỏng vấn';
    case 'OFFERED': return 'Đã mời nhận việc';
    case 'HIRED': return 'Đã tuyển dụng';
    case 'REJECTED': return 'Đã từ chối';
    case 'WITHDRAWN': return 'Đã rút hồ sơ';
    default: return status || 'Khác';
  }
}
