import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Module, ModuleBulkImportResult } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ModuleService {
  private apiUrl = `${environment.apiUrl}/Modules`;

  constructor(private http: HttpClient) {}

  getModules(): Observable<Module[]> {
    return this.http.get<Module[]>(this.apiUrl);
  }

  createModule(data: Module): Observable<string> {
    return this.http.post(this.apiUrl, data, { responseType: 'text' });
  }

  downloadBulkTemplate(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/bulk-template`, { responseType: 'blob' });
  }

  bulkCreateModules(file: File): Observable<ModuleBulkImportResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ModuleBulkImportResult>(`${this.apiUrl}/bulk`, formData);
  }
}
