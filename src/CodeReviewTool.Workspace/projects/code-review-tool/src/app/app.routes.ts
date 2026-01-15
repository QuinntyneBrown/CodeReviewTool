import { Routes } from '@angular/router';
import { Home } from './pages/home/home';
import { Review } from './pages/review/review';

export const routes: Routes = [
  {
    path: '',
    component: Home
  },
  {
    path: 'review/:id',
    component: Review
  },
  {
    path: '**',
    redirectTo: ''
  }
];
