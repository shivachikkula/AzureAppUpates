import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AzureApplication } from '../models/domain.models';

@Injectable({ providedIn: 'root' })
export class ApplicationsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/applications`;

  getMyApplications(): Observable<AzureApplication[]> {
    return this.http.get<AzureApplication[]>(`${this.baseUrl}/mine`);
  }

  getAllApplications(): Observable<AzureApplication[]> {
    return this.http.get<AzureApplication[]>(this.baseUrl);
  }
}
