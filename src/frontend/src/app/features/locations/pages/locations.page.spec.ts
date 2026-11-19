import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GENERIC_ERROR_MESSAGE } from '../../../core/api/api-error';
import { authInterceptor } from '../../../core/auth/auth.interceptor';
import { ErrorNotifier } from '../../../core/errors/error-notifier';
import { errorInterceptor } from '../../../core/errors/error.interceptor';
import { Bin, Warehouse, ZoneNode } from '../data/location.models';
import { LocationsPage } from './locations.page';

const API = '/api/v1/';

const WAREHOUSE: Warehouse = { id: 1, code: 'MAIN', name: 'Main warehouse', isActive: true, version: 3 };

function bin(id: number, code: string): Bin {
  return { id, zoneId: 1, zoneCode: 'A1', code, name: `Bin ${code}`, path: `A1/${code}`, isActive: true, version: 1 };
}

const TREE: ZoneNode[] = [
  {
    id: 1,
    warehouseId: 1,
    code: 'A1',
    name: 'Aisle one',
    type: 'Storage',
    isActive: true,
    binCount: 1,
    version: 1,
    bins: [bin(11, 'B1')],
  },
];

interface Setup {
  fixture: ComponentFixture<LocationsPage>;
  el: HTMLElement;
  ctrl: HttpTestingController;
}

function setup(): Setup {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
      provideHttpClientTesting(),
      { provide: ErrorNotifier, useValue: { notify: vi.fn() } },
    ],
  });
  const fixture = TestBed.createComponent(LocationsPage);
  fixture.detectChanges();
  return { fixture, el: fixture.nativeElement as HTMLElement, ctrl: TestBed.inject(HttpTestingController) };
}

function warehouseReq(ctrl: HttpTestingController): TestRequest {
  return ctrl.expectOne((r) => r.url === `${API}warehouse`);
}

function treeReqs(ctrl: HttpTestingController): { all: TestRequest; active: TestRequest } {
  const reqs = ctrl.match((r) => r.url === `${API}locations/tree`);
  expect(reqs.length).toBe(2);
  const active = reqs.find((r) => r.request.params.get('isActive') === 'true');
  const all = reqs.find((r) => !r.request.params.has('isActive'));
  if (!active || !all) {
    throw new Error('Expected one tree request with isActive=true and one without');
  }
  return { all, active };
}

/** Answers the three page requests. */
async function load(s: Setup, tree: ZoneNode[] = TREE): Promise<void> {
  warehouseReq(s.ctrl).flush(WAREHOUSE);
  const { all, active } = treeReqs(s.ctrl);
  all.flush(tree);
  active.flush(tree.filter((z) => z.isActive).map(({ bins: _bins, ...zone }) => zone));
  await settle(s.fixture);
}

async function settle(fixture: ComponentFixture<LocationsPage>): Promise<void> {
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();
}

async function openTab(s: Setup, label: string): Promise<void> {
  tab(s.el, label).click();
  // The tab body attaches its lazy content after Material's own change detection and timers; wait for it.
  for (let i = 0; i < 100 && !s.el.querySelector('app-bin-list'); i++) {
    await settle(s.fixture);
    await new Promise((resolve) => setTimeout(resolve, 20));
  }
  await settle(s.fixture);
}

function button(el: HTMLElement, label: string): HTMLButtonElement | undefined {
  return Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.trim() === label);
}

function tab(el: HTMLElement, label: string): HTMLElement {
  const found = Array.from(el.querySelectorAll<HTMLElement>('[role="tab"]')).find((t) => t.textContent?.trim() === label);
  if (!found) {
    throw new Error(`No tab ${label}`);
  }
  return found;
}

