import { AbstractControl } from '@angular/forms';
import { ApiError } from '../../core/api/api-error';

/**
 * Sets each server field error on the matching control as a `server` error and marks it touched.
 * Keys use the backend's camelCase dotted paths (for example `name` or `lines.0.qty`), which
 * `AbstractControl.get` resolves. Messages for keys with no matching control are returned, never dropped,
 * so the page can show them in a banner.
 *
 * Template: `@if (ctrl.hasError('server')) { <mat-error>{{ ctrl.getError('server') }}</mat-error> }`
 * The `server` error clears itself on the next edit, because Angular re-runs the validators when the value changes
 * (proved by TASK-009 tests). Call `ctrl.updateValueAndValidity()` to clear it without an edit.
 */
export function applyServerErrors(form: AbstractControl, error: ApiError): string[] {
  const unmatched: string[] = [];
  for (const [key, messages] of Object.entries(error.errors)) {
    if (messages.length === 0) {
      continue;
    }
    // `Lines[0].Qty` style keys are read as `lines.0.qty`-style paths too.
    const control = form.get(key.replace(/\[(\d+)\]/g, '.$1'));
    if (control) {
      control.setErrors({ ...control.errors, server: messages.join(' ') });
      control.markAsTouched();
    } else {
      unmatched.push(...messages);
    }
  }
  return unmatched;
}

/** The server message set by `applyServerErrors`, or null. */
export function serverErrorMessage(control: AbstractControl | null): string | null {
  const message: unknown = control?.getError('server');
  return typeof message === 'string' ? message : null;
}
