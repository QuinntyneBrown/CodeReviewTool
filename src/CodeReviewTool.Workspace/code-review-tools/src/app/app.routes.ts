import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'comparison',
    pathMatch: 'full'
  },
  {
    path: 'comparison',
    loadComponent: () => import('./pages/comparison/comparison').then(m => m.Comparison)
  },
  {
    path: 'review',
    loadComponent: () => import('./pages/review/review').then(m => m.Review)
  }
];
