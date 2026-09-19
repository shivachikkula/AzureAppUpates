export type AppEnvironment = 'Development' | 'Staging' | 'Production';

export type UserRole = 'Developer' | 'Manager' | 'Admin';

export interface AzureApplication {
  id: string;
  name: string;
  resourceGroup: string;
  subscriptionId: string;
  environment: AppEnvironment;
  defaultHostName: string;
}

export interface ConnectionStringUpdateRequest {
  applicationId: string;
  key: string;
  value: string;
  type: 'SQLAzure' | 'Custom' | 'PostgreSQL' | 'MySql';
  reason?: string;
}

export type ChangeRequestStatus = 'PendingApproval' | 'Approved' | 'Rejected' | 'Applied' | 'Failed';

export interface ChangeRequest {
  id: string;
  applicationId: string;
  applicationName: string;
  environment: AppEnvironment;
  requestedByName: string;
  requestedByEmail: string;
  key: string;
  value: string;
  type: string;
  reason?: string;
  status: ChangeRequestStatus;
  createdUtc: string;
  decidedByName?: string;
  decidedUtc?: string;
  decisionNote?: string;
}

export interface SaveConnectionStringResult {
  applied: boolean;
  requiresApproval: boolean;
  changeRequestId?: string;
  message: string;
}

export interface LogEntry {
  timestampUtc: string;
  level: 'Info' | 'Warning' | 'Error' | 'Debug' | 'Trace';
  message: string;
}
