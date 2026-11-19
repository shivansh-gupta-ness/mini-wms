import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Observable } from 'rxjs';
import { GENERIC_ERROR_MESSAGE, isApiError } from '../../core/api/api-error';
import { ErrorBanner } from '../error-banner/error-banner';

export interface ConfirmDialogData {
  /** For example `Deactivate bin A-01?`. */
  title: string;
  message: string;
  confirmLabel: string;
  cancelLabel?: string;
  /**
   * Runs the action while the dialog stays open. The dialog shows a spinner meanwhile, closes with `true` on success
   * and shows the error message in a banner on failure (so a 409 keeps the dialog open). Omit it for a plain confirm
   * that closes with `true` at once.
   */
  confirm?: () => Observable<unknown>;
}

/** Open with `MatDialog.open(ConfirmDialog, { data })`; `afterClosed()` emits `true` when confirmed, else undefined. */
@Component({
  selector: 'app-confirm-dialog',
  imports: [MatDialogModule, MatButtonModule, MatProgressSpinnerModule, ErrorBanner],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <app-error-banner [message]="error()" />
      <p>{{ data.message }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" cdkFocusInitial [disabled]="saving()" (click)="cancel()">
        {{ data.cancelLabel ?? 'Cancel' }}
      </button>
      <button mat-flat-button type="button" [disabled]="saving()" (click)="confirm()">
        @if (saving()) {
          <mat-spinner diameter="18" aria-label="Saving" />
        }
        {{ data.confirmLabel }}
      </button>
    </mat-dialog-actions>
  `,
})
export class ConfirmDialog {
  protected readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
  private readonly ref = inject<MatDialogRef<ConfirmDialog, boolean>>(MatDialogRef);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  protected cancel(): void {
    this.ref.close();
  }

  protected confirm(): void {
    if (this.saving()) {
      return;
    }
    if (!this.data.confirm) {
      this.ref.close(true);
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    this.data.confirm().subscribe({
      next: () => this.ref.close(true),
      error: (err: unknown) => {
        this.saving.set(false);
        this.error.set(isApiError(err) ? err.userMessage : GENERIC_ERROR_MESSAGE);
      },
    });
  }
}
