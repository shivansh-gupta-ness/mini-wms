import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApiError, isApiError } from '../../../core/api/api-error';
import { ErrorNotifier } from '../../../core/errors/error-notifier';
import { errorInterceptor } from '../../../core/errors/error.interceptor';
import { Bin, Page, Warehouse, Zone, ZoneNode } from './location.models';
import { LocationsApi } from './locations.api';

const warehouse: Warehouse = { id: 1, code: 'WH', name: 'Main', isActive: true, version: 3 };
const zone: Zone = { id: 7, warehouseId: 1, code: 'A', name: 'Zone A', type: 'Storage', isActive: true, binCount: 2, version: 1 };
const bin: Bin = { id: 9, zoneId: 7, zoneCode: 'A', code: 'A-01', name: 'Bin 1', path: 'A / A-01', isActive: true, version: 2 };
const emptyPage = { items: [], page: 1, pageSize: 20, totalItems: 0 };

// Real error interceptor, so a 400 problem+json arrives as an ApiError.
function setup(): { api: LocationsApi; ctrl: HttpTestingController } {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([errorInterceptor])),
      provideHttpClientTesting(),
      { provide: ErrorNotifier, useValue: { notify: vi.fn() } },
    ],
  });
  return { api: TestBed.inject(LocationsApi), ctrl: TestBed.inject(HttpTestingController) };
}

