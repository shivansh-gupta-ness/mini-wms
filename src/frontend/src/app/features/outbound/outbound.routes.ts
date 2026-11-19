import { Routes } from '@angular/router';

const routes: Routes = [
  {
    path: '',
    title: 'Orders',
    loadComponent: () => import('./pages/outbound-placeholder.page').then((m) => m.OutboundPlaceholderPage),
  },
];

export default routes;
