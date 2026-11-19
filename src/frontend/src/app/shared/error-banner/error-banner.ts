import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/**
 * Banner for messages that belong to no field (a 409, unmatched server errors). Hidden when empty.
 * Project an action (for example a "Reload" button) into the banner with `<app-error-banner><button ...>`.
 */
@Component({
  selector: 'app-error-banner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (visible()) {
      <div class="error-banner" role="alert">
        <div class="error-banner__messages">
          @for (text of list(); track $index) {
            <p>{{ text }}</p>
          }
        </div>
        <div class="error-banner__action"><ng-content /></div>
      </div>
    }
  `,
  styles: `
    .error-banner {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      padding: 8px 16px;
      margin-bottom: 16px;
      border-radius: 4px;
      background: var(--mat-sys-error-container, #fdecea);
      color: var(--mat-sys-on-error-container, #410002);
    }
    .error-banner p {
      margin: 4px 0;
    }
  `,
})
export class ErrorBanner {
  /** Messages to show, one paragraph each. */
  readonly messages = input<readonly string[]>([]);
  /** Convenience for a single message; shown before `messages`. */
  readonly message = input<string | null | undefined>(null);

  protected readonly list = computed(() => {
    const single = this.message();
    return [...(single ? [single] : []), ...this.messages().filter((m) => m.length > 0)];
  });
  protected readonly visible = computed(() => this.list().length > 0);
}
