import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ReportsComponent } from './reports.component';

describe('ReportsComponent — Helpers', () => {
  let component: ReportsComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReportsComponent, HttpClientTestingModule]
    }).compileComponents();

    const fixture = TestBed.createComponent(ReportsComponent);
    component = fixture.componentInstance;
    // Do not call detectChanges — avoids Chart.js canvas instantiation in JSDOM
  });

  // ── fmt ──────────────────────────────────────────────────────────────────────

  it('should display null as an em-dash', () => {
    expect(component.fmt(null)).toBe('—');
  });

  it('should display undefined as an em-dash', () => {
    expect(component.fmt(undefined)).toBe('—');
  });

  it('should pass string values through unchanged', () => {
    expect(component.fmt('One-on-One')).toBe('One-on-One');
  });

  it('should convert numbers to strings', () => {
    expect(component.fmt(42)).toBe('42');
  });

  // ── fmtPdf ───────────────────────────────────────────────────────────────────

  it('should display null as em-dash in PDF format', () => {
    expect(component.fmtPdf(null)).toBe('—');
  });

  it('should format a decimal number to two decimal places', () => {
    expect(component.fmtPdf(4.5678)).toBe('4.57');
  });

  it('should pass integer values through as strings', () => {
    expect(component.fmtPdf(42)).toBe('42');
  });

  // ── humanKey ─────────────────────────────────────────────────────────────────

  it('should convert camelCase to Title Case with spaces', () => {
    expect(component.humanKey('tutorName')).toBe('Tutor Name');
  });

  it('should capitalise the first character', () => {
    expect(component.humanKey('amount')).toBe('Amount');
  });

  // ── formatCurrency ───────────────────────────────────────────────────────────

  it('should format a number as a Rand currency string', () => {
    expect(component.formatCurrency(350)).toBe('R 350.00');
  });

  it('should handle null as R 0.00', () => {
    expect(component.formatCurrency(null)).toBe('R 0.00');
  });
});
