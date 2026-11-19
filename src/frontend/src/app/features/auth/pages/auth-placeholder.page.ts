import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-auth-placeholder',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '<h1>Sign in</h1>',
})
export class AuthPlaceholderPage {}
