import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { GENERIC_ERROR_MESSAGE, isApiError } from '../../../core/api/api-error';
import { ErrorBanner } from '../../../shared/error-banner/error-banner';
import { applyServerErrors, serverErrorMessage } from '../../../shared/forms/apply-server-errors';
import { Bin, Zone } from '../data/location.models';
import { LocationsApi } from '../data/locations.api';

export interface BinFormDialogData {
  /** Present for edit mode, absent for create. */
  bin?: Bin;
  /** Create: the Active zones. Edit: zones that include the bin's current zone, even if Inactive. */
  zones: readonly Zone[];
  /** Create only: the zone to preselect. */
  zoneId?: number;
}

interface ZoneOption {
  id: number;
  label: string;
}

type BinForm = FormGroup<{ zoneId: FormControl<number | null>; code: FormControl<string>; name: FormControl<string> }>;

/** Open with `MatDialog.open(BinFormDialog, { data })`; closes with the saved `Bin`. */
@Component({
  selector: 'app-bin-form-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule,
    ErrorBanner,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ isEdit ? 'Edit bin' : 'New bin' }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()" novalidate>
      <mat-dialog-content>
        <app-error-banner [messages]="messages()">
          @if (stale()) {
            <button mat-button type="button" [disabled]="saving()" (click)="reload()">Reload</button>
          }
        </app-error-banner>
        <mat-form-field appearance="outline" class="dialog-field">
          <mat-label>Zone</mat-label>
          <mat-select formControlName="zoneId" required>
            @for (z of zoneOptions; track z.id) {
              <mat-option [value]="z.id">{{ z.label }}</mat-option>
            }
          </mat-select>
          <mat-hint>{{ isEdit ? 'A bin cannot be moved to another zone.' : 'Required' }}</mat-hint>
          @if (form.controls.zoneId.hasError('server')) {
            <mat-error>{{ serverError(form.controls.zoneId) }}</mat-error>
          } @else if (form.controls.zoneId.hasError('required')) {
            <mat-error>Zone is required</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline" class="dialog-field">
          <mat-label>Code</mat-label>
          <input matInput formControlName="code" required autocomplete="off" />
          <mat-hint>{{ isEdit ? 'The code cannot be changed.' : 'Required, up to 30 characters' }}</mat-hint>
          @if (form.controls.code.hasError('server')) {
            <mat-error>{{ serverError(form.controls.code) }}</mat-error>
          } @else if (form.controls.code.hasError('required')) {
            <mat-error>Code is required</mat-error>
          } @else if (form.controls.code.hasError('maxlength')) {
            <mat-error>Code must be 30 characters or fewer</mat-error>
          }
        </mat-form-field>
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
export class BinFormDialog {
  private readonly data = inject<BinFormDialogData>(MAT_DIALOG_DATA);
  private readonly ref = inject<MatDialogRef<BinFormDialog, Bin>>(MatDialogRef);
  private readonly api = inject(LocationsApi);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly bin = this.data.bin;
  protected readonly isEdit = this.bin !== undefined;
  protected readonly zoneOptions: readonly ZoneOption[] = this.buildOptions();

  protected readonly form: BinForm = this.fb.group({
    zoneId: [this.initialZoneId(), [Validators.required]],
    code: [this.bin?.code ?? '', [Validators.required, Validators.maxLength(30)]],
    name: [this.bin?.name ?? '', [Validators.required, Validators.maxLength(100)]],
  });
  protected readonly saving = signal(false);
  protected readonly messages = signal<string[]>([]);
  protected readonly stale = signal(false);
  private version = this.bin?.version ?? 0;

  protected readonly serverError = serverErrorMessage;

  constructor() {
    if (this.isEdit) {
      this.form.controls.zoneId.disable();
      this.form.controls.code.disable();
    }
  }

  private buildOptions(): ZoneOption[] {
    const label = (z: { code: string; name: string }) => `${z.code} - ${z.name}`;
    if (this.bin) {
      const zoneId = this.bin.zoneId;
      const current = this.data.zones.find((z) => z.id === zoneId);
      return [{ id: zoneId, label: current ? label(current) : this.bin.zoneCode }];
    }
    return this.data.zones.filter((z) => z.isActive).map((z) => ({ id: z.id, label: label(z) }));
  }

  private initialZoneId(): number | null {
    if (this.bin) {
      return this.bin.zoneId;
    }
    const wanted = this.data.zoneId;
    return wanted !== undefined && this.zoneOptions.some((z) => z.id === wanted) ? wanted : null;
  }

  protected cancel(): void {
    this.ref.close();
  }

  protected save(): void {
    if (this.form.invalid || this.saving()) {
      return;
    }
    const { zoneId, code, name } = this.form.getRawValue();
    if (zoneId === null) {
      return;
    }
    this.saving.set(true);
    this.messages.set([]);
    this.stale.set(false);
    const request$ = this.bin
      ? this.api.updateBin(this.bin.id, { name, version: this.version })
      : this.api.createBin({ zoneId, code: code.trim(), name });
    request$.subscribe({
      next: (saved) => this.ref.close(saved),
      error: (err: unknown) => this.onError(err),
    });
  }

  private onError(err: unknown): void {
    this.saving.set(false);
    if (!isApiError(err)) {
      this.messages.set([GENERIC_ERROR_MESSAGE]);
      return;
    }
    if (err.status === 409) {
      this.stale.set(this.isEdit);
      this.messages.set([err.userMessage]);
      return;
    }
    const unmatched = err.status === 400 ? applyServerErrors(this.form, err) : [];
    this.messages.set(unmatched.length > 0 || Object.keys(err.errors).length > 0 ? unmatched : [err.userMessage]);
  }

  /** Fetches the bin again and refreshes the name and version; the dialog stays open. */
  protected reload(): void {
    if (!this.bin) {
      return;
    }
    this.saving.set(true);
    this.api.getBin(this.bin.id).subscribe({
      next: (bin) => {
        this.version = bin.version;
        this.form.controls.name.setValue(bin.name);
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
