import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map, shareReplay, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface Faq {
  faq_ID: number;
  question: string;
  answer: string;
  faq_Category_ID: number;
  applicable_Pages: string | null;
}

export interface FaqCategory {
  faq_Category_ID: number;
  category_Name: string;
}

export interface FaqPayload {
  question: string;
  answer: string;
  FAQ_Category_ID: number;
  Applicable_Pages: string;
}

@Injectable({ providedIn: 'root' })
export class FaqService {
  private apiUrl = `${environment.apiUrl}/AdminContent`;
  private faqs$: Observable<Faq[]> | null = null;

  constructor(private http: HttpClient) {}

  // Cached — every <app-help-icon> on a page shares one network call instead of one each.
  getFaqs(forceRefresh = false): Observable<Faq[]> {
    if (!this.faqs$ || forceRefresh) {
      this.faqs$ = this.http.get<Faq[]>(`${this.apiUrl}/faqs`).pipe(shareReplay(1));
    }
    return this.faqs$;
  }

  getCategories(): Observable<FaqCategory[]> {
    return this.http.get<FaqCategory[]>(`${this.apiUrl}/faq-categories`);
  }

  getFaqsForPage(pageKey: string): Observable<Faq[]> {
    return this.getFaqs().pipe(
      map(faqs => faqs.filter(f => this.pagesOf(f).includes(pageKey)))
    );
  }

  pagesOf(faq: Faq): string[] {
    return (faq.applicable_Pages ?? '').split(',').map(s => s.trim()).filter(Boolean);
  }

  createFaq(payload: FaqPayload): Observable<any> {
    return this.http.post(`${this.apiUrl}/faqs`, payload).pipe(tap(() => this.refreshFaqs()));
  }

  updateFaq(id: number, payload: FaqPayload): Observable<any> {
    return this.http.put(`${this.apiUrl}/faqs/${id}`, payload).pipe(tap(() => this.refreshFaqs()));
  }

  deleteFaq(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/faqs/${id}`).pipe(tap(() => this.refreshFaqs()));
  }

  // Invalidates the cached FAQ list so the next getFaqs() call (and thus every open help icon
  // / the FAQ viewer) refetches fresh data instead of serving stale cached results.
  refreshFaqs() {
    this.faqs$ = null;
  }
}
