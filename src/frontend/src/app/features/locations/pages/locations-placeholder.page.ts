import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-locations-placeholder',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<h1>Locations</h1>',
})
export class LocationsPlaceholderPage {}