describe('FEAT-001.16 locations page: header', () => {
  it('FEAT-001.16 the page loads the warehouse with GET /api/v1/warehouse', () => {
    const { ctrl } = setup();
    const req = warehouseReq(ctrl);
    expect(req.request.method).toBe('GET');
    ctrl.match(() => true);
  });

  it('FEAT-001.16 the h1 is exactly "Locations" and the warehouse name shows with a Rename button', async () => {
    const s = setup();
    expect(s.el.querySelector('h1')?.textContent).toBe('Locations');
    await load(s);
    expect(s.el.querySelector('h1')?.textContent).toBe('Locations');
    expect(s.el.querySelector('.locations__warehouse')?.textContent).toContain('Main warehouse');
    expect(button(s.el, 'Rename')).toBeDefined();
    expect(button(s.el, 'Rename')?.disabled).toBe(false);
  });

  it('FEAT-001.16 Rename is disabled until the warehouse has loaded', () => {
    const s = setup();
    expect(button(s.el, 'Rename')?.disabled).toBe(true);
    s.ctrl.match(() => true);
  });

  it('FEAT-001.16 there is no control to create or delete a warehouse', async () => {
    const s = setup();
    await load(s);
    const all = (s.el.textContent ?? '').toLowerCase();
    expect(all).not.toContain('new warehouse');
    expect(all).not.toContain('delete warehouse');
    expect(all).not.toContain('create warehouse');
    const labels = Array.from(s.el.querySelectorAll('button')).map((b) => `${b.textContent} ${b.getAttribute('aria-label')}`.toLowerCase());
    expect(labels.some((l) => /warehouse/.test(l) && /(new|create|delete|add)/.test(l))).toBe(false);
    expect(labels.some((l) => l.includes('delete'))).toBe(false);
  });

  it('FEAT-001.16 the header offers a New zone button', async () => {
    const s = setup();
    await load(s);
    expect(button(s.el, 'New zone')).toBeDefined();
  });
});

describe('FEAT-001.24 locations page: loading and errors', () => {
  it('FEAT-001.24 an indeterminate progress bar shows on first load and no tabs yet', () => {
    const s = setup();
    const bar = s.el.querySelector('mat-progress-bar');
    expect(bar).not.toBeNull();
    expect(bar?.getAttribute('mode')).toBe('indeterminate');
    expect(bar?.getAttribute('aria-label')).toBe('Loading locations');
    expect(s.el.querySelector('mat-tab-group')).toBeNull();
    s.ctrl.match(() => true);
  });

  it('FEAT-001.24 the progress bar goes away once all three responses arrive, and not before', async () => {
    const s = setup();
    warehouseReq(s.ctrl).flush(WAREHOUSE);
    const { all, active } = treeReqs(s.ctrl);
    all.flush(TREE);
    await settle(s.fixture);
    expect(s.el.querySelector('mat-progress-bar')).not.toBeNull();
    active.flush([]);
    await settle(s.fixture);
    expect(s.el.querySelector('mat-progress-bar')).toBeNull();
    expect(s.el.querySelector('mat-tab-group')).not.toBeNull();
  });

  it('FEAT-001.24 a 500 shows a generic banner (no detail), a Retry button and hides the progress bar', async () => {
    const s = setup();
    warehouseReq(s.ctrl).flush(
      { title: 'Internal Server Error', detail: 'SqlException: secret', traceId: 'trace-9' },
      { status: 500, statusText: 'Server Error' },
    );
    s.ctrl.match(() => true);
    await settle(s.fixture);
    const banner = s.el.querySelector('app-error-banner [role="alert"]');
    expect(banner?.textContent).toContain(GENERIC_ERROR_MESSAGE);
    expect(banner?.textContent).not.toContain('secret');
    expect(banner?.textContent).not.toContain('trace-9');
    expect(button(s.el, 'Retry')).toBeDefined();
    expect(s.el.querySelector('mat-progress-bar')).toBeNull();
    expect(s.el.querySelector('mat-tab-group')).toBeNull();
  });

  it('FEAT-001.24 a network error shows the generic banner', async () => {
    const s = setup();
    warehouseReq(s.ctrl).error(new ProgressEvent('error'));
    s.ctrl.match(() => true);
    await settle(s.fixture);
    expect(s.el.querySelector('app-error-banner [role="alert"]')?.textContent).toContain(GENERIC_ERROR_MESSAGE);
  });

  it('FEAT-001.24 Retry calls reload(): a second set of GET requests, and success clears the banner', async () => {
    const s = setup();
    warehouseReq(s.ctrl).flush({}, { status: 500, statusText: 'Server Error' });
    s.ctrl.match(() => true);
    await settle(s.fixture);
    button(s.el, 'Retry')?.click();
    await settle(s.fixture);
    expect(s.el.querySelector('app-error-banner [role="alert"]')).toBeNull();
    await load(s);
    expect(s.el.querySelector('app-error-banner [role="alert"]')).toBeNull();
    expect(s.el.querySelector('.locations__warehouse')?.textContent).toContain('Main warehouse');
    s.ctrl.verify();
  });

  it('FEAT-001.24 a 4xx shows the backend message in the banner', async () => {
    const s = setup();
    warehouseReq(s.ctrl).flush({ title: 'Forbidden', detail: 'You may not view this.' }, { status: 403, statusText: 'Forbidden' });
    s.ctrl.match(() => true);
    await settle(s.fixture);
    expect(s.el.querySelector('app-error-banner [role="alert"]')?.textContent).toContain('You may not view this.');
  });
});

