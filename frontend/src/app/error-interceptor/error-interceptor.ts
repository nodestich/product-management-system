import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Injector, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';

import { AuthService } from '../../core/auth/auth-service';
import type { ProblemDetails } from './problem-details';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);
  const injector = inject(Injector);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const problem = error.error as ProblemDetails | null;

      // Auth calls report their own failures: the startup session check returning 401
      // just means "not signed in", and the login page shows an inline message.
      if (req.url.includes('/api/auth/')) {
        return throwError(() => error);
      }

      const validation = problem?.errors
        ? Object.values(problem.errors).flat().join(' ')
        : undefined;

      const message = problem?.detail ?? validation ?? problem?.title ?? 'Unexpected error';

      snackBar.open(message, 'Dismiss', { duration: 5000 });

      if (error.status === 401) {
        // Resolved lazily: catchError runs outside the injection context.
        injector.get(AuthService).clear();
        injector.get(Router).navigate(['/login']);
      }

      return throwError(() => error);
    })
  );
};