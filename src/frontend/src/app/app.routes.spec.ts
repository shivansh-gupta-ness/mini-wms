import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from './app.routes';

async function open(url: string): Promise<{ el: HTMLElement; router: Router; harness: RouterTestingHarness }> {
  TestBed.configureTestingModule({ providers: [provideRouter(routes)] });
  const harness = await RouterTestingHarness.create();
  await harness.navigateByUrl(url);
  harness.detectChanges();
  return { el: harness.routeNativeElement as HTMLElement, router: TestBed.inject(Router), harness };
}

const moduleRoutes: { path: string; heading: string }[] = [
  { path: 'stock', heading: 'Stock' },
  { path: 'receipts', heading: 'Receipts' },
  { path: 'orders', heading: 'Orders' },
  { path: 'products', heading: 'Products' },
  { path: 'locations', heading: 'Locations' },
  { path: 'sign-in', heading: 'Sign in' },
];

describe('TASK-008 lazy module routes (AC 3)', () => {
  for (const { path, heading } of moduleRoutes) {
    it(`TASK-008.3 /${path} renders the placeholder heading "${heading}" inside the shell`, async () => {
      const { el } = await open('/' + path);
      expect(el.querySelector('nav[aria-label="Main navigation"]')).not.toBeNull();
      expect(el.querySelector('main#main-content h1')?.textContent?.trim()).toBe(heading);
    });

    it(`TASK-008.3 route "${path}" is lazy-loaded with loadChildren`, () => {
      const route = (routes[0].children ?? []).find((r) => r.path === path);
      expect(route).toBeDefined();
      expect(typeof route?.loadChildren).toBe('function');
      expect(route?.component).toBeUndefined();
    });
  }

  it('TASK-008.3 the default URL redirects to /stock', async () => {
    const { el, router } = await open('/');
    expect(router.url).toBe('/stock');
    expect(el.querySelector('main#main-content h1')?.textContent?.trim()).toBe('Stock');
  });
});

describe('TASK-008 not-found page (AC 4)', () => {
  it('TASK-008.4 an unknown URL renders "Page not found" inside the shell', async () => {
    const { el } = await open('/does-not-exist');
    expect(el.querySelector('main#main-content h1')?.textContent?.trim()).toBe('Page not found');
    expect(el.querySelector('nav[aria-label="Main navigation"]')).not.toBeNull();
    expect(el.querySelector('[role="banner"]')).not.toBeNull();
  });

  it('TASK-008.4 a nested unknown URL also renders the not-found page', async () => {
    const { el } = await open('/stock/does/not/exist');
    expect(el.querySelector('main#main-content h1')?.textContent?.trim()).toBe('Page not found');
  });

  it('TASK-008.4 the link back to Stock navigates to /stock', async () => {
    const { el, router, harness } = await open('/does-not-exist');
    const link = Array.from(el.querySelectorAll<HTMLAnchorElement>('main a')).find((a) =>
      (a.textContent ?? '').includes('Stock'),
    );
    expect(link).toBeDefined();
    expect(link?.getAttribute('href')).toBe('/stock');
    link?.click();
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(router.url).toBe('/stock');
    expect(el.querySelector('main#main-content h1')?.textContent?.trim()).toBe('Stock');
  });
});