describe('FEAT-001.26 locations page: requests and reload', () => {
  it('FEAT-001.26 the page fetches the warehouse, the full tree and the active tree under /api/v1/', () => {
    const { ctrl } = setup();
    expect(warehouseReq(ctrl).request.method).toBe('GET');
    const { all, active } = treeReqs(ctrl);
    expect(all.request.method).toBe('GET');
    expect(all.request.params.keys()).toEqual([]);
    expect(active.request.params.get('isActive')).toBe('true');
    ctrl.verify();
  });

  it('FEAT-001.26 reload() fetches the warehouse and both trees again', async () => {
    const s = setup();
    await load(s);
    s.fixture.componentInstance.reload();
    warehouseReq(s.ctrl).flush({ ...WAREHOUSE, name: 'Renamed warehouse', version: 4 });
    const { all, active } = treeReqs(s.ctrl);
    all.flush(TREE);
    active.flush([]);
    await settle(s.fixture);
    expect(s.el.querySelector('.locations__warehouse')?.textContent).toContain('Renamed warehouse');
    s.ctrl.verify();
  });
});

describe('FEAT-001.18 and FEAT-001.19 locations page: tabs', () => {
  it('FEAT-001.19 the tabs are Tree and List and the Tree tab shows the tree first', async () => {
    const s = setup();
    await load(s);
    const labels = Array.from(s.el.querySelectorAll('[role="tab"]')).map((t) => t.textContent?.trim());
    expect(labels).toEqual(['Tree', 'List']);
    expect(s.el.querySelector('app-location-tree')).not.toBeNull();
    expect(s.el.textContent).toContain('Aisle one');
    expect(s.el.querySelector('app-bin-list')).toBeNull();
    s.ctrl.expectNone((r) => r.url === `${API}bins`);
  });

  it('FEAT-001.19 the List tab is lazy: it requests GET /api/v1/bins only when opened, with the active zones for the filter', async () => {
    const s = setup();
    await load(s);
    await openTab(s, 'List');
    const req = s.ctrl.expectOne((r) => r.url === `${API}bins`);
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('25');
    req.flush({ items: [bin(11, 'B1')], page: 1, pageSize: 25, totalItems: 1 });
    await settle(s.fixture);
    expect(s.el.querySelector('app-bin-list')).not.toBeNull();
    expect(s.el.querySelectorAll('tr.mat-mdc-row').length).toBe(1);
  });

  it('FEAT-001.19 reload() bumps the token so the open List fetches again', async () => {
    const s = setup();
    await load(s);
    await openTab(s, 'List');
    s.ctrl.expectOne((r) => r.url === `${API}bins`).flush({ items: [], page: 1, pageSize: 25, totalItems: 0 });
    await settle(s.fixture);
    s.fixture.componentInstance.reload();
    warehouseReq(s.ctrl).flush(WAREHOUSE);
    const { all, active } = treeReqs(s.ctrl);
    all.flush(TREE);
    active.flush([]);
    await settle(s.fixture);
    s.ctrl.expectOne((r) => r.url === `${API}bins`).flush({ items: [], page: 1, pageSize: 25, totalItems: 0 });
  });
});

describe('FEAT-001.25 locations page: accessibility', () => {
  it('FEAT-001.25 tabs are reachable: both are role=tab and neither has tabindex -1 on the selected one', async () => {
    const s = setup();
    await load(s);
    const tabs = Array.from(s.el.querySelectorAll('[role="tab"]'));
    expect(tabs.length).toBe(2);
    expect(s.el.querySelector('[role="tablist"]')).not.toBeNull();
    const selected = tabs.find((t) => t.getAttribute('aria-selected') === 'true');
    expect(selected?.textContent?.trim()).toBe('Tree');
    expect(selected?.getAttribute('tabindex')).toBe('0');
    expect(s.el.querySelector('[role="tabpanel"]')).not.toBeNull();
  });

  it('FEAT-001.25 the header buttons are native buttons with text names and the progress bar is labelled', async () => {
    const s = setup();
    expect(s.el.querySelector('mat-progress-bar')?.getAttribute('aria-label')).toBeTruthy();
    await load(s);
    for (const label of ['Rename', 'New zone']) {
      expect(button(s.el, label)?.tagName).toBe('BUTTON');
      expect(button(s.el, label)?.getAttribute('type')).toBe('button');
    }
  });
});
