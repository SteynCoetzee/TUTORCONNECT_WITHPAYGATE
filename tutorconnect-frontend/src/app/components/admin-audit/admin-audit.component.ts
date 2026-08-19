import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { extractErrorMessage } from '../../interceptors/error.interceptor';
import { HelpIconComponent } from '../help-icon/help-icon.component';

interface AuditEntry {
  audit_Log_ID: number;
  audit_Date: string;
  audit_Time: string;
  user_ID: number;
  userName: string;
  transaction_Type: string;
  critical_Data: string;
}

@Component({
  selector: 'app-admin-audit',
  standalone: true,
  imports: [CommonModule, FormsModule, HelpIconComponent],
  templateUrl: './admin-audit.component.html',
  styleUrl: './admin-audit.component.css'
})
export class AdminAuditComponent implements OnInit {
  logs: AuditEntry[] = [];
  loading = false;
  filterType = '';
  filterCategory = '';
  searchQuery = '';
  filterDateFrom = '';
  filterDateTo = '';
  private apiUrl = environment.apiUrl;

  readonly categoryMap: Record<string, string[]> = {
    'Login / Security': ['User Login', 'User Registered', 'Password Changed', 'Password Reset'],
    'Bookings': ['Session Booked', 'Booking Cancelled', 'Booking Slot Created', 'Booking Slot Deleted'],
    'Enrollments': ['Student Enrolled', 'Student Unenrolled'],
    'Modules': ['Module Created', 'Module Updated', 'Module Deleted'],
    'Content': ['Assignment Created', 'Quiz Created', 'Announcement Created', 'Resource Added'],
    'Reviews / Approvals': ['Hours Approved', 'Hours Rejected', 'Tutor Review Submitted', 'Testimonial Approved'],
    'Payments': ['Payment Initiated', 'Payment Completed'],
    'Admin Actions': ['User Archived', 'User Deleted', 'User Role Changed'],
  };

  get filteredLogs(): AuditEntry[] {
    const q = this.searchQuery.toLowerCase().trim();
    const categoryTypes = this.filterCategory ? this.categoryMap[this.filterCategory] ?? [] : [];
    return this.logs.filter(l => {
      const matchType     = !this.filterType     || l.transaction_Type === this.filterType;
      const matchCategory = !this.filterCategory || categoryTypes.some(t => l.transaction_Type.includes(t.split(' ')[1] ?? t));
      const matchFrom     = !this.filterDateFrom || l.audit_Date >= this.filterDateFrom;
      const matchTo       = !this.filterDateTo   || l.audit_Date <= this.filterDateTo;
      const matchSearch   = !q ||
        l.userName.toLowerCase().includes(q) ||
        l.critical_Data?.toLowerCase().includes(q) ||
        l.transaction_Type.toLowerCase().includes(q) ||
        String(l.user_ID).includes(q);
      return matchType && matchCategory && matchFrom && matchTo && matchSearch;
    });
  }

  constructor(private http: HttpClient) {}

  ngOnInit() { this.loadLogs(); }

  loadLogs() {
    this.loading = true;
    this.http.get<AuditEntry[]>(`${this.apiUrl}/AuditLogs`).subscribe({
      next: (data) => { this.logs = data; this.loading = false; },
      error: (_err) => { this.loading = false; }
    });
  }

  get transactionTypes(): string[] {
    return [...new Set(this.logs.map(l => l.transaction_Type))].sort();
  }

  get categoryKeys(): string[] { return Object.keys(this.categoryMap); }

  resetFilters() {
    this.filterType = '';
    this.filterCategory = '';
    this.searchQuery = '';
    this.filterDateFrom = '';
    this.filterDateTo = '';
  }

  formatTime(t: string): string {
    try { return t.substring(0, 5); } catch { return t; }
  }

  getBadgeClass(type: string): string {
    if (type.includes('Login') || type.includes('Registered')) return 'action-badge badge-blue';
    if (type.includes('Deleted') || type.includes('Rejected') || type.includes('Cancelled')) return 'action-badge badge-red';
    if (type.includes('Created') || type.includes('Enrolled') || type.includes('Booked') || type.includes('Submitted')) return 'action-badge badge-green';
    if (type.includes('Approved') || type.includes('Graded')) return 'action-badge badge-purple';
    if (type.includes('Changed') || type.includes('Updated') || type.includes('Unenrolled')) return 'action-badge badge-orange';
    return 'action-badge badge-gray';
  }
}
