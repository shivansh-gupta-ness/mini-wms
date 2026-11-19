import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Bin, ZoneNode } from '../data/location.models';
import { DeactivateTarget } from './location-actions';
import { LocationTree } from './location-tree';

function bin(id: number, zoneId: number, code: string, isActive = true): Bin {
  return { id, zoneId, zoneCode: 'A1', code, name: `Bin ${code}`, path: `A1/${code}`, isActive, version: 1 };
}

function zone(id: number, code: string, isActive: boolean, bins: Bin[], type: ZoneNode['type'] = 'Storage'): ZoneNode {
  return { id, warehouseId: 1, code, name: `Zone ${code}`, type, isActive, binCount: bins.length, version: 1, bins };
}

const ZONES: ZoneNode[] = [
  zone(1, 'A1', true, [bin(11, 1, 'B1'), bin(12, 1, 'B2', false)], 'Storage'),
  zone(2, 'R1', false, [bin(21, 2, 'B3')], 'Receiving'),
];

function setup(zones: ZoneNode[] = ZONES): { fixture: ComponentFixture<LocationTree>; el: HTMLElement } {
  const fixture = TestBed.createComponent(LocationTree);
  fixture.componentRef.setInput('zones', zones);
  fixture.detectChanges();
  return { fixture, el: fixture.nativeElement as HTMLElement };
}

function byLabel(el: HTMLElement, label: string): HTMLButtonElement {
  const found = el.querySelector<HTMLButtonElement>(`button[aria-label="${label}"]`);
  if (!found) {
    throw new Error(`No button labelled "${label}"`);
  }
  return found;
}

function toggle(el: HTMLElement): HTMLElement {
  return el.querySelector('mat-slide-toggle button') as HTMLElement;
}

function text(el: Element): string {
  return (el.textContent ?? '').replace(/\s+/g, ' ');
}

describe('FEAT-001.18 location tree: structure', () => {
  it('FEAT-001.18 zones are parents and their bins are nested children', () => {
    const { el } = setup();
    const zones = el.querySelectorAll('ul.tree > li.tree__zone');
    expect(zones.length).toBe(2);
    expect(zones[0].querySelectorAll('ul.tree__bins > li').length).toBe(2);
    expect(zones[1].querySelectorAll('ul.tree__bins > li').length).toBe(1);
  });

  it('FEAT-001.18 each zone shows code, name, type word and a status chip with text', () => {
    const { el } = setup();
    const [a1, r1] = Array.from(el.querySelectorAll('li.tree__zone'));
    const a1Row = a1.querySelector('.tree__row') as HTMLElement;
    const r1Row = r1.querySelector('.tree__row') as HTMLElement;
    expect(text(a1Row)).toContain('A1');
    expect(text(a1Row)).toContain('Zone A1');
    expect(a1Row.querySelector('.tree__type')?.textContent).toBe('Storage');
    expect(a1Row.querySelector('app-status-chip')?.textContent?.trim()).toBe('Active');
    expect(r1Row.querySelector('.tree__type')?.textContent).toBe('Receiving');
    expect(r1Row.querySelector('app-status-chip')?.textContent?.trim()).toBe('Inactive');
  });

  it('FEAT-001.18 each bin shows code, name and an Active or Inactive chip', () => {
    const { el } = setup();
    const bins = Array.from(el.querySelectorAll('li.tree__bin'));
    expect(text(bins[0])).toContain('B1');
    expect(text(bins[0])).toContain('Bin B1');
    expect(bins[0].querySelector('app-status-chip')?.textContent?.trim()).toBe('Active');
    expect(bins[1].querySelector('app-status-chip')?.textContent?.trim()).toBe('Inactive');
  });

  it('FEAT-001.18 an empty tree shows the "No zones yet" state with a New zone button that emits', () => {
    const { fixture, el } = setup([]);
    const emitted = vi.fn();
    fixture.componentInstance.newZoneRequested.subscribe(emitted);
    expect(text(el)).toContain('No zones yet');
    expect(el.querySelector('mat-slide-toggle')).toBeNull();
    el.querySelector<HTMLButtonElement>('app-empty-state button')?.click();
    expect(emitted).toHaveBeenCalledTimes(1);
  });

  it('FEAT-001.18 a zone without bins says so', () => {
    const { el } = setup([zone(3, 'S1', true, [])]);
    expect(text(el)).toContain('No bins in this zone.');
  });
});

describe('FEAT-001.18 location tree: Show inactive items toggle', () => {
  it('FEAT-001.18 the toggle is on by default and everything is visible', () => {
    const { el } = setup();
    expect(text(el)).toContain('Show inactive items');
    expect(toggle(el).getAttribute('aria-checked')).toBe('true');
    expect(el.querySelectorAll('li.tree__zone').length).toBe(2);
    expect(el.querySelectorAll('li.tree__bin').length).toBe(3);
  });

  it('FEAT-001.18 turning it off hides Inactive zones and Inactive bins', () => {
    const { fixture, el } = setup();
    toggle(el).click();
    fixture.detectChanges();
    expect(el.querySelectorAll('li.tree__zone').length).toBe(1);
    expect(text(el)).not.toContain('Zone R1');
    expect(text(el)).not.toContain('Bin B2');
    expect(text(el)).toContain('Bin B1');
  });

  it('FEAT-001.18 inactive bins are hidden independently inside an Active zone', () => {
    const { fixture, el } = setup([zone(1, 'A1', true, [bin(11, 1, 'B1'), bin(12, 1, 'B2', false)])]);
    toggle(el).click();
    fixture.detectChanges();
    expect(el.querySelectorAll('li.tree__zone').length).toBe(1);
    expect(el.querySelectorAll('li.tree__bin').length).toBe(1);
  });

  it('FEAT-001.18 inactive zones are hidden even when their bins are Active', () => {
    const { fixture, el } = setup([zone(2, 'R1', false, [bin(21, 2, 'B3')]), zone(1, 'A1', true, [])]);
    toggle(el).click();
    fixture.detectChanges();
    expect(text(el)).not.toContain('Zone R1');
    expect(text(el)).not.toContain('Bin B3');
  });

  it('FEAT-001.18 turning it on again shows the Inactive items again', () => {
    const { fixture, el } = setup();
    toggle(el).click();
    fixture.detectChanges();
    toggle(el).click();
    fixture.detectChanges();
    expect(el.querySelectorAll('li.tree__zone').length).toBe(2);
    expect(el.querySelectorAll('li.tree__bin').length).toBe(3);
  });

  it('FEAT-001.18 when only Inactive zones exist and the toggle is off, a hint explains', () => {
    const { fixture, el } = setup([zone(2, 'R1', false, [])]);
    toggle(el).click();
    fixture.detectChanges();
    expect(text(el)).toContain('All zones are inactive');
  });
});

