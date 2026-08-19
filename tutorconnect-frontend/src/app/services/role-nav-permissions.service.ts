import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map, shareReplay, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface RoleNavSetting {
  role_Nav_Setting_ID: number;
  role: string;
  hiddenItems: string[];
}

@Injectable({ providedIn: 'root' })
export class RoleNavPermissionsService {
  private apiUrl = `${environment.apiUrl}/RoleNavPermissions`;
  private cache$: Observable<RoleNavSetting[]> | null = null;

  constructor(private http: HttpClient) {}

  getAll(): Observable<RoleNavSetting[]> {
    if (!this.cache$) {
      this.cache$ = this.http.get<RoleNavSetting[]>(this.apiUrl).pipe(shareReplay(1));
    }
    return this.cache$;
  }

  getHiddenItemsForRole(role: string): Observable<string[]> {
    return this.getAll().pipe(map(all => all.find(s => s.role === role)?.hiddenItems ?? []));
  }

  updateHiddenItems(role: string, hiddenItems: string[]): Observable<any> {
    return this.http.put(`${this.apiUrl}/${role}`, { hiddenItems }).pipe(tap(() => { this.cache$ = null; }));
  }
}
