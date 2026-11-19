import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GENERIC_ERROR_MESSAGE } from '../../../core/api/api-error';
import { authInterceptor } from '../../../core/auth/auth.interceptor';
import { ErrorNotifier } from '../../../core/errors/error-notifier';
import { errorInterceptor } from '../../../core/errors/error.interceptor';
import { Bin, Page, Zone } from '../data/location.models';
import { DeactivateTarget } from './location-actions';
import { BinList } from './bin-list';

const BINS_URL = '/api/v1/bins';

const ZONES: Zone[] = [
  { id: 1, warehouseId: 1, code: 'A1', name: 'Aisle one', type: 'Storage', isActive: true, binCount: 2, version: 1 },
  { id: 2, warehouseId: 1, code: 'S1', name: 'Shipping', type: 'Shipping', isActive: true, binCount: 0, version: 1 },
];

function bin(id: number, code: string, isActive = true): Bin {
  return { id, zoneId: 1, zoneCode: 'A1', code, name: `Bin ${code}`, path: `A1/${code}`, isActive, version: 1 };
}

function page(items: Bin[], totalItems = items.length): Page<Bin> {
  return { items, page: 1, pageSize: 25, totalItems };
}

interface Setup {
  fixture: ComponentFixture<BinList>;
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
  const fixture = TestBed.createComponent(BinList);
  fixture.componentRef.setInput('zones', ZONES);
  fixture.detectChanges();
  return { fixture, el: fixture.nativeElement as HTMLElement, ctrl: TestBed.inject(HttpTestingController) };
}

/** Expects exactly one GET /bins; flushes it with the given items when `items` is set. */
function expectBins(ctrl: HttpTestingController, flush?: Page<Bin>): TestRequest {
  const req = ctrl.expectOne((r) => r.url === BINS_URL);
  expect(req.request.method).toBe('GET');
  if (flush) {
    req.flush(flush);
  }
  return req;
}

async function settle(fixture: ComponentFixture<BinList>): Promise<void> {
  fixture.detectChanges();
  await vi.advanceTimersByTimeAsync(0);
  fixture.detectChanges();
}

/** Creates the list and answers the first request. */
async function ready(first: Page<Bin> = page([bin(1, 'B1'), bin(2, 'B2', false)])): Promise<Setup> {
  const s = setup();
  expectBins(s.ctrl, first);
  await settle(s.fixture);
  return s;
}

function header(el: HTMLElement, label: string): HTMLElement {
  const found = Array.from(el.querySelectorAll<HTMLElement>('th')).find((th) => th.textContent?.trim() === label);
  if (!found) {
    throw new Error(`No header ${label}`);
  }
  return found;
}

/** Lets the debounce elapse and runs the effects that follow. */
async function debounced(s: Setup): Promise<void> {
  await vi.advanceTimersByTimeAsync(300);
  await settle(s.fixture);
}

async function type(s: Setup, value: string): Promise<void> {
  const input = s.el.querySelector('input[type="search"]') as HTMLInputElement;
  input.value = value;
  input.dispatchEvent(new Event('input'));
  await settle(s.fixture);
}

function overlayOptions(): HTMLElement[] {
  return Array.from(document.querySelectorAll<HTMLElement>('mat-option'));
}

async function pickOption(s: Setup, trigger: HTMLElement, label: string): Promise<void> {
  trigger.click();
  await settle(s.fixture);
  const option = overlayOptions().find((o) => o.textContent?.trim().startsWith(label));
  if (!option) {
    throw new Error(`No option ${label}`);
  }
  option.click();
  await settle(s.fixture);
}

function zoneTrigger(el: HTMLElement): HTMLElement {
  return el.querySelector('mat-select') as HTMLElement;
}

function pageSizeTrigger(el: HTMLElement): HTMLElement {
  return el.querySelector('mat-paginator mat-select') as HTMLElement;
}

