import { FileSearch } from 'lucide-react';

interface EmptyStateProps {
  title: string;
  description?: string;
  action?: React.ReactNode;
  icon?: React.ReactNode;
  children?: React.ReactNode;
}

export function EmptyState({ title, description, action, icon, children }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center min-h-[400px] gap-4 text-center px-4">
      <div className="rounded-full bg-muted p-4">
        {icon || <FileSearch className="w-8 h-8 text-muted-foreground" />}
      </div>
      <div className="space-y-1">
        <h3 className="text-base font-semibold text-foreground">{title}</h3>
        {description && (
          <p className="text-sm text-muted-foreground max-w-sm">{description}</p>
        )}
      </div>
      {action && <div>{action}</div>}
      {children && <div>{children}</div>}
    </div>
  );
}
