import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { catchError, debounceTime, of, switchMap, tap } from 'rxjs';
import { GENERIC_ERROR_MESSAGE, isApiError } from '../../../core/api/api-error';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorBanner } from '../../../shared/error-banner/error-banner';
import { StatusChip } from '../../../shared/status-chip/status-chip';
import { Bin, BinListOptions, Zone } from '../data/location.models';
import { LocationsApi } from '../data/locations.api';
import { DeactivateTarget } from './location-actions';

/** Bins table with server-side sort, search, zone filter and paging. */
@Component({
  selector: 'app-bin-list',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    MatSortModule,
    MatTableModule,
    EmptyState,
    ErrorBanner,
    StatusChip,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-error-banner [message]="error()" />
    <div class="bins__filters">
      <mat-form-field>
        <mat-label>Search by code or name</mat-label>
        <input matInput type="search" [formControl]="search" />
      </mat-form-field>
      <mat-form-field>
        <mat-label>Zone</mat-label>
        <mat-select [formControl]="zoneFilter">
          <mat-option [value]="null">All zones</mat-option>
          @for (zone of zones(); track zone.id) {
            <mat-option [value]="zone.id">{{ zone.code }} - {{ zone.name }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
    </div>
    @if (loading()) {
      <mat-progress-bar mode="indeterminate" aria-label="Loading bins" />
    }
    <table mat-table matSort [dataSource]="items()" (matSortChange)="onSort($event)" aria-label="Bins">
      <ng-container matColumnDef="code">
        <th mat-header-cell *matHeaderCellDef mat-sort-header="code">Code</th>
        <td mat-cell *matCellDef="let bin">{{ bin.code }}</td>
      </ng-container>
      <ng-container matColumnDef="name">
        <th mat-header-cell *matHeaderCellDef mat-sort-header="name">Name</th>
        <td mat-cell *matCellDef="let bin">{{ bin.name }}</td>
      </ng-container>
      <ng-container matColumnDef="zone">
        <th mat-header-cell *matHeaderCellDef mat-sort-header="zone">Zone</th>
        <td mat-cell *matCellDef="let bin">{{ bin.zoneCode }}</td>
      </ng-container>
      <ng-container matColumnDef="isActive">
        <th mat-header-cell *matHeaderCellDef mat-sort-header="isActive">Status</th>
        <td mat-cell *matCellDef="let bin"><app-status-chip [active]="bin.isActive" /></td>
      </ng-container>
      <ng-container matColumnDef="actions">
        <th mat-header-cell *matHeaderCellDef>Actions</th>
        <td mat-cell *matCellDef="let bin">
          <button mat-button type="button" [attr.aria-label]="'Edit bin ' + bin.code" (click)="editBinRequested.emit(bin)">Edit</button>
          <button
            mat-button
            type="button"
            [attr.aria-label]="'Deactivate bin ' + bin.code"
            (click)="deactivateRequested.emit({ kind: 'bin', bin })"
          >
            Deactivate
          </button>
        </td>
      </ng-container>
      <tr mat-header-row *matHeaderRowDef="columns"></tr>
      <tr mat-row *matRowDef="let row; columns: columns"></tr>
    </table>
    @if (loaded() && items().length === 0 && !error()) {
      @if (filtered()) {
        <p class="bins__none" role="status">No bins match your search or zone filter.</p>
      } @else {
        <app-empty-state
          title="No bins yet"
          message="Create a zone first, then add bins to it from the Tree tab."
        />
      }
    }
    <mat-paginator
      [length]="total()"
      [pageIndex]="page() - 1"
      [pageSize]="pageSize()"
      [pageSizeOptions]="pageSizeOptions"
      (page)="onPage($event)"
      aria-label="Select page of bins"
    />
  `,
  styles: `
    .bins__filters {
      display: flex;
      flex-wrap: wrap;
      gap: 16px;
    }
    .bins__none {
      padding: 16px;
      text-align: center;
    }
  `,
})
export class BinList {
  private readonly api = inject(LocationsApi);

  /** Active zones for the filter (the page passes the `isActive=true` tree). */
  readonly zones = input<readonly Zone[]>([]);
  /** Change this number to make the list fetch again (the page does after a change). */
  readonly reloadToken = input(0);

  readonly editBinRequested = output<Bin>();
  readonly deactivateRequested = output<DeactivateTarget>();

  protected readonly columns = ['code', 'name', 'zone', 'isActive', 'actions'];
  protected readonly pageSizeOptions = [25, 50, 100];

  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly zoneFilter = new FormControl<number | null>(null);

  protected readonly items = signal<Bin[]>([]);
  protected readonly total = signal(0);
  protected readonly loading = signal(false);
  protected readonly loaded = signal(false);
  protected readonly error = signal<string | null>(null);

  /** 1-based, as the API; the paginator is shown 0-based. */
  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  private readonly sort = signal<string | undefined>(undefined);
  private readonly q = signal('');
  private readonly zoneId = signal<number | null>(null);

  protected readonly filtered = computed(() => this.q() !== '' || this.zoneId() !== null);

  private readonly request = computed<BinListOptions>(() => {
    const options: BinListOptions = { page: this.page(), pageSize: this.pageSize() };
    const sort = this.sort();
    const q = this.q();
    const zoneId = this.zoneId();
    if (sort) {
      options.sort = sort;
    }
    if (q) {
      options.q = q;
    }
    if (zoneId !== null) {
      options.zoneId = zoneId;
    }
    return options;
  });

  constructor() {
    this.search.valueChanges.pipe(debounceTime(300), takeUntilDestroyed()).subscribe((value) => {
      this.q.set(value.trim());
      this.page.set(1);
    });
    this.zoneFilter.valueChanges.pipe(takeUntilDestroyed()).subscribe((value) => {
      this.zoneId.set(value);
      this.page.set(1);
    });

    // Every change of request or reload token fetches again; a newer request cancels an older one.
    const trigger = computed(() => ({ options: this.request(), token: this.reloadToken() }));
    toObservable(trigger)
      .pipe(
        tap(() => this.loading.set(true)),
        switchMap(({ options }) =>
          this.api.listBins(options).pipe(
            tap((result) => {
              this.items.set(result.items);
              this.total.set(result.totalItems);
              this.error.set(null);
            }),
            catchError((err: unknown) => {
              this.error.set(isApiError(err) ? err.userMessage : GENERIC_ERROR_MESSAGE);
              return of(null);
            }),
          ),
        ),
        tap(() => {
          this.loading.set(false);
          this.loaded.set(true);
        }),
        takeUntilDestroyed(),
      )
      .subscribe();
  }

  protected onSort(sort: Sort): void {
    this.sort.set(sort.direction ? (sort.direction === 'desc' ? '-' : '') + sort.active : undefined);
    this.page.set(1);
  }

  protected onPage(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.page.set(event.pageIndex + 1);
  }
}
