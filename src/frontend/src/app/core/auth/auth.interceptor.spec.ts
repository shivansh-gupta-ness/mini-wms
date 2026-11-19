import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthSession } from './auth-session';
import { authInterceptor } from './auth.interceptor';

function setup(): { http: HttpClient; ctrl: HttpTestingController; session: AuthSession } {
  TestBed.configureTestingModule({
    providers: [provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting()],
  });
  return {
    http: TestBed.inject(HttpClient),
    ctrl: TestBed.inject(HttpTestingController),
    session: TestBed.inject(AuthSession),
  };
}

describe('TASK-009 token interceptor (AC 2)', () => {
  afterEach(() => vi.restoreAllMocks());

  it('TASK-009.2 adds the bearer token when a session token exists', () => {
    const { http, ctrl, session } = setup();
    session.setToken('abc');
    http.get('/api/v1/x').subscribe();
    const req = ctrl.expectOne('/api/v1/x');
    expect(req.request.headers.get('Authorization')).toBe('Bearer abc');
    req.flush(null);
  });

  it('TASK-009.2 adds no Authorization header when there is no token', () => {
    const { http, ctrl } = setup();
    http.get('/api/v1/x').subscribe();
    const req = ctrl.expectOne('/api/v1/x');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush(null);
  });

  it('TASK-009.2 adds no Authorization header after the session is cleared', () => {
    const { http, ctrl, session } = setup();
    session.setToken('abc');
    session.clear();
    http.get('/api/v1/x').subscribe();
    const req = ctrl.expectOne('/api/v1/x');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush(null);
  });

  it('TASK-009.2 uses the latest token after it is replaced', () => {
    const { http, ctrl, session } = setup();
    session.setToken('abc');
    session.setToken('def');
    http.get('/api/v1/x').subscribe();
    const req = ctrl.expectOne('/api/v1/x');
    expect(req.request.headers.get('Authorization')).toBe('Bearer def');
    req.flush(null);
  });

  it('TASK-009.2 does not write the token to localStorage or sessionStorage', () => {
    const setItem = vi.spyOn(Storage.prototype, 'setItem');
    const { http, ctrl, session } = setup();
    session.setToken('secret-token-value');
    http.get('/api/v1/x').subscribe();
    ctrl.expectOne('/api/v1/x').flush(null);
    expect(setItem).not.toHaveBeenCalled();
    expect(JSON.stringify({ ...localStorage })).not.toContain('secret-token-value');
    expect(JSON.stringify({ ...sessionStorage })).not.toContain('secret-token-value');
  });
});
