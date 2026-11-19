import { HttpClient, HttpContext, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { authInterceptor } from '../auth/auth.interceptor';
import { AuthSession } from '../auth/auth-session';
import { ErrorNotifier } from '../errors/error-notifier';
import { errorInterceptor, SKIP_ERROR_NOTIFICATION } from '../errors/error.interceptor';
import { applyServerErrors } from '../../shared/forms/apply-server-errors';
import { ApiClient } from './api-client';
import { ApiError } from './api-error';

describe('TASK-009 review fixes', () => {
  function setup() {
    const notify = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
        provideHttpClientTesting(),
        { provide: ErrorNotifier, useValue: { notify } },
      ],
    });
    return { http: TestBed.inject(HttpClient), ctrl: TestBed.inject(HttpTestingController), notify };
  }

  it('TASK-009.2 the bearer token is not sent to a URL outside the API', () => {
    const { http, ctrl } = setup();
    TestBed.inject(AuthSession).setToken('secret');
    http.get('https://cdn.example.com/x').subscribe();
    http.get('/assets/config.json').subscribe();
    expect(ctrl.expectOne('https://cdn.example.com/x').request.headers.has('Authorization')).toBe(false);
    expect(ctrl.expectOne('/assets/config.json').request.headers.has('Authorization')).toBe(false);
  });

  it('TASK-009.4 a request that opts out does not open the snackbar, but the ApiError is still thrown', () => {
    const { http, ctrl, notify } = setup();
    let caught: unknown;
    http
      .get('/api/v1/x', { context: new HttpContext().set(SKIP_ERROR_NOTIFICATION, true) })
      .subscribe({ error: (e: unknown) => (caught = e) });
    ctrl.expectOne('/api/v1/x').flush({ title: 'Boom' }, { status: 500, statusText: 'Err' });
    expect(notify).not.toHaveBeenCalled();
    expect(caught).toBeInstanceOf(ApiError);
  });

  it('TASK-009.3 for a 4xx the user message is the backend detail text when there is one', () => {
    expect(new ApiError({ status: 409, title: 'Conflict', detail: 'Location A-01 is already occupied.' }).userMessage)
      .toBe('Location A-01 is already occupied.');
    expect(new ApiError({ status: 404, title: 'Not found' }).userMessage).toBe('Not found');
  });

  it.each(['../admin', 'a/../b', './x', String.raw`a\b`, 'x?y=1', 'x#frag'])(
    'TASK-009.1 the API client refuses the path %s',
    (path) => {
      setup();
      expect(() => TestBed.inject(ApiClient).get(path)).toThrow();
    },
  );

  it('TASK-009.5 a server error with no messages is skipped and bracket paths find the control', () => {
    const form = new FormGroup({ name: new FormControl(''), lines: new FormGroup({ '0': new FormGroup({ qty: new FormControl(0) }) }) });
    const unmatched = applyServerErrors(
      form,
      new ApiError({ status: 400, errors: { name: [], 'lines[0].qty': ['Too big.'] } }),
    );
    expect(unmatched).toEqual([]);
    expect(form.get('name')?.errors).toBeNull();
    expect(form.get('lines.0.qty')?.getError('server')).toBe('Too big.');
  });
});
