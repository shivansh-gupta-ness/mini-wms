import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-inventory-placeholder',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<h1>Stock</h1>',
})
export class InventoryPlaceholderPage {}
