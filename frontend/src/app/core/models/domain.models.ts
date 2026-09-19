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

export interface Team {
  id: string;
  name: string;
  applicationCount: number;
  managerCount: number;
}

export interface CreateTeam {
  name: string;
}

export interface ApplicationAdmin {
  id: string;
  name: string;
  resourceGroup: string;
  subscriptionId: string;
  environment: AppEnvironment;
  defaultHostName: string;
  teamId: string;
  teamName: string;
}

export interface ApplicationUpsert {
  name: string;
  resourceGroup: string;
  subscriptionId: string;
  environment: AppEnvironment;
  defaultHostName: string;
  teamId: string;
}

export interface AppAssignmentAdmin {
  id: string;
  applicationId: string;
  applicationName: string;
  userObjectId: string;
  userEmail: string;
  userDisplayName: string;
}

export interface CreateAppAssignment {
  applicationId: string;
  userObjectId: string;
  userEmail: string;
  userDisplayName: string;
}

export interface ManagerAssignment {
  id: string;
  teamId: string;
  teamName: string;
  userObjectId: string;
  userEmail: string;
  userDisplayName: string;
}

export interface CreateManagerAssignment {
  teamId: string;
  userObjectId: string;
  userEmail: string;
  userDisplayName: string;
}
