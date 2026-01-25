import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/comparison/comparison').then((m) => m.Comparison),
  },
  {
    path: 'comparison',
    redirectTo: '',
    pathMatch: 'full',
  },
  {
    path: 'review',
    loadComponent: () => import('./pages/review/review').then((m) => m.Review),
  },
  {
    path: 'review/:id',
    loadComponent: () => import('./pages/review/review').then((m) => m.Review),
  },
];
