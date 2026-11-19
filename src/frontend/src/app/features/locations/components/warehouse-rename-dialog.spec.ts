import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { GENERIC_ERROR_MESSAGE } from '../../../core/api/api-error';
import { ErrorNotifier } from '../../../core/errors/error-notifier';
import { errorInterceptor } from '../../../core/errors/error.interceptor';
import { Warehouse } from '../data/location.models';
import { WarehouseRenameDialog } from './warehouse-rename-dialog';

const WAREHOUSE: Warehouse = { id: 1, code: 'WH', name: 'Main', isActive: true, version: 3 };
const URL = '/api/v1/warehouse';
const NOT_CLOSED = 'not closed';

async function settle(): Promise<void> {
  await TestBed.inject(ApplicationRef).whenStable();
  TestBed.tick();
}

async function open(): Promise<{ ctrl: HttpTestingController; closed: () => unknown }> {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([errorInterceptor])),
      provideHttpClientTesting(),
      { provide: ErrorNotifier, useValue: { notify: vi.fn() } },
    ],
  });
  const ref = TestBed.inject(MatDialog).open<WarehouseRenameDialog, { warehouse: Warehouse }, Warehouse>(
    WarehouseRenameDialog,
    { data: { warehouse: WAREHOUSE }, autoFocus: 'first-tabbable' },
  );
  let result: unknown = NOT_CLOSED;
  ref.afterClosed().subscribe((value) => (result = value));
  await settle();
  return { ctrl: TestBed.inject(HttpTestingController), closed: () => result };
}

const input = (): HTMLInputElement => document.body.querySelector('input') as HTMLInputElement;
const submit = (): HTMLButtonElement => document.body.querySelector('button[type="submit"]') as HTMLButtonElement;
const cancel = (): HTMLButtonElement => document.body.querySelector('mat-dialog-actions button') as HTMLButtonElement;
const fieldError = (): string | undefined => document.body.querySelector('mat-error')?.textContent?.trim();
const banner = (): string | undefined => document.body.querySelector('.error-banner__messages')?.textContent?.trim();
const reloadButton = (): HTMLButtonElement | undefined =>
  Array.from(document.body.querySelectorAll<HTMLButtonElement>('[role="alert"] button')).find(
    (b) => b.textContent?.trim() === 'Reload',
  );

async function type(value: string): Promise<void> {
  const el = input();
  el.value = value;
  el.dispatchEvent(new Event('input'));
  el.dispatchEvent(new Event('blur'));
  await settle();
}

