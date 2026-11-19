import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { EmptyState } from './empty-state';

@Component({
  imports: [EmptyState],
  template: `<app-empty-state title="No zones yet" message="Add the first zone to get started."
    ><button type="button" id="add">Add zone</button></app-empty-state
  >`,
})
class Host {}

function render(): HTMLElement {
  const fixture = TestBed.createComponent(Host);
  fixture.detectChanges();
  return fixture.nativeElement as HTMLElement;
}

describe('FEAT-001 empty state (AC 24)', () => {
  it('FEAT-001.24 shows the title as a heading', () => {
    expect(render().querySelector('h2')?.textContent?.trim()).toBe('No zones yet');
  });

  it('FEAT-001.24 shows the message as a paragraph', () => {
    expect(render().querySelector('p')?.textContent?.trim()).toBe('Add the first zone to get started.');
  });

  it('FEAT-001.24 renders the projected action', () => {
    expect(render().querySelector('button#add')?.textContent?.trim()).toBe('Add zone');
  });
});
