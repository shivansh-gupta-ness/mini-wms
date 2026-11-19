import { Routes } from '@angular/router';

const routes: Routes = [
  {
    path: '',
    title: 'Locations',
    loadComponent: () => import('./pages/locations.page').then((m) => m.LocationsPage),
  },
];

export default routes;
