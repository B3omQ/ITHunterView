import { Button } from '@/components/ui/button';
import { ChevronLeft, ChevronRight } from 'lucide-react';

interface ListPaginationProps {
  page: number;
  totalPages: number;
  setPage: (page: number | ((p: number) => number)) => void;
}

export function ListPagination({ page, totalPages, setPage }: ListPaginationProps) {
  if (totalPages <= 1) return null;

  return (
    <div className="flex items-center justify-center gap-2 pt-6">
      <Button variant="outline" size="icon"
        onClick={() => setPage(p => Math.max(1, p - 1))}
        disabled={page === 1}
        className="h-9 w-9 rounded-lg border-border hover:bg-muted">
        <ChevronLeft className="h-4 w-4" />
      </Button>

      {/* Page numbers */}
      {totalPages <= 7 && (
        <div className="flex items-center gap-1">
          {Array.from({ length: totalPages }, (_, i) => i + 1).map(p => (
            <Button key={p} size="sm"
              variant={page === p ? "default" : "outline"}
              onClick={() => setPage(p)}
              className={`h-9 w-9 rounded-lg transition-colors ${
                page === p
                  ? "bg-primary text-primary-foreground font-bold"
                  : "border-border hover:bg-muted text-muted-foreground"
              }`}
            >
              {p}
            </Button>
          ))}
        </div>
      )}

      {/* Text fallback for many pages */}
      {totalPages > 7 && (
        <span className="text-sm font-medium text-muted-foreground">
          Page {page} of {totalPages}
        </span>
      )}

      <Button variant="outline" size="icon"
        onClick={() => setPage(p => Math.min(totalPages, p + 1))}
        disabled={page === totalPages}
        className="h-9 w-9 rounded-lg border-border hover:bg-muted">
        <ChevronRight className="h-4 w-4" />
      </Button>
    </div>
  );
}
