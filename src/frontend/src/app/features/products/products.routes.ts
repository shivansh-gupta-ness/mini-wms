import { Routes } from '@angular/router';

const routes: Routes = [
  {
    path: '',
    title: 'Products',
    loadComponent: () => import('./pages/products-placeholder.page').then((m) => m.ProductsPlaceholderPage),
  },
];

export default routes;
