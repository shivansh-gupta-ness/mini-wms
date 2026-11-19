import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_PATH } from './api-base';

export interface ApiRequestOptions {
  params?: HttpParams | Record<string, string | number | boolean | ReadonlyArray<string | number | boolean>>;
  context?: HttpContext;
}

/** The single way feature services call the backend. Prefixes `API_BASE_PATH` to a relative path. */
@Injectable({ providedIn: 'root' })
export class ApiClient {
  private readonly http = inject(HttpClient);

  get<T>(path: string, options?: ApiRequestOptions): Observable<T> {
    return this.http.get<T>(this.url(path), options);
  }

  post<T>(path: string, body?: unknown, options?: ApiRequestOptions): Observable<T> {
    return this.http.post<T>(this.url(path), body, options);
  }

  put<T>(path: string, body?: unknown, options?: ApiRequestOptions): Observable<T> {
    return this.http.put<T>(this.url(path), body, options);
  }

  patch<T>(path: string, body?: unknown, options?: ApiRequestOptions): Observable<T> {
    return this.http.patch<T>(this.url(path), body, options);
  }

  delete<T>(path: string, options?: ApiRequestOptions): Observable<T> {
    return this.http.delete<T>(this.url(path), options);
  }

  private url(path: string): string {
    if (/^(?:[a-z][a-z\d+.-]*:)?\/\//i.test(path)) {
      throw new Error('ApiClient accepts relative paths only; the API host is never part of a path.');
    }
    if (/[\\?#]/.test(path) || path.split('/').some((segment) => segment === '..' || segment === '.')) {
      throw new Error('ApiClient paths cannot contain backslashes, dot segments, a query string or a fragment; use the params option.');
    }
    return `${API_BASE_PATH}/${path.replace(/^\/+/, '')}`;
  }
}
