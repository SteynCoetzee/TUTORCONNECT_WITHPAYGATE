import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface UserNavSetting {
  userId: number;
  hasOverride: boolean;
  hiddenItems: string[];
}

/**
 * Per-user overrides on top of the role-wide RoleNavPermissionsService defaults. Not cached —
 * unlike the role-level list (read by every sidebar/topnav on every page), this is only hit once
 * per session (each user's own self-check) or on-demand when an admin picks someone in the
 * Business Logic "customize for a specific user" dropdown, so staleness isn't worth the tradeoff.
 */
@Injectable({ providedIn: 'root' })
export class UserNavPermissionsService {
  private apiUrl = `${environment.apiUrl}/UserNavPermissions`;

  constructor(private http: HttpClient) {}

  get(userId: number): Observable<UserNavSetting> {
    return this.http.get<UserNavSetting>(`${this.apiUrl}/${userId}`);
  }

  update(userId: number, hiddenItems: string[]): Observable<any> {
    return this.http.put(`${this.apiUrl}/${userId}`, { hiddenItems });
  }

  remove(userId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${userId}`);
  }
}
