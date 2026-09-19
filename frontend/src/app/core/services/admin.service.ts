import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AppAssignmentAdmin,
  ApplicationAdmin,
  ApplicationUpsert,
  CreateAppAssignment,
  CreateManagerAssignment,
  CreateTeam,
  ManagerAssignment,
  Team,
} from '../models/domain.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/admin`;

  getTeams(): Observable<Team[]> {
    return this.http.get<Team[]>(`${this.baseUrl}/teams`);
  }

  createTeam(dto: CreateTeam): Observable<Team> {
    return this.http.post<Team>(`${this.baseUrl}/teams`, dto);
  }

  getApplications(): Observable<ApplicationAdmin[]> {
    return this.http.get<ApplicationAdmin[]>(`${this.baseUrl}/applications`);
  }

  createApplication(dto: ApplicationUpsert): Observable<ApplicationAdmin> {
    return this.http.post<ApplicationAdmin>(`${this.baseUrl}/applications`, dto);
  }

  updateApplication(id: string, dto: ApplicationUpsert): Observable<ApplicationAdmin> {
    return this.http.put<ApplicationAdmin>(`${this.baseUrl}/applications/${id}`, dto);
  }

  deleteApplication(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/applications/${id}`);
  }

  getAppAssignments(applicationId?: string): Observable<AppAssignmentAdmin[]> {
    return this.http.get<AppAssignmentAdmin[]>(`${this.baseUrl}/assignments`, {
      params: applicationId ? { applicationId } : undefined,
    });
  }

  createAppAssignment(dto: CreateAppAssignment): Observable<AppAssignmentAdmin> {
    return this.http.post<AppAssignmentAdmin>(`${this.baseUrl}/assignments`, dto);
  }

  deleteAppAssignment(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/assignments/${id}`);
  }

  getManagerAssignments(teamId?: string): Observable<ManagerAssignment[]> {
    return this.http.get<ManagerAssignment[]>(`${this.baseUrl}/manager-assignments`, {
      params: teamId ? { teamId } : undefined,
    });
  }

  createManagerAssignment(dto: CreateManagerAssignment): Observable<ManagerAssignment> {
    return this.http.post<ManagerAssignment>(`${this.baseUrl}/manager-assignments`, dto);
  }

  deleteManagerAssignment(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/manager-assignments/${id}`);
  }
}
