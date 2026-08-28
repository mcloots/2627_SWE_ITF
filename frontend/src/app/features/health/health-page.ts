import { DatePipe, JsonPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { PageTitle } from '../../shared/ui/page-title';
import { Card } from '../../shared/ui/card';
import { HealthApiService } from '../../core/services/health-api.service';
import { HealthResponse } from '../../core/models/health-response.model';

type LoadState = 'loading' | 'loaded' | 'error';

@Component({
  selector: 'app-health-page',
  imports: [DatePipe, JsonPipe, PageTitle, Card],
  templateUrl: './health-page.html',
})
export class HealthPage {
  private readonly healthApi = inject(HealthApiService);
  
  readonly state = signal<LoadState>('loading');
  readonly health = signal<HealthResponse | null>(null);
  readonly error = signal<string | null>(null);

  readonly isLoading = computed(() => this.state() === 'loading');
  readonly isError = computed(() => this.state() === 'error');
  readonly isLoaded = computed(() => this.state() === 'loaded');

  constructor() {
    this.loadHealth();
  }

  private loadHealth(): void {
    this.state.set('loading');
    this.error.set(null);

    this.healthApi.getHealth().subscribe({
      next: (response) => {
        this.health.set(response);
        this.state.set('loaded');
      },
      error: (error: unknown) => {
        this.health.set(null);
        this.error.set(error instanceof Error ? error.message : 'Unable to reach the API.');
        this.state.set('error');
      },
    });
  }
}
