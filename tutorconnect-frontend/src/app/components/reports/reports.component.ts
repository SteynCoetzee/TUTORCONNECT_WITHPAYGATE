import { Component, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportService } from '../../services/report.service';
import { AuthService } from '../../services/auth.service';
import { Chart, registerables } from 'chart.js';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';
Chart.register(...registerables);

type ReportTab = 'simple' | 'transactional' | 'criteria' | 'management';
type SimpleReport = 'tutor-summary' | 'module-catalogue' | 'monthly-enrollments';
type TransactionalReport = 'revenue-detail' | 'tutor-hours-detail';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.css'
})
export class ReportsComponent implements OnInit, OnDestroy {
  @ViewChild('mgmtCanvas') mgmtCanvas!: ElementRef<HTMLCanvasElement>;

  activeTab: ReportTab = 'simple';
  loading = false;
  errorMessage = '';
  successMessage = '';

  // ── Simple list ──────────────────────────────────────────────────────────────
  activeSimple: SimpleReport = 'tutor-summary';
  simpleData: any[] | null = null;
  simpleSearch    = '';
  simpleMinRating = '';

  // ── Transactional ────────────────────────────────────────────────────────────
  activeTx: TransactionalReport = 'revenue-detail';
  txData: any[] | null = null;
  txFrom = '';
  txTo   = '';
  txGroups: { key: string; rows: any[]; subtotal: number }[] = [];

  // ── Adjustable Criteria ──────────────────────────────────────────────────────
  criteriaFrom = '';
  criteriaTo   = '';
  criteriaType = 'All';
  criteriaData: any[] | null = null;

  // ── Management / Graph ───────────────────────────────────────────────────────
  mgmtData: any[] | null = null;
  private mgmtChart: Chart | null = null;
  mgmtReady = false;
  mgmtChartType: 'bar' | 'pie' | 'doughnut' = 'bar';
  mgmtFrom = '';
  mgmtTo   = '';

  private generatedBy = '';
  private logoBase64  = '';
  private readonly BRAND_COLOR  = '#1a6b6b';
  private readonly ACCENT_COLOR = '#5bbfba';
  private readonly COLORS = ['#5bbfba','#2d3748','#10b981','#f59e0b','#3b82f6','#8b5cf6','#ef4444','#ec4899'];

  constructor(private reportService: ReportService, private authService: AuthService) {}

  ngOnInit() {
    this.generatedBy = this.authService.getCurrentUserName() || 'Admin';
    this.loadSimple();
    this.loadLogoBase64();
  }

  private loadLogoBase64() {
    const img = new Image();
    img.onload = () => {
      const canvas = document.createElement('canvas');
      canvas.width  = img.width;
      canvas.height = img.height;
      canvas.getContext('2d')!.drawImage(img, 0, 0);
      this.logoBase64 = canvas.toDataURL('image/png');
    };
    img.src = 'circle logo 1.png';
  }

  ngOnDestroy() { this.mgmtChart?.destroy(); }

  // ── Tab switching ────────────────────────────────────────────────────────────
  setTab(tab: ReportTab) {
    this.activeTab = tab;
    this.errorMessage = '';
    this.successMessage = '';
    if (tab === 'simple'        && !this.simpleData)  this.loadSimple();
    if (tab === 'transactional' && !this.txData)       this.loadTx();
    if (tab === 'criteria'      && !this.criteriaData) this.loadCriteria();
    if (tab === 'management') {
      if (!this.mgmtData) {
        this.loadManagement();
      } else {
        // Canvas was removed from DOM while tab was hidden — rebuild chart after Angular re-renders it
        setTimeout(() => this.buildMgmtChart(), 0);
      }
    }
  }

  // ── Simple reports ───────────────────────────────────────────────────────────
  setSimple(r: SimpleReport) {
    this.activeSimple = r;
    this.simpleData = null;
    this.resetSimpleFilters();
    this.loadSimple();
  }

  resetSimpleFilters() {
    this.simpleSearch    = '';
    this.simpleMinRating = '';
  }

  get filteredSimpleData(): any[] | null {
    if (!this.simpleData) return null;
    let data = [...this.simpleData];
    const q = this.simpleSearch.trim().toLowerCase();

    if (this.activeSimple === 'tutor-summary') {
      if (q) data = data.filter(r =>
        String(r['tutorName'] ?? '').toLowerCase().includes(q));
      if (this.simpleMinRating)
        data = data.filter(r => Number(r['averageRating'] ?? 0) >= Number(this.simpleMinRating));

    } else if (this.activeSimple === 'module-catalogue') {
      if (q) data = data.filter(r =>
        String(r['moduleCode'] ?? '').toLowerCase().includes(q) ||
        String(r['moduleName'] ?? '').toLowerCase().includes(q));

    } else if (this.activeSimple === 'monthly-enrollments') {
      if (q) data = data.filter(r =>
        String(r['month'] ?? '').toLowerCase().includes(q) ||
        String(r['newStudents'] ?? '').toLowerCase().includes(q));
    }
    return data;
  }

