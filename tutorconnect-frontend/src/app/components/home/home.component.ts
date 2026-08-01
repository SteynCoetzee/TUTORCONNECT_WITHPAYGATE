import { Component, OnInit, OnDestroy, AfterViewInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule, DatePipe, NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Chart, registerables } from 'chart.js';
import { AuthService } from '../../services/auth.service';
import { ModuleService } from '../../services/module.service';
import { AnnouncementService } from '../../services/announcement.service';
import { Module, Announcement, Testimonial } from '../../models/models';
import { environment } from '../../../environments/environment';

Chart.register(...registerables);

interface MediaContent { media_ID: number; media_Name: string; media_Address: string; }
interface WishlistItem { wishlist_ID: number; module_Code: string; module_Name: string; date_Submitted: string; }

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink, DatePipe, NgClass],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('revenueCanvas')    revenueCanvas!:    ElementRef<HTMLCanvasElement>;
  @ViewChild('tutorHoursCanvas') tutorHoursCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('enrollCanvas')     enrollCanvas!:     ElementRef<HTMLCanvasElement>;

  userName = '';
  role = '';
  modules: Module[] = [];
  announcements: Announcement[] = [];
  recentAnnouncements: Announcement[] = [];
  mediaItems: MediaContent[] = [];
  testimonials: Testimonial[] = [];
  wishlistItems: WishlistItem[] = [];

  // Chart data
  revenueReady   = false;
  tutorHrsReady  = false;
  enrollReady    = false;
  chartsData: { revenue?: any[]; tutorHours?: any[]; enroll?: any[] } = {};

  private charts: Chart[] = [];
  private chartsBuilt = false;

  private apiUrl = environment.apiUrl;

  constructor(
    private authService: AuthService,
    private moduleService: ModuleService,
    private announcementService: AnnouncementService,
    private http: HttpClient
  ) {}

  ngOnInit() {
    this.userName = this.authService.getCurrentUserName();
    this.role = this.authService.getCurrentUserRole();

    this.moduleService.getModules().subscribe({ next: (d) => { this.modules = d; }, error: () => {} });
    this.announcementService.getAnnouncements().subscribe({
      next: (d) => { this.announcements = d; this.recentAnnouncements = d.slice(0, 3); },
      error: () => {}
    });
    this.http.get<MediaContent[]>(`${this.apiUrl}/AdminContent/media`).subscribe({
      next: (d) => { this.mediaItems = d; }, error: () => {}
    });
    this.http.get<Testimonial[]>(`${this.apiUrl}/Testimonials/approved`).subscribe({
      next: (d) => { this.testimonials = d.slice(0, 4); }, error: () => {}
    });

    const userId = this.authService.getCurrentUserId();
    if (this.role === 'Student' && userId) {
      this.http.get<WishlistItem[]>(`${this.apiUrl}/ModuleWishlist/student/${userId}`).subscribe({
        next: (d) => { this.wishlistItems = d; }, error: () => {}
      });
    }

    if (this.role === 'Admin') {
      this.loadAdminCharts();
    }
  }

  private loadAdminCharts() {
    this.http.get<any[]>(`${this.apiUrl}/Reports/monthly-income`).subscribe({
      next: (d) => {
        this.chartsData.revenue = d.slice(-6); // last 6 months
        this.revenueReady = true;
        this.tryBuildCharts();
      },
      error: () => { this.revenueReady = true; this.tryBuildCharts(); }
    });

    this.http.get<any[]>(`${this.apiUrl}/Reports/tutor-hours`).subscribe({
      next: (d) => {
        this.chartsData.tutorHours = d.slice(0, 8);
        this.tutorHrsReady = true;
        this.tryBuildCharts();
      },
      error: () => { this.tutorHrsReady = true; this.tryBuildCharts(); }
    });

    this.http.get<any[]>(`${this.apiUrl}/Reports/module-enrollments`).subscribe({
      next: (d) => {
        this.chartsData.enroll = d;
        this.enrollReady = true;
        this.tryBuildCharts();
      },
      error: () => { this.enrollReady = true; this.tryBuildCharts(); }
    });
  }

  ngAfterViewInit() {
    this.tryBuildCharts();
  }

  private tryBuildCharts() {
    if (this.chartsBuilt) return;
    if (this.role !== 'Admin') return;
    if (!this.revenueReady || !this.tutorHrsReady || !this.enrollReady) return;
    if (!this.revenueCanvas || !this.tutorHoursCanvas || !this.enrollCanvas) return;
    this.chartsBuilt = true;
    this.buildRevenueChart();
    this.buildTutorHoursChart();
    this.buildEnrollChart();
  }

  private buildRevenueChart() {
    const data = this.chartsData.revenue ?? [];
    const labels = data.map(d => d.period);
    const values = data.map(d => d.totalIncome ?? 0);
    const ctx = this.revenueCanvas.nativeElement.getContext('2d')!;
    this.charts.push(new Chart(ctx, {
      type: 'bar',
      data: {
        labels,
        datasets: [{
          label: 'Revenue (R)',
          data: values,
          backgroundColor: 'rgba(20,184,166,0.75)',
          borderColor: '#0d9488',
          borderWidth: 1,
          borderRadius: 6
        }]
      },
      options: {
        responsive: true, maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: { callbacks: { label: (ctx) => `R${(ctx.parsed.y as number).toLocaleString('en-ZA', {minimumFractionDigits:2})}` } }
        },
        scales: {
          y: { beginAtZero: true, ticks: { callback: (v) => `R${Number(v).toLocaleString('en-ZA')}` } }
        }
      }
    }));
  }

  private buildTutorHoursChart() {
    const data = this.chartsData.tutorHours ?? [];
    const labels = data.map(d => d.tutorName);
    const values = data.map(d => d.totalHoursWorked ?? 0);
    const palette = ['#6366f1','#8b5cf6','#a78bfa','#c084fc','#e879f9','#f472b6','#fb7185','#fbbf24'];
    const ctx = this.tutorHoursCanvas.nativeElement.getContext('2d')!;
    this.charts.push(new Chart(ctx, {
      type: 'bar',
      data: {
        labels,
        datasets: [{
          label: 'Hours Logged',
          data: values,
          backgroundColor: labels.map((_, i) => palette[i % palette.length]),
          borderRadius: 6
        }]
      },
      options: {
        indexAxis: 'y',
        responsive: true, maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: { callbacks: { label: (ctx) => `${ctx.parsed.x}h` } }
        },
        scales: {
          x: { beginAtZero: true, ticks: { callback: (v) => `${v}h` } }
        }
      }
    }));
  }

  private buildEnrollChart() {
    const data = this.chartsData.enroll ?? [];
    const labels = data.map(d => d.moduleCode ?? d.moduleName);
    const values = data.map(d => d.enrolledStudents ?? 0);
    const palette = ['#14b8a6','#3b82f6','#f59e0b','#ef4444','#8b5cf6','#ec4899','#10b981','#f97316'];
    const ctx = this.enrollCanvas.nativeElement.getContext('2d')!;
    this.charts.push(new Chart(ctx, {
      type: 'bar',
      data: {
        labels,
        datasets: [{
          label: 'Enrolled Students',
          data: values,
          backgroundColor: labels.map((_, i) => palette[i % palette.length] + 'CC'),
          borderColor: labels.map((_, i) => palette[i % palette.length]),
          borderWidth: 1,
          borderRadius: 6
        }]
      },
      options: {
        responsive: true, maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: { callbacks: { label: (ctx) => `${ctx.parsed.y} student${ctx.parsed.y !== 1 ? 's' : ''}` } }
        },
        scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } }
      }
    }));
  }

  ngOnDestroy() {
    this.charts.forEach(c => c.destroy());
  }

  getBadgeClass(type: string): string {
    const map: Record<string, string> = {
      'Update': 'badge badge-teal',
      'Deadline': 'badge badge-warning',
      'Event': 'badge badge-info',
      'Resource': 'badge badge-purple'
    };
    return map[type] || 'badge badge-teal';
  }

  getGreeting(): string {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 17) return 'Good afternoon';
    return 'Good evening';
  }
}
