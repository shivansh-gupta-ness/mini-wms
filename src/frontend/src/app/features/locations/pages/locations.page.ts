import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { forkJoin } from 'rxjs';
import { GENERIC_ERROR_MESSAGE, isApiError } from '../../../core/api/api-error';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog';
import { ErrorBanner } from '../../../shared/error-banner/error-banner';
import { BinFormDialog } from '../components/bin-form-dialog';
import { BinList } from '../components/bin-list';
import { DeactivateTarget } from '../components/location-actions';
import { LocationTree } from '../components/location-tree';
import { WarehouseRenameDialog } from '../components/warehouse-rename-dialog';
import { ZoneFormDialog } from '../components/zone-form-dialog';
import { Bin, Warehouse, Zone, ZoneNode } from '../data/location.models';
import { LocationsApi } from '../data/locations.api';

const DIALOG_OPTIONS = { width: '480px', maxWidth: '95vw', autoFocus: 'first-tabbable' } as const;

@Component({
  selector: 'app-locations-page',
  imports: [MatButtonModule, MatProgressBarModule, MatTabsModule, ErrorBanner, BinList, LocationTree],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="locations__header">
      <div>
        <h1>Locations</h1>
        @if (warehouse(); as w) {
          <p class="locations__warehouse">Warehouse: {{ w.name }}</p>
        }
      </div>
      <div class="locations__actions">
        <button mat-button type="button" [disabled]="!warehouse()" (click)="renameRequested()">Rename</button>
        <button mat-flat-button type="button" (click)="newZoneRequested()">New zone</button>
      </div>
    </header>
    @if (!loaded() && !error()) {
      <mat-progress-bar mode="indeterminate" aria-label="Loading locations" />
    }
    <app-error-banner [message]="error()">
      <button mat-button type="button" (click)="reload()">Retry</button>
    </app-error-banner>
    @if (loaded()) {
      <mat-tab-group>
        <mat-tab label="Tree">
          <ng-template matTabContent>
            <app-location-tree
              [zones]="tree()"
              (newZoneRequested)="newZoneRequested()"
              (newBinRequested)="newBinRequested($event)"
              (editZoneRequested)="editZoneRequested($event)"
              (editBinRequested)="editBinRequested($event)"
              (deactivateRequested)="deactivateRequested($event)"
            />
          </ng-template>
        </mat-tab>
        <mat-tab label="List">
          <ng-template matTabContent>
            <app-bin-list
              [zones]="activeZones()"
              [reloadToken]="reloadToken()"
              (editBinRequested)="editBinRequested($event)"
              (deactivateRequested)="deactivateRequested($event)"
            />
          </ng-template>
        </mat-tab>
      </mat-tab-group>
    }
  `,
  styles: `
    .locations__header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      flex-wrap: wrap;
      gap: 16px;
    }
    .locations__warehouse {
      margin: 0;
    }
    .locations__actions {
      display: flex;
      gap: 8px;
    }
  `,
})
export class LocationsPage implements OnInit {
  private readonly api = inject(LocationsApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialog = inject(MatDialog);

  protected readonly warehouse = signal<Warehouse | null>(null);
  /** Every zone and bin, both statuses. */
  protected readonly tree = signal<ZoneNode[]>([]);
  /** Zones from `GET /locations/tree?isActive=true`, for the List filter (and later the bin dialog). */
  protected readonly activeZones = signal<Zone[]>([]);
  protected readonly loaded = signal(false);
  protected readonly error = signal<string | null>(null);
  /** The bin list fetches again when this changes. */
  protected readonly reloadToken = signal(0);

  ngOnInit(): void {
    this.reload();
  }

  /** Fetches the warehouse and both trees again. Stage 3 calls it after a create, edit or deactivate. */
  reload(): void {
    this.error.set(null);
    forkJoin({
      warehouse: this.api.getWarehouse(),
      tree: this.api.getTree(),
      activeTree: this.api.getTree(true),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ warehouse, tree, activeTree }) => {
          this.warehouse.set(warehouse);
          this.tree.set(tree);
          this.activeZones.set(activeTree);
          this.loaded.set(true);
          this.reloadToken.update((n) => n + 1);
        },
        error: (err: unknown) => this.error.set(isApiError(err) ? err.userMessage : GENERIC_ERROR_MESSAGE),
      });
  }

  protected renameRequested(): void {
    const warehouse = this.warehouse();
    if (!warehouse) {
      return;
    }
    this.afterClose(this.dialog.open(WarehouseRenameDialog, { ...DIALOG_OPTIONS, data: { warehouse } }));
  }

  protected newZoneRequested(): void {
    this.afterClose(this.dialog.open(ZoneFormDialog, { ...DIALOG_OPTIONS, data: {} }));
  }

  protected newBinRequested(zone?: Zone): void {
    this.afterClose(
      this.dialog.open(BinFormDialog, { ...DIALOG_OPTIONS, data: { zones: this.activeZones(), zoneId: zone?.id } }),
    );
  }

  protected editZoneRequested(zone: Zone): void {
    this.afterClose(this.dialog.open(ZoneFormDialog, { ...DIALOG_OPTIONS, data: { zone } }));
  }

  protected editBinRequested(bin: Bin): void {
    // The full tree, so the bin's own zone is listed even when it is Inactive.
    this.afterClose(this.dialog.open(BinFormDialog, { ...DIALOG_OPTIONS, data: { bin, zones: this.tree() } }));
  }

  protected deactivateRequested(item: DeactivateTarget): void {
    const data: ConfirmDialogData =
      item.kind === 'zone'
        ? {
            title: `Deactivate zone ${item.zone.code}?`,
            message: 'The zone becomes Inactive. It stays in history and can no longer receive new bins.',
            confirmLabel: 'Deactivate',
            confirm: () => this.api.deactivateZone(item.zone.id, { version: item.zone.version }),
          }
        : {
            title: `Deactivate bin ${item.bin.code}?`,
            message: 'The bin becomes Inactive. It stays in history and can no longer be used.',
            confirmLabel: 'Deactivate',
            confirm: () => this.api.deactivateBin(item.bin.id, { version: item.bin.version }),
          };
    this.afterClose(this.dialog.open(ConfirmDialog, { width: '420px', maxWidth: '95vw', data }));
  }

  /** Reloads the page data when a dialog closed with a saved item (or `true`). */
  private afterClose(ref: MatDialogRef<unknown, unknown>): void {
    ref
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result) {
          this.reload();
        }
      });
  }
}
