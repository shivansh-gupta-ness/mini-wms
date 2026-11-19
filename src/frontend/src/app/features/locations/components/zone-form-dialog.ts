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
import { UpdateZoneRequest, Zone, ZoneType } from '../data/location.models';
import { LocationsApi } from '../data/locations.api';

export interface ZoneFormDialogData {
  /** Present for edit mode, absent for create. */
  zone?: Zone;
}

type ZoneForm = FormGroup<{ code: FormControl<string>; name: FormControl<string>; type: FormControl<ZoneType | ''> }>;

/** Open with `MatDialog.open(ZoneFormDialog, { data })`; closes with the saved `Zone`. */
@Component({
  selector: 'app-zone-form-dialog',
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
    <h2 mat-dialog-title>{{ isEdit ? 'Edit zone' : 'New zone' }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()" novalidate>
      <mat-dialog-content>
        <app-error-banner [messages]="messages()">
          @if (stale()) {
            <button mat-button type="button" [disabled]="saving()" (click)="reload()">Reload</button>
          }
        </app-error-banner>
        <mat-form-field appearance="outline" class="dialog-field">
          <mat-label>Code</mat-label>
          <input matInput formControlName="code" required autocomplete="off" />
          <mat-hint>{{ isEdit ? 'The code cannot be changed.' : 'Required, up to 20 characters' }}</mat-hint>
          @if (form.controls.code.hasError('server')) {
            <mat-error>{{ serverError(form.controls.code) }}</mat-error>
          } @else if (form.controls.code.hasError('required')) {
            <mat-error>Code is required</mat-error>
          } @else if (form.controls.code.hasError('maxlength')) {
            <mat-error>Code must be 20 characters or fewer</mat-error>
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
        <mat-form-field appearance="outline" class="dialog-field">
          <mat-label>Type</mat-label>
          <mat-select formControlName="type" required>
            @for (t of types; track t) {
              <mat-option [value]="t">{{ t }}</mat-option>
            }
          </mat-select>
          @if (typeLocked()) {
            <mat-hint>The type cannot be changed because this zone has bins.</mat-hint>
          } @else {
            <mat-hint>Required</mat-hint>
          }
          @if (form.controls.type.hasError('server')) {
            <mat-error>{{ serverError(form.controls.type) }}</mat-error>
          } @else if (form.controls.type.hasError('required')) {
            <mat-error>Type is required</mat-error>
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
export class ZoneFormDialog {
  private readonly data = inject<ZoneFormDialogData | null>(MAT_DIALOG_DATA, { optional: true });
  private readonly ref = inject<MatDialogRef<ZoneFormDialog, Zone>>(MatDialogRef);
  private readonly api = inject(LocationsApi);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly types: readonly ZoneType[] = ['Receiving', 'Storage', 'Shipping'];
  protected readonly isEdit = this.data?.zone !== undefined;

  protected readonly form: ZoneForm = this.fb.group({
    code: [this.data?.zone?.code ?? '', [Validators.required, Validators.maxLength(20)]],
    name: [this.data?.zone?.name ?? '', [Validators.required, Validators.maxLength(100)]],
    type: [(this.data?.zone?.type ?? '') as ZoneType | '', [Validators.required]],
  });
  protected readonly saving = signal(false);
  protected readonly messages = signal<string[]>([]);
  protected readonly stale = signal(false);
  protected readonly typeLocked = signal((this.data?.zone?.binCount ?? 0) > 0);
  private version = this.data?.zone?.version ?? 0;
  private original = this.data?.zone;

  protected readonly serverError = serverErrorMessage;

  constructor() {
    if (this.isEdit) {
      this.form.controls.code.disable();
    }
    if (this.typeLocked()) {
      this.form.controls.type.disable();
    }
  }

  protected cancel(): void {
    this.ref.close();
  }

  protected save(): void {
    if (this.form.invalid || this.saving()) {
      return;
    }
    const { code, name, type } = this.form.getRawValue();
    if (type === '') {
      return;
    }
    this.saving.set(true);
    this.messages.set([]);
    this.stale.set(false);
    const request$ = this.original
      ? this.api.updateZone(this.original.id, this.updateBody(name, type))
      : this.api.createZone({ code: code.trim(), name, type });
    request$.subscribe({
      next: (saved) => this.ref.close(saved),
      error: (err: unknown) => this.onError(err),
    });
  }

  private updateBody(name: string, type: ZoneType): UpdateZoneRequest {
    const body: UpdateZoneRequest = { name, version: this.version };
    if (this.form.controls.type.enabled && type !== this.original?.type) {
      body.type = type;
    }
    return body;
  }

  private onError(err: unknown): void {
    this.saving.set(false);
    if (!isApiError(err)) {
      this.messages.set([GENERIC_ERROR_MESSAGE]);
      return;
    }
    if (err.status === 409) {
      // A type change with bins and a stale version are both 409; Reload fixes the second and is harmless for the first.
      this.stale.set(this.isEdit);
      this.messages.set([err.userMessage]);
      return;
    }
    const unmatched = err.status === 400 ? applyServerErrors(this.form, err) : [];
    this.messages.set(unmatched.length > 0 || Object.keys(err.errors).length > 0 ? unmatched : [err.userMessage]);
  }

  /** Fetches the zone again and refreshes the form and version; the dialog stays open. */
  protected reload(): void {
    if (!this.original) {
      return;
    }
    this.saving.set(true);
    this.api.getZone(this.original.id).subscribe({
      next: (zone) => {
        this.original = zone;
        this.version = zone.version;
        this.form.controls.name.setValue(zone.name);
        this.form.controls.type.setValue(zone.type);
        if (zone.binCount > 0) {
          this.form.controls.type.disable();
        } else {
          this.form.controls.type.enable();
        }
        this.typeLocked.set(zone.binCount > 0);
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
