import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { ApiError, GENERIC_ERROR_MESSAGE, isApiError } from '../api/api-error';
import { ErrorNotifier } from './error-notifier';
import { errorInterceptor } from './error.interceptor';

const URL = '/api/v1/items';

function setup(): { http: HttpClient; ctrl: HttpTestingController; notify: ReturnType<typeof vi.fn> } {
  const notify = vi.fn();
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([errorInterceptor])),
      provideHttpClientTesting(),
      { provide: ErrorNotifier, useValue: { notify } },
    ],
  });
  return { http: TestBed.inject(HttpClient), ctrl: TestBed.inject(HttpTestingController), notify };
}

/** Sends a GET, flushes the given failure and returns the error the caller receives. */
function fail(
  setupResult: ReturnType<typeof setup>,
  body: string | object | null,
  status: number,
  statusText = 'Err',
): unknown {
  let caught: unknown;
  setupResult.http.get(URL).subscribe({ error: (e: unknown) => (caught = e) });
  setupResult.ctrl.expectOne(URL).flush(body, { status, statusText });
  return caught;
}

describe('TASK-009 error interceptor: problem+json mapping (AC 3)', () => {
  it('TASK-009.3 a 400 validation body becomes an ApiError with all fields', () => {
    const s = setup();
    const err = fail(
      s,
      {
        type: 'validation',
        title: 'Validation failed',
        status: 400,
        traceId: 'trace-1',
        errors: { sku: ['Required.'], 'lines.0.qty': ['Too big.'] },
      },
      400,
    );
    expect(isApiError(err)).toBe(true);
    expect(err).toBeInstanceOf(ApiError);
    const apiError = err as ApiError;
    expect(apiError.status).toBe(400);
    expect(apiError.title).toBe('Validation failed');
    expect(apiError.type).toBe('validation');
    expect(apiError.traceId).toBe('trace-1');
    expect(apiError.errors).toEqual({ sku: ['Required.'], 'lines.0.qty': ['Too big.'] });
  });

  it('TASK-009.3 a 400 does not notify the user', () => {
    const s = setup();
    fail(s, { title: 'Validation failed', status: 400 }, 400);
    expect(s.notify).not.toHaveBeenCalled();
  });

  it('TASK-009.3 a 409 body keeps its detail', () => {
    const s = setup();
    const err = fail(s, { type: 'conflict', title: 'Conflict', status: 409, detail: 'Stock was changed.' }, 409) as ApiError;
    expect(err.status).toBe(409);
    expect(err.title).toBe('Conflict');
    expect(err.detail).toBe('Stock was changed.');
    expect(err.userMessage).toBe('Stock was changed.');
  });

  it('TASK-009.3 a 404 maps status and title, is rethrown and does not notify', () => {
    const s = setup();
    const err = fail(s, { title: 'Not found', status: 404 }, 404) as ApiError;
    expect(isApiError(err)).toBe(true);
    expect(err.status).toBe(404);
    expect(err.title).toBe('Not found');
    expect(s.notify).not.toHaveBeenCalled();
  });

  it('TASK-009.3 a 401 maps status and title, is rethrown, does not notify and does not redirect', () => {
    const s = setup();
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate');
    const navigateByUrl = vi.spyOn(router, 'navigateByUrl');
    const err = fail(s, { title: 'Unauthorized', status: 401 }, 401) as ApiError;
    expect(isApiError(err)).toBe(true);
    expect(err.status).toBe(401);
    expect(err.title).toBe('Unauthorized');
    expect(s.notify).not.toHaveBeenCalled();
    expect(navigate).not.toHaveBeenCalled();
    expect(navigateByUrl).not.toHaveBeenCalled();
  });

  it('TASK-009.3 a body without errors yields an empty errors object', () => {
    const s = setup();
    const err = fail(s, { title: 'Bad request', status: 400 }, 400) as ApiError;
    expect(err.errors).toEqual({});
    expect(err.detail).toBeUndefined();
    expect(err.traceId).toBeUndefined();
  });

  it('TASK-009.3 a body without a title falls back to the generic title and keeps the status', () => {
    const s = setup();
    const err = fail(s, { status: 422 }, 422) as ApiError;
    expect(err.status).toBe(422);
    expect(err.title).toBe(GENERIC_ERROR_MESSAGE);
  });

  it('TASK-009.3 malformed errors values are tolerated', () => {
    const s = setup();
    const err = fail(s, { title: 'Bad', errors: { a: 'single', b: [1, 'ok'], c: 5 } }, 400) as ApiError;
    expect(err.errors).toEqual({ a: ['single'], b: ['ok'] });
  });

  it('TASK-009.3 an HTML error body maps to a generic ApiError with the HTTP status', () => {
    const s = setup();
    const err = fail(s, '<html><body>Bad gateway</body></html>', 400) as ApiError;
    expect(isApiError(err)).toBe(true);
    expect(err.status).toBe(400);
    expect(err.title).toBe(GENERIC_ERROR_MESSAGE);
    expect(err.errors).toEqual({});
    expect(err.detail).toBeUndefined();
  });

  it('TASK-009.3 a JSON array body maps to a generic ApiError with the HTTP status', () => {
    const s = setup();
    const err = fail(s, ['x'], 404) as ApiError;
    expect(isApiError(err)).toBe(true);
    expect(err.status).toBe(404);
    expect(err.title).toBe(GENERIC_ERROR_MESSAGE);
  });

  it('TASK-009.3 a null body maps to a generic ApiError with the HTTP status', () => {
    const s = setup();
    const err = fail(s, null, 403) as ApiError;
    expect(isApiError(err)).toBe(true);
    expect(err.status).toBe(403);
  });

  it('TASK-009.3 successful responses pass through untouched', () => {
    const s = setup();
    let value: unknown;
    s.http.get(URL).subscribe((v) => (value = v));
    s.ctrl.expectOne(URL).flush({ ok: true });
    expect(value).toEqual({ ok: true });
    expect(s.notify).not.toHaveBeenCalled();
  });
});

