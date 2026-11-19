import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatListModule } from '@angular/material/list';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ApiStatus } from './api-status';

interface NavItem {
  readonly label: string;
  readonly path: string;
}

@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ApiStatus, MatListModule, MatToolbarModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  protected readonly navItems: readonly NavItem[] = [
    { label: 'Stock', path: '/stock' },
    { label: 'Receipts', path: '/receipts' },
    { label: 'Orders', path: '/orders' },
    { label: 'Products', path: '/products' },
    { label: 'Locations', path: '/locations' },
  ];
  protected readonly userLabel = 'Dev Admin';

  // A plain "#main-content" href would resolve against <base href="/"> and reload the page,
  // so the skip link moves focus itself.
  protected focusMain(event: Event): void {
    event.preventDefault();
    document.getElementById('main-content')?.focus();
  }
}
