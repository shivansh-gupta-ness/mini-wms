import { Routes } from '@angular/router';

const routes: Routes = [
  {
    path: '',
    title: 'Receipts',
    loadComponent: () => import('./pages/inbound-placeholder.page').then((m) => m.InboundPlaceholderPage),
  },
];

export default routes;