describe('FEAT-001.19 bin list: columns and rows', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('FEAT-001.19 the table has the columns Code, Name, Zone and Status (plus Actions)', async () => {
    const { el } = await ready();
    const headers = Array.from(el.querySelectorAll('th')).map((th) => th.textContent?.trim());
    expect(headers).toEqual(['Code', 'Name', 'Zone', 'Status', 'Actions']);
  });

  it('FEAT-001.19 rows show code, name, zone code and a status chip with text', async () => {
    const { el } = await ready();
    const rows = Array.from(el.querySelectorAll('tr.mat-mdc-row'));
    expect(rows.length).toBe(2);
    const cells = Array.from(rows[0].querySelectorAll('td')).map((td) => td.textContent?.trim());
    expect(cells.slice(0, 3)).toEqual(['B1', 'Bin B1', 'A1']);
    expect(rows[0].querySelector('app-status-chip')?.textContent?.trim()).toBe('Active');
    expect(rows[1].querySelector('app-status-chip')?.textContent?.trim()).toBe('Inactive');
  });

  it('FEAT-001.19 the first request sends page 1 and pageSize 25 and nothing else', () => {
    vi.useFakeTimers();
    const { ctrl } = setup();
    const req = expectBins(ctrl);
    expect(req.request.params.keys().sort()).toEqual(['page', 'pageSize']);
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('25');
    expect(req.request.params.has('sort')).toBe(false);
    expect(req.request.params.has('q')).toBe(false);
    expect(req.request.params.has('zoneId')).toBe(false);
    req.flush(page([]));
  });

  it('FEAT-001.19 the row buttons emit the bin and the deactivate target', async () => {
    const s = await ready();
    const edit = vi.fn();
    const deactivate = vi.fn<(t: DeactivateTarget) => void>();
    s.fixture.componentInstance.editBinRequested.subscribe(edit);
    s.fixture.componentInstance.deactivateRequested.subscribe(deactivate);
    s.el.querySelector<HTMLButtonElement>('button[aria-label="Edit bin B1"]')?.click();
    s.el.querySelector<HTMLButtonElement>('button[aria-label="Deactivate bin B2"]')?.click();
    expect(edit).toHaveBeenCalledWith(bin(1, 'B1'));
    expect(deactivate).toHaveBeenCalledWith({ kind: 'bin', bin: bin(2, 'B2', false) });
  });
});

describe('FEAT-001.19 bin list: server-side sort', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('FEAT-001.19 clicking Code sends sort=code, then sort=-code, then no sort', async () => {
    const s = await ready();
    header(s.el, 'Code').click();
    await settle(s.fixture);
    const asc = expectBins(s.ctrl, page([]));
    expect(asc.request.params.get('sort')).toBe('code');
    header(s.el, 'Code').click();
    await settle(s.fixture);
    const desc = expectBins(s.ctrl, page([]));
    expect(desc.request.params.get('sort')).toBe('-code');
    header(s.el, 'Code').click();
    await settle(s.fixture);
    const cleared = expectBins(s.ctrl, page([]));
    expect(cleared.request.params.has('sort')).toBe(false);
  });

  it('FEAT-001.19 the sortable fields are code, name, zone and isActive', async () => {
    const s = await ready();
    for (const [label, field] of [
      ['Name', 'name'],
      ['Zone', 'zone'],
      ['Status', 'isActive'],
    ]) {
      header(s.el, label).click();
      await settle(s.fixture);
      expect(expectBins(s.ctrl, page([])).request.params.get('sort')).toBe(field);
    }
  });

  it('FEAT-001.19 the Actions column is not sortable', async () => {
    const { el } = await ready();
    expect(header(el, 'Actions').classList.contains('mat-sort-header')).toBe(false);
  });
});

