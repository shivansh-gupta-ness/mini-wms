import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { GENERIC_ERROR_MESSAGE } from '../../../core/api/api-error';
import { ErrorNotifier } from '../../../core/errors/error-notifier';
import { errorInterceptor } from '../../../core/errors/error.interceptor';
import { Zone } from '../data/location.models';
import { ZoneFormDialog, ZoneFormDialogData } from './zone-form-dialog';

const ZONE: Zone = {
  id: 7,
  warehouseId: 1,
  code: 'A1',
  name: 'Receiving dock',
  type: 'Receiving',
  isActive: true,
  binCount: 2,
  version: 4,
};
const EMPTY_ZONE: Zone = { ...ZONE, binCount: 0 };
const NOT_CLOSED = 'not closed';
const TYPE_HINT = 'The type cannot be changed because this zone has bins.';

async function settle(): Promise<void> {
  await TestBed.inject(ApplicationRef).whenStable();
  TestBed.tick();
}

async function open(data: ZoneFormDialogData = {}): Promise<{ ctrl: HttpTestingController; closed: () => unknown }> {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([errorInterceptor])),
      provideHttpClientTesting(),
      { provide: ErrorNotifier, useValue: { notify: vi.fn() } },
    ],
  });
  const ref = TestBed.inject(MatDialog).open<ZoneFormDialog, ZoneFormDialogData, Zone>(ZoneFormDialog, {
    data,
    autoFocus: 'first-tabbable',
  });
  let result: unknown = NOT_CLOSED;
  ref.afterClosed().subscribe((value) => (result = value));
  await settle();
  return { ctrl: TestBed.inject(HttpTestingController), closed: () => result };
}

const field = (label: string): HTMLElement =>
  Array.from(document.body.querySelectorAll<HTMLElement>('mat-form-field')).find((f) =>
    f.querySelector('mat-label')?.textContent?.trim().startsWith(label),
  ) as HTMLElement;
const inputOf = (label: string): HTMLInputElement => field(label).querySelector('input') as HTMLInputElement;
const selectOf = (label: string): HTMLElement => field(label).querySelector('mat-select') as HTMLElement;
const errorOf = (label: string): string | undefined => field(label).querySelector('mat-error')?.textContent?.trim();
const hintOf = (label: string): string | undefined => field(label).querySelector('mat-hint')?.textContent?.trim();
const submit = (): HTMLButtonElement => document.body.querySelector('button[type="submit"]') as HTMLButtonElement;
const cancel = (): HTMLButtonElement => document.body.querySelector('mat-dialog-actions button') as HTMLButtonElement;
const banner = (): string | undefined => document.body.querySelector('.error-banner__messages')?.textContent?.trim();
const reloadButton = (): HTMLButtonElement | undefined =>
  Array.from(document.body.querySelectorAll<HTMLButtonElement>('[role="alert"] button')).find(
    (b) => b.textContent?.trim() === 'Reload',
  );
const typeDisabled = (): boolean => selectOf('Type').getAttribute('aria-disabled') === 'true';

async function type(label: string, value: string): Promise<void> {
  const el = inputOf(label);
  el.value = value;
  el.dispatchEvent(new Event('input'));
  el.dispatchEvent(new Event('blur'));
  await settle();
}

async function choose(label: string, option: string): Promise<void> {
  (selectOf(label).querySelector('.mat-mdc-select-trigger') as HTMLElement).click();
  await settle();
  const match = Array.from(document.body.querySelectorAll<HTMLElement>('mat-option')).find(
    (o) => o.textContent?.trim() === option,
  );
  match?.click();
  await settle();
}

async function fillCreate(code = 'A1', name = 'Receiving dock', typeName = 'Receiving'): Promise<void> {
  await type('Code', code);
  await type('Name', name);
  await choose('Type', typeName);
}

const problem = (status: number, body: object) => [
  { title: 'Problem', status, ...body },
  { status, statusText: 'Err' },
] as const;

