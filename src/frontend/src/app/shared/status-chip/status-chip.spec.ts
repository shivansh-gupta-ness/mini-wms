import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StatusChip } from './status-chip';

@Component({ imports: [StatusChip], template: `<app-status-chip [active]="active()" />` })
class Host {
  readonly active = signal(true);
}

/** The element that carries the aria-label (the chip host or the button MatChip renders inside it). */
function labelled(fixture: ComponentFixture<Host>): HTMLElement | null {
  return (fixture.nativeElement as HTMLElement).querySelector('[aria-label]');
}

function render(active: boolean): { fixture: ComponentFixture<Host>; chip: HTMLElement } {
  const fixture = TestBed.createComponent(Host);
  fixture.componentInstance.active.set(active);
  fixture.detectChanges();
  return { fixture, chip: fixture.nativeElement.querySelector('mat-chip') as HTMLElement };
}

describe('FEAT-001 status chip (AC 18)', () => {
  it('FEAT-001.18 an active item shows exactly the text Active', () => {
    const { chip } = render(true);
    expect(chip.textContent?.trim()).toBe('Active');
  });

  it('FEAT-001.18 an inactive item shows exactly the text Inactive', () => {
    const { chip } = render(false);
    expect(chip.textContent?.trim()).toBe('Inactive');
  });

  it('FEAT-001.18 the active chip has the aria-label Status: Active', () => {
    const { fixture } = render(true);
    expect(labelled(fixture)?.getAttribute('aria-label')).toBe('Status: Active');
  });

  it('FEAT-001.18 the inactive chip has the aria-label Status: Inactive', () => {
    const { fixture } = render(false);
    expect(labelled(fixture)?.getAttribute('aria-label')).toBe('Status: Inactive');
  });

  it('FEAT-001.18 the inactive state differs from the active one in text, not only in styling', () => {
    const active = render(true).chip.textContent?.trim();
    TestBed.resetTestingModule();
    const inactive = render(false).chip.textContent?.trim();
    expect(inactive).not.toBe(active);
    expect(inactive).toBe('Inactive');
  });

  it('FEAT-001.18 the text and label follow the input when it changes', () => {
    const { fixture, chip } = render(true);
    fixture.componentInstance.active.set(false);
    fixture.detectChanges();
    expect(chip.textContent?.trim()).toBe('Inactive');
    expect(labelled(fixture)?.getAttribute('aria-label')).toBe('Status: Inactive');
  });

  it('FEAT-001.25 the chip has an accessible name that includes its state', () => {
    const { fixture } = render(false);
    expect(labelled(fixture)?.getAttribute('aria-label') ?? '').toMatch(/^Status: /);
  });
});