describe('FEAT-001.19 bin list: search and zone filter', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('FEAT-001.19 typing sends nothing at 299 ms and ONE request with q at 300 ms', async () => {
    const s = await ready();
    await type(s, 'B');
    await vi.advanceTimersByTimeAsync(150);
    await type(s, 'B1');
    await vi.advanceTimersByTimeAsync(299);
    s.ctrl.expectNone((r) => r.url === BINS_URL);
    await vi.advanceTimersByTimeAsync(1);
    s.fixture.detectChanges();
    const req = expectBins(s.ctrl, page([bin(1, 'B1')]));
    expect(req.request.params.get('q')).toBe('B1');
    s.ctrl.verify();
  });

  it('FEAT-001.19 the search text is trimmed and a blank search sends no q', async () => {
    const s = await ready();
    await type(s, '  B1  ');
    await debounced(s);
    expect(expectBins(s.ctrl, page([])).request.params.get('q')).toBe('B1');
    await type(s, '   ');
    await debounced(s);
    expect(expectBins(s.ctrl, page([])).request.params.has('q')).toBe(false);
  });

  it('FEAT-001.19 the zone filter lists All zones and the active zones and sends zoneId', async () => {
    const s = await ready();
    zoneTrigger(s.el).click();
    await settle(s.fixture);
    expect(overlayOptions().map((o) => o.textContent?.trim())).toEqual(['All zones', 'A1 - Aisle one', 'S1 - Shipping']);
    overlayOptions()[2].click();
    await settle(s.fixture);
    expect(expectBins(s.ctrl, page([])).request.params.get('zoneId')).toBe('2');
  });

  it('FEAT-001.19 choosing All zones again removes zoneId', async () => {
    const s = await ready();
    await pickOption(s, zoneTrigger(s.el), 'A1');
    expectBins(s.ctrl, page([]));
    await pickOption(s, zoneTrigger(s.el), 'All zones');
    expect(expectBins(s.ctrl, page([])).request.params.has('zoneId')).toBe(false);
  });

  it('FEAT-001.19 combined search, zone and sort are all sent together', async () => {
    const s = await ready();
    await pickOption(s, zoneTrigger(s.el), 'A1');
    expectBins(s.ctrl, page([]));
    header(s.el, 'Zone').click();
    await settle(s.fixture);
    expectBins(s.ctrl, page([]));
    await type(s, 'B');
    await debounced(s);
    const params = expectBins(s.ctrl, page([])).request.params;
    expect(params.get('zoneId')).toBe('1');
    expect(params.get('sort')).toBe('zone');
    expect(params.get('q')).toBe('B');
    expect(params.get('page')).toBe('1');
  });
});

describe('FEAT-001.19 bin list: paginator', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  const many = (): Page<Bin> => page([bin(1, 'B1')], 120);

  it('FEAT-001.19 the paginator offers 25, 50 and 100 and starts at 25', async () => {
    const s = await ready(many());
    pageSizeTrigger(s.el).click();
    await settle(s.fixture);
    expect(overlayOptions().map((o) => o.textContent?.trim())).toEqual(['25', '50', '100']);
    expect(pageSizeTrigger(s.el).textContent).toContain('25');
  });

  it('FEAT-001.19 next page sends the 1-based page 2 with pageSize 25', async () => {
    const s = await ready(many());
    s.el.querySelector<HTMLButtonElement>('button.mat-mdc-paginator-navigation-next')?.click();
    await settle(s.fixture);
    const params = expectBins(s.ctrl, many()).request.params;
    expect(params.get('page')).toBe('2');
    expect(params.get('pageSize')).toBe('25');
  });

  it('FEAT-001.19 choosing page size 50 sends pageSize=50', async () => {
    const s = await ready(many());
    await pickOption(s, pageSizeTrigger(s.el), '50');
    const params = expectBins(s.ctrl, many()).request.params;
    expect(params.get('pageSize')).toBe('50');
    expect(params.get('page')).toBe('1');
  });

  it('FEAT-001.19 changing the search resets the page to 1', async () => {
    const s = await ready(many());
    s.el.querySelector<HTMLButtonElement>('button.mat-mdc-paginator-navigation-next')?.click();
    await settle(s.fixture);
    expectBins(s.ctrl, many());
    await type(s, 'x');
    await debounced(s);
    expect(expectBins(s.ctrl, many()).request.params.get('page')).toBe('1');
  });

  it('FEAT-001.19 changing the zone resets the page to 1', async () => {
    const s = await ready(many());
    s.el.querySelector<HTMLButtonElement>('button.mat-mdc-paginator-navigation-next')?.click();
    await settle(s.fixture);
    expectBins(s.ctrl, many());
    await pickOption(s, zoneTrigger(s.el), 'A1');
    expect(expectBins(s.ctrl, many()).request.params.get('page')).toBe('1');
  });

  it('FEAT-001.19 changing the sort resets the page to 1', async () => {
    const s = await ready(many());
    s.el.querySelector<HTMLButtonElement>('button.mat-mdc-paginator-navigation-next')?.click();
    await settle(s.fixture);
    expectBins(s.ctrl, many());
    header(s.el, 'Code').click();
    await settle(s.fixture);
    const params = expectBins(s.ctrl, many()).request.params;
    expect(params.get('page')).toBe('1');
    expect(params.get('sort')).toBe('code');
  });

  it('FEAT-001.19 a new reloadToken fetches again with the same options', async () => {
    const s = await ready(many());
    s.fixture.componentRef.setInput('reloadToken', 1);
    await settle(s.fixture);
    const params = expectBins(s.ctrl, many()).request.params;
    expect(params.get('page')).toBe('1');
    expect(params.get('pageSize')).toBe('25');
  });
});

