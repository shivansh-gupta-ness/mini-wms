import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';

async function render(url: string): Promise<HTMLElement> {
  TestBed.configureTestingModule({ providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()] });
  const harness = await RouterTestingHarness.create();
  await harness.navigateByUrl(url);
  harness.detectChanges();
  return harness.routeNativeElement as HTMLElement;
}

const navLinks = (el: HTMLElement): HTMLAnchorElement[] =>
  Array.from(el.querySelectorAll<HTMLAnchorElement>('nav[aria-label="Main navigation"] a'));

describe('TASK-008 shell layout (AC 1)', () => {
  it('TASK-008.1 shows a top bar with banner role, app name and user label', async () => {
    const el = await render('/stock');
    const banner = el.querySelector('[role="banner"]');
    expect(banner).not.toBeNull();
    expect(banner?.textContent).toContain('Mini WMS');
    expect(banner?.textContent).toContain('Dev Admin');
  });

  it('TASK-008.1 shows a left navigation labelled Main navigation', async () => {
    const el = await render('/stock');
    expect(el.querySelector('nav[aria-label="Main navigation"]')).not.toBeNull();
  });

  it('TASK-008.1 shows a content area main#main-content containing the routed page', async () => {
    const el = await render('/stock');
    const main = el.querySelector('main#main-content');
    expect(main).not.toBeNull();
    expect(main?.querySelector('h1')?.textContent).toContain('Stock');
  });
});

describe('TASK-008 navigation items (AC 2)', () => {
  it('TASK-008.2 nav labels are Stock, Receipts, Orders, Products, Locations in order', async () => {
    const el = await render('/stock');
    const labels = navLinks(el).map((a) => (a.textContent ?? '').trim());
    expect(labels).toEqual(['Stock', 'Receipts', 'Orders', 'Products', 'Locations']);
  });

  it('TASK-008.2 nav hrefs are /stock /receipts /orders /products /locations', async () => {
    const el = await render('/stock');
    const hrefs = navLinks(el).map((a) => a.getAttribute('href'));
    expect(hrefs).toEqual(['/stock', '/receipts', '/orders', '/products', '/locations']);
  });

  it('TASK-008.2 nav has no Users item and no sign-in item', async () => {
    const el = await render('/stock');
    const text = navLinks(el).map((a) => (a.textContent ?? '').toLowerCase()).join('|');
    const hrefs = navLinks(el).map((a) => a.getAttribute('href') ?? '');
    expect(text).not.toContain('users');
    expect(text).not.toContain('sign');
    expect(hrefs.some((h) => h.includes('sign-in') || h.includes('users'))).toBe(false);
  });

  it('TASK-008.2 every nav link target is a configured route', async () => {
    const el = await render('/stock');
    const shellChildren = routes[0].children ?? [];
    const configured = shellChildren.map((r) => '/' + r.path);
    for (const a of navLinks(el)) {
      expect(configured).toContain(a.getAttribute('href'));
    }
  });
});