describe('TASK-009 error interceptor: 5xx and network errors (AC 4)', () => {
  const secret = 'SECRET-STACK-TRACE-at-Db.Connect';
  const fiveHundred = { type: 'server', title: 'Internal Server Error', status: 500, detail: secret, traceId: 'trace-xyz' };

  it('TASK-009.4 a 500 gives an ApiError whose userMessage is the generic message only', () => {
    const s = setup();
    const err = fail(s, fiveHundred, 500) as ApiError;
    expect(isApiError(err)).toBe(true);
    expect(err.status).toBe(500);
    expect(err.userMessage).toBe(GENERIC_ERROR_MESSAGE);
    for (const leaked of ['Internal Server Error', secret, 'trace-xyz']) {
      expect(err.userMessage).not.toContain(leaked);
    }
  });

  it('TASK-009.4 a 500 drops detail from the ApiError', () => {
    const s = setup();
    const err = fail(s, fiveHundred, 500) as ApiError;
    expect(err.detail).toBeUndefined();
  });

  it('TASK-009.4 a 500 notifies once with exactly the generic message', () => {
    const s = setup();
    fail(s, fiveHundred, 500);
    expect(s.notify).toHaveBeenCalledTimes(1);
    expect(s.notify).toHaveBeenCalledWith(GENERIC_ERROR_MESSAGE);
  });

  it('TASK-009.4 a 503 notifies once with the generic message', () => {
    const s = setup();
    fail(s, { title: 'Unavailable', detail: secret }, 503);
    expect(s.notify).toHaveBeenCalledTimes(1);
    expect(s.notify).toHaveBeenCalledWith(GENERIC_ERROR_MESSAGE);
  });

  it('TASK-009.4 a 500 with an HTML body still notifies the generic message', () => {
    const s = setup();
    const err = fail(s, '<html>boom</html>', 500) as ApiError;
    expect(err.status).toBe(500);
    expect(err.userMessage).toBe(GENERIC_ERROR_MESSAGE);
    expect(s.notify).toHaveBeenCalledOnce();
    expect(s.notify).toHaveBeenCalledWith(GENERIC_ERROR_MESSAGE);
  });

  it('TASK-009.4 a network error (status 0) notifies the generic message and rethrows an ApiError', () => {
    const s = setup();
    let caught: unknown;
    s.http.get(URL).subscribe({ error: (e: unknown) => (caught = e) });
    s.ctrl.expectOne(URL).error(new ProgressEvent('error'));
    expect(isApiError(caught)).toBe(true);
    expect((caught as ApiError).status).toBe(0);
    expect((caught as ApiError).userMessage).toBe(GENERIC_ERROR_MESSAGE);
    expect(s.notify).toHaveBeenCalledOnce();
    expect(s.notify).toHaveBeenCalledWith(GENERIC_ERROR_MESSAGE);
  });

  it('TASK-009.4 a 4xx does not notify', () => {
    const s = setup();
    fail(s, { title: 'Conflict', detail: 'x' }, 409);
    expect(s.notify).not.toHaveBeenCalled();
  });
});

describe('TASK-009 ErrorNotifier (AC 4)', () => {
  it('TASK-009.4 notify opens a MatSnackBar with exactly the given message', () => {
    TestBed.configureTestingModule({});
    const snackBar = TestBed.inject(MatSnackBar);
    const open = vi.spyOn(snackBar, 'open').mockReturnValue(undefined as never);
    TestBed.inject(ErrorNotifier).notify(GENERIC_ERROR_MESSAGE);
    expect(open).toHaveBeenCalledTimes(1);
    expect(open.mock.calls[0][0]).toBe(GENERIC_ERROR_MESSAGE);
  });

  it('TASK-009.4 notify renders the message in a real snack bar without extra providers', () => {
    TestBed.configureTestingModule({});
    TestBed.inject(ErrorNotifier).notify(GENERIC_ERROR_MESSAGE);
    expect(document.body.textContent).toContain(GENERIC_ERROR_MESSAGE);
  });
});
