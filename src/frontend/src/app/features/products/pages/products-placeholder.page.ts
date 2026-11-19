import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-products-placeholder',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<h1>Products</h1>',
})
export class ProductsPlaceholderPage {}
