import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ErrorBanner } from './error-banner';

@Component({
  imports: [ErrorBanner],
  template: `<app-error-banner [message]="message()" [messages]="messages()"
    ><button type="button" id="reload">Reload</button></app-error-banner
  >`,
})
class Host {
  readonly message = signal<string | null>(null);
  readonly messages = signal<string[]>([]);
}

function render(message: string | null, messages: string[]): HTMLElement {
  const fixture = TestBed.createComponent(Host);
  fixture.componentInstance.message.set(message);
  fixture.componentInstance.messages.set(messages);
  fixture.detectChanges();
  return fixture.nativeElement as HTMLElement;
}

describe('FEAT-001 error banner (AC 24)', () => {
  it('FEAT-001.24 is hidden when there are no messages', () => {
    const el = render(null, []);
    expect(el.querySelector('[role="alert"]')).toBeNull();
    expect(el.querySelector('button#reload')).toBeNull();
  });

  it('FEAT-001.24 is hidden when the only messages are empty strings', () => {
    expect(render('', ['']).querySelector('[role="alert"]')).toBeNull();
  });

  it('FEAT-001.24 shows one paragraph per message inside a role=alert region', () => {
    const el = render(null, ['First problem', 'Second problem']);
    const paragraphs = Array.from(el.querySelectorAll('[role="alert"] p')).map((p) => p.textContent?.trim());
    expect(paragraphs).toEqual(['First problem', 'Second problem']);
  });

  it('FEAT-001.24 the single message shortcut shows before the list', () => {
    const el = render('Shortcut', ['Listed']);
    const paragraphs = Array.from(el.querySelectorAll('p')).map((p) => p.textContent?.trim());
    expect(paragraphs).toEqual(['Shortcut', 'Listed']);
  });

  it('FEAT-001.24 the single message shortcut alone shows one paragraph', () => {
    expect(render('Only one', []).querySelectorAll('p').length).toBe(1);
  });

  it('FEAT-001.24 renders the projected action inside the banner', () => {
    const el = render('Stale data', []);
    expect(el.querySelector('[role="alert"] button#reload')?.textContent?.trim()).toBe('Reload');
  });

  it('FEAT-001.25 the banner is announced through role=alert and its text is its content', () => {
    const alert = render('Problem', []).querySelector('[role="alert"]');
    expect(alert?.getAttribute('role')).toBe('alert');
    expect(alert?.textContent).toContain('Problem');
  });
});
