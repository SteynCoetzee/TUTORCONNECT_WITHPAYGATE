import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { extractErrorMessage } from '../../interceptors/error.interceptor';

interface PaymentRow {
  payment_ID: number;
  amount: number;
  payment_Date: string;
  payment_Status: string;
  module_Code: string;
  payment_Reference: string | null;
  enrollment_Items_Json: string | null;
  studentName: string;
  studentEmail: string;
  student_ID: number;
  processing?: boolean;
  processResult?: string;
  processError?: string;
}

@Component({
  selector: 'app-admin-payments',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './admin-payments.component.html',
  styleUrl: './admin-payments.component.css'
})
export class AdminPaymentsComponent implements OnInit {
  payments: PaymentRow[] = [];
  loading = false;
  errorMessage = '';
  filterStatus = '';
  searchQuery = '';

  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.errorMessage = '';
    this.http.get<PaymentRow[]>(`${this.apiUrl}/PayFast/payments`).subscribe({
      next: (data) => { this.payments = data; this.loading = false; },
      error: (err) => { this.errorMessage = extractErrorMessage(err, 'Failed to load payments.'); this.loading = false; }
    });
  }

  get filtered(): PaymentRow[] {
    const q = this.searchQuery.toLowerCase().trim();
    return this.payments.filter(p => {
      const matchStatus = !this.filterStatus || p.payment_Status === this.filterStatus;
      const matchSearch = !q || p.studentName.toLowerCase().includes(q) ||
        p.studentEmail.toLowerCase().includes(q) ||
        p.module_Code.toLowerCase().includes(q) ||
        String(p.payment_ID).includes(q);
      return matchStatus && matchSearch;
    });
  }

  processPayment(p: PaymentRow) {
    p.processing = true;
    p.processResult = '';
    p.processError = '';
    this.http.post(`${this.apiUrl}/PayFast/admin/process/${p.payment_ID}`, {}, { responseType: 'text' }).subscribe({
      next: (msg) => {
        p.processing = false;
        p.processResult = msg || 'Enrollment processed successfully.';
        p.payment_Status = 'Paid';
      },
      error: (err) => {
        p.processing = false;
        p.processError = extractErrorMessage(err, 'Failed to process enrollment.');
      }
    });
  }

  getItems(json: string | null): string {
    if (!json) return '—';
    try {
      const items = JSON.parse(json) as any[];
      return items.map(i => {
        const name = i.ModuleName ?? i.moduleName ?? i.ModuleCode ?? i.moduleCode ?? '?';
        const type = i.SessionType ?? i.sessionType ?? '?';
        return `${name} (${type})`;
      }).join(', ');
    } catch { return json ?? '—'; }
  }

  badgeClass(status: string): string {
    if (status === 'Paid')    return 'badge badge-green';
    if (status === 'Pending') return 'badge badge-warning';
    if (status === 'Failed')  return 'badge badge-red';
    return 'badge badge-gray';
  }
}
