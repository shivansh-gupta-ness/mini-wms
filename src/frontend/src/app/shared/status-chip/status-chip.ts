import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';

/** Shows Active or Inactive as text in a chip (never colour alone). */
@Component({
  selector: 'app-status-chip',
  imports: [MatChipsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-chip [class.status-chip--inactive]="!active()" [aria-label]="'Status: ' + label()" disableRipple>{{
      label()
    }}</mat-chip>
  `,
  styles: `
    .status-chip--inactive {
      opacity: 0.75;
      font-style: italic;
    }
  `,
})
export class StatusChip {
  readonly active = input.required<boolean>();
  protected readonly label = computed(() => (this.active() ? 'Active' : 'Inactive'));
}
