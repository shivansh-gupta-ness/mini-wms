import { Routes } from '@angular/router';

const routes: Routes = [
  {
    path: '',
    title: 'Sign in',
    loadComponent: () => import('./pages/auth-placeholder.page').then((m) => m.AuthPlaceholderPage),
  },
];

export default routes;
