import { Component, input } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ApiError } from '../../core/api/api-error';
import { applyServerErrors, serverErrorMessage } from './apply-server-errors';

@Component({
  selector: 'app-test-host',
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule],
  template: `
    <form [formGroup]="form()">
      <mat-form-field>
        <mat-label>SKU</mat-label>
        <input matInput formControlName="sku" />
        @if (form().controls['sku'].hasError('server')) {
          <mat-error>{{ form().controls['sku'].getError('server') }}</mat-error>
        }
      </mat-form-field>
      <mat-form-field>
        <mat-label>Name</mat-label>
        <input matInput formControlName="name" />
        @if (form().controls['name'].hasError('server')) {
          <mat-error>{{ form().controls['name'].getError('server') }}</mat-error>
        }
      </mat-form-field>
    </form>
  `,
})
class HostComponent {
  readonly form = input.required<FormGroup>();
}

const apiError = (errors: Record<string, string[]>): ApiError => new ApiError({ status: 400, title: 'Validation failed', errors });

function makeForm(): FormGroup {
  return new FormGroup({
    sku: new FormControl('', { nonNullable: true }),
    name: new FormControl('', { nonNullable: true }),
    lines: new FormArray([new FormGroup({ qty: new FormControl(0, { nonNullable: true }) })]),
  });
}

function render(form: FormGroup): ComponentFixture<HostComponent> {
  const fixture = TestBed.createComponent(HostComponent);
  fixture.componentRef.setInput('form', form);
  fixture.detectChanges();
  return fixture;
}

const errorTexts = (fixture: ComponentFixture<HostComponent>): string[] =>
  Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('mat-error')).map((e) => e.textContent?.trim() ?? '');

describe('TASK-009 applyServerErrors (AC 5)', () => {
  it('TASK-009.5 shows the server message under the matching sku field', () => {
    const form = makeForm();
    const fixture = render(form);
    applyServerErrors(form, apiError({ sku: ['SKU already exists.'] }));
    fixture.detectChanges();
    const fields = (fixture.nativeElement as HTMLElement).querySelectorAll('mat-form-field');
    expect(fields[0].querySelector('mat-error')?.textContent?.trim()).toBe('SKU already exists.');
    expect(fields[1].querySelector('mat-error')).toBeNull();
  });

  it('TASK-009.5 sets a server error on the control and marks it touched', () => {
    const form = makeForm();
    applyServerErrors(form, apiError({ sku: ['SKU already exists.'] }));
    const sku = form.controls['sku'];
    expect(sku.hasError('server')).toBe(true);
    expect(serverErrorMessage(sku)).toBe('SKU already exists.');
    expect(sku.touched).toBe(true);
    expect(form.controls['name'].touched).toBe(false);
    expect(sku.invalid).toBe(true);
  });

  it('TASK-009.5 returns unmatched messages and not matched ones', () => {
    const form = makeForm();
    const unmatched = applyServerErrors(
      form,
      apiError({ sku: ['Dup.'], unknown: ['Unknown failed.'], 'nothing.here': ['Nested missing.'] }),
    );
    expect(unmatched).toEqual(['Unknown failed.', 'Nested missing.']);
  });

  it('TASK-009.5 returns an empty array when every key matches', () => {
    expect(applyServerErrors(makeForm(), apiError({ sku: ['A'], name: ['B'] }))).toEqual([]);
  });

  it('TASK-009.5 returns an empty array and changes nothing when there are no field errors', () => {
    const form = makeForm();
    expect(applyServerErrors(form, apiError({}))).toEqual([]);
    expect(form.valid).toBe(true);
  });

  it('TASK-009.5 resolves the dotted path lines.0.qty through a FormArray', () => {
    const form = makeForm();
    const unmatched = applyServerErrors(form, apiError({ 'lines.0.qty': ['Too big.'] }));
    const qty = form.get('lines.0.qty');
    expect(unmatched).toEqual([]);
    expect(serverErrorMessage(qty)).toBe('Too big.');
    expect(qty?.touched).toBe(true);
  });

  it('TASK-009.5 an out-of-range FormArray index is returned as unmatched', () => {
    const form = makeForm();
    expect(applyServerErrors(form, apiError({ 'lines.5.qty': ['Nope.'] }))).toEqual(['Nope.']);
  });

  it('TASK-009.5 two messages for one control are both shown in the DOM', () => {
    const form = makeForm();
    const fixture = render(form);
    applyServerErrors(form, apiError({ sku: ['Too short.', 'Bad chars.'] }));
    fixture.detectChanges();
    const texts = errorTexts(fixture);
    expect(texts).toHaveLength(1);
    expect(texts[0]).toContain('Too short.');
    expect(texts[0]).toContain('Bad chars.');
  });

  it('TASK-009.5 keeps existing validator errors alongside the server error', () => {
    const form = makeForm();
    form.controls['sku'].addValidators(Validators.required);
    form.controls['sku'].updateValueAndValidity();
    applyServerErrors(form, apiError({ sku: ['Dup.'] }));
    expect(form.controls['sku'].hasError('required')).toBe(true);
    expect(form.controls['sku'].hasError('server')).toBe(true);
  });

  it('TASK-009.5 serverErrorMessage returns null for a control without a server error and for null', () => {
    const form = makeForm();
    expect(serverErrorMessage(form.controls['sku'])).toBeNull();
    expect(serverErrorMessage(null)).toBeNull();
  });

  it('TASK-009.5 typing a new value into the control clears the server error (Angular re-runs validators)', () => {
    const form = makeForm();
    const fixture = render(form);
    applyServerErrors(form, apiError({ sku: ['SKU already exists.'] }));
    fixture.detectChanges();
    expect(errorTexts(fixture)).toEqual(['SKU already exists.']);

    const input = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement>('input[formcontrolname="sku"]')!;
    input.value = 'NEW-SKU';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(form.controls['sku'].hasError('server')).toBe(false);
    expect(errorTexts(fixture)).toEqual([]);
  });
});
