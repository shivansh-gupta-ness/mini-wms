import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/** Shows an unobtrusive, dismissible message. MatSnackBar announces it through an aria-live region. */
@Injectable({ providedIn: 'root' })
export class ErrorNotifier {
  private readonly snackBar = inject(MatSnackBar);

  notify(message: string): void {
    this.snackBar.open(message, 'Dismiss', {
      duration: 8000,
      politeness: 'polite',
      verticalPosition: 'bottom',
      horizontalPosition: 'center',
    });
  }
}
