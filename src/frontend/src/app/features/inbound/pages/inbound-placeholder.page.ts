import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-inbound-placeholder',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<h1>Receipts</h1>',
})
export class InboundPlaceholderPage {}