  loadSimple() {
    this.loading = true;
    this.simpleData = null;
    this.errorMessage = '';
    let obs$;
    switch (this.activeSimple) {
      case 'tutor-summary':    obs$ = this.reportService.getTutorRatingsReport();  break;
      case 'module-catalogue': obs$ = this.reportService.getModuleCatalogue();     break;
      case 'monthly-enrollments': obs$ = this.reportService.getMonthlyStudentEnrollments(); break;
    }
    obs$.subscribe({
      next:  (d) => { this.simpleData = d; this.loading = false; },
      error: ()  => { this.errorMessage = 'Failed to load report.'; this.loading = false; }
    });
  }

  get simpleColumns(): string[] {
    return this.simpleData?.length ? Object.keys(this.simpleData[0]) : [];
  }

  get simpleFilterActive(): boolean {
    return this.simpleSearch.trim() !== '' ||
           this.simpleMinRating !== '';
  }

  simpleLabel(): string {
    switch (this.activeSimple) {
      case 'tutor-summary':    return 'Tutor Rating Summary';
      case 'module-catalogue': return 'Module Catalogue';
      case 'monthly-enrollments': return 'Monthly Student Enrollments';
    }
  }

  // ── Transactional ────────────────────────────────────────────────────────────
  setTx(r: TransactionalReport) {
    this.activeTx = r;
    this.txData = null;
    this.txGroups = [];
    this.loadTx();
  }

  loadTx() {
    this.loading = true;
    this.txData = null;
    this.txGroups = [];
    this.errorMessage = '';
    const from = this.txFrom || undefined;
    const to   = this.txTo   || undefined;
    const obs$ = this.activeTx === 'revenue-detail'
      ? this.reportService.getRevenueDetail(from, to)
      : this.reportService.getTutorHoursDetail(from, to);
    obs$.subscribe({
      next: (d) => {
        this.txData = d;
        this.buildGroups(d);
        this.loading = false;
      },
      error: () => { this.errorMessage = 'Failed to load report.'; this.loading = false; }
    });
  }

  private buildGroups(data: any[]) {
    const groupKey = this.activeTx === 'revenue-detail' ? 'period' : 'tutorName';
    const valueKey = this.activeTx === 'revenue-detail' ? 'amount'  : 'hours';
    const map = new Map<string, any[]>();
    for (const row of data) {
      const k = String(row[groupKey] ?? '');
      if (!map.has(k)) map.set(k, []);
      map.get(k)!.push(row);
    }
    this.txGroups = Array.from(map.entries()).map(([key, rows]) => ({
      key,
      rows,
      subtotal: rows.reduce((s, r) => s + Number(r[valueKey] ?? 0), 0)
    }));
  }

  get txGrandTotal(): number {
    return this.txGroups.reduce((s, g) => s + g.subtotal, 0);
  }

  get txRowKeys(): string[] {
    if (!this.txData?.length) return [];
    const skip = this.activeTx === 'revenue-detail' ? 'period' : 'tutorName';
    return Object.keys(this.txData[0]).filter(k => k !== skip);
  }

  txLabel(): string {
    return this.activeTx === 'revenue-detail' ? 'Monthly Revenue Detail' : 'Tutor Hours Detail';
  }

  txSubtotalLabel(): string {
    return this.activeTx === 'revenue-detail' ? 'Month Subtotal' : 'Tutor Subtotal';
  }

  txValueLabel(): string {
    return this.activeTx === 'revenue-detail' ? 'Amount (R)' : 'Hours';
  }

  formatTxSubtotal(val: number): string {
    if (this.activeTx === 'revenue-detail') return `R ${val.toFixed(2)}`;
    return `${val.toFixed(1)} hrs`;
  }

  // ── Adjustable Criteria ──────────────────────────────────────────────────────
  loadCriteria() {
    this.loading = true;
    this.criteriaData = null;
    this.errorMessage = '';
    this.reportService.getSessionsReport(
      this.criteriaFrom || undefined,
      this.criteriaTo   || undefined,
      this.criteriaType !== 'All' ? this.criteriaType : undefined
    ).subscribe({
      next:  (d) => { this.criteriaData = d; this.loading = false; },
      error: ()  => { this.errorMessage = 'Failed to load report.'; this.loading = false; }
    });
  }

