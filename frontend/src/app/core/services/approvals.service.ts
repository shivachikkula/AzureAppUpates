import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ChangeRequest } from '../models/domain.models';

@Injectable({ providedIn: 'root' })
export class ApprovalsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/approvals`;

  getPending(): Observable<ChangeRequest[]> {
    return this.http.get<ChangeRequest[]>(`${this.baseUrl}/pending`);
  }

  getMine(): Observable<ChangeRequest[]> {
    return this.http.get<ChangeRequest[]>(`${this.baseUrl}/mine`);
  }

  approve(id: string, note?: string): Observable<ChangeRequest> {
    return this.http.post<ChangeRequest>(`${this.baseUrl}/${id}/approve`, { note });
  }

  reject(id: string, note?: string): Observable<ChangeRequest> {
    return this.http.post<ChangeRequest>(`${this.baseUrl}/${id}/reject`, { note });
  }
}
