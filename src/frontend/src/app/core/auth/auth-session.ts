import { Injectable, signal } from '@angular/core';

/** Holds the bearer token in memory only (ADR-004): never written to localStorage or sessionStorage. */
@Injectable({ providedIn: 'root' })
export class AuthSession {
  private readonly tokenState = signal<string | null>(null);

  readonly token = this.tokenState.asReadonly();

  setToken(token: string): void {
    this.tokenState.set(token);
  }

  clear(): void {
    this.tokenState.set(null);
  }
}
