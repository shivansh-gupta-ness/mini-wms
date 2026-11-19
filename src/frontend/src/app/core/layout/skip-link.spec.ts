import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';

describe('TASK-008 skip link behaviour (AC 5)', () => {
  afterEach(() => document.body.replaceChildren());

  it('TASK-008.5 activating the skip link moves focus to the main content without navigating', async () => {
    TestBed.configureTestingModule({ providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()] });
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/stock');
    harness.detectChanges();
    const host = harness.routeNativeElement as HTMLElement;
    document.body.appendChild(host);

    const skip = host.querySelector<HTMLAnchorElement>('a.skip-link');
    const main = host.querySelector<HTMLElement>('main#main-content');
    expect(skip).not.toBeNull();
    expect(main).not.toBeNull();

    const click = new MouseEvent('click', { bubbles: true, cancelable: true });
    skip!.dispatchEvent(click);

    expect(click.defaultPrevented).toBe(true);
    expect(document.activeElement).toBe(main);
  });
});
