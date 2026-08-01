import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ReportService {
  private apiUrl = `${environment.apiUrl}/Reports`;
  constructor(private http: HttpClient) {}

  private params(from?: string, to?: string): HttpParams {
    let p = new HttpParams();
    if (from) p = p.set('from', from);
    if (to)   p = p.set('to', to);
    return p;
  }

  // ── Simple List Reports ──────────────────────────────────────────────────────
  getTutorHoursReport(from?: string, to?: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/tutor-hours`, { params: this.params(from, to) });
  }
  getTutorRatingsReport(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/tutor-ratings`);
  }
  getModuleCatalogue(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/module-catalogue`);
  }
  getSessionsReport(from?: string, to?: string, sessionType?: string): Observable<any[]> {
    let p = this.params(from, to);
    if (sessionType && sessionType !== 'All') p = p.set('sessionType', sessionType);
    return this.http.get<any[]>(`${this.apiUrl}/sessions`, { params: p });
  }
  getMonthlyStudentEnrollments(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/monthly-student-enrollments`);
  }

  // ── Transactional Reports ────────────────────────────────────────────────────
  getTutorHoursDetail(from?: string, to?: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/tutor-hours-detail`, { params: this.params(from, to) });
  }
  getRevenueDetail(from?: string, to?: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/revenue-detail`, { params: this.params(from, to) });
  }

  // ── Management / Chart Reports ───────────────────────────────────────────────
  getModuleEnrollments(from?: string, to?: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/module-enrollments`, { params: this.params(from, to) });
  }
  getMonthlyIncome(from?: string, to?: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/monthly-income`, { params: this.params(from, to) });
  }

  // ── Custom Query ─────────────────────────────────────────────────────────────
  getCustomReport(entity: string, groupBy: string, from?: string, to?: string): Observable<any[]> {
    let p = new HttpParams().set('entity', entity).set('groupBy', groupBy);
    if (from) p = p.set('from', from);
    if (to)   p = p.set('to', to);
    return this.http.get<any[]>(`${this.apiUrl}/custom`, { params: p });
  }
}
