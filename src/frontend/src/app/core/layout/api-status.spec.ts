import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../app.routes';
import { authInterceptor } from '../auth/auth.interceptor';
import { ErrorNotifier } from '../errors/error-notifier';
import { errorInterceptor } from '../errors/error.interceptor';
import { ApiStatus, HEALTH_POLL_INTERVAL_MS, HEALTH_TIMEOUT_MS } from './api-status';

const HEALTH = '/api/v1/health';

interface Setup {
  fixture: ComponentFixture<ApiStatus>;
  ctrl: HttpTestingController;
  notify: ReturnType<typeof vi.fn>;
}

function providers(notify: ReturnType<typeof vi.fn>): unknown[] {
  return [
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    provideHttpClientTesting(),
    { provide: ErrorNotifier, useValue: { notify } },
  ];
}

/** Creates the component under fake timers and lets the first (timer 0) check fire. */
async function setup(): Promise<Setup> {
  const notify = vi.fn();
  TestBed.configureTestingModule({ providers: providers(notify) as never[] });
  const fixture = TestBed.createComponent(ApiStatus);
  fixture.detectChanges();
  await vi.advanceTimersByTimeAsync(0);
  return { fixture, ctrl: TestBed.inject(HttpTestingController), notify };
}

async function text(fixture: ComponentFixture<ApiStatus>): Promise<string> {
  fixture.detectChanges();
  await fixture.whenStable();
  return (fixture.nativeElement.querySelector('.api-status') as HTMLElement).textContent ?? '';
}

describe('TASK-011 API status: dev proxy and request (AC 1)', () => {
  // The real browser check (npm start + API on :5080, no CORS errors) is manual and NOT RUN here.
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('TASK-011.1 the health call goes to /api/v1/health right after the component is created', async () => {
    const { ctrl } = await setup();
    const req = ctrl.expectOne(HEALTH);
    expect(req.request.method).toBe('GET');
    req.flush({ status: 'ok' });
  });
});

describe('TASK-011 API status: shows ok as text (AC 2)', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('TASK-011.2 the text is empty before the first answer', async () => {
    const { fixture, ctrl } = await setup();
    expect(await text(fixture)).toBe('');
    ctrl.expectOne(HEALTH).flush({ status: 'ok' });
  });

  it('TASK-011.2 after {status:ok} the text is exactly "API: ok"', async () => {
    const { fixture, ctrl } = await setup();
    ctrl.expectOne(HEALTH).flush({ status: 'ok' });
    expect(await text(fixture)).toBe('API: ok');
  });

  it('TASK-011.2 the indicator is a polite status live region', async () => {
    const { fixture, ctrl } = await setup();
    const span = fixture.nativeElement.querySelector('.api-status') as HTMLElement;
    expect(span.getAttribute('role')).toBe('status');
    expect(span.getAttribute('aria-live')).toBe('polite');
    ctrl.expectOne(HEALTH).flush({ status: 'ok' });
  });

  it.each([
    ['a degraded status', { status: 'degraded' }],
    ['a null body', null],
    ['a string body', 'ok'],
  ])('TASK-011.2 a 200 with %s shows "API: unavailable"', async (_name, body) => {
    const { fixture, ctrl } = await setup();
    ctrl.expectOne(HEALTH).flush(body);
    expect(await text(fixture)).toBe('API: unavailable');
  });
});

