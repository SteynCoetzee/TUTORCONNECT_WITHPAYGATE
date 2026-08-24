import { Component, OnInit, Input, Output, EventEmitter, HostBinding } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../services/auth.service';
import { RoleNavPermissionsService } from '../../services/role-nav-permissions.service';
import { UserNavPermissionsService } from '../../services/user-nav-permissions.service';
import { NavItem, NavSection, ADMIN_SIDEBAR, STUDENT_SIDEBAR, TUTOR_SIDEBAR, HARDCODED_ADMIN_EMAIL } from '../../shared/nav-config';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css'
})
export class SidebarComponent implements OnInit {
  sections: NavSection[] = [];
  role = '';
  collapsed = false;

  @Input() mobileOpen = false;
  @Output() mobileClose = new EventEmitter<void>();

  @HostBinding('class.mobile-open') get isMobileOpen() { return this.mobileOpen; }

  // AW-Tutor navigation is NOT configurable — stays exactly as it always has been.
  private awTutorSections: NavSection[] = [
    {
      heading: 'Account',
      items: [
        { key: 'user-info', label: 'My Profile', icon: 'person', route: '/dashboard/user-info' },
      ]
    },
  ];

  constructor(
    private authService: AuthService,
    private roleNavPermsService: RoleNavPermissionsService,
    private userNavPermsService: UserNavPermissionsService
  ) {}

  ngOnInit() {
    this.role = this.authService.getCurrentUserRole();
    if (this.role === 'AW-Tutor') {
      this.sections = this.awTutorSections;
      return;
    }

    // The one hardcoded super-admin account is always exempt from Admin nav permissions —
    // it always sees the full Admin nav, regardless of what's configured for other admins.
    if (this.role === 'Admin' && this.authService.getCurrentUserEmail()?.toLowerCase() === HARDCODED_ADMIN_EMAIL.toLowerCase()) {
      this.sections = ADMIN_SIDEBAR;
      return;
    }

    const base = this.role === 'Admin' ? ADMIN_SIDEBAR : this.role === 'Tutor' ? TUTOR_SIDEBAR : STUDENT_SIDEBAR;
    // Show the full nav immediately (no loading flicker; also the safe fail-open default if the
    // permissions calls error), then narrow it once we know what's hidden — a personal override
    // (if this specific user has been individually customized) takes precedence over the role default.
    this.sections = base;
    const userId = this.authService.getCurrentUserId();
    forkJoin([
      this.roleNavPermsService.getHiddenItemsForRole(this.role),
      userId ? this.userNavPermsService.get(userId) : Promise.resolve({ hasOverride: false, hiddenItems: [] as string[] })
    ]).subscribe({
      next: ([roleHidden, userSetting]) => {
        const hidden = userSetting.hasOverride ? userSetting.hiddenItems : roleHidden;
        this.sections = base
          .map(section => ({ ...section, items: section.items.filter(i => !hidden.includes(i.key)) }))
          .filter(section => section.items.length > 0);
      },
      error: () => { /* keep the full unfiltered nav shown */ }
    });
  }

  toggle() {
    this.collapsed = !this.collapsed;
  }
}
