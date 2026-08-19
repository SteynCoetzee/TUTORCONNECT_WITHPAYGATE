import { Component, ViewChild, ElementRef, OnInit, OnDestroy } from '@angular/core';
import { RouterOutlet, Router, NavigationStart, NavigationEnd } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { TopnavComponent } from '../topnav/topnav.component';
import { AuthService } from '../../services/auth.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopnavComponent, CommonModule],
  templateUrl: './dashboard-layout.component.html',
  styleUrl: './dashboard-layout.component.css'
})
export class DashboardLayoutComponent implements OnInit, OnDestroy {
  @ViewChild('contentArea') contentArea!: ElementRef<HTMLDivElement>;

  // First-login popups
  showCompleteProfilePopup = false;
  showAwaitingApprovalPopup = false;

  // "Are you still there?" AFK warning popup
  showAfkWarningPopup = false;
  afkSecondsLeft = 0;
  private afkCountdownTimer: ReturnType<typeof setInterval> | null = null;

  // Mobile sidebar
  sidebarMobileOpen = false;

  private apiUrl = environment.apiUrl;
  private afkSubscriptions: Subscription[] = [];
  private activityDebounce: ReturnType<typeof setTimeout> | null = null;
  private readonly activityEvents = ['mousemove', 'keydown', 'click', 'touchstart', 'scroll'];
  private readonly onActivity = () => {
    if (this.activityDebounce) return;
    this.activityDebounce = setTimeout(() => { this.activityDebounce = null; }, 1000);
    this.authService.resetInactivityTimer();
  };

  constructor(
    private router: Router,
    private authService: AuthService,
    private http: HttpClient
  ) {
    this.router.events.subscribe(event => {
      if (event instanceof NavigationStart) {
        const isBack = event.navigationTrigger === 'popstate';
        this.animateContent(isBack ? 'slide-from-left' : 'slide-from-right');
        this.sidebarMobileOpen = false;
      }
    });
  }

  ngOnInit() {
    this.activityEvents.forEach(e => window.addEventListener(e, this.onActivity, { passive: true }));
    this.loadAfkTimeout();
    this.afkSubscriptions.push(
      this.authService.afkWarning$.subscribe(seconds => this.startAfkCountdown(seconds)),
      this.authService.afkWarningDismissed$.subscribe(() => this.hideAfkWarning())
    );
    const role = this.authService.getCurrentUserRole();
    const userId = this.authService.getCurrentUserId();
    if (!userId) return;

    if (role === 'AW-Tutor') {
      // Show approval notice first; complete-profile shows after they dismiss it
      this.showAwaitingApprovalPopup = true;
    } else if (role === 'Student') {
      this.checkStudentProfile(userId);
    }
  }

  private readonly PROFILE_DISMISSED_KEY = 'profile_prompt_dismissed';

  private checkStudentProfile(userId: number) {
    if (sessionStorage.getItem(this.PROFILE_DISMISSED_KEY)) return;
    this.http.get<any>(`${this.apiUrl}/Users/${userId}`).subscribe({
      next: (user) => {
        if (!user.firstName || !user.lastName || !user.phone) {
          this.showCompleteProfilePopup = true;
          return;
        }
        this.http.get<any>(`${this.apiUrl}/Users/${userId}/role-profile`).subscribe({
          next: (rp) => {
            if (!rp || !rp.student_Number) {
              this.showCompleteProfilePopup = true;
            }
          },
          error: () => { this.showCompleteProfilePopup = true; }
        });
      },
      error: () => { this.showCompleteProfilePopup = true; }
    });
  }

  dismissAwaitingPopup() {
    this.showAwaitingApprovalPopup = false;
    if (!sessionStorage.getItem(this.PROFILE_DISMISSED_KEY)) {
      this.showCompleteProfilePopup = true;
    }
  }

  goToProfile() {
    this.showCompleteProfilePopup = false;
    sessionStorage.setItem(this.PROFILE_DISMISSED_KEY, '1');
    this.router.navigate(['/dashboard/user-info']);
  }

  dismissPopup() {
    this.showCompleteProfilePopup = false;
    this.showAwaitingApprovalPopup = false;
    sessionStorage.setItem(this.PROFILE_DISMISSED_KEY, '1');
  }

  ngOnDestroy() {
    this.activityEvents.forEach(e => window.removeEventListener(e, this.onActivity));
    if (this.activityDebounce) clearTimeout(this.activityDebounce);
    this.stopAfkCountdown();
    this.afkSubscriptions.forEach(s => s.unsubscribe());
    this.authService.stopInactivityTimer();
  }

  private loadAfkTimeout() {
    this.http.get<any[]>(`${this.apiUrl}/BusinessRules`).subscribe({
      next: (rules) => {
        const rule = rules.find(r => r.rule_Name === 'session_timeout_minutes');
        const warnRule = rules.find(r => r.rule_Name === 'afk_warning_minutes');
        const minutes = rule ? Math.max(1, Number(rule.rule_Value)) : 30;
        const warnMinutes = warnRule ? Math.max(0, Number(warnRule.rule_Value)) : 0;
        this.authService.startInactivityTimer(minutes, warnMinutes);
      },
      error: () => { this.authService.startInactivityTimer(30); }
    });
  }

  // ── "Are you still there?" AFK warning ──────────────────────────────────
  private startAfkCountdown(seconds: number) {
    this.stopAfkCountdown();
    this.afkSecondsLeft = seconds;
    this.showAfkWarningPopup = true;
    this.afkCountdownTimer = setInterval(() => {
      this.afkSecondsLeft--;
      if (this.afkSecondsLeft <= 0) this.stopAfkCountdown();
    }, 1000);
  }

  private hideAfkWarning() {
    this.showAfkWarningPopup = false;
    this.stopAfkCountdown();
  }

  private stopAfkCountdown() {
    if (this.afkCountdownTimer) {
      clearInterval(this.afkCountdownTimer);
      this.afkCountdownTimer = null;
    }
  }

  get afkCountdownLabel(): string {
    const m = Math.floor(this.afkSecondsLeft / 60);
    const s = this.afkSecondsLeft % 60;
    return `${m}:${s.toString().padStart(2, '0')}`;
  }

  stayLoggedIn() {
    this.hideAfkWarning();
    this.authService.confirmStillActive();
  }

  signOutNow() {
    this.hideAfkWarning();
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  toggleMobileSidebar() { this.sidebarMobileOpen = !this.sidebarMobileOpen; }
  closeMobileSidebar() { this.sidebarMobileOpen = false; }

  private animateContent(cls: string) {
    const el = this.contentArea?.nativeElement;
    if (!el) return;
    el.classList.remove('slide-from-right', 'slide-from-left');
    void el.offsetWidth;
    el.classList.add(cls);
  }
}
