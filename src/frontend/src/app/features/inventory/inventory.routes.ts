import { Routes } from '@angular/router';

const routes: Routes = [
  {
    path: '',
    title: 'Stock',
    loadComponent: () => import('./pages/inventory-placeholder.page').then((m) => m.InventoryPlaceholderPage),
  },
];

export default routes;