describe('FEAT-001.24 bin list: loading, empty and error states', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('FEAT-001.24 a progress bar shows until the first response, then goes away', async () => {
    const s = setup();
    const req = expectBins(s.ctrl);
    await settle(s.fixture);
    expect(s.el.querySelector('mat-progress-bar')).not.toBeNull();
    expect(s.el.querySelector('mat-progress-bar')?.getAttribute('aria-label')).toBe('Loading bins');
    req.flush(page([bin(1, 'B1')]));
    await settle(s.fixture);
    expect(s.el.querySelector('mat-progress-bar')).toBeNull();
  });

  it('FEAT-001.24 no empty state shows while the first request is still running', async () => {
    const s = setup();
    expectBins(s.ctrl);
    await settle(s.fixture);
    expect(s.el.textContent).not.toContain('No bins yet');
  });

  it('FEAT-001.24 an empty list without filters shows "No bins yet" and how to add one', async () => {
    const { el } = await ready(page([]));
    expect(el.textContent).toContain('No bins yet');
    expect(el.textContent).toContain('add bins to it from the Tree tab');
    expect(el.textContent).not.toContain('No bins match');
  });

  it('FEAT-001.24 an empty search result shows "No bins match your search or zone filter."', async () => {
    const s = await ready();
    await type(s, 'zzz');
    await debounced(s);
    expectBins(s.ctrl, page([]));
    await settle(s.fixture);
    expect(s.el.textContent).toContain('No bins match your search or zone filter.');
    expect(s.el.textContent).not.toContain('No bins yet');
  });

  it('FEAT-001.24 an empty result for a zone filter also shows the no-match text', async () => {
    const s = await ready();
    await pickOption(s, zoneTrigger(s.el), 'S1');
    expectBins(s.ctrl, page([]));
    await settle(s.fixture);
    expect(s.el.textContent).toContain('No bins match your search or zone filter.');
  });

  it('FEAT-001.24 a 500 shows a generic banner with no technical detail', async () => {
    const s = setup();
    expectBins(s.ctrl).flush(
      { title: 'Internal Server Error', detail: 'NullReferenceException at Foo', traceId: 'trace-123' },
      { status: 500, statusText: 'Server Error' },
    );
    await settle(s.fixture);
    const banner = s.el.querySelector('app-error-banner [role="alert"]');
    expect(banner?.textContent).toContain(GENERIC_ERROR_MESSAGE);
    expect(banner?.textContent).not.toContain('NullReference');
    expect(banner?.textContent).not.toContain('trace-123');
    expect(s.el.querySelector('mat-progress-bar')).toBeNull();
    expect(s.el.textContent).not.toContain('No bins yet');
  });

  it('FEAT-001.24 a network error shows the generic banner', async () => {
    const s = setup();
    expectBins(s.ctrl).error(new ProgressEvent('error'));
    await settle(s.fixture);
    expect(s.el.querySelector('app-error-banner [role="alert"]')?.textContent).toContain(GENERIC_ERROR_MESSAGE);
  });

  it('FEAT-001.24 the banner goes away after the next successful request', async () => {
    const s = setup();
    expectBins(s.ctrl).flush({}, { status: 500, statusText: 'Server Error' });
    await settle(s.fixture);
    s.fixture.componentRef.setInput('reloadToken', 1);
    await settle(s.fixture);
    expectBins(s.ctrl, page([bin(1, 'B1')]));
    await settle(s.fixture);
    expect(s.el.querySelector('app-error-banner [role="alert"]')).toBeNull();
    expect(s.el.querySelectorAll('tr.mat-mdc-row').length).toBe(1);
  });
});

