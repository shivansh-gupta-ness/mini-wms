import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Explains an empty list and how to add the first item. Project an action button as content. */
@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="empty-state">
      <h2 class="empty-state__title">{{ title() }}</h2>
      <p class="empty-state__message">{{ message() }}</p>
      <div class="empty-state__action"><ng-content /></div>
    </div>
  `,
  styles: `
    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 32px 16px;
      text-align: center;
    }
  `,
})
export class EmptyState {
  readonly title = input.required<string>();
  readonly message = input.required<string>();
}