describe('TASK-011 API status: failures keep the app usable (AC 3)', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('TASK-011.3 a network error shows "API: unavailable"', async () => {
    const { fixture, ctrl } = await setup();
    ctrl.expectOne(HEALTH).error(new ProgressEvent('error'));
    expect(await text(fixture)).toBe('API: unavailable');
  });

  it.each([500, 503])('TASK-011.3 a %i shows "API: unavailable"', async (status) => {
    const { fixture, ctrl } = await setup();
    ctrl.expectOne(HEALTH).flush({ title: 'x' }, { status, statusText: 'Err' });
    expect(await text(fixture)).toBe('API: unavailable');
  });

  it('TASK-011.3 a failing health request (500) does not call the global notifier', async () => {
    const { ctrl, notify } = await setup();
    ctrl.expectOne(HEALTH).flush({ title: 'x' }, { status: 500, statusText: 'Err' });
    expect(notify).not.toHaveBeenCalled();
  });

  it('TASK-011.3 a failing health request (network error) does not call the global notifier', async () => {
    const { ctrl, notify } = await setup();
    ctrl.expectOne(HEALTH).error(new ProgressEvent('error'));
    expect(notify).not.toHaveBeenCalled();
  });

  it('TASK-011.3 a request that never answers is unavailable after the 5 s timeout, not before', async () => {
    const { fixture, ctrl } = await setup();
    await vi.advanceTimersByTimeAsync(HEALTH_TIMEOUT_MS - 1);
    expect(await text(fixture)).toBe('');
    await vi.advanceTimersByTimeAsync(1);
    expect(await text(fixture)).toBe('API: unavailable');
    ctrl.match(HEALTH);
  });

  it('TASK-011.3 the shell still shows navigation and banner, with the indicator inside the banner', async () => {
    const notify = vi.fn();
    TestBed.configureTestingModule({ providers: [provideRouter(routes), ...(providers(notify) as never[])] });
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/stock');
    await vi.advanceTimersByTimeAsync(0);
    TestBed.inject(HttpTestingController).expectOne(HEALTH).error(new ProgressEvent('error'));
    harness.detectChanges();
    const el = harness.routeNativeElement as HTMLElement;
    const banner = el.querySelector('[role="banner"]');
    expect(el.querySelector('nav[aria-label="Main navigation"]')).not.toBeNull();
    expect(banner).not.toBeNull();
    expect(banner?.querySelector('.api-status')?.textContent).toBe('API: unavailable');
    expect(banner?.textContent).toContain('Dev Admin');
    expect(notify).not.toHaveBeenCalled();
  });
});

describe('TASK-011 API status: polling at most every 30 seconds (AC 4)', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('TASK-011.4 the poll interval constant is 30 seconds', () => {
    expect(HEALTH_POLL_INTERVAL_MS).toBe(30_000);
  });

  it('TASK-011.4 nothing is sent at 29,999 ms and exactly one request at 30,000 ms after a success', async () => {
    const { ctrl } = await setup();
    ctrl.expectOne(HEALTH).flush({ status: 'ok' });
    await vi.advanceTimersByTimeAsync(HEALTH_POLL_INTERVAL_MS - 1);
    ctrl.expectNone(HEALTH);
    await vi.advanceTimersByTimeAsync(1);
    ctrl.expectOne(HEALTH).flush({ status: 'ok' });
  });

  it('TASK-011.4 after a failure the retry is not earlier than 30 s', async () => {
    const { ctrl } = await setup();
    ctrl.expectOne(HEALTH).error(new ProgressEvent('error'));
    await vi.advanceTimersByTimeAsync(HEALTH_POLL_INTERVAL_MS - 1);
    ctrl.expectNone(HEALTH);
    await vi.advanceTimersByTimeAsync(1);
    ctrl.expectOne(HEALTH).error(new ProgressEvent('error'));
  });

  it('TASK-011.4 an unanswered request is not repeated before the 5 s timeout ends it', async () => {
    // The 5 s timeout always frees the in-flight slot before the next 30 s tick, so the
    // exhaustMap guard itself cannot be observed with the real constants.
    const { ctrl } = await setup();
    const first = ctrl.expectOne(HEALTH);
    await vi.advanceTimersByTimeAsync(HEALTH_TIMEOUT_MS - 1);
    ctrl.expectNone(HEALTH);
    expect(first.cancelled).toBe(false);
    await vi.advanceTimersByTimeAsync(1);
    expect(first.cancelled).toBe(true);
  });

  it('TASK-011.4 after an unanswered request timed out, the next request comes at 30 s', async () => {
    const { ctrl } = await setup();
    ctrl.expectOne(HEALTH);
    await vi.advanceTimersByTimeAsync(HEALTH_POLL_INTERVAL_MS - 1);
    ctrl.expectNone(HEALTH);
    await vi.advanceTimersByTimeAsync(1);
    ctrl.expectOne(HEALTH).flush({ status: 'ok' });
  });

  it('TASK-011.4 after a failed check a later successful one turns the text back to "API: ok"', async () => {
    const { fixture, ctrl } = await setup();
    ctrl.expectOne(HEALTH).error(new ProgressEvent('error'));
    expect(await text(fixture)).toBe('API: unavailable');
    await vi.advanceTimersByTimeAsync(HEALTH_POLL_INTERVAL_MS);
    ctrl.expectOne(HEALTH).flush({ status: 'ok' });
    expect(await text(fixture)).toBe('API: ok');
  });

  it('TASK-011.4 destroying the component stops polling', async () => {
    const { fixture, ctrl } = await setup();
    ctrl.expectOne(HEALTH).flush({ status: 'ok' });
    fixture.destroy();
    await vi.advanceTimersByTimeAsync(60_000);
    ctrl.expectNone(HEALTH);
  });
});
