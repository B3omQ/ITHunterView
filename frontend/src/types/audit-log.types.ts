export interface AuditLogDto {
  id: string;
  userId: string | null;
  actorRole: string;
  actionCategory: string;
  actorEmail: string;
  action: string;
  status: string;
  ipAddress: string;
  userAgent: string;
  createdAt: string;
  tableName: string | null;
  operationType: string | null;
  snapshotDiff: string | null;
}
