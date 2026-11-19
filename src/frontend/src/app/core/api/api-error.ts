/** Message shown for 5xx and network failures. Never contains title, detail or traceId. */
export const GENERIC_ERROR_MESSAGE = 'Something went wrong. Please try again.';

export interface ApiErrorInit {
  status: number;
  type?: string;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

/** Typed form of the backend's `application/problem+json` body (ADR-003). */
export class ApiError extends Error {
  readonly status: number;
  readonly type?: string;
  readonly title: string;
  /** Present only on 4xx text written by the backend (for example a 409). Never set from a 5xx. */
  readonly detail?: string;
  /** Field errors keyed by camelCase path, for example `name` or `lines.0.qty`. */
  readonly errors: Record<string, string[]>;
  /** For logs and support only; never shown to the user. */
  readonly traceId?: string;

  constructor(init: ApiErrorInit) {
    super(init.title ?? GENERIC_ERROR_MESSAGE);
    this.name = 'ApiError';
    this.status = init.status;
    this.type = init.type;
    this.title = init.title ?? GENERIC_ERROR_MESSAGE;
    this.detail = init.detail;
    this.errors = init.errors ?? {};
    this.traceId = init.traceId;
  }

  /** Safe to show: generic for network and 5xx; for 4xx the backend's own `detail` text, else its title. */
  get userMessage(): string {
    return this.status === 0 || this.status >= 500 ? GENERIC_ERROR_MESSAGE : (this.detail ?? this.title);
  }
}

export function isApiError(value: unknown): value is ApiError {
  return value instanceof ApiError;
}
