import { FileSearch } from 'lucide-react';
import Image from 'next/image';

interface EmptyStateProps {
  title: string;
  description?: string;
  action?: React.ReactNode;
  icon?: React.ReactNode;
  imageUrl?: string;
  children?: React.ReactNode;
}

export function EmptyState({ title, description, action, icon, imageUrl = '/images/emptyPage.png', children }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center min-h-[400px] gap-4 text-center px-4">
      {icon ? (
        <div className="rounded-full bg-muted p-4 mb-2">
          {icon}
        </div>
      ) : (
        <div className="flex justify-center mb-0 sm:mb-2">
          <Image
            src={imageUrl}
            alt="Empty State"
            width={320}
            height={320}
            className="w-56 sm:w-72 h-auto object-contain opacity-90"
            priority
          />
        </div>
      )}
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
