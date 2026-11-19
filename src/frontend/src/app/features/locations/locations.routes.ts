import { Routes } from '@angular/router';

const routes: Routes = [
  {
    path: '',
    title: 'Locations',
    loadComponent: () => import('./pages/locations-placeholder.page').then((m) => m.LocationsPlaceholderPage),
  },
];

export default routes;
