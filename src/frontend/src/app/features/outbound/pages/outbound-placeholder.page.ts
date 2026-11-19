import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-outbound-placeholder',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<h1>Orders</h1>',
})
export class OutboundPlaceholderPage {}