  get criteriaColumns(): string[] {
    return this.criteriaData?.length ? Object.keys(this.criteriaData[0]) : [];
  }

  // ── Management / Graph ───────────────────────────────────────────────────────
  loadManagement() {
    this.loading = true;
    this.mgmtData = null;
    this.mgmtReady = false;
    this.mgmtChart?.destroy();
    this.mgmtChart = null;
    this.errorMessage = '';
    this.reportService.getModuleEnrollments(
      this.mgmtFrom || undefined,
      this.mgmtTo   || undefined
    ).subscribe({
      next: (d) => {
        this.mgmtData = d;
        this.loading = false;
        setTimeout(() => this.buildMgmtChart(), 100);
      },
      error: () => { this.errorMessage = 'Failed to load report.'; this.loading = false; }
    });
  }

  setChartType(type: 'bar' | 'pie' | 'doughnut') {
    this.mgmtChartType = type;
    this.buildMgmtChart();
  }

  private buildMgmtChart() {
    const canvas = this.mgmtCanvas?.nativeElement;
    if (!canvas || !this.mgmtData?.length) return;
    this.mgmtChart?.destroy();
    const labels = this.mgmtData.map(d => d['moduleName'] ?? d['moduleCode']);
    const values = this.mgmtData.map(d => Number(d['enrolledStudents'] ?? 0));
    const isRadial = this.mgmtChartType === 'pie' || this.mgmtChartType === 'doughnut';
    this.mgmtChart = new Chart(canvas, {
      type: this.mgmtChartType,
      data: {
        labels,
        datasets: [{
          label: 'Enrolled Students',
          data: values,
          backgroundColor: this.COLORS.slice(0, labels.length),
          borderRadius: isRadial ? 0 : 6,
          borderWidth: isRadial ? 2 : 0,
          borderColor: '#fff'
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: isRadial,
            position: 'right',
            labels: { color: '#374151', font: { size: 11 }, boxWidth: 14, padding: 10 }
          },
          tooltip: {
            callbacks: {
              label: isRadial
                ? (c) => ` ${c.label}: ${c.parsed} students`
                : (c) => ` ${c.parsed.y} students`
            }
          }
        },
        ...(isRadial ? {} : {
          scales: {
            x: { grid: { color: '#f0f0f0' }, ticks: { color: '#374151', font: { size: 11 } } },
            y: { grid: { color: '#f0f0f0' }, ticks: { color: '#374151', font: { size: 11 } }, beginAtZero: true }
          }
        })
      }
    });
    this.mgmtReady = true;
  }

  // ── PDF Generation ───────────────────────────────────────────────────────────
  // Adds standard header: logo block + title + metadata line
  private pdfHeader(doc: jsPDF, title: string): number {
    const now = new Date();
    const dateStr = now.toLocaleDateString('en-ZA', { year: 'numeric', month: 'long', day: 'numeric' });

    // Logo image (circle logo 1)
    if (this.logoBase64) {
      doc.addImage(this.logoBase64, 'PNG', 14, 8, 18, 18);
    } else {
      // Fallback: teal box with text if image not loaded yet
      doc.setFillColor(26, 107, 107);
      doc.rect(14, 8, 18, 18, 'F');
    }

    // Title
    doc.setTextColor(26, 107, 107);
    doc.setFontSize(14);
    doc.setFont('helvetica', 'bold');
    doc.text(title, 36, 16);

    // Metadata
    doc.setFontSize(9);
    doc.setTextColor(100, 100, 100);
    doc.setFont('helvetica', 'normal');
    doc.text(`Generated: ${dateStr}   |   Generated by: ${this.generatedBy}`, 36, 22);

    // Divider
    doc.setDrawColor(26, 107, 107);
    doc.setLineWidth(0.5);
    doc.line(14, 28, 196, 28);

    return 34; // Y position after header
  }

