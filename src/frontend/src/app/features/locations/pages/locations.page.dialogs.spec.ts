import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { ErrorNotifier } from '../../../core/errors/error-notifier';
import { errorInterceptor } from '../../../core/errors/error.interceptor';
import { Bin, Warehouse, ZoneNode } from '../data/location.models';
import { LocationsPage } from './locations.page';

const WAREHOUSE: Warehouse = { id: 1, code: 'WH', name: 'Main', isActive: true, version: 3 };
const BIN: Bin = { id: 9, zoneId: 7, zoneCode: 'A1', code: 'B1', name: 'Bin one', path: 'A1 / B1', isActive: true, version: 5 };
const ZONE: ZoneNode = {
  id: 7,
  warehouseId: 1,
  code: 'A1',
  name: 'Receiving dock',
  type: 'Receiving',
  isActive: true,
  binCount: 1,
  version: 2,
  bins: [BIN],
};
const TREE_URL = '/api/v1/locations/tree';
const ACTIVE_TREE_URL = '/api/v1/locations/tree?isActive=true';

let fixture: ComponentFixture<LocationsPage>;
let ctrl: HttpTestingController;

async function settle(): Promise<void> {
  await TestBed.inject(ApplicationRef).whenStable();
  TestBed.tick();
}

function flushLoad(warehouse: Warehouse = WAREHOUSE, tree: ZoneNode[] = [ZONE]): void {
  ctrl.expectOne('/api/v1/warehouse').flush(warehouse);
  ctrl.expectOne(TREE_URL).flush(tree);
  ctrl.expectOne(ACTIVE_TREE_URL).flush(tree.filter((z) => z.isActive));
}

/** After a dialog closes the page loads everything again (the close animation makes this asynchronous). */
async function expectReload(warehouse: Warehouse = WAREHOUSE, tree: ZoneNode[] = [ZONE]): Promise<void> {
  await vi.waitFor(() => ctrl.expectOne('/api/v1/warehouse').flush(warehouse));
  ctrl.expectOne(TREE_URL).flush(tree);
  ctrl.expectOne(ACTIVE_TREE_URL).flush(tree.filter((z) => z.isActive));
  await settle();
}

async function createPage(): Promise<void> {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([errorInterceptor])),
      provideHttpClientTesting(),
      { provide: ErrorNotifier, useValue: { notify: vi.fn() } },
    ],
  });
  ctrl = TestBed.inject(HttpTestingController);
  fixture = TestBed.createComponent(LocationsPage);
  fixture.detectChanges();
  flushLoad();
  await settle();
}

const host = (): HTMLElement => fixture.nativeElement as HTMLElement;
const pageButton = (label: string): HTMLButtonElement =>
  Array.from(host().querySelectorAll<HTMLButtonElement>('button')).find(
    (b) => b.textContent?.trim() === label || b.getAttribute('aria-label') === label,
  ) as HTMLButtonElement;
const dialogButtons = (): HTMLButtonElement[] =>
  Array.from(document.body.querySelectorAll<HTMLButtonElement>('mat-dialog-actions button'));
const dialogTitle = (): string | undefined => document.body.querySelector('h2')?.textContent?.trim();
const dialogOpen = (): boolean => document.body.querySelector('mat-dialog-container') !== null;
const banner = (): string | undefined => document.body.querySelector('.error-banner__messages')?.textContent?.trim();
const field = (label: string): HTMLElement =>
  Array.from(document.body.querySelectorAll<HTMLElement>('mat-form-field')).find((f) =>
    f.querySelector('mat-label')?.textContent?.trim().startsWith(label),
  ) as HTMLElement;
const submit = (): HTMLButtonElement => document.body.querySelector('button[type="submit"]') as HTMLButtonElement;

async function type(label: string, value: string): Promise<void> {
  const el = field(label).querySelector('input') as HTMLInputElement;
  el.value = value;
  el.dispatchEvent(new Event('input'));
  el.dispatchEvent(new Event('blur'));
  await settle();
}

async function chooseZoneType(option: string): Promise<void> {
  (field('Type').querySelector('.mat-mdc-select-trigger') as HTMLElement).click();
  await settle();
  Array.from(document.body.querySelectorAll<HTMLElement>('mat-option'))
    .find((o) => o.textContent?.trim() === option)
    ?.click();
  await settle();
}

async function click(button: HTMLElement): Promise<void> {
  button.click();
  await settle();
}

