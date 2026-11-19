import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { Observable, Subject } from 'rxjs';
import { ApiError, GENERIC_ERROR_MESSAGE } from '../../core/api/api-error';
import { ConfirmDialog, ConfirmDialogData } from './confirm-dialog';

const BASE: ConfirmDialogData = {
  title: 'Deactivate bin A-01?',
  message: 'The bin will no longer accept stock.',
  confirmLabel: 'Deactivate',
};
const NOT_CLOSED = 'not closed';

async function settle(): Promise<void> {
  await TestBed.inject(ApplicationRef).whenStable();
  TestBed.tick();
}

async function open(data: ConfirmDialogData): Promise<{ closed: () => unknown }> {
  const ref = TestBed.inject(MatDialog).open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data });
  let result: unknown = NOT_CLOSED;
  ref.afterClosed().subscribe((value) => (result = value));
  await settle();
  return { closed: () => result };
}

const buttons = (): HTMLButtonElement[] => Array.from(document.body.querySelectorAll('mat-dialog-actions button'));
const cancelButton = (): HTMLButtonElement => buttons()[0];
const confirmButton = (): HTMLButtonElement => buttons()[1];
const alertText = (): string | undefined => document.body.querySelector('[role="alert"]')?.textContent?.trim();

describe('FEAT-001 confirm dialog (AC 23)', () => {
  beforeEach(() => {
    // jsdom has no layout, so the focus trap treats every element as hidden and never focuses cdkFocusInitial.
    vi.spyOn(HTMLElement.prototype, 'getClientRects').mockReturnValue([{}] as unknown as DOMRectList);
  });

  afterEach(() => {
    TestBed.inject(MatDialog).closeAll();
    vi.restoreAllMocks();
  });

  it('FEAT-001.23 opening shows the title and the message', async () => {
    await open(BASE);
    expect(document.body.querySelector('h2')?.textContent?.trim()).toBe('Deactivate bin A-01?');
    expect(document.body.querySelector('mat-dialog-content p')?.textContent?.trim()).toBe(
      'The bin will no longer accept stock.',
    );
  });

  it('FEAT-001.23 Cancel is the focused element after the dialog opens', async () => {
    await open(BASE);
    await vi.waitFor(() => expect(document.activeElement).toBe(cancelButton()));
  });

  it('FEAT-001.23 Cancel closes the dialog with undefined', async () => {
    const { closed } = await open(BASE);
    cancelButton().click();
    await vi.waitFor(() => expect(closed()).toBeUndefined());
  });

  it('FEAT-001.23 a custom cancel label is used', async () => {
    await open({ ...BASE, cancelLabel: 'Keep it' });
    expect(cancelButton().textContent?.trim()).toBe('Keep it');
  });

  it('FEAT-001.23 confirm without a callback closes with true', async () => {
    const { closed } = await open(BASE);
    confirmButton().click();
    await vi.waitFor(() => expect(closed()).toBe(true));
  });

  it('FEAT-001.23 while the callback is pending a spinner shows and both buttons are disabled', async () => {
    const pending = new Subject<unknown>();
    const { closed } = await open({ ...BASE, confirm: () => pending });
    confirmButton().click();
    await settle();
    expect(document.body.querySelector('mat-spinner')).not.toBeNull();
    expect(cancelButton().disabled).toBe(true);
    expect(confirmButton().disabled).toBe(true);
    expect(closed()).toBe(NOT_CLOSED);
  });

  it('FEAT-001.23 a second click while pending does not run the callback again', async () => {
    const confirm = vi.fn((): Observable<unknown> => new Subject<unknown>());
    await open({ ...BASE, confirm });
    confirmButton().click();
    confirmButton().click();
    await settle();
    expect(confirm).toHaveBeenCalledTimes(1);
  });

  it('FEAT-001.23 closes with true when the callback succeeds', async () => {
    const pending = new Subject<unknown>();
    const { closed } = await open({ ...BASE, confirm: () => pending });
    confirmButton().click();
    pending.next({});
    pending.complete();
    await vi.waitFor(() => expect(closed()).toBe(true));
  });

  it('FEAT-001.23 a 409 keeps the dialog open and shows the detail', async () => {
    const failing = new Subject<unknown>();
    const { closed } = await open({ ...BASE, confirm: () => failing });
    confirmButton().click();
    failing.error(new ApiError({ status: 409, title: 'Conflict', detail: 'Bin holds 5 units' }));
    await settle();
    expect(closed()).toBe(NOT_CLOSED);
    expect(alertText()).toBe('Bin holds 5 units');
  });

  it('FEAT-001.23 after a 409 the buttons are enabled again and the spinner is gone', async () => {
    const failing = new Subject<unknown>();
    await open({ ...BASE, confirm: () => failing });
    confirmButton().click();
    failing.error(new ApiError({ status: 409, title: 'Conflict', detail: 'Bin holds 5 units' }));
    await settle();
    expect(cancelButton().disabled).toBe(false);
    expect(confirmButton().disabled).toBe(false);
    expect(document.body.querySelector('mat-spinner')).toBeNull();
  });

  it('FEAT-001.23 after a failure the action can be retried and closes with true on success', async () => {
    const attempts: Subject<unknown>[] = [];
    const confirm = (): Observable<unknown> => {
      const subject = new Subject<unknown>();
      attempts.push(subject);
      return subject;
    };
    const { closed } = await open({ ...BASE, confirm });
    confirmButton().click();
    attempts[0].error(new ApiError({ status: 409, title: 'Conflict', detail: 'busy' }));
    await settle();
    expect(alertText()).toBe('busy');
    confirmButton().click();
    attempts[1].next({});
    await vi.waitFor(() => expect(closed()).toBe(true));
    expect(attempts.length).toBe(2);
  });

  it('FEAT-001.23 after a failure Cancel still closes the dialog with undefined', async () => {
    const failing = new Subject<unknown>();
    const { closed } = await open({ ...BASE, confirm: () => failing });
    confirmButton().click();
    failing.error(new ApiError({ status: 409, title: 'Conflict', detail: 'x' }));
    await settle();
    cancelButton().click();
    await vi.waitFor(() => expect(closed()).toBeUndefined());
  });

  it('FEAT-001.23 a 500 shows the generic text and never the detail', async () => {
    const failing = new Subject<unknown>();
    const { closed } = await open({ ...BASE, confirm: () => failing });
    confirmButton().click();
    failing.error(new ApiError({ status: 500, title: 'Internal Server Error', detail: 'stack trace' }));
    await settle();
    expect(alertText()).toBe(GENERIC_ERROR_MESSAGE);
    expect(document.body.textContent).not.toContain('stack trace');
    expect(closed()).toBe(NOT_CLOSED);
  });

  it('FEAT-001.23 an error that is not an ApiError shows the generic text', async () => {
    const failing = new Subject<unknown>();
    await open({ ...BASE, confirm: () => failing });
    confirmButton().click();
    failing.error(new Error('boom internals'));
    await settle();
    expect(alertText()).toBe(GENERIC_ERROR_MESSAGE);
    expect(document.body.textContent).not.toContain('boom internals');
  });

  it('FEAT-001.23 no banner shows before any failure', async () => {
    await open(BASE);
    expect(document.body.querySelector('[role="alert"]')).toBeNull();
  });

  it('FEAT-001.25 the confirm and cancel buttons have accessible names', async () => {
    await open(BASE);
    expect(cancelButton().textContent?.trim()).toBe('Cancel');
    expect(confirmButton().textContent?.trim()).toBe('Deactivate');
  });

  it('FEAT-001.25 the dialog is labelled by its title', async () => {
    await open(BASE);
    const dialog = document.body.querySelector('mat-dialog-container') as HTMLElement;
    const title = document.body.querySelector('h2[mat-dialog-title]') as HTMLElement;
    expect(title.id).not.toBe('');
    expect(dialog.getAttribute('aria-labelledby')).toBe(title.id);
  });

  it('FEAT-001.25 the saving spinner has an accessible name', async () => {
    const pending = new Subject<unknown>();
    await open({ ...BASE, confirm: () => pending });
    confirmButton().click();
    await settle();
    expect(document.body.querySelector('mat-spinner')?.getAttribute('aria-label')).toBe('Saving');
  });
});
