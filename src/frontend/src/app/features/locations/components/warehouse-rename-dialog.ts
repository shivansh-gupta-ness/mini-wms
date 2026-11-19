import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { GENERIC_ERROR_MESSAGE, isApiError } from '../../../core/api/api-error';
import { ErrorBanner } from '../../../shared/error-banner/error-banner';
import { applyServerErrors, serverErrorMessage } from '../../../shared/forms/apply-server-errors';
import { Warehouse } from '../data/location.models';
import { LocationsApi } from '../data/locations.api';

export interface WarehouseRenameDialogData {
  warehouse: Warehouse;
}

/** Open with `MatDialog.open(WarehouseRenameDialog, { data: { warehouse } })`; closes with the saved `Warehouse`. */
@Component({
  selector: 'app-warehouse-rename-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    ErrorBanner,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Rename warehouse</h2>
    <form [formGroup]="form" (ngSubmit)="save()" novalidate>
      <mat-dialog-content>
        <app-error-banner [messages]="messages()">
          @if (stale()) {
            <button mat-button type="button" [disabled]="saving()" (click)="reload()">Reload</button>
          }
        </app-error-banner>
        <mat-form-field appearance="outline" class="dialog-field">
          <mat-label>Name</mat-label>
          <input matInput formControlName="name" required autocomplete="off" />
          <mat-hint>Required, up to 100 characters</mat-hint>
          @if (form.controls.name.hasError('server')) {
            <mat-error>{{ serverError(form.controls.name) }}</mat-error>
          } @else if (form.controls.name.hasError('required')) {
            <mat-error>Name is required</mat-error>
          } @else if (form.controls.name.hasError('maxlength')) {
            <mat-error>Name must be 100 characters or fewer</mat-error>
          }
        </mat-form-field>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" [disabled]="saving()" (click)="cancel()">Cancel</button>
        <button mat-flat-button type="submit" [disabled]="form.invalid || saving()">
          @if (saving()) {
            <mat-spinner diameter="18" aria-label="Saving" />
          }
          Save
        </button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    .dialog-field {
      width: 100%;
    }
  `,
})
export class WarehouseRenameDialog {
  private readonly data = inject<WarehouseRenameDialogData>(MAT_DIALOG_DATA);
  private readonly ref = inject<MatDialogRef<WarehouseRenameDialog, Warehouse>>(MatDialogRef);
  private readonly api = inject(LocationsApi);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly form: FormGroup<{ name: FormControl<string> }> = this.fb.group({
    name: [this.data.warehouse.name, [Validators.required, Validators.maxLength(100)]],
  });
  protected readonly saving = signal(false);
  protected readonly messages = signal<string[]>([]);
  protected readonly stale = signal(false);
  private version = this.data.warehouse.version;

  protected readonly serverError = serverErrorMessage;

  protected cancel(): void {
    this.ref.close();
  }

  protected save(): void {
    if (this.form.invalid || this.saving()) {
      return;
    }
    this.saving.set(true);
    this.messages.set([]);
    this.stale.set(false);
    this.api.renameWarehouse({ name: this.form.controls.name.value, version: this.version }).subscribe({
      next: (saved) => this.ref.close(saved),
      error: (err: unknown) => {
        this.saving.set(false);
        if (!isApiError(err)) {
          this.messages.set([GENERIC_ERROR_MESSAGE]);
          return;
        }
        if (err.status === 409) {
          this.stale.set(true);
          this.messages.set([err.userMessage]);
          return;
        }
        const unmatched = err.status === 400 ? applyServerErrors(this.form, err) : [];
        this.messages.set(unmatched.length > 0 || Object.keys(err.errors).length > 0 ? unmatched : [err.userMessage]);
      },
    });
  }

  /** Fetches the warehouse again, takes its name and version, and keeps the dialog open. */
  protected reload(): void {
    this.saving.set(true);
    this.api.getWarehouse().subscribe({
      next: (warehouse) => {
        this.version = warehouse.version;
        this.form.controls.name.setValue(warehouse.name);
        this.messages.set([]);
        this.stale.set(false);
        this.saving.set(false);
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.messages.set([isApiError(err) ? err.userMessage : GENERIC_ERROR_MESSAGE]);
      },
    });
  }
}
