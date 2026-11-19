import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { API_BASE_PATH } from '../api/api-base';
import { AuthSession } from './auth-session';

/**
 * Adds `Authorization: Bearer <token>` when a session token exists and the request goes to our own API
 * (`/api/v1/...`). Requests to any other URL are left untouched, so the token never reaches a third party.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthSession).token();
  if (!token || !req.url.startsWith(`${API_BASE_PATH}/`)) {
    return next(req);
  }
  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