  generateSimplePdf() {
    const data = this.filteredSimpleData;
    if (!data?.length) return;
    const title = this.simpleLabel();
    const doc = new jsPDF();
    let y = this.pdfHeader(doc, title);

    // If filters are active, print a filter summary line
    if (this.simpleFilterActive) {
      const parts: string[] = [];
      if (this.simpleSearch)    parts.push(`Search: "${this.simpleSearch}"`);
      if (this.simpleMinRating) parts.push(`Min rating: ${this.simpleMinRating}★`);
      doc.setFillColor(245, 251, 251);
      doc.rect(14, y, 182, 8, 'F');
      doc.setFontSize(8);
      doc.setTextColor(60, 60, 60);
      doc.setFont('helvetica', 'normal');
      doc.text(`Filters applied — ${parts.join('   |   ')}`, 17, y + 5.5);
      y += 13;
    }

    const cols = this.simpleColumns.map(c => this.humanKey(c));
    const rows = data.map(r => this.simpleColumns.map(c => this.fmtPdf(r[c])));
    autoTable(doc, {
      startY: y,
      head: [cols],
      body: rows,
      headStyles: { fillColor: [26, 107, 107], textColor: 255, fontStyle: 'bold', fontSize: 9 },
      bodyStyles: { fontSize: 8, textColor: [40, 40, 40] },
      alternateRowStyles: { fillColor: [245, 251, 251] },
      margin: { left: 14, right: 14 }
    });
    const finalY = (doc as any).lastAutoTable.finalY + 6;
    doc.setFontSize(8);
    doc.setTextColor(100, 100, 100);
    doc.text(`Records: ${data.length}${this.simpleFilterActive ? ` (filtered from ${this.simpleData!.length} total)` : ''}`, 14, finalY);
    doc.save(`${title.replace(/\s+/g, '-').toLowerCase()}-${new Date().toISOString().slice(0,10)}.pdf`);
    this.successMessage = 'PDF downloaded.';
  }

  generateTransactionalPdf() {
    if (!this.txGroups.length) return;
    const title = this.txLabel();
    const doc = new jsPDF();
    let y = this.pdfHeader(doc, title);
    const rowKeys = this.txRowKeys;
    const valueKey = this.activeTx === 'revenue-detail' ? 'amount' : 'hours';

    for (const group of this.txGroups) {
      // Control break header
      doc.setFontSize(10);
      doc.setFont('helvetica', 'bold');
      doc.setTextColor(26, 107, 107);
      doc.text(group.key, 14, y);
      y += 4;

      // Detail rows
      const head = [rowKeys.map(k => this.humanKey(k))];
      const body = group.rows.map(r => rowKeys.map(k =>
        (this.activeTx === 'revenue-detail' && k === 'amount')
          ? `R ${Number(r[k]).toFixed(2)}`
          : this.fmtPdf(r[k])
      ));
      autoTable(doc, {
        startY: y,
        head,
        body,
        headStyles: { fillColor: [91, 191, 186], textColor: 255, fontStyle: 'bold', fontSize: 8 },
        bodyStyles: { fontSize: 8 },
        margin: { left: 14, right: 14 }
      });
      y = (doc as any).lastAutoTable.finalY + 2;

      // Subtotal row
      doc.setFillColor(230, 247, 247);
      doc.rect(14, y, 182, 7, 'F');
      doc.setFontSize(9);
      doc.setFont('helvetica', 'bold');
      doc.setTextColor(26, 107, 107);
      doc.text(`${this.txSubtotalLabel()}: ${this.formatTxSubtotal(group.subtotal)}`, 194, y + 5, { align: 'right' });
      y += 12;

      if (y > 260) { doc.addPage(); y = 20; }
    }

    // Grand total
    doc.setFillColor(26, 107, 107);
    doc.rect(14, y, 182, 9, 'F');
    doc.setFontSize(10);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(255, 255, 255);
    doc.text(`GRAND TOTAL: ${this.formatTxSubtotal(this.txGrandTotal)}`, 194, y + 6.5, { align: 'right' });

    doc.save(`${title.replace(/\s+/g, '-').toLowerCase()}-${new Date().toISOString().slice(0,10)}.pdf`);
    this.successMessage = 'PDF downloaded.';
  }

