import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject, tap } from 'rxjs';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { DecodedToken } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/Auth`;
  private tokenKey = 'auth_token';
  private afkTimer: ReturnType<typeof setTimeout> | null = null;
  private afkWarnTimer: ReturnType<typeof setTimeout> | null = null;
  private afkMinutes = 30;
  private afkWarnMinutes = 0;

  /** True once the "are you still there?" warning has fired, until the user responds or gets logged out. */
  warningActive = false;

  /** Emits the countdown length (seconds) when the AFK warning should be shown. */
  readonly afkWarning$ = new Subject<number>();
  /** Emits when the warning should be dismissed (user confirmed activity, or already logged out). */
  readonly afkWarningDismissed$ = new Subject<void>();

  constructor(private http: HttpClient, private router: Router) {}

  login(credentials: { email: string; password: string }): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/login`, credentials).pipe(
      tap(token => this.setToken(token))
    );
  }

  register(userData: any): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/register`, userData);
  }

  setToken(token: string): void {
    localStorage.setItem(this.tokenKey, token);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    if (!token) return false;
    const decoded = this.decodeToken(token);
    if (!decoded) return false;
    // Check expiry
    return decoded.exp * 1000 > Date.now();
  }

  logout(): void {
    this.stopInactivityTimer();
    localStorage.removeItem(this.tokenKey);
  }

  startInactivityTimer(minutes: number, warnMinutes: number = 0) {
    this.afkMinutes = minutes;
    // Cap at remaining token life so the timer never fires after the token is already dead
    const token = this.getToken();
    if (token) {
      const decoded = this.decodeToken(token);
      if (decoded) {
        const remainingMinutes = (decoded.exp * 1000 - Date.now()) / 60000;
        if (remainingMinutes > 0 && remainingMinutes < this.afkMinutes) {
          this.afkMinutes = remainingMinutes;
        }
      }
    }
    // The warning can't eat into more time than the full timeout allows
    this.afkWarnMinutes = Math.max(0, Math.min(warnMinutes, this.afkMinutes - 0.1));
    this.resetInactivityTimer();
  }

  /** Called on every detected user activity. While the "still there?" warning is showing, activity is
   *  ignored — only an explicit confirmStillActive() (or sign-out) call resolves it. */
  resetInactivityTimer() {
    if (this.warningActive) return;
    this.clearTimers();

    const warnMs = this.afkWarnMinutes * 60 * 1000;
    if (warnMs > 0) {
      this.afkWarnTimer = setTimeout(() => {
        this.warningActive = true;
        this.afkWarning$.next(Math.round(this.afkWarnMinutes * 60));
      }, (this.afkMinutes * 60 * 1000) - warnMs);
    }

    this.afkTimer = setTimeout(() => {
      this.warningActive = false;
      this.afkWarningDismissed$.next();
      this.logout();
      this.router.navigate(['/login'], { queryParams: { reason: 'timeout' } });
    }, this.afkMinutes * 60 * 1000);
  }

  /** User confirmed they're still active from the warning popup — resume the normal cycle. */
  confirmStillActive() {
    this.warningActive = false;
    this.afkWarningDismissed$.next();
    this.resetInactivityTimer();
  }

  private clearTimers() {
    if (this.afkTimer !== null) {
      clearTimeout(this.afkTimer);
      this.afkTimer = null;
    }
    if (this.afkWarnTimer !== null) {
      clearTimeout(this.afkWarnTimer);
      this.afkWarnTimer = null;
    }
  }

  stopInactivityTimer() {
    this.clearTimers();
    this.warningActive = false;
  }

  decodeToken(token: string): DecodedToken | null {
    try {
      const payload = token.split('.')[1];
      return JSON.parse(atob(payload));
    } catch {
      return null;
    }
  }

  getCurrentUserId(): number | null {
    const token = this.getToken();
    if (!token) return null;
    const decoded = this.decodeToken(token);
    if (!decoded) return null;
    const id = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
    return id ? parseInt(id, 10) : null;
  }

  getCurrentUserName(): string {
    const token = this.getToken();
    if (!token) return '';
    const decoded = this.decodeToken(token);
    return decoded?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ?? '';
  }

  getCurrentUserEmail(): string {
    const token = this.getToken();
    if (!token) return '';
    const decoded = this.decodeToken(token);
    return decoded?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ?? '';
  }

  getCurrentUserRole(): string {
    const token = this.getToken();
    if (!token) return '';
    const decoded = this.decodeToken(token);
    return decoded?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? '';
  }

  isAdmin(): boolean {
    return this.getCurrentUserRole() === 'Admin';
  }

  isTutor(): boolean {
    return this.getCurrentUserRole() === 'Tutor';
  }

  isStudent(): boolean {
    return this.getCurrentUserRole() === 'Student';
  }
}
