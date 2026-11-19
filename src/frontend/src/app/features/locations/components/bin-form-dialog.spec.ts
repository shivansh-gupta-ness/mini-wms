import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { GENERIC_ERROR_MESSAGE } from '../../../core/api/api-error';
import { ErrorNotifier } from '../../../core/errors/error-notifier';
import { errorInterceptor } from '../../../core/errors/error.interceptor';
import { Bin, Zone } from '../data/location.models';
import { BinFormDialog, BinFormDialogData } from './bin-form-dialog';

const zone = (id: number, code: string, isActive: boolean): Zone => ({
  id,
  warehouseId: 1,
  code,
  name: `Zone ${code}`,
  type: 'Storage',
  isActive,
  binCount: 0,
  version: 1,
});
const ZONES: Zone[] = [zone(1, 'A', true), zone(2, 'B', false), zone(3, 'C', true)];
const BIN: Bin = { id: 9, zoneId: 2, zoneCode: 'B', code: 'B-01', name: 'Bin one', path: 'B / B-01', isActive: true, version: 6 };
const NOT_CLOSED = 'not closed';

async function settle(): Promise<void> {
  await TestBed.inject(ApplicationRef).whenStable();
  TestBed.tick();
}

async function open(data: BinFormDialogData): Promise<{ ctrl: HttpTestingController; closed: () => unknown }> {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([errorInterceptor])),
      provideHttpClientTesting(),
      { provide: ErrorNotifier, useValue: { notify: vi.fn() } },
    ],
  });
  const ref = TestBed.inject(MatDialog).open<BinFormDialog, BinFormDialogData, Bin>(BinFormDialog, {
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
const zoneSelect = (): HTMLElement => field('Zone').querySelector('mat-select') as HTMLElement;
const zoneValue = (): string | undefined => zoneSelect().querySelector('.mat-mdc-select-value')?.textContent?.trim();
const zoneDisabled = (): boolean => zoneSelect().getAttribute('aria-disabled') === 'true';
const errorOf = (label: string): string | undefined => field(label).querySelector('mat-error')?.textContent?.trim();
const hintOf = (label: string): string | undefined => field(label).querySelector('mat-hint')?.textContent?.trim();
const submit = (): HTMLButtonElement => document.body.querySelector('button[type="submit"]') as HTMLButtonElement;
const cancel = (): HTMLButtonElement => document.body.querySelector('mat-dialog-actions button') as HTMLButtonElement;
const banner = (): string | undefined => document.body.querySelector('.error-banner__messages')?.textContent?.trim();
const reloadButton = (): HTMLButtonElement | undefined =>
  Array.from(document.body.querySelectorAll<HTMLButtonElement>('[role="alert"] button')).find(
    (b) => b.textContent?.trim() === 'Reload',
  );

async function type(label: string, value: string): Promise<void> {
  const el = inputOf(label);
  el.value = value;
  el.dispatchEvent(new Event('input'));
  el.dispatchEvent(new Event('blur'));
  await settle();
}

async function openZoneList(): Promise<string[]> {
  (zoneSelect().querySelector('.mat-mdc-select-trigger') as HTMLElement).click();
  await settle();
  return Array.from(document.body.querySelectorAll('mat-option')).map((o) => o.textContent?.trim() ?? '');
}

async function choose(option: string): Promise<void> {
  await openZoneList();
  Array.from(document.body.querySelectorAll<HTMLElement>('mat-option'))
    .find((o) => o.textContent?.trim() === option)
    ?.click();
  await settle();
}

const problem = (status: number, body: object) => [
  { title: 'Problem', status, ...body },
  { status, statusText: 'Err' },
] as const;

describe('FEAT-001 bin form dialog (AC 21, 22)', () => {
  beforeEach(() => {
    // jsdom has no layout, so the focus trap would treat every element as hidden.
    vi.spyOn(HTMLElement.prototype, 'getClientRects').mockReturnValue([{}] as unknown as DOMRectList);
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.inject(MatDialog).closeAll();
    vi.restoreAllMocks();
  });

  it('FEAT-001.21 create lists the Active zones only', async () => {
    await open({ zones: ZONES });
    expect(document.body.querySelector('h2')?.textContent?.trim()).toBe('New bin');
    expect(await openZoneList()).toEqual(['A - Zone A', 'C - Zone C']);
  });

  it('FEAT-001.21 the zone is preselected from zoneId', async () => {
    await open({ zones: ZONES, zoneId: 3 });
    expect(zoneValue()).toBe('C - Zone C');
  });

  it('FEAT-001.21 an Inactive zoneId is not preselected', async () => {
    await open({ zones: ZONES, zoneId: 2 });
    expect(zoneValue()).not.toContain('B - Zone B');
    expect(submit().disabled).toBe(true);
  });

  it('FEAT-001.21 Zone, Code and Name are marked required and submit stays disabled until all are filled', async () => {
    await open({ zones: ZONES });
    expect(zoneSelect().getAttribute('aria-required')).toBe('true');
    expect(inputOf('Code').required).toBe(true);
    expect(inputOf('Name').required).toBe(true);
    await type('Code', 'A-01');
    await type('Name', 'Bin one');
    expect(submit().disabled).toBe(true);
    await choose('A - Zone A');
    expect(submit().disabled).toBe(false);
  });

  it('FEAT-001.21 a 31-character code is blocked client side with a message and no request', async () => {
    const { ctrl } = await open({ zones: ZONES, zoneId: 1 });
    await type('Code', 'C'.repeat(31));
    await type('Name', 'Bin one');
    expect(errorOf('Code')).toBe('Code must be 30 characters or fewer');
    expect(submit().disabled).toBe(true);
    submit().click();
    ctrl.expectNone('/api/v1/bins');
  });

  it('FEAT-001.21 a 30-character code is accepted', async () => {
    await open({ zones: ZONES, zoneId: 1 });
    await type('Code', 'C'.repeat(30));
    await type('Name', 'Bin one');
    expect(errorOf('Code')).toBeUndefined();
    expect(submit().disabled).toBe(false);
  });

  it('FEAT-001.21 saving POSTs exactly zoneId, code (trimmed) and name, then closes with the bin', async () => {
    const { ctrl, closed } = await open({ zones: ZONES, zoneId: 3 });
    await type('Code', '  C-01 ');
    await type('Name', 'Bin one');
    submit().click();
    const req = ctrl.expectOne('/api/v1/bins');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toStrictEqual({ zoneId: 3, code: 'C-01', name: 'Bin one' });
    const saved = { ...BIN, zoneId: 3, zoneCode: 'C', code: 'C-01' };
    req.flush(saved);
    await vi.waitFor(() => expect(closed()).toEqual(saved));
  });

  it('FEAT-001.21 a picked zone is sent as zoneId', async () => {
    const { ctrl } = await open({ zones: ZONES });
    await choose('A - Zone A');
    await type('Code', 'A-01');
    await type('Name', 'Bin one');
    submit().click();
    expect(ctrl.expectOne('/api/v1/bins').request.body).toStrictEqual({ zoneId: 1, code: 'A-01', name: 'Bin one' });
  });

  it('FEAT-001.21 a 400 under zoneId and under code show under their fields; the dialog stays open', async () => {
    const { ctrl, closed } = await open({ zones: ZONES, zoneId: 1 });
    await type('Code', 'A-01');
    await type('Name', 'Bin one');
    submit().click();
    ctrl.expectOne('/api/v1/bins').flush(
      ...problem(400, { errors: { zoneId: ['The zone is inactive.'], code: ['Code A-01 is already used.'] } }),
    );
    await settle();
    expect(errorOf('Zone')).toBe('The zone is inactive.');
    expect(errorOf('Code')).toBe('Code A-01 is already used.');
    expect(banner()).toBeUndefined();
    expect(closed()).toBe(NOT_CLOSED);
    ctrl.expectNone('/api/v1/bins');
  });

  it('FEAT-001.26 an unmatched error key goes to the banner while a matched one stays under its field', async () => {
    const { ctrl } = await open({ zones: ZONES, zoneId: 1 });
    await type('Code', 'A-01');
    await type('Name', 'Bin one');
    submit().click();
    ctrl.expectOne('/api/v1/bins').flush(
      ...problem(400, { errors: { name: ['Name is bad.'], capacity: ['Capacity is bad.'] } }),
    );
    await settle();
    expect(errorOf('Name')).toBe('Name is bad.');
    expect(banner()).toBe('Capacity is bad.');
  });

  it('FEAT-001.24 while saving a spinner shows and submit and Cancel are disabled', async () => {
    const { ctrl } = await open({ zones: ZONES, zoneId: 1 });
    await type('Code', 'A-01');
    await type('Name', 'Bin one');
    submit().click();
    await settle();
    expect(document.body.querySelector('mat-spinner')?.getAttribute('aria-label')).toBe('Saving');
    expect(submit().disabled).toBe(true);
    expect(cancel().disabled).toBe(true);
    ctrl.expectOne('/api/v1/bins').flush(BIN);
  });

  it('FEAT-001.24 a 500 shows the generic message with no technical detail, the dialog stays open', async () => {
    const { ctrl, closed } = await open({ zones: ZONES, zoneId: 1 });
    await type('Code', 'A-01');
    await type('Name', 'Bin one');
    submit().click();
    ctrl.expectOne('/api/v1/bins').flush(...problem(500, { detail: 'SqlException: table bins' }));
    await settle();
    expect(banner()).toBe(GENERIC_ERROR_MESSAGE);
    expect(document.body.textContent).not.toContain('SqlException');
    expect(closed()).toBe(NOT_CLOSED);
    expect(submit().disabled).toBe(false);
  });

  it('FEAT-001.22 edit disables Code, shows only the current zone even if Inactive, with the no-move hint', async () => {
    await open({ bin: BIN, zones: ZONES });
    expect(document.body.querySelector('h2')?.textContent?.trim()).toBe('Edit bin');
    expect(inputOf('Code').value).toBe('B-01');
    expect(inputOf('Code').disabled).toBe(true);
    expect(zoneDisabled()).toBe(true);
    expect(zoneValue()).toBe('B - Zone B');
    expect(hintOf('Zone')).toBe('A bin cannot be moved to another zone.');
  });

  it('FEAT-001.22 saving PATCHes exactly name and version, with no zoneId and no code', async () => {
    const { ctrl, closed } = await open({ bin: BIN, zones: ZONES });
    await type('Name', 'Renamed bin');
    submit().click();
    const req = ctrl.expectOne('/api/v1/bins/9');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toStrictEqual({ name: 'Renamed bin', version: 6 });
    const saved = { ...BIN, name: 'Renamed bin', version: 7 };
    req.flush(saved);
    await vi.waitFor(() => expect(closed()).toEqual(saved));
  });

  it('FEAT-001.22 a 409 shows the detail with Reload; Reload GETs the bin, updates name and version, keeps the dialog open', async () => {
    const { ctrl, closed } = await open({ bin: BIN, zones: ZONES });
    await type('Name', 'Renamed bin');
    submit().click();
    ctrl.expectOne('/api/v1/bins/9').flush(...problem(409, { detail: 'The bin was changed by someone else.' }));
    await settle();
    expect(banner()).toBe('The bin was changed by someone else.');
    reloadButton()?.click();
    const get = ctrl.expectOne('/api/v1/bins/9');
    expect(get.request.method).toBe('GET');
    get.flush({ ...BIN, name: 'Fresh bin', version: 11 });
    await settle();
    expect(inputOf('Name').value).toBe('Fresh bin');
    expect(document.body.querySelector('[role="alert"]')).toBeNull();
    expect(closed()).toBe(NOT_CLOSED);
    submit().click();
    expect(ctrl.expectOne('/api/v1/bins/9').request.body).toStrictEqual({ name: 'Fresh bin', version: 11 });
  });

  it('FEAT-001.25 every control has a visible label, an accessible name and an associated hint', async () => {
    await open({ zones: ZONES });
    for (const label of ['Zone', 'Code', 'Name']) {
      const f = field(label);
      expect(f.querySelector('mat-label')?.textContent?.trim().length).toBeGreaterThan(0);
      const control = (f.querySelector('input') ?? f.querySelector('mat-select')) as HTMLElement;
      const hint = f.querySelector('mat-hint') as HTMLElement;
      expect(control.getAttribute('aria-describedby') ?? '').toContain(hint.id);
      if (control.tagName === 'INPUT') {
        expect(f.querySelector(`label[for="${control.id}"]`)).not.toBeNull();
      } else {
        expect(control.getAttribute('aria-labelledby') ?? '').toContain(f.querySelector('label')?.id ?? 'no-label');
      }
    }
  });

  it('FEAT-001.25 the dialog is labelled by its title, and Cancel and Save are named buttons', async () => {
    await open({ zones: ZONES });
    const dialog = document.body.querySelector('mat-dialog-container') as HTMLElement;
    const title = document.body.querySelector('h2[mat-dialog-title]') as HTMLElement;
    expect(dialog.getAttribute('aria-labelledby')).toBe(title.id);
    expect(cancel().tagName).toBe('BUTTON');
    expect(cancel().textContent?.trim()).toBe('Cancel');
    expect(submit().textContent?.trim()).toBe('Save');
  });

  it('FEAT-001.25 the first tabbable control (the Zone select) has the focus on open in create mode', async () => {
    await open({ zones: ZONES });
    await vi.waitFor(() => expect(document.activeElement).toBe(zoneSelect()));
  });

  it('FEAT-001.25 in edit mode the focus lands on the Name input', async () => {
    await open({ bin: BIN, zones: ZONES });
    await vi.waitFor(() => expect(document.activeElement).toBe(inputOf('Name')));
  });
});
