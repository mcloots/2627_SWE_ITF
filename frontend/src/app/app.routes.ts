import { Routes } from '@angular/router';
import { HomePage } from './features/home/home-page';
import { HealthPage } from './features/health/health-page';

export const routes: Routes = [
  {
    path: '',
    component: HomePage,
    title: 'ITF Pulse',
  },
  {
    path: 'health',
    component: HealthPage,
    title: 'Health',
  },
  {
    path: '**',
    redirectTo: '',
  },
];
