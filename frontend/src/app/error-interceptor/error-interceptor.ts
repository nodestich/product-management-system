import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const problem = error.error as ProblemDetails | null;

      const validation = problem?.errors
        ? Object.values(problem.errors).flat().join(' ')
        : undefined;

      const message = problem?.detail ?? validation ?? problem?.title ?? 'Unexpected error';

      snackBar.open(message, 'Dismiss', { duration: 5000 });
      return throwError(() => error);
    })
  );
};
