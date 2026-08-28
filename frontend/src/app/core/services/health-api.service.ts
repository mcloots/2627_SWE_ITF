import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { appEnv } from '../config/app-env';
import { HealthResponse } from '../models/health-response.model';

@Injectable({
  providedIn: 'root'
})
export class HealthApiService {
  private readonly http = inject(HttpClient);

  getHealth(): Observable<HealthResponse> {
    return this.http.get<HealthResponse>(`${appEnv.apiBaseUrl}/health`);
  }
}