describe('FEAT-001.18 location tree: disclosure buttons', () => {
  it('FEAT-001.18 zones start expanded with aria-expanded true, aria-controls and the Collapse label', () => {
    const { el } = setup();
    const button = byLabel(el, 'Collapse zone A1');
    expect(button.getAttribute('aria-expanded')).toBe('true');
    const controlled = el.querySelector(`#${button.getAttribute('aria-controls')}`);
    expect(controlled?.querySelectorAll('li').length).toBe(2);
  });

  it('FEAT-001.18 clicking collapses the zone: aria-expanded false, label Expand, bins removed', () => {
    const { fixture, el } = setup();
    byLabel(el, 'Collapse zone A1').click();
    fixture.detectChanges();
    expect(byLabel(el, 'Expand zone A1').getAttribute('aria-expanded')).toBe('false');
    expect(text(el)).not.toContain('Bin B1');
    expect(text(el)).toContain('Bin B3');
  });

  it('FEAT-001.18 clicking again expands the zone and shows the bins', () => {
    const { fixture, el } = setup();
    byLabel(el, 'Collapse zone A1').click();
    fixture.detectChanges();
    byLabel(el, 'Expand zone A1').click();
    fixture.detectChanges();
    expect(byLabel(el, 'Collapse zone A1').getAttribute('aria-expanded')).toBe('true');
    expect(text(el)).toContain('Bin B1');
  });
});

describe('FEAT-001.18 location tree: outputs', () => {
  it('FEAT-001.18 row buttons emit the zone or bin', () => {
    const { fixture, el } = setup();
    const c = fixture.componentInstance;
    const newBin = vi.fn();
    const editZone = vi.fn();
    const editBin = vi.fn();
    c.newBinRequested.subscribe(newBin);
    c.editZoneRequested.subscribe(editZone);
    c.editBinRequested.subscribe(editBin);
    byLabel(el, 'New bin in zone A1').click();
    byLabel(el, 'Edit zone A1').click();
    byLabel(el, 'Edit bin B1').click();
    expect(newBin).toHaveBeenCalledWith(ZONES[0]);
    expect(editZone).toHaveBeenCalledWith(ZONES[0]);
    expect(editBin).toHaveBeenCalledWith(ZONES[0].bins[0]);
  });

  it('FEAT-001.18 the Deactivate buttons emit a zone or bin target', () => {
    const { fixture, el } = setup();
    const emitted: DeactivateTarget[] = [];
    fixture.componentInstance.deactivateRequested.subscribe((t) => emitted.push(t));
    byLabel(el, 'Deactivate zone A1').click();
    byLabel(el, 'Deactivate bin B1').click();
    expect(emitted).toEqual([
      { kind: 'zone', zone: ZONES[0] },
      { kind: 'bin', bin: ZONES[0].bins[0] },
    ]);
  });
});

describe('FEAT-001.25 location tree: accessibility', () => {
  it('FEAT-001.25 every button has an accessible name and is a native button', () => {
    const { el } = setup();
    const buttons = Array.from(el.querySelectorAll('button'));
    expect(buttons.length).toBeGreaterThan(8);
    for (const b of buttons) {
      const labelledBy = b.getAttribute('aria-labelledby');
      const labelElement = labelledBy ? el.querySelector(`#${labelledBy}`) : null;
      const name = b.getAttribute('aria-label') || labelElement?.textContent?.trim() || b.textContent?.trim() || '';
      expect(name.length).toBeGreaterThan(0);
    }
  });

  it('FEAT-001.25 the disclosure and row buttons are native buttons with the expected labels', () => {
    const { el } = setup();
    for (const label of ['Collapse zone R1', 'Edit zone A1', 'New bin in zone A1', 'Deactivate bin B1', 'Deactivate zone R1']) {
      expect(byLabel(el, label).tagName).toBe('BUTTON');
    }
  });

  it('FEAT-001.25 no interactive item has tabindex -1', () => {
    const { el } = setup();
    for (const item of Array.from(el.querySelectorAll('button, input'))) {
      expect(item.getAttribute('tabindex')).not.toBe('-1');
    }
  });

  it('FEAT-001.25 the toggle has a visible label and the lists are labelled', () => {
    const { el } = setup();
    expect(el.querySelector('mat-slide-toggle label')?.textContent).toContain('Show inactive items');
    expect(el.querySelector('ul.tree')?.getAttribute('aria-label')).toBe('Zones and bins');
    expect(el.querySelector('ul.tree__bins')?.getAttribute('aria-label')).toBe('Bins in zone A1');
  });
});
