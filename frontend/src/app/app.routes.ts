import { Routes } from '@angular/router';
import { HomePage } from './features/home/home-page';
import { HealthPage } from './features/health/health-page';
import { CreatePost } from './features/posts/create-post/create-post';
import { FeedPageComponent } from './features/feed/feed-page';

export const routes: Routes = [
  { path: 'feed', component: FeedPageComponent, title: 'Your feed' },
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
    path: 'posts/create',
    component: CreatePost,
    title: 'Create Post',
  },
  {
    path: '**',
    redirectTo: '',
  },
];
