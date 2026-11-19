import { HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ApiError } from '../api/api-error';
import { ErrorNotifier } from './error-notifier';

/**
 * Set to true on a request (`new HttpContext().set(SKIP_ERROR_NOTIFICATION, true)`) when the caller shows its own
 * message, so a 5xx or network failure does not also open the global snackbar. The `ApiError` is still thrown.
 */
export const SKIP_ERROR_NOTIFICATION = new HttpContextToken<boolean>(() => false);

const isRecord = (value: unknown): value is Record<string, unknown> =>
  typeof value === 'object' && value !== null && !Array.isArray(value);

const text = (value: unknown): string | undefined =>
  typeof value === 'string' && value.length > 0 ? value : undefined;

function parseErrors(value: unknown): Record<string, string[]> {
  const result: Record<string, string[]> = {};
  if (!isRecord(value)) {
    return result;
  }
  for (const [key, messages] of Object.entries(value)) {
    if (Array.isArray(messages)) {
      result[key] = messages.filter((m): m is string => typeof m === 'string');
    } else if (typeof messages === 'string') {
      result[key] = [messages];
    }
  }
  return result;
}

function toApiError(response: HttpErrorResponse): ApiError {
  const body: unknown = response.error;
  if (response.status === 0 || !isRecord(body)) {
    return new ApiError({ status: response.status });
  }
  const isServerError = response.status >= 500;
  return new ApiError({
    status: response.status,
    type: text(body['type']),
    title: text(body['title']),
    detail: isServerError ? undefined : text(body['detail']),
    errors: parseErrors(body['errors']),
    traceId: text(body['traceId']),
  });
}

/** Maps every failed HTTP call to a typed `ApiError` and rethrows it. 401 is not handled here (auth module). */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifier = inject(ErrorNotifier);
  return next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }
      const apiError = toApiError(error);
      if ((apiError.status === 0 || apiError.status >= 500) && !req.context.get(SKIP_ERROR_NOTIFICATION)) {
        notifier.notify(apiError.userMessage);
      }
      return throwError(() => apiError);
    }),
  );
};
