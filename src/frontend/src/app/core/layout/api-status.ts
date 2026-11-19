import { HttpContext } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, exhaustMap, map, of, timeout, timer } from 'rxjs';
import { ApiClient } from '../api/api-client';
import { SKIP_ERROR_NOTIFICATION } from '../errors/error.interceptor';

export const HEALTH_POLL_INTERVAL_MS = 30_000;
export const HEALTH_TIMEOUT_MS = 5_000;

export type ApiStatusState = 'unknown' | 'ok' | 'unavailable';

/** Top-bar indicator: polls `health` through `ApiClient` and shows the result as text. */
@Component({
  selector: 'app-api-status',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="api-status" role="status" aria-live="polite">{{ label() }}</span>`,
})
export class ApiStatus {
  private readonly api = inject(ApiClient);
  protected readonly state = signal<ApiStatusState>('unknown');

  protected readonly label = computed(() => {
    const state = this.state();
    return state === 'unknown' ? '' : `API: ${state}`;
  });

  constructor() {
    const context = new HttpContext().set(SKIP_ERROR_NOTIFICATION, true);
    timer(0, HEALTH_POLL_INTERVAL_MS)
      .pipe(
        exhaustMap(() =>
          this.api.get<{ status: string }>('health', { context }).pipe(
            timeout(HEALTH_TIMEOUT_MS),
            map((body): ApiStatusState => (isOk(body) ? 'ok' : 'unavailable')),
            catchError(() => of<ApiStatusState>('unavailable')),
          ),
        ),
        takeUntilDestroyed(inject(DestroyRef)),
      )
      .subscribe((state) => this.state.set(state));
  }
}

function isOk(body: unknown): boolean {
  return typeof body === 'object' && body !== null && (body as { status?: unknown }).status === 'ok';
}
