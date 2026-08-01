import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) {
        // Token expired or invalid — clear session and redirect to login
        authService.logout();
        router.navigate(['/login'], {
          queryParams: { sessionExpired: 'true' }
        });
      }
      return throwError(() => err);
    })
  );
};

/**
 * Extracts a human-readable error message from an HttpErrorResponse.
 * Handles plain strings, ValidationProblemDetails objects, and network errors.
 */
export function extractErrorMessage(err: any, fallback: string): string {
  if (!err) return fallback;

  // Network error (no connection)
  if (err.status === 0) return 'Unable to reach the server. Please check your connection.';

  const body = err.error;
  if (!body) return fallback;

  // Plain string response
  if (typeof body === 'string' && body.trim()) return body.trim();

  // { error: "..." } from our global exception handler
  if (typeof body.error === 'string' && body.error.trim()) return body.error.trim();

  // { message: "..." }
  if (typeof body.message === 'string' && body.message.trim()) return body.message.trim();

  // ValidationProblemDetails: { title: "...", errors: { field: ["msg"] } }
  if (typeof body.title === 'string') {
    const errors = body.errors as Record<string, string[]> | undefined;
    if (errors) {
      const firstMessages = Object.values(errors).flat();
      if (firstMessages.length > 0) return firstMessages[0];
    }
    return body.title;
  }

  return fallback;
}
