"use client";

import { useEffect, useState } from "react";
import { useStaffJobs } from "@/hooks/useStaffJobs";
import { useSignalR } from "@/hooks/useSignalR";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, DialogDescription } from "@/components/ui/dialog";
import { format } from "date-fns";
import { Ban, CheckCircle2, Search, RefreshCcw, Eye } from "lucide-react";
import { toast } from "sonner";
import Link from "next/link";
import JobDetailModal from "@/components/jobs/JobDetailModal";

export default function StaffJobPostingsPage() {
  const { data, loading, fetchJobs, banJob, unbanJob } = useStaffJobs();
  const [searchTerm, setSearchTerm] = useState("");
  const connection = useSignalR("/hubs/notification");
  
  // Ban dialog state
  const [isBanDialogOpen, setIsBanDialogOpen] = useState(false);
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null);
  const [banReason, setBanReason] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Detail modal state
  const [isDetailModalOpen, setIsDetailModalOpen] = useState(false);
  const [viewJobId, setViewJobId] = useState<string | null>(null);

  useEffect(() => {
    fetchJobs(1, 20, searchTerm);
  }, [fetchJobs]);

  useEffect(() => {
    if (connection) {
      connection.on("JobCreated", (job) => {
        toast.success(`Bài đăng mới: ${job.title}`);
        fetchJobs(1, 20, searchTerm); // Refresh list
      });
      connection.on("JobStatusChanged", () => {
        fetchJobs(1, 20, searchTerm);
      });
    }
    return () => {
      if (connection) {
        connection.off("JobCreated");
        connection.off("JobStatusChanged");
      }
    };
  }, [connection, fetchJobs, searchTerm]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    fetchJobs(1, 20, searchTerm);
  };

  const openBanDialog = (id: string) => {
    setSelectedJobId(id);
    setBanReason("");
    setIsBanDialogOpen(true);
  };

  const handleBanSubmit = async () => {
    if (!selectedJobId || !banReason.trim()) return;
    setIsSubmitting(true);
    const res = await banJob(selectedJobId, banReason);
    setIsSubmitting(false);
    
    if (res.success) {
      toast.success("Đã khóa bài đăng thành công");
      setIsBanDialogOpen(false);
      fetchJobs(1, 20, searchTerm);
    } else {
      toast.error(res.message || "Không thể khóa bài đăng");
    }
  };

  const handleUnban = async (id: string) => {
    if (confirm("Bạn có chắc chắn muốn mở khóa bài đăng này?")) {
      const res = await unbanJob(id);
      if (res.success) {
        toast.success("Đã mở khóa bài đăng");
        fetchJobs(1, 20, searchTerm);
      } else {
        toast.error(res.message || "Lỗi mở khóa");
      }
    }
  };

  const openDetailModal = (id: string) => {
    setViewJobId(id);
    setIsDetailModalOpen(true);
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold tracking-tight text-foreground">Quản lý bài đăng (Job Postings)</h1>
        <p className="text-sm text-muted-foreground">Theo dõi và hậu kiểm các bài đăng tuyển dụng trên hệ thống.</p>
      </div>

      <div className="flex items-center justify-between gap-4">
        <form onSubmit={handleSearch} className="flex max-w-sm w-full gap-2">
          <div className="relative w-full">
            <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Tìm theo tiêu đề, mã job..."
              className="pl-8"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>
          <Button type="submit" variant="secondary" disabled={loading}>
            Tìm kiếm
          </Button>
        </form>
        <Button variant="outline" onClick={() => fetchJobs(1, 20, searchTerm)} disabled={loading}>
          <RefreshCcw className={`mr-2 h-4 w-4 ${loading ? "animate-spin" : ""}`} />
          Làm mới
        </Button>
      </div>

      <div className="rounded-md border bg-card">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Mã Job</TableHead>
              <TableHead>Tiêu đề</TableHead>
              <TableHead>Ngày đăng</TableHead>
              <TableHead>Trạng thái</TableHead>
              <TableHead>Hậu kiểm</TableHead>
              <TableHead className="text-right">Thao tác</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && data.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="text-center h-32 text-muted-foreground">
                  Đang tải dữ liệu...
                </TableCell>
              </TableRow>
            ) : data.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="text-center h-32 text-muted-foreground">
                  Không tìm thấy bài đăng nào
                </TableCell>
              </TableRow>
            ) : (
              data.items.map((job) => (
                <TableRow key={job.id}>
                  <TableCell className="font-medium text-xs">{job.jobCode}</TableCell>
                  <TableCell>
                    <div className="max-w-[300px] truncate font-medium">{job.title}</div>
                  </TableCell>
                  <TableCell className="text-muted-foreground">
                    {job.publishedAt ? format(new Date(job.publishedAt), "dd/MM/yyyy") : "-"}
                  </TableCell>
                  <TableCell>
                    <Badge variant={job.status === "PUBLISHED" ? "default" : "secondary"}>
                      {job.status}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    {job.isBanned ? (
                      <Badge variant="destructive" className="flex w-max items-center gap-1">
                        <Ban className="h-3 w-3" /> Bị khóa
                      </Badge>
                    ) : (
                      <Badge variant="outline" className="flex w-max items-center gap-1 border-green-500/50 text-green-600">
                        <CheckCircle2 className="h-3 w-3" /> Hợp lệ
                      </Badge>
                    )}
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex justify-end gap-2">
                      <Button 
                        variant="secondary" 
                        size="sm" 
                        className="hover:bg-blue-50 hover:text-blue-600 border-blue-200" 
                        title="Xem chi tiết"
                        onClick={() => openDetailModal(job.id)}
                      >
                        <Eye className="h-4 w-4" />
                      </Button>
                      {job.isBanned ? (
                        <Button 
                          variant="outline" 
                          size="sm" 
                          onClick={() => handleUnban(job.id)}
                          className="hover:bg-green-50 hover:text-green-600 border-green-200"
                        >
                          Mở khóa
                        </Button>
                      ) : (
                        <Button 
                          variant="destructive" 
                          size="sm" 
                          onClick={() => openBanDialog(job.id)}
                        >
                          Khóa tin
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      <Dialog open={isBanDialogOpen} onOpenChange={setIsBanDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Khóa bài đăng</DialogTitle>
            <DialogDescription>
              Vui lòng nhập lý do khóa bài đăng. Lý do này sẽ được gửi thông báo đến Recruiter.
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <Input 
              placeholder="Lý do khóa (VD: Vi phạm nội dung, Spam...)" 
              value={banReason}
              onChange={(e) => setBanReason(e.target.value)}
              autoFocus
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsBanDialogOpen(false)}>Hủy</Button>
            <Button variant="destructive" onClick={handleBanSubmit} disabled={isSubmitting || !banReason.trim()}>
              {isSubmitting ? "Đang khóa..." : "Xác nhận khóa"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <JobDetailModal
        isOpen={isDetailModalOpen}
        onClose={() => setIsDetailModalOpen(false)}
        jobId={viewJobId || undefined}
        isCandidateMode={false} // Staff doesn't need to see "Apply" button
      />
    </div>
  );
}
