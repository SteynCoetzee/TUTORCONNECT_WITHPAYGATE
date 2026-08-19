import { Component, OnInit, OnDestroy, HostListener, Output, EventEmitter } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
import { NotificationService } from '../../services/notification.service';
import { RoleNavPermissionsService } from '../../services/role-nav-permissions.service';
import { Notification } from '../../models/models';
import { NavItem, ADMIN_TOPNAV, STUDENT_TOPNAV, TUTOR_TOPNAV, HARDCODED_ADMIN_EMAIL } from '../../shared/nav-config';
import { environment } from '../../../environments/environment';

interface AnnouncementNotif {
  announcement_ID: number;
  announcement_Name: string;
  announcement_Details: string;
  date_Posted: string;
  module_Code: string | null;
}

@Component({
  selector: 'app-topnav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './topnav.component.html',
  styleUrl: './topnav.component.css'
})
export class TopnavComponent implements OnInit, OnDestroy {
  @Output() hamburgerClick = new EventEmitter<void>();

  userName = '';
  profilePictureUrl: string | null = null;
  role = '';
  notifications: Notification[] = [];
  announcements: AnnouncementNotif[] = [];
  unreadCount = 0;
  showNotifications = false;
  topnavLinks: NavItem[] = [];

  private apiUrl = environment.apiUrl;
  private seenKey = '';
  private profileSub?: Subscription;

  constructor(
    private authService: AuthService,
    private userService: UserService,
    private notificationService: NotificationService,
    private roleNavPermsService: RoleNavPermissionsService,
    private http: HttpClient,
    private router: Router
  ) {}

  ngOnDestroy() { this.profileSub?.unsubscribe(); }

  private loadProfile(userId: number) {
    this.userService.getUser(userId).subscribe({
      next: (u) => {
        const first = u.firstName?.trim() ?? '';
        const last  = u.lastName?.trim()  ?? '';
        this.userName = first && last ? `${first} ${last}` : (first || last || this.authService.getCurrentUserName());
        this.profilePictureUrl = u.profile_Picture_Url ?? null;
      },
      error: () => {}
    });
  }

  ngOnInit() {
    this.userName = this.authService.getCurrentUserName();
    this.role = this.authService.getCurrentUserRole();
    const userId = this.authService.getCurrentUserId();
    this.seenKey = `seen_announcements_${userId}`;

    // AW-Tutor quick-links stay hardcoded in the template — its nav is minimal, never configurable.
    // Admin/Tutor/Student are data-driven and filtered by the admin-configurable Navigation
    // Permissions, EXCEPT the one hardcoded super-admin account, which always sees the full set.
    const isHardcodedAdmin = this.role === 'Admin'
      && this.authService.getCurrentUserEmail()?.toLowerCase() === HARDCODED_ADMIN_EMAIL.toLowerCase();

    if ((this.role === 'Admin' || this.role === 'Tutor' || this.role === 'Student') && !isHardcodedAdmin) {
      const base = this.role === 'Admin' ? ADMIN_TOPNAV : this.role === 'Tutor' ? TUTOR_TOPNAV : STUDENT_TOPNAV;
      this.topnavLinks = base;
      this.roleNavPermsService.getHiddenItemsForRole(this.role).subscribe({
        next: (hidden) => { this.topnavLinks = base.filter(l => !hidden.includes(l.key)); },
        error: () => { /* keep the full unfiltered links shown */ }
      });
    } else if (isHardcodedAdmin) {
      this.topnavLinks = ADMIN_TOPNAV;
    }

    if (userId) {
      this.loadProfile(userId);
      this.profileSub = this.userService.profileChanged$.subscribe(() => this.loadProfile(userId));

      this.notificationService.getUserNotifications(userId).subscribe({
        next: (data) => { this.notifications = data; this.updateBadge(); },
        error: () => {}
      });

      if (this.role === 'Tutor') {
        this.http.get<AnnouncementNotif[]>(`${this.apiUrl}/Announcements/website`).subscribe({
          next: (data) => { this.announcements = data; this.updateBadge(); },
          error: () => {}
        });
      } else if (this.role === 'Student') {
        this.http.get<AnnouncementNotif[]>(`${this.apiUrl}/Announcements/student/${userId}`).subscribe({
          next: (data) => { this.announcements = data; this.updateBadge(); },
          error: () => {}
        });
      }
    }
  }

  private getSeenIds(): number[] {
    try { return JSON.parse(localStorage.getItem(this.seenKey) ?? '[]'); }
    catch { return []; }
  }

  private updateBadge() {
    const seen = this.getSeenIds();
    const unreadNotifs = this.notifications.filter(n => !n.is_Read).length;
    const unreadAnnounce = this.announcements.filter(a => !seen.includes(a.announcement_ID)).length;
    this.unreadCount = unreadNotifs + unreadAnnounce;
  }

  isAnnouncementNew(id: number): boolean {
    return !this.getSeenIds().includes(id);
  }

  toggleNotifications() {
    this.showNotifications = !this.showNotifications;
    if (this.showNotifications) {
      // Mark announcements as seen in localStorage
      const allIds = this.announcements.map(a => a.announcement_ID);
      localStorage.setItem(this.seenKey, JSON.stringify(allIds));

      // Mark all system notifications as read on the backend
      const userId = this.authService.getCurrentUserId();
      if (userId) {
        this.http.put(`${this.apiUrl}/Notifications/user/${userId}/read-all`, {}).subscribe({
          next: () => {
            this.notifications.forEach(n => n.is_Read = true);
            this.updateBadge();
          },
          error: () => {}
        });
      } else {
        this.updateBadge();
      }
    }
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    const target = event.target as HTMLElement;
    if (!target.closest('.notif-wrapper')) {
      this.showNotifications = false;
    }
  }

  signOut() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  getInitials(): string {
    const parts = this.userName.trim().split(/\s+/);
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return parts[0]?.[0]?.toUpperCase() ?? 'U';
  }

  getDisplayName(): string {
    return this.userName.trim().split(/\s+/)[0] ?? this.userName;
  }
}
