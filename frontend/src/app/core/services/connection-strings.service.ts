import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ConnectionStringUpdateRequest, SaveConnectionStringResult } from '../models/domain.models';

@Injectable({ providedIn: 'root' })
export class ConnectionStringsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/connection-strings`;

  save(request: ConnectionStringUpdateRequest): Observable<SaveConnectionStringResult> {
    return this.http.post<SaveConnectionStringResult>(this.baseUrl, request);
  }
}
