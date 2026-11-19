/** Types mirroring `docs/specs/api/locations.yaml`. */
export type ZoneType = 'Receiving' | 'Storage' | 'Shipping';

export interface Warehouse {
  id: number;
  code: string;
  name: string;
  isActive: boolean;
  version: number;
}

export interface Zone {
  id: number;
  warehouseId: number;
  code: string;
  name: string;
  type: ZoneType;
  isActive: boolean;
  binCount: number;
  version: number;
}

export interface Bin {
  id: number;
  zoneId: number;
  zoneCode: string;
  code: string;
  name: string;
  /** zone / bin, for display */
  path: string;
  isActive: boolean;
  version: number;
}

export interface ZoneNode extends Zone {
  bins: Bin[];
}

export interface Page<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
}

export interface RenameRequest {
  name: string;
  version: number;
}

export interface CreateZoneRequest {
  code: string;
  name: string;
  type: ZoneType;
}

export interface UpdateZoneRequest {
  name: string;
  type?: ZoneType;
  version: number;
}

export interface CreateBinRequest {
  zoneId: number;
  code: string;
  name: string;
}

export interface UpdateBinRequest {
  name: string;
  version: number;
}

export interface VersionRequest {
  version: number;
}

/** Query options shared by both lists. `sort` is one field, prefixed with `-` for descending. */
export interface ListOptions {
  page?: number;
  pageSize?: number;
  sort?: string;
  q?: string;
  isActive?: boolean;
}

export interface ZoneListOptions extends ListOptions {
  type?: ZoneType;
}

export interface BinListOptions extends ListOptions {
  zoneId?: number;
}