describe('FEAT-001.25 bin list: accessibility', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('FEAT-001.25 the search input and the zone select are labelled', async () => {
    const { el } = await ready();
    const labels = Array.from(el.querySelectorAll('mat-label')).map((l) => l.textContent?.trim());
    expect(labels).toEqual(['Search by code or name', 'Zone']);
    const input = el.querySelector('input[type="search"]') as HTMLInputElement;
    const labelFor = el.querySelector(`label[for="${input.id}"]`);
    expect(labelFor?.textContent).toContain('Search by code or name');
    expect(zoneTrigger(el).getAttribute('aria-labelledby')).toBeTruthy();
  });

  it('FEAT-001.25 every button and input has an accessible name', async () => {
    const { el } = await ready();
    const controls = Array.from(el.querySelectorAll<HTMLElement>('button, input'));
    expect(controls.length).toBeGreaterThan(4);
    for (const c of controls) {
      const labelledBy = c.getAttribute('aria-labelledby');
      const named =
        c.getAttribute('aria-label') ||
        c.textContent?.trim() ||
        (c.id && el.querySelector(`label[for="${c.id}"]`)) ||
        (labelledBy && labelledBy.split(' ').some((id) => document.getElementById(id)?.textContent?.trim()));
      expect(named, c.outerHTML.slice(0, 120)).toBeTruthy();
    }
  });

  it('FEAT-001.25 the row buttons are native buttons named with the bin code, and the table and paginator are labelled', async () => {
    const { el } = await ready();
    expect(el.querySelector('button[aria-label="Edit bin B1"]')?.tagName).toBe('BUTTON');
    expect(el.querySelector('button[aria-label="Deactivate bin B2"]')?.tagName).toBe('BUTTON');
    expect(el.querySelector('table')?.getAttribute('aria-label')).toBe('Bins');
    expect(el.querySelector('mat-paginator')?.getAttribute('aria-label')).toBe('Select page of bins');
  });

  it('FEAT-001.25 no interactive item has tabindex -1 and sort headers are keyboard operable', async () => {
    const { el } = await ready();
    // Material's own disabled paginator buttons (first page: previous/first) use tabindex -1 and are skipped.
    const enabled = Array.from(el.querySelectorAll('button, input')).filter((i) => i.getAttribute('aria-disabled') !== 'true');
    expect(enabled.length).toBeGreaterThan(4);
    for (const item of enabled) {
      expect(item.getAttribute('tabindex'), item.outerHTML.slice(0, 200)).not.toBe('-1');
    }
    expect(header(el, 'Code').querySelector('[role="button"], button, .mat-sort-header-container')).not.toBeNull();
  });
});

describe('FEAT-001.26 bin list: calls go through the versioned API', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('FEAT-001.26 the list requests GET /api/v1/bins and nothing else', async () => {
    const s = setup();
    const req = s.ctrl.expectOne((r) => r.url.startsWith('/api/v1/'));
    expect(req.request.url).toBe(BINS_URL);
    req.flush(page([]));
    s.ctrl.verify();
  });
});
