import { Routes } from '@angular/router';
import { NotFoundPage } from './core/layout/not-found.page';
import { Shell } from './core/layout/shell';

export const routes: Routes = [
  {
    path: '',
    component: Shell,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'stock' },
      { path: 'stock', loadChildren: () => import('./features/inventory/inventory.routes') },
      { path: 'receipts', loadChildren: () => import('./features/inbound/inbound.routes') },
      { path: 'orders', loadChildren: () => import('./features/outbound/outbound.routes') },
      { path: 'products', loadChildren: () => import('./features/products/products.routes') },
      { path: 'locations', loadChildren: () => import('./features/locations/locations.routes') },
      { path: 'sign-in', loadChildren: () => import('./features/auth/auth.routes') },
      { path: '**', title: 'Page not found', component: NotFoundPage },
    ],
  },
];