describe('FEAT-001 zone form dialog (AC 20, 22)', () => {
  beforeEach(() => {
    // jsdom has no layout, so the focus trap would treat every element as hidden.
    vi.spyOn(HTMLElement.prototype, 'getClientRects').mockReturnValue([{}] as unknown as DOMRectList);
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.inject(MatDialog).closeAll();
    vi.restoreAllMocks();
  });

  it('FEAT-001.20 create shows Code, Name and Type, all marked required, with the limits as hints', async () => {
    await open();
    expect(document.body.querySelector('h2')?.textContent?.trim()).toBe('New zone');
    expect(inputOf('Code').required).toBe(true);
    expect(inputOf('Name').required).toBe(true);
    expect(selectOf('Type').getAttribute('aria-required')).toBe('true');
    expect(hintOf('Code')).toContain('20');
    expect(hintOf('Name')).toContain('100');
  });

  it('FEAT-001.20 with nothing filled submit is disabled', async () => {
    await open();
    expect(submit().disabled).toBe(true);
  });

  it('FEAT-001.20 a missing field keeps submit disabled', async () => {
    await open();
    await type('Code', 'A1');
    await type('Name', 'Receiving dock');
    expect(submit().disabled).toBe(true);
    await choose('Type', 'Storage');
    expect(submit().disabled).toBe(false);
  });

  it('FEAT-001.20 a 21-character code shows the message under Code and disables submit', async () => {
    await open();
    await fillCreate('C'.repeat(21));
    expect(errorOf('Code')).toBe('Code must be 20 characters or fewer');
    expect(submit().disabled).toBe(true);
  });

  it('FEAT-001.20 a 101-character name shows the message under Name and disables submit', async () => {
    await open();
    await fillCreate('A1', 'n'.repeat(101));
    expect(errorOf('Name')).toBe('Name must be 100 characters or fewer');
    expect(submit().disabled).toBe(true);
  });

  it('FEAT-001.20 a blank code shows "Code is required" under the field after it is touched', async () => {
    await open();
    await type('Code', '');
    expect(errorOf('Code')).toBe('Code is required');
  });

  it('FEAT-001.20 saving POSTs exactly code, name and type with the code trimmed, then closes with the zone', async () => {
    const { ctrl, closed } = await open();
    await fillCreate('  A1  ', 'Receiving dock', 'Shipping');
    submit().click();
    const req = ctrl.expectOne('/api/v1/zones');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toStrictEqual({ code: 'A1', name: 'Receiving dock', type: 'Shipping' });
    const saved = { ...ZONE, type: 'Shipping' };
    req.flush(saved);
    await vi.waitFor(() => expect(closed()).toEqual(saved));
  });

  it('FEAT-001.20 a duplicate code (400 under code) shows under Code, keeps the dialog open and sends nothing else', async () => {
    const { ctrl, closed } = await open();
    await fillCreate();
    submit().click();
    ctrl.expectOne('/api/v1/zones').flush(...problem(400, { errors: { code: ['Code A1 is already used.'] } }));
    await settle();
    expect(errorOf('Code')).toBe('Code A1 is already used.');
    expect(banner()).toBeUndefined();
    expect(closed()).toBe(NOT_CLOSED);
    ctrl.expectNone('/api/v1/zones');
  });

  it('FEAT-001.20 a create 409 shows the detail in the banner and offers no Reload', async () => {
    const { ctrl } = await open();
    await fillCreate();
    submit().click();
    ctrl.expectOne('/api/v1/zones').flush(...problem(409, { detail: 'Warehouse is inactive.' }));
    await settle();
    expect(banner()).toBe('Warehouse is inactive.');
    expect(reloadButton()).toBeUndefined();
  });

  it('FEAT-001.26 a 400 under type and name shows under those controls, an unknown key goes to the banner', async () => {
    const { ctrl } = await open();
    await fillCreate();
    submit().click();
    ctrl.expectOne('/api/v1/zones').flush(
      ...problem(400, { errors: { type: ['Type is invalid.'], name: ['Name is bad.'], warehouseId: ['No warehouse.'] } }),
    );
    await settle();
    expect(errorOf('Type')).toBe('Type is invalid.');
    expect(errorOf('Name')).toBe('Name is bad.');
    expect(banner()).toBe('No warehouse.');
  });

  it('FEAT-001.24 while saving a spinner shows and submit and Cancel are disabled', async () => {
    const { ctrl } = await open();
    await fillCreate();
    submit().click();
    await settle();
    expect(document.body.querySelector('mat-spinner')).not.toBeNull();
    expect(submit().disabled).toBe(true);
    expect(cancel().disabled).toBe(true);
    ctrl.expectOne('/api/v1/zones').flush(ZONE);
  });

  it('FEAT-001.24 a second submit while saving sends no second request', async () => {
    const { ctrl } = await open();
    await fillCreate();
    submit().click();
    submit().click();
    await settle();
    ctrl.expectOne('/api/v1/zones').flush(ZONE);
  });

  it('FEAT-001.24 a 500 shows the generic message with no technical detail, the dialog stays open', async () => {
    const { ctrl, closed } = await open();
    await fillCreate();
    submit().click();
    ctrl.expectOne('/api/v1/zones').flush(...problem(500, { detail: 'SqlException: table zones', traceId: 't-1' }));
    await settle();
    expect(banner()).toBe(GENERIC_ERROR_MESSAGE);
    expect(document.body.textContent).not.toContain('SqlException');
    expect(document.body.textContent).not.toContain('t-1');
    expect(closed()).toBe(NOT_CLOSED);
    expect(document.body.querySelector('mat-spinner')).toBeNull();
    expect(submit().disabled).toBe(false);
  });

  it('FEAT-001.22 edit shows Code disabled with its value and the zone name', async () => {
    await open({ zone: ZONE });
    expect(document.body.querySelector('h2')?.textContent?.trim()).toBe('Edit zone');
    expect(inputOf('Code').value).toBe('A1');
    expect(inputOf('Code').disabled).toBe(true);
    expect(inputOf('Name').value).toBe('Receiving dock');
  });

  it('FEAT-001.22 with bins the Type is disabled and the hint says why', async () => {
    await open({ zone: ZONE });
    expect(typeDisabled()).toBe(true);
    expect(hintOf('Type')).toBe(TYPE_HINT);
  });

  it('FEAT-001.22 saving a name change PATCHes exactly name and version, with no code and no type', async () => {
    const { ctrl, closed } = await open({ zone: ZONE });
    await type('Name', 'Dock A');
    submit().click();
    const req = ctrl.expectOne('/api/v1/zones/7');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toStrictEqual({ name: 'Dock A', version: 4 });
    const saved = { ...ZONE, name: 'Dock A', version: 5 };
    req.flush(saved);
    await vi.waitFor(() => expect(closed()).toEqual(saved));
  });

  it('FEAT-001.22 without bins the Type is enabled, has no lock hint, and an unchanged type is not sent', async () => {
    const { ctrl } = await open({ zone: EMPTY_ZONE });
    expect(typeDisabled()).toBe(false);
    expect(hintOf('Type')).not.toBe(TYPE_HINT);
    await type('Name', 'Dock A');
    submit().click();
    expect(ctrl.expectOne('/api/v1/zones/7').request.body).toStrictEqual({ name: 'Dock A', version: 4 });
  });

  it('FEAT-001.22 without bins a changed type is sent', async () => {
    const { ctrl } = await open({ zone: EMPTY_ZONE });
    await choose('Type', 'Storage');
    submit().click();
    expect(ctrl.expectOne('/api/v1/zones/7').request.body).toStrictEqual({
      name: 'Receiving dock',
      type: 'Storage',
      version: 4,
    });
  });

  it('FEAT-001.22 a 409 shows the detail with Reload; Reload GETs the zone, updates form and version, keeps the dialog open', async () => {
    const { ctrl, closed } = await open({ zone: EMPTY_ZONE });
    await type('Name', 'Dock A');
    submit().click();
    ctrl.expectOne('/api/v1/zones/7').flush(...problem(409, { detail: 'The zone was changed by someone else.' }));
    await settle();
    expect(banner()).toBe('The zone was changed by someone else.');
    reloadButton()?.click();
    const get = ctrl.expectOne('/api/v1/zones/7');
    expect(get.request.method).toBe('GET');
    get.flush({ ...ZONE, name: 'Fresh', type: 'Storage', binCount: 3, version: 8 });
    await settle();
    expect(inputOf('Name').value).toBe('Fresh');
    expect(typeDisabled()).toBe(true);
    expect(hintOf('Type')).toBe(TYPE_HINT);
    expect(document.body.querySelector('[role="alert"]')).toBeNull();
    expect(closed()).toBe(NOT_CLOSED);
    submit().click();
    expect(ctrl.expectOne('/api/v1/zones/7').request.body).toStrictEqual({ name: 'Fresh', version: 8 });
  });

  it('FEAT-001.25 every control has a visible label, an accessible name and an associated hint', async () => {
    await open();
    for (const label of ['Code', 'Name', 'Type']) {
      const f = field(label);
      const labelEl = f.querySelector('mat-label') as HTMLElement;
      expect(labelEl.textContent?.trim().length).toBeGreaterThan(0);
      const control = (f.querySelector('input') ?? f.querySelector('mat-select')) as HTMLElement;
      const hint = f.querySelector('mat-hint') as HTMLElement;
      expect(control.getAttribute('aria-describedby') ?? '').toContain(hint.id);
      if (control.tagName === 'INPUT') {
        expect(f.querySelector(`label[for="${control.id}"]`)).not.toBeNull();
      } else {
        const labelledBy = control.getAttribute('aria-labelledby') ?? '';
        expect(labelledBy).toContain(f.querySelector('label')?.id ?? 'no-label');
      }
    }
  });

  it('FEAT-001.25 the dialog is labelled by its title, and Cancel and Save are named buttons', async () => {
    await open();
    const dialog = document.body.querySelector('mat-dialog-container') as HTMLElement;
    const title = document.body.querySelector('h2[mat-dialog-title]') as HTMLElement;
    expect(dialog.getAttribute('aria-labelledby')).toBe(title.id);
    expect(cancel().tagName).toBe('BUTTON');
    expect(cancel().textContent?.trim()).toBe('Cancel');
    expect(submit().textContent?.trim()).toBe('Save');
  });

  it('FEAT-001.25 the first tabbable control (Code) has the focus on open', async () => {
    await open();
    await vi.waitFor(() => expect(document.activeElement).toBe(inputOf('Code')));
  });

  it('FEAT-001.25 the saving spinner has an accessible name', async () => {
    const { ctrl } = await open();
    await fillCreate();
    submit().click();
    await settle();
    expect(document.body.querySelector('mat-spinner')?.getAttribute('aria-label')).toBe('Saving');
    ctrl.expectOne('/api/v1/zones').flush(ZONE);
  });
});
