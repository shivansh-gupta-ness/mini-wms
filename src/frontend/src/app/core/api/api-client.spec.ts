import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_PATH } from './api-base';
import { ApiClient } from './api-client';

type Method = 'get' | 'post' | 'put' | 'patch' | 'delete';
const methods: Method[] = ['get', 'post', 'put', 'patch', 'delete'];

function setup(): { client: ApiClient; http: HttpTestingController } {
  TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  return { client: TestBed.inject(ApiClient), http: TestBed.inject(HttpTestingController) };
}

function send(client: ApiClient, method: Method, path: string): void {
  if (method === 'get' || method === 'delete') {
    client[method](path).subscribe();
  } else {
    client[method](path, { a: 1 }).subscribe();
  }
}

describe('TASK-009 ApiClient base path (AC 1)', () => {
  it('TASK-009.1 API_BASE_PATH equals /api/v1', () => {
    expect(API_BASE_PATH).toBe('/api/v1');
  });

  for (const method of methods) {
    it(`TASK-009.1 ${method} prefixes /api/v1 for a path without a leading slash`, () => {
      const { client, http } = setup();
      send(client, method, 'items/5');
      const req = http.expectOne('/api/v1/items/5');
      expect(req.request.method).toBe(method.toUpperCase());
      req.flush(null);
      http.verify();
    });

    it(`TASK-009.1 ${method} prefixes /api/v1 for a path with a leading slash`, () => {
      const { client, http } = setup();
      send(client, method, '/items/5');
      const req = http.expectOne('/api/v1/items/5');
      expect(req.request.method).toBe(method.toUpperCase());
      req.flush(null);
      http.verify();
    });
  }

  it('TASK-009.1 query params are passed through on the prefixed URL', () => {
    const { client, http } = setup();
    client.get('items', { params: { page: 2 } }).subscribe();
    const req = http.expectOne('/api/v1/items?page=2');
    req.flush([]);
    http.verify();
  });

  for (const url of ['http://example.test/x', 'https://example.test/x', '//example.test/x']) {
    for (const method of methods) {
      it(`TASK-009.1 ${method} throws for the non-relative URL ${url}`, () => {
        const { client, http } = setup();
        expect(() => send(client, method, url)).toThrow();
        http.expectNone(() => true);
        http.verify();
      });
    }
  }
});