  generateCriteriaPdf() {
    if (!this.criteriaData?.length) return;
    const title = `Sessions Report${this.criteriaType !== 'All' ? ' — ' + this.criteriaType : ''}`;
    const dateRange = this.criteriaFrom || this.criteriaTo
      ? `  (${this.criteriaFrom || 'Any'} → ${this.criteriaTo || 'Any'})`
      : '';
    const doc = new jsPDF({ orientation: 'landscape' });
    let y = this.pdfHeader(doc, title);

    // Criteria summary box
    doc.setFillColor(245, 251, 251);
    doc.rect(14, y, 268, 8, 'F');
    doc.setFontSize(8);
    doc.setTextColor(60, 60, 60);
    doc.setFont('helvetica', 'normal');
    doc.text(`Filters applied — Session Type: ${this.criteriaType}${dateRange}`, 17, y + 5.5);
    y += 13;

    const cols = this.criteriaColumns.map(c => this.humanKey(c));
    const rows = this.criteriaData.map(r => this.criteriaColumns.map(c => this.fmtPdf(r[c])));
    autoTable(doc, {
      startY: y,
      head: [cols],
      body: rows,
      headStyles: { fillColor: [26, 107, 107], textColor: 255, fontStyle: 'bold', fontSize: 8 },
      bodyStyles: { fontSize: 7.5 },
      alternateRowStyles: { fillColor: [245, 251, 251] },
      margin: { left: 14, right: 14 }
    });
    const finalY = (doc as any).lastAutoTable.finalY + 6;
    doc.setFontSize(8);
    doc.setTextColor(100, 100, 100);
    doc.text(`Total sessions matching criteria: ${this.criteriaData.length}`, 14, finalY);
    doc.save(`sessions-criteria-report-${new Date().toISOString().slice(0,10)}.pdf`);
    this.successMessage = 'PDF downloaded.';
  }

  generateManagementPdf() {
    if (!this.mgmtData?.length || !this.mgmtChart) return;
    const doc = new jsPDF();
    let y = this.pdfHeader(doc, 'Module Enrollment — Management Report');

    // Embed chart — preserve the canvas's actual pixel aspect ratio so
    // pie charts stay circular and bars stay proportional.
    const canvas = this.mgmtCanvas.nativeElement;
    const canvasRatio = canvas.width / canvas.height;   // e.g. ~2.4 for a wide container
    const chartImg = this.mgmtChart.toBase64Image('image/png', 1);
    const pdfW = 170;
    const pdfH = Math.round(pdfW / canvasRatio);
    const xOff = (210 - pdfW) / 2;                     // centre horizontally on A4
    doc.addImage(chartImg, 'PNG', xOff, y, pdfW, pdfH);
    y += pdfH + 8;

    // Table below chart
    const cols = ['Module Code', 'Module Name', 'Enrolled Students', 'First Enrollment', 'Last Enrollment'];
    const rows = this.mgmtData.map(d => [
      String(d['moduleCode'] ?? ''),
      String(d['moduleName'] ?? ''),
      String(d['enrolledStudents'] ?? 0),
      String(d['earliestEnrollment'] ?? ''),
      String(d['latestEnrollment'] ?? '')
    ]);
    autoTable(doc, {
      startY: y,
      head: [cols],
      body: rows,
      headStyles: { fillColor: [26, 107, 107], textColor: 255, fontStyle: 'bold', fontSize: 9 },
      bodyStyles: { fontSize: 8 },
      alternateRowStyles: { fillColor: [245, 251, 251] },
      margin: { left: 14, right: 14 }
    });
    const total = this.mgmtData.reduce((s, d) => s + Number(d['enrolledStudents'] ?? 0), 0);
    const finalY = (doc as any).lastAutoTable.finalY + 6;
    doc.setFontSize(9);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(26, 107, 107);
    doc.text(`Total enrolled students (across all modules): ${total}`, 14, finalY);
    doc.save(`module-enrollment-management-${new Date().toISOString().slice(0,10)}.pdf`);
    this.successMessage = 'PDF downloaded.';
  }

  // ── Helpers ──────────────────────────────────────────────────────────────────
  humanKey(k: string): string {
    return k.replace(/([A-Z])/g, ' $1').replace(/^./, s => s.toUpperCase()).trim();
  }

  fmtPdf(v: any): string {
    if (v == null) return '—';
    if (typeof v === 'number') {
      if (String(v).includes('.')) return Number(v).toFixed(2);
    }
    return String(v);
  }

  fmt(v: any): string {
    if (v == null) return '—';
    return String(v);
  }

  formatCurrency(v: any): string {
    return `R ${Number(v ?? 0).toFixed(2)}`;
  }

  get txDatesInvalid(): boolean {
    return !!(this.txFrom && this.txTo && this.txTo < this.txFrom);
  }

  get criteriaDatesInvalid(): boolean {
    return !!(this.criteriaFrom && this.criteriaTo && this.criteriaTo < this.criteriaFrom);
  }

  get mgmtDatesInvalid(): boolean {
    return !!(this.mgmtFrom && this.mgmtTo && this.mgmtTo < this.mgmtFrom);
  }

  get totalEnrolled(): number {
    return (this.mgmtData ?? []).reduce((s, d) => s + Number(d['enrolledStudents'] ?? 0), 0);
  }
}