describe('FEAT-001 LocationsApi: requests (AC 26)', () => {
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('FEAT-001.26 getWarehouse sends GET /api/v1/warehouse and returns the typed warehouse', () => {
    const { api, ctrl } = setup();
    let result: Warehouse | undefined;
    api.getWarehouse().subscribe((w) => (result = w));
    const req = ctrl.expectOne('/api/v1/warehouse');
    expect(req.request.method).toBe('GET');
    req.flush(warehouse);
    expect(result).toEqual(warehouse);
  });

  it('FEAT-001.26 renameWarehouse sends PATCH /api/v1/warehouse with name and version', () => {
    const { api, ctrl } = setup();
    let result: Warehouse | undefined;
    api.renameWarehouse({ name: 'New', version: 3 }).subscribe((w) => (result = w));
    const req = ctrl.expectOne('/api/v1/warehouse');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ name: 'New', version: 3 });
    req.flush({ ...warehouse, name: 'New', version: 4 });
    expect(result?.version).toBe(4);
  });

  it('FEAT-001.26 listZones without options sends GET /api/v1/zones with no query string', () => {
    const { api, ctrl } = setup();
    let result: Page<Zone> | undefined;
    api.listZones().subscribe((p) => (result = p));
    const req = ctrl.expectOne('/api/v1/zones');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys()).toEqual([]);
    const page: Page<Zone> = { items: [zone], page: 1, pageSize: 20, totalItems: 1 };
    req.flush(page);
    expect(result).toEqual(page);
  });

  it('FEAT-001.26 listZones sends sort, q, page, pageSize, isActive and type as query params', () => {
    const { api, ctrl } = setup();
    api.listZones({ sort: '-name', q: 'cold', page: 2, pageSize: 50, isActive: false, type: 'Storage' }).subscribe();
    const req = ctrl.expectOne((r) => r.url === '/api/v1/zones');
    expect(req.request.params.get('sort')).toBe('-name');
    expect(req.request.params.get('q')).toBe('cold');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('50');
    expect(req.request.params.get('isActive')).toBe('false');
    expect(req.request.params.get('type')).toBe('Storage');
    req.flush(emptyPage);
  });

  it('FEAT-001.26 listZones omits undefined, null and empty values', () => {
    const { api, ctrl } = setup();
    api.listZones({ q: '', sort: undefined, type: null as never, page: 1 }).subscribe();
    const req = ctrl.expectOne((r) => r.url === '/api/v1/zones');
    expect(req.request.params.keys()).toEqual(['page']);
    req.flush(emptyPage);
  });

  it('FEAT-001.26 listZones keeps isActive=false and page=0 (falsy but meaningful)', () => {
    const { api, ctrl } = setup();
    api.listZones({ isActive: false, page: 0 }).subscribe();
    const req = ctrl.expectOne((r) => r.url === '/api/v1/zones');
    expect(req.request.params.get('isActive')).toBe('false');
    expect(req.request.params.get('page')).toBe('0');
    req.flush(emptyPage);
  });

  it('FEAT-001.26 createZone sends POST /api/v1/zones with code, name and type', () => {
    const { api, ctrl } = setup();
    let result: Zone | undefined;
    api.createZone({ code: 'A', name: 'Zone A', type: 'Receiving' }).subscribe((z) => (result = z));
    const req = ctrl.expectOne('/api/v1/zones');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ code: 'A', name: 'Zone A', type: 'Receiving' });
    req.flush(zone);
    expect(result).toEqual(zone);
  });

  it('FEAT-001.26 getZone sends GET /api/v1/zones/{id}', () => {
    const { api, ctrl } = setup();
    let result: Zone | undefined;
    api.getZone(7).subscribe((z) => (result = z));
    const req = ctrl.expectOne('/api/v1/zones/7');
    expect(req.request.method).toBe('GET');
    req.flush(zone);
    expect(result).toEqual(zone);
  });

  it('FEAT-001.26 updateZone sends PATCH /api/v1/zones/{id} with name, type and version', () => {
    const { api, ctrl } = setup();
    api.updateZone(7, { name: 'Renamed', type: 'Shipping', version: 1 }).subscribe();
    const req = ctrl.expectOne('/api/v1/zones/7');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ name: 'Renamed', type: 'Shipping', version: 1 });
    req.flush(zone);
  });

  it('FEAT-001.26 updateZone without a type sends only name and version', () => {
    const { api, ctrl } = setup();
    api.updateZone(7, { name: 'Renamed', version: 1 }).subscribe();
    const req = ctrl.expectOne('/api/v1/zones/7');
    expect(req.request.body).toEqual({ name: 'Renamed', version: 1 });
    req.flush(zone);
  });

  it('FEAT-001.26 deactivateZone sends POST /api/v1/zones/{id}/deactivate with only the version', () => {
    const { api, ctrl } = setup();
    api.deactivateZone(7, { version: 5 }).subscribe();
    const req = ctrl.expectOne('/api/v1/zones/7/deactivate');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ version: 5 });
    req.flush({ ...zone, isActive: false });
  });

  it('FEAT-001.26 listBins sends zoneId and the other options as query params', () => {
    const { api, ctrl } = setup();
    let result: Page<Bin> | undefined;
    api.listBins({ zoneId: 7, q: 'A-', sort: 'code', page: 3, pageSize: 10, isActive: true }).subscribe((p) => (result = p));
    const req = ctrl.expectOne((r) => r.url === '/api/v1/bins');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('zoneId')).toBe('7');
    expect(req.request.params.get('q')).toBe('A-');
    expect(req.request.params.get('sort')).toBe('code');
    expect(req.request.params.get('page')).toBe('3');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.get('isActive')).toBe('true');
    const page: Page<Bin> = { items: [bin], page: 3, pageSize: 10, totalItems: 21 };
    req.flush(page);
    expect(result).toEqual(page);
  });

  it('FEAT-001.26 listBins omits an undefined zoneId and an empty q', () => {
    const { api, ctrl } = setup();
    api.listBins({ zoneId: undefined, q: '' }).subscribe();
    const req = ctrl.expectOne('/api/v1/bins');
    expect(req.request.params.keys()).toEqual([]);
    req.flush(emptyPage);
  });

  it('FEAT-001.26 createBin sends POST /api/v1/bins with zoneId, code and name', () => {
    const { api, ctrl } = setup();
    let result: Bin | undefined;
    api.createBin({ zoneId: 7, code: 'A-01', name: 'Bin 1' }).subscribe((b) => (result = b));
    const req = ctrl.expectOne('/api/v1/bins');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ zoneId: 7, code: 'A-01', name: 'Bin 1' });
    req.flush(bin);
    expect(result).toEqual(bin);
  });

  it('FEAT-001.26 getBin sends GET /api/v1/bins/{id}', () => {
    const { api, ctrl } = setup();
    let result: Bin | undefined;
    api.getBin(9).subscribe((b) => (result = b));
    const req = ctrl.expectOne('/api/v1/bins/9');
    expect(req.request.method).toBe('GET');
    req.flush(bin);
    expect(result).toEqual(bin);
  });

  it('FEAT-001.26 updateBin sends PATCH /api/v1/bins/{id} with name and version', () => {
    const { api, ctrl } = setup();
    api.updateBin(9, { name: 'Renamed', version: 2 }).subscribe();
    const req = ctrl.expectOne('/api/v1/bins/9');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ name: 'Renamed', version: 2 });
    req.flush(bin);
  });

  it('FEAT-001.26 deactivateBin sends POST /api/v1/bins/{id}/deactivate with only the version', () => {
    const { api, ctrl } = setup();
    api.deactivateBin(9, { version: 2 }).subscribe();
    const req = ctrl.expectOne('/api/v1/bins/9/deactivate');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ version: 2 });
    req.flush({ ...bin, isActive: false });
  });

  it('FEAT-001.26 getTree without a filter sends GET /api/v1/locations/tree with no query string', () => {
    const { api, ctrl } = setup();
    let result: ZoneNode[] | undefined;
    api.getTree().subscribe((t) => (result = t));
    const req = ctrl.expectOne('/api/v1/locations/tree');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys()).toEqual([]);
    const tree: ZoneNode[] = [{ ...zone, bins: [bin] }];
    req.flush(tree);
    expect(result).toEqual(tree);
  });

  it('FEAT-001.26 getTree(false) sends isActive=false', () => {
    const { api, ctrl } = setup();
    api.getTree(false).subscribe();
    const req = ctrl.expectOne((r) => r.url === '/api/v1/locations/tree');
    expect(req.request.params.get('isActive')).toBe('false');
    req.flush([]);
  });

  it('FEAT-001.26 getTree(true) sends isActive=true', () => {
    const { api, ctrl } = setup();
    api.getTree(true).subscribe();
    const req = ctrl.expectOne((r) => r.url === '/api/v1/locations/tree');
    expect(req.request.params.get('isActive')).toBe('true');
    req.flush([]);
  });
});

