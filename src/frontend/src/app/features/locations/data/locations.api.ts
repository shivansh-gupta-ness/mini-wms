import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import {
  Bin,
  BinListOptions,
  CreateBinRequest,
  CreateZoneRequest,
  Page,
  RenameRequest,
  UpdateBinRequest,
  UpdateZoneRequest,
  VersionRequest,
  Warehouse,
  Zone,
  ZoneListOptions,
  ZoneNode,
} from './location.models';

type QueryParams = Record<string, string | number | boolean>;

/** Keeps only defined, non-empty values so the query string never carries `undefined` or blanks. */
function toParams(options: object | undefined): QueryParams {
  const params: QueryParams = {};
  for (const [key, value] of Object.entries(options ?? {})) {
    if (value !== undefined && value !== null && value !== '') {
      params[key] = value as string | number | boolean;
    }
  }
  return params;
}

/** One method per endpoint of `docs/specs/api/locations.yaml`. All calls go through `ApiClient`. */
@Injectable({ providedIn: 'root' })
export class LocationsApi {
  private readonly api = inject(ApiClient);

  getWarehouse(): Observable<Warehouse> {
    return this.api.get<Warehouse>('warehouse');
  }

  renameWarehouse(body: RenameRequest): Observable<Warehouse> {
    return this.api.patch<Warehouse>('warehouse', body);
  }

  listZones(options?: ZoneListOptions): Observable<Page<Zone>> {
    return this.api.get<Page<Zone>>('zones', { params: toParams(options) });
  }

  createZone(body: CreateZoneRequest): Observable<Zone> {
    return this.api.post<Zone>('zones', body);
  }

  getZone(id: number): Observable<Zone> {
    return this.api.get<Zone>(`zones/${id}`);
  }

  updateZone(id: number, body: UpdateZoneRequest): Observable<Zone> {
    return this.api.patch<Zone>(`zones/${id}`, body);
  }

  deactivateZone(id: number, body: VersionRequest): Observable<Zone> {
    return this.api.post<Zone>(`zones/${id}/deactivate`, body);
  }

  listBins(options?: BinListOptions): Observable<Page<Bin>> {
    return this.api.get<Page<Bin>>('bins', { params: toParams(options) });
  }

  createBin(body: CreateBinRequest): Observable<Bin> {
    return this.api.post<Bin>('bins', body);
  }

  getBin(id: number): Observable<Bin> {
    return this.api.get<Bin>(`bins/${id}`);
  }

  updateBin(id: number, body: UpdateBinRequest): Observable<Bin> {
    return this.api.patch<Bin>(`bins/${id}`, body);
  }

  deactivateBin(id: number, body: VersionRequest): Observable<Bin> {
    return this.api.post<Bin>(`bins/${id}/deactivate`, body);
  }

  getTree(isActive?: boolean): Observable<ZoneNode[]> {
    return this.api.get<ZoneNode[]>('locations/tree', { params: toParams({ isActive }) });
  }
}
