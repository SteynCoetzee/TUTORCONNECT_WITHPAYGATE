import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';
import { environment } from '../../environments/environment';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router      = inject(Router);
  const authService = inject(AuthService);
  const toast       = inject(ToastService);

  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      // Only handle errors from our own API — skip external calls (e.g. Nominatim)
      if (!req.url.startsWith(environment.apiUrl)) {
        return throwError(() => err);
      }

      if (err.status === 401) {
        // Token expired or invalid — clear session and redirect to login
        authService.logout();
        router.navigate(['/login'], { queryParams: { sessionExpired: 'true' } });
      } else {
        // Show a user-friendly toast for all other errors
        const msg = extractErrorMessage(err, defaultMessage(err.status));
        toast.error(msg);
      }

      return throwError(() => err);
    })
  );
};

/**
 * Returns a sensible fallback message for a given HTTP status code.
 */
function defaultMessage(status: number): string {
  if (status === 0)   return 'Unable to reach the server. Please check your internet connection.';
  if (status === 400) return 'The request was invalid. Please check your input and try again.';
  if (status === 403) return "You don't have permission to do that.";
  if (status === 404) return 'The requested resource was not found.';
  if (status === 409) return 'This action conflicts with existing data. Please refresh and try again.';
  if (status === 413) return 'The file you uploaded is too large.';
  if (status === 422) return 'The submitted data is invalid. Please check your input and try again.';
  if (status >= 500)  return 'Something went wrong on the server. Please try again in a moment.';
  return 'An unexpected error occurred. Please try again.';
}

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
