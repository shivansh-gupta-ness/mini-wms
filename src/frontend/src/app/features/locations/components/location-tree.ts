import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { StatusChip } from '../../../shared/status-chip/status-chip';
import { Bin, Zone, ZoneNode } from '../data/location.models';
import { DeactivateTarget } from './location-actions';

/** Zones as parents and bins as children, in nested lists with disclosure buttons. */
@Component({
  selector: 'app-location-tree',
  imports: [MatButtonModule, MatSlideToggleModule, EmptyState, StatusChip],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (zones().length === 0) {
      <app-empty-state
        title="No zones yet"
        message="Add the first zone to start building the warehouse layout. Bins are added inside a zone."
      >
        <button mat-flat-button type="button" (click)="newZoneRequested.emit()">New zone</button>
      </app-empty-state>
    } @else {
      <mat-slide-toggle [checked]="showInactive()" (change)="showInactive.set($event.checked)">
        Show inactive items
      </mat-slide-toggle>
      <ul class="tree" aria-label="Zones and bins">
        @for (zone of visibleZones(); track zone.id) {
          <li class="tree__zone">
            <div class="tree__row">
              <button
                mat-icon-button
                type="button"
                class="tree__toggle"
                [attr.aria-expanded]="isExpanded(zone)"
                [attr.aria-controls]="'zone-bins-' + zone.id"
                [attr.aria-label]="(isExpanded(zone) ? 'Collapse zone ' : 'Expand zone ') + zone.code"
                (click)="toggle(zone)"
              >
                <span aria-hidden="true">{{ isExpanded(zone) ? '▾' : '▸' }}</span>
              </button>
              <span class="tree__code">{{ zone.code }}</span>
              <span>{{ zone.name }}</span>
              <span class="tree__type">{{ zone.type }}</span>
              <app-status-chip [active]="zone.isActive" />
              <span class="tree__actions">
                <button mat-button type="button" [attr.aria-label]="'New bin in zone ' + zone.code" (click)="newBinRequested.emit(zone)">
                  New bin
                </button>
                <button mat-button type="button" [attr.aria-label]="'Edit zone ' + zone.code" (click)="editZoneRequested.emit(zone)">
                  Edit
                </button>
                <button
                  mat-button
                  type="button"
                  [attr.aria-label]="'Deactivate zone ' + zone.code"
                  (click)="deactivateRequested.emit({ kind: 'zone', zone })"
                >
                  Deactivate
                </button>
              </span>
            </div>
            @if (isExpanded(zone)) {
              <ul class="tree__bins" [id]="'zone-bins-' + zone.id" [attr.aria-label]="'Bins in zone ' + zone.code">
                @for (bin of zone.bins; track bin.id) {
                  <li class="tree__row tree__bin">
                    <span class="tree__code">{{ bin.code }}</span>
                    <span>{{ bin.name }}</span>
                    <span class="tree__path">{{ bin.path }}</span>
                    <app-status-chip [active]="bin.isActive" />
                    <span class="tree__actions">
                      <button mat-button type="button" [attr.aria-label]="'Edit bin ' + bin.code" (click)="editBinRequested.emit(bin)">
                        Edit
                      </button>
                      <button
                        mat-button
                        type="button"
                        [attr.aria-label]="'Deactivate bin ' + bin.code"
                        (click)="deactivateRequested.emit({ kind: 'bin', bin })"
                      >
                        Deactivate
                      </button>
                    </span>
                  </li>
                } @empty {
                  <li class="tree__none">No bins in this zone.</li>
                }
              </ul>
            }
          </li>
        } @empty {
          <li class="tree__none">All zones are inactive. Turn on "Show inactive items" to see them.</li>
        }
      </ul>
    }
  `,
  styles: `
    .tree,
    .tree__bins {
      list-style: none;
      margin: 0;
      padding: 0;
    }
    .tree__bins {
      padding-left: 48px;
    }
    .tree__row {
      display: flex;
      align-items: center;
      flex-wrap: wrap;
      gap: 12px;
      padding: 4px 0;
    }
    .tree__code {
      font-weight: 500;
    }
    .tree__path {
      opacity: 0.75;
    }
    .tree__actions {
      margin-left: auto;
    }
    .tree__none {
      padding: 8px 0;
    }
  `,
})
export class LocationTree {
  /** The full tree as the API returns it (all items, both statuses). */
  readonly zones = input<readonly ZoneNode[]>([]);

  readonly newZoneRequested = output<void>();
  readonly newBinRequested = output<Zone>();
  readonly editZoneRequested = output<Zone>();
  readonly editBinRequested = output<Bin>();
  readonly deactivateRequested = output<DeactivateTarget>();

  protected readonly showInactive = signal(true);
  private readonly collapsed = signal<ReadonlySet<number>>(new Set());

  protected readonly visibleZones = computed(() => {
    const all = this.showInactive();
    return this.zones()
      .filter((zone) => all || zone.isActive)
      .map((zone) => (all ? zone : { ...zone, bins: zone.bins.filter((bin) => bin.isActive) }));
  });

  protected isExpanded(zone: ZoneNode): boolean {
    return !this.collapsed().has(zone.id);
  }

  protected toggle(zone: ZoneNode): void {
    const next = new Set(this.collapsed());
    if (!next.delete(zone.id)) {
      next.add(zone.id);
    }
    this.collapsed.set(next);
  }
}