describe('FEAT-001 locations page: dialogs and deactivate wiring (AC 17, 20 to 23)', () => {
  beforeEach(() => {
    // jsdom has no layout, so the focus trap would treat every element as hidden.
    vi.spyOn(HTMLElement.prototype, 'getClientRects').mockReturnValue([{}] as unknown as DOMRectList);
  });

  afterEach(() => {
    ctrl.verify();
    TestBed.inject(MatDialog).closeAll();
    vi.restoreAllMocks();
  });

  it('FEAT-001.17 Rename opens the rename dialog and, after saving, the header shows the new name after the reload', async () => {
    await createPage();
    expect(host().textContent).toContain('Warehouse: Main');
    await click(pageButton('Rename'));
    expect(dialogTitle()).toBe('Rename warehouse');
    await type('Name', 'Central');
    submit().click();
    const patch = ctrl.expectOne('/api/v1/warehouse');
    expect(patch.request.method).toBe('PATCH');
    expect(patch.request.body).toStrictEqual({ name: 'Central', version: 3 });
    patch.flush({ ...WAREHOUSE, name: 'Central', version: 4 });
    await expectReload({ ...WAREHOUSE, name: 'Central', version: 4 });
    expect(host().textContent).toContain('Warehouse: Central');
    await vi.waitFor(() => expect(dialogOpen()).toBe(false));
  });

  it('FEAT-001.17 cancelling the rename dialog makes no request and does not reload', async () => {
    await createPage();
    await click(pageButton('Rename'));
    await click(dialogButtons()[0]);
    await vi.waitFor(() => expect(dialogOpen()).toBe(false));
    ctrl.expectNone('/api/v1/warehouse');
  });

  it('FEAT-001.20 New zone opens the zone dialog; saving POSTs the zone, closes and reloads the page', async () => {
    await createPage();
    await click(pageButton('New zone'));
    expect(dialogTitle()).toBe('New zone');
    await type('Code', 'S1');
    await type('Name', 'Storage hall');
    await chooseZoneType('Storage');
    submit().click();
    const post = ctrl.expectOne('/api/v1/zones');
    expect(post.request.body).toStrictEqual({ code: 'S1', name: 'Storage hall', type: 'Storage' });
    post.flush({ ...ZONE, id: 8, code: 'S1', name: 'Storage hall', type: 'Storage', binCount: 0, version: 1 });
    await expectReload(WAREHOUSE, [ZONE, { ...ZONE, id: 8, code: 'S1', name: 'Storage hall', bins: [] }]);
    await vi.waitFor(() => expect(dialogOpen()).toBe(false));
    expect(host().textContent).toContain('Storage hall');
  });

  it('FEAT-001.20 a duplicate code keeps the dialog open and the page does not reload', async () => {
    await createPage();
    await click(pageButton('New zone'));
    await type('Code', 'A1');
    await type('Name', 'Dup');
    await chooseZoneType('Storage');
    submit().click();
    ctrl.expectOne('/api/v1/zones').flush(
      { title: 'Validation failed', status: 400, errors: { code: ['Code A1 is already used.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    await settle();
    expect(field('Code').querySelector('mat-error')?.textContent?.trim()).toBe('Code A1 is already used.');
    expect(dialogOpen()).toBe(true);
    ctrl.expectNone('/api/v1/warehouse');
  });

  it('FEAT-001.21 "New bin in zone A1" opens the bin dialog with that zone preselected; saving POSTs and reloads', async () => {
    await createPage();
    await click(pageButton('New bin in zone A1'));
    expect(dialogTitle()).toBe('New bin');
    expect(field('Zone').querySelector('.mat-mdc-select-value')?.textContent?.trim()).toBe('A1 - Receiving dock');
    await type('Code', 'B2');
    await type('Name', 'Second bin');
    submit().click();
    const post = ctrl.expectOne('/api/v1/bins');
    expect(post.request.body).toStrictEqual({ zoneId: 7, code: 'B2', name: 'Second bin' });
    post.flush({ ...BIN, id: 10, code: 'B2', name: 'Second bin' });
    await expectReload(WAREHOUSE, [{ ...ZONE, bins: [BIN, { ...BIN, id: 10, code: 'B2', name: 'Second bin' }] }]);
    await vi.waitFor(() => expect(dialogOpen()).toBe(false));
    expect(host().textContent).toContain('Second bin');
  });

  it('FEAT-001.22 "Edit zone A1" opens the zone dialog in edit mode, saving PATCHes name and version and reloads', async () => {
    await createPage();
    await click(pageButton('Edit zone A1'));
    expect(dialogTitle()).toBe('Edit zone');
    await type('Name', 'Dock A');
    submit().click();
    const patch = ctrl.expectOne('/api/v1/zones/7');
    expect(patch.request.method).toBe('PATCH');
    expect(patch.request.body).toStrictEqual({ name: 'Dock A', version: 2 });
    patch.flush({ ...ZONE, name: 'Dock A', version: 3 });
    await expectReload(WAREHOUSE, [{ ...ZONE, name: 'Dock A', version: 3 }]);
    await vi.waitFor(() => expect(dialogOpen()).toBe(false));
  });

  it('FEAT-001.22 "Edit bin B1" opens the bin dialog in edit mode, saving PATCHes name and version and reloads', async () => {
    await createPage();
    await click(pageButton('Edit bin B1'));
    expect(dialogTitle()).toBe('Edit bin');
    await type('Name', 'Renamed');
    submit().click();
    const patch = ctrl.expectOne('/api/v1/bins/9');
    expect(patch.request.body).toStrictEqual({ name: 'Renamed', version: 5 });
    patch.flush({ ...BIN, name: 'Renamed', version: 6 });
    await expectReload();
    await vi.waitFor(() => expect(dialogOpen()).toBe(false));
  });

  it('FEAT-001.23 Deactivate zone opens a confirm dialog that names the zone, with Cancel focused', async () => {
    await createPage();
    await click(pageButton('Deactivate zone A1'));
    expect(dialogTitle()).toBe('Deactivate zone A1?');
    expect(dialogButtons().map((b) => b.textContent?.trim())).toEqual(['Cancel', 'Deactivate']);
    await vi.waitFor(() => expect(document.activeElement).toBe(dialogButtons()[0]));
  });

  it('FEAT-001.23 Cancel on the zone confirm makes no request and does not reload', async () => {
    await createPage();
    await click(pageButton('Deactivate zone A1'));
    await click(dialogButtons()[0]);
    await vi.waitFor(() => expect(dialogOpen()).toBe(false));
    ctrl.expectNone('/api/v1/zones/7/deactivate');
    ctrl.expectNone('/api/v1/warehouse');
  });

  it('FEAT-001.23 confirming POSTs /zones/{id}/deactivate with the version; the dialog closes and the page reloads', async () => {
    await createPage();
    await click(pageButton('Deactivate zone A1'));
    await click(dialogButtons()[1]);
    const post = ctrl.expectOne('/api/v1/zones/7/deactivate');
    expect(post.request.method).toBe('POST');
    expect(post.request.body).toStrictEqual({ version: 2 });
    post.flush({ ...ZONE, isActive: false, version: 3 });
    await expectReload(WAREHOUSE, [{ ...ZONE, isActive: false, version: 3 }]);
    await vi.waitFor(() => expect(dialogOpen()).toBe(false));
  });

  it('FEAT-001.23 a 409 (active bins) keeps the zone dialog open, shows the detail and does not reload', async () => {
    await createPage();
    await click(pageButton('Deactivate zone A1'));
    await click(dialogButtons()[1]);
    ctrl.expectOne('/api/v1/zones/7/deactivate').flush(
      { title: 'Conflict', status: 409, detail: 'The zone still has active bins.' },
      { status: 409, statusText: 'Conflict' },
    );
    await settle();
    expect(dialogOpen()).toBe(true);
    expect(banner()).toBe('The zone still has active bins.');
    ctrl.expectNone('/api/v1/warehouse');
  });

  it('FEAT-001.23 Deactivate bin names the bin; confirming POSTs /bins/{id}/deactivate with the version and reloads', async () => {
    await createPage();
    await click(pageButton('Deactivate bin B1'));
    expect(dialogTitle()).toBe('Deactivate bin B1?');
    await click(dialogButtons()[1]);
    const post = ctrl.expectOne('/api/v1/bins/9/deactivate');
    expect(post.request.body).toStrictEqual({ version: 5 });
    post.flush({ ...BIN, isActive: false, version: 6 });
    await expectReload();
    await vi.waitFor(() => expect(dialogOpen()).toBe(false));
  });

  it('FEAT-001.23 a 409 (stock on hand) keeps the bin dialog open and shows the detail', async () => {
    await createPage();
    await click(pageButton('Deactivate bin B1'));
    await click(dialogButtons()[1]);
    ctrl.expectOne('/api/v1/bins/9/deactivate').flush(
      { title: 'Conflict', status: 409, detail: 'The bin holds stock on hand.' },
      { status: 409, statusText: 'Conflict' },
    );
    await settle();
    expect(dialogOpen()).toBe(true);
    expect(banner()).toBe('The bin holds stock on hand.');
    expect(dialogTitle()).toBe('Deactivate bin B1?');
  });

  it('FEAT-001.24 a 5xx on deactivate shows the generic message and keeps the dialog open', async () => {
    await createPage();
    await click(pageButton('Deactivate bin B1'));
    await click(dialogButtons()[1]);
    ctrl.expectOne('/api/v1/bins/9/deactivate').flush(
      { title: 'Internal Server Error', status: 500, detail: 'SqlException' },
      { status: 500, statusText: 'Err' },
    );
    await settle();
    expect(banner()).toBe('Something went wrong. Please try again.');
    expect(document.body.textContent).not.toContain('SqlException');
    expect(dialogOpen()).toBe(true);
  });
});