describe('FEAT-001 LocationsApi: errors (AC 26)', () => {
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('FEAT-001.26 a 400 problem+json arrives as an ApiError with its field errors', () => {
    const { api, ctrl } = setup();
    let caught: unknown;
    api.createZone({ code: '', name: '', type: 'Storage' }).subscribe({ error: (e: unknown) => (caught = e) });
    ctrl.expectOne('/api/v1/zones').flush(
      { type: 'validation', title: 'Validation failed', status: 400, errors: { code: ['Required.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    expect(isApiError(caught)).toBe(true);
    expect((caught as ApiError).status).toBe(400);
    expect((caught as ApiError).errors).toEqual({ code: ['Required.'] });
  });

  it('FEAT-001.26 a 409 on deactivate arrives as an ApiError with its detail', () => {
    const { api, ctrl } = setup();
    let caught: unknown;
    api.deactivateBin(9, { version: 2 }).subscribe({ error: (e: unknown) => (caught = e) });
    ctrl.expectOne('/api/v1/bins/9/deactivate').flush(
      { title: 'Conflict', status: 409, detail: 'Bin holds 5 units' },
      { status: 409, statusText: 'Conflict' },
    );
    expect((caught as ApiError).userMessage).toBe('Bin holds 5 units');
  });
});

// The test tsconfig has no Node types, so Node modules load through untyped dynamic imports (as in scaffold.spec.ts).
const fsModule = 'node:fs';
const pathModule = 'node:path';
const { readdirSync, readFileSync, existsSync } = (await import(/* @vite-ignore */ fsModule)) as any;
const { join } = (await import(/* @vite-ignore */ pathModule)) as any;
const appDir: string = join((globalThis as any).process.cwd(), 'src', 'app');

const listSources = (dir: string): string[] =>
  !existsSync(dir)
    ? []
    : readdirSync(dir, { withFileTypes: true }).flatMap((entry: any) => {
        const full = join(dir, entry.name);
        if (entry.isDirectory()) {
          return listSources(full);
        }
        return entry.name.endsWith('.ts') && !entry.name.endsWith('.spec.ts') ? [full] : [];
      });

describe('FEAT-001 locations and shared code: no direct HTTP, no hard-coded hosts (AC 26)', () => {
  const files = [...listSources(join(appDir, 'features', 'locations')), ...listSources(join(appDir, 'shared'))];

  it('FEAT-001.26 the scan finds the locations and shared source files', () => {
    expect(files.some((f) => f.endsWith('locations.api.ts'))).toBe(true);
    expect(files.some((f) => f.endsWith('confirm-dialog.ts'))).toBe(true);
  });

  it('FEAT-001.26 no source file imports HttpClient', () => {
    const offenders = files.filter((f) => /\bHttpClient\b/.test(readFileSync(f, 'utf8')));
    expect(offenders).toEqual([]);
  });

  it('FEAT-001.26 no source file contains http://, https:// or localhost', () => {
    const offenders = files.filter((f) => /https?:\/\/|localhost/i.test(readFileSync(f, 'utf8')));
    expect(offenders).toEqual([]);
  });
});