describe('FEAT-001 warehouse rename dialog (AC 17)', () => {
  beforeEach(() => {
    // jsdom has no layout, so the focus trap would treat every element as hidden.
    vi.spyOn(HTMLElement.prototype, 'getClientRects').mockReturnValue([{}] as unknown as DOMRectList);
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.inject(MatDialog).closeAll();
    vi.restoreAllMocks();
  });

  it('FEAT-001.17 opens with the current name and a title', async () => {
    await open();
    expect(document.body.querySelector('h2')?.textContent?.trim()).toBe('Rename warehouse');
    expect(input().value).toBe('Main');
    expect(submit().disabled).toBe(false);
  });

  it('FEAT-001.17 a blank name shows the message under the field and disables submit', async () => {
    await open();
    await type('');
    expect(fieldError()).toBe('Name is required');
    expect(submit().disabled).toBe(true);
  });

  it('FEAT-001.17 a 101-character name shows the message under the field and disables submit', async () => {
    await open();
    await type('x'.repeat(101));
    expect(fieldError()).toBe('Name must be 100 characters or fewer');
    expect(submit().disabled).toBe(true);
  });

  it('FEAT-001.17 a 100-character name is accepted', async () => {
    await open();
    await type('x'.repeat(100));
    expect(fieldError()).toBeUndefined();
    expect(submit().disabled).toBe(false);
  });

  it('FEAT-001.17 saving PATCHes exactly name and version and closes with the saved warehouse', async () => {
    const { ctrl, closed } = await open();
    await type('Central');
    submit().click();
    const req = ctrl.expectOne(URL);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toStrictEqual({ name: 'Central', version: 3 });
    const saved = { ...WAREHOUSE, name: 'Central', version: 4 };
    req.flush(saved);
    await vi.waitFor(() => expect(closed()).toEqual(saved));
  });

  it('FEAT-001.17 a 400 under name shows under the field and the dialog stays open', async () => {
    const { ctrl, closed } = await open();
    await type('Central');
    submit().click();
    ctrl.expectOne(URL).flush(
      { title: 'Validation failed', status: 400, errors: { name: ['Name is already used.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    await settle();
    expect(fieldError()).toBe('Name is already used.');
    expect(banner()).toBeUndefined();
    expect(closed()).toBe(NOT_CLOSED);
  });

  it('FEAT-001.17 a 409 shows the detail in the banner with a Reload button and keeps the dialog open', async () => {
    const { ctrl, closed } = await open();
    await type('Central');
    submit().click();
    ctrl.expectOne(URL).flush(
      { title: 'Conflict', status: 409, detail: 'The warehouse was changed by someone else.' },
      { status: 409, statusText: 'Conflict' },
    );
    await settle();
    expect(banner()).toBe('The warehouse was changed by someone else.');
    expect(reloadButton()).toBeDefined();
    expect(closed()).toBe(NOT_CLOSED);
  });

  it('FEAT-001.17 Reload GETs the warehouse, updates the name and the version, and keeps the dialog open', async () => {
    const { ctrl, closed } = await open();
    await type('Central');
    submit().click();
    ctrl.expectOne(URL).flush({ title: 'Conflict', status: 409, detail: 'Stale.' }, { status: 409, statusText: 'Conflict' });
    await settle();
    reloadButton()?.click();
    const get = ctrl.expectOne(URL);
    expect(get.request.method).toBe('GET');
    get.flush({ ...WAREHOUSE, name: 'Fresh name', version: 9 });
    await settle();
    expect(input().value).toBe('Fresh name');
    expect(document.body.querySelector('[role="alert"]')).toBeNull();
    expect(closed()).toBe(NOT_CLOSED);
    submit().click();
    expect(ctrl.expectOne(URL).request.body).toStrictEqual({ name: 'Fresh name', version: 9 });
  });

  it('FEAT-001.24 while saving a spinner shows and submit and Cancel are disabled', async () => {
    const { ctrl } = await open();
    await type('Central');
    submit().click();
    await settle();
    expect(document.body.querySelector('mat-spinner')).not.toBeNull();
    expect(submit().disabled).toBe(true);
    expect(cancel().disabled).toBe(true);
    ctrl.expectOne(URL).flush({ title: 'x', status: 500 }, { status: 500, statusText: 'Err' });
    await settle();
    expect(document.body.querySelector('mat-spinner')).toBeNull();
    expect(submit().disabled).toBe(false);
  });

  it('FEAT-001.24 a 500 shows the generic message without technical detail and the dialog stays open', async () => {
    const { ctrl, closed } = await open();
    await type('Central');
    submit().click();
    ctrl.expectOne(URL).flush(
      { title: 'Internal Server Error', status: 500, detail: 'NullReferenceException at Foo', traceId: 'abc' },
      { status: 500, statusText: 'Err' },
    );
    await settle();
    expect(banner()).toBe(GENERIC_ERROR_MESSAGE);
    expect(document.body.textContent).not.toContain('NullReference');
    expect(closed()).toBe(NOT_CLOSED);
  });

  it('FEAT-001.26 a server error key with no control is shown in the banner, not dropped', async () => {
    const { ctrl } = await open();
    await type('Central');
    submit().click();
    ctrl.expectOne(URL).flush(
      { title: 'Validation failed', status: 400, errors: { version: ['Version is invalid.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    await settle();
    expect(banner()).toBe('Version is invalid.');
    expect(fieldError()).toBeUndefined();
  });

  it('FEAT-001.25 the input has a visible label, a required marker and an associated hint', async () => {
    await open();
    const label = document.body.querySelector('mat-label') as HTMLElement;
    expect(label.textContent?.trim()).toBe('Name');
    expect(document.body.querySelector(`label[for="${input().id}"]`)).not.toBeNull();
    expect(input().required).toBe(true);
    const hint = document.body.querySelector('mat-hint') as HTMLElement;
    expect(hint.id).not.toBe('');
    expect(input().getAttribute('aria-describedby')).toContain(hint.id);
  });

  it('FEAT-001.25 the dialog is labelled by its title and the buttons are real buttons with names', async () => {
    await open();
    const dialog = document.body.querySelector('mat-dialog-container') as HTMLElement;
    const title = document.body.querySelector('h2[mat-dialog-title]') as HTMLElement;
    expect(dialog.getAttribute('aria-labelledby')).toBe(title.id);
    expect(cancel().tagName).toBe('BUTTON');
    expect(cancel().textContent?.trim()).toBe('Cancel');
    expect(submit().tagName).toBe('BUTTON');
    expect(submit().textContent?.trim()).toBe('Save');
  });

  it('FEAT-001.25 the first tabbable control (the name input) has the focus on open', async () => {
    await open();
    await vi.waitFor(() => expect(document.activeElement).toBe(input()));
  });

  it('FEAT-001.25 the saving spinner has an accessible name', async () => {
    await open();
    submit().click();
    await settle();
    expect(document.body.querySelector('mat-spinner')?.getAttribute('aria-label')).toBe('Saving');
    TestBed.inject(HttpTestingController).expectOne(URL).flush(WAREHOUSE);
  });

  it('FEAT-001.17 Cancel closes without a request', async () => {
    const { closed } = await open();
    cancel().click();
    await vi.waitFor(() => expect(closed()).toBeUndefined());
  });
});
