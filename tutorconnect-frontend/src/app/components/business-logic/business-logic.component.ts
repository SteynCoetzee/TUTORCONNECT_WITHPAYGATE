import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { extractErrorMessage } from '../../interceptors/error.interceptor';
import { HelpIconComponent } from '../help-icon/help-icon.component';
import { RoleNavPermissionsService } from '../../services/role-nav-permissions.service';
import {
  ADMIN_SIDEBAR, STUDENT_SIDEBAR, TUTOR_SIDEBAR,
  ADMIN_TOPNAV, STUDENT_TOPNAV, TUTOR_TOPNAV,
  HARDCODED_ADMIN_EMAIL
} from '../../shared/nav-config';
import { AuthService } from '../../services/auth.service';

type ConfigurableRole = 'Admin' | 'Tutor' | 'Student';

interface BusinessRule {
  rule_ID: number;
  rule_Name: string;
  rule_Value: number;
  description: string;
}

const RULE_META: Record<string, { label: string; unit: string; min: number; max: number; icon: string; hint: string; section: string }> = {
  session_timeout_minutes: {
    label: 'AFK Session Timeout',
    unit: 'minutes',
    min: 1,
    max: 480,
    icon: 'timer_off',
    hint: 'Users will be automatically logged out after this many minutes of inactivity (no mouse or keyboard activity).',
    section: 'Session'
  },
  afk_warning_minutes: {
    label: 'AFK Warning Popup',
    unit: 'minutes before sign-out',
    min: 0,
    max: 60,
    icon: 'schedule',
    hint: 'How long before the AFK sign-out an "Are you still there?" popup is shown, giving the user a chance to stay signed in. Set to 0 to disable the warning.',
    section: 'Session'
  },
  password_reset_code_expiration_minutes: {
    label: 'Password Reset Code Validity',
    unit: 'minutes',
    min: 1,
    max: 1440,
    icon: 'mail_lock',
    hint: 'How long a password reset code stays valid after it is emailed to a user before it expires.',
    section: 'Security'
  },
  module_max_price_oneonone: {
    label: 'Max One-on-One Price',
    unit: 'Rand (R)',
    min: 0,
    max: 1000000,
    icon: 'payments',
    hint: 'The highest price (in Rand) an admin can set for a module\'s one-on-one session price.',
    section: 'Pricing'
  },
  module_max_price_group: {
    label: 'Max Group Session Price',
    unit: 'Rand (R)',
    min: 0,
    max: 1000000,
    icon: 'payments',
    hint: 'The highest price (in Rand) an admin can set for a module\'s group session price.',
    section: 'Pricing'
  }
};

@Component({
  selector: 'app-business-logic',
  standalone: true,
  imports: [CommonModule, FormsModule, HelpIconComponent],
  templateUrl: './business-logic.component.html',
  styleUrl: './business-logic.component.css'
})
export class BusinessLogicComponent implements OnInit {
  rules: BusinessRule[] = [];
  editValues: Record<number, number> = {};
  saving: Record<number, boolean> = {};
  saved: Record<number, boolean> = {};
  loading = false;
  errorMessage = '';

  // ── Navigation Permissions ──────────────────────────────────────────────
  readonly adminSidebar = ADMIN_SIDEBAR;
  readonly tutorSidebar = TUTOR_SIDEBAR;
  readonly studentSidebar = STUDENT_SIDEBAR;
  readonly adminTopbar = ADMIN_TOPNAV;
  readonly tutorTopbar = TUTOR_TOPNAV;
  readonly studentTopbar = STUDENT_TOPNAV;
  isHardcodedAdmin = false;
  permsLoading = false;
  hiddenItems: Record<ConfigurableRole, Set<string>> = { Admin: new Set(), Tutor: new Set(), Student: new Set() };
  private savedHiddenItems: Record<ConfigurableRole, Set<string>> = { Admin: new Set(), Tutor: new Set(), Student: new Set() };
  savingPerms: Record<ConfigurableRole, boolean> = { Admin: false, Tutor: false, Student: false };
  savedPerms: Record<ConfigurableRole, boolean> = { Admin: false, Tutor: false, Student: false };

  private apiUrl = environment.apiUrl;

  constructor(
    private http: HttpClient,
    private roleNavPermsService: RoleNavPermissionsService,
    private authService: AuthService
  ) {
    this.isHardcodedAdmin = this.authService.getCurrentUserEmail()?.toLowerCase() === HARDCODED_ADMIN_EMAIL.toLowerCase();
  }

  ngOnInit() {
    this.loadRules();
    this.loadPerms();
  }

  loadPerms() {
    this.permsLoading = true;
    this.roleNavPermsService.getAll().subscribe({
      next: (settings) => {
        for (const role of ['Admin', 'Tutor', 'Student'] as const) {
          const hidden = new Set(settings.find(s => s.role === role)?.hiddenItems ?? []);
          this.hiddenItems[role] = hidden;
          this.savedHiddenItems[role] = new Set(hidden);
        }
        this.permsLoading = false;
      },
      error: (err) => { this.errorMessage = extractErrorMessage(err, 'Failed to load navigation permissions.'); this.permsLoading = false; }
    });
  }

  isItemVisible(role: ConfigurableRole, key: string): boolean {
    return !this.hiddenItems[role].has(key);
  }

  toggleItem(role: ConfigurableRole, key: string) {
    const set = this.hiddenItems[role];
    if (set.has(key)) set.delete(key); else set.add(key);
  }

  isPermsDirty(role: ConfigurableRole): boolean {
    const current = this.hiddenItems[role];
    const saved = this.savedHiddenItems[role];
    if (current.size !== saved.size) return true;
    for (const key of current) if (!saved.has(key)) return true;
    return false;
  }

  savePerms(role: ConfigurableRole) {
    this.savingPerms[role] = true;
    this.errorMessage = '';
    this.roleNavPermsService.updateHiddenItems(role, [...this.hiddenItems[role]]).subscribe({
      next: () => {
        this.savingPerms[role] = false;
        this.savedPerms[role] = true;
        this.savedHiddenItems[role] = new Set(this.hiddenItems[role]);
        setTimeout(() => { this.savedPerms[role] = false; }, 2500);
      },
      error: (err) => {
        this.savingPerms[role] = false;
        this.errorMessage = extractErrorMessage(err, 'Failed to save navigation permissions.');
      }
    });
  }

  loadRules() {
    this.loading = true;
    this.http.get<BusinessRule[]>(`${this.apiUrl}/BusinessRules`).subscribe({
      next: (data) => {
        this.rules = data;
        this.editValues = {};
        data.forEach(r => { this.editValues[r.rule_ID] = r.rule_Value; });
        this.loading = false;
      },
      error: (err) => { this.errorMessage = extractErrorMessage(err, 'Failed to load settings.'); this.loading = false; }
    });
  }

  getMeta(name: string) {
    return RULE_META[name] ?? { label: name, unit: '', min: 0, max: 9999, icon: 'settings', hint: '', section: 'General' };
  }

  // Groups rules into their labelled sections, preserving the order rules already arrive in
  // (the backend sorts them to match RULE_META's declared order) so related rules land together.
  get groupedRules(): { section: string; rules: BusinessRule[] }[] {
    const groups: { section: string; rules: BusinessRule[] }[] = [];
    for (const rule of this.rules) {
      const section = this.getMeta(rule.rule_Name).section;
      let group = groups.find(g => g.section === section);
      if (!group) { group = { section, rules: [] }; groups.push(group); }
      group.rules.push(rule);
    }
    return groups;
  }

  saveRule(rule: BusinessRule) {
    const val = this.editValues[rule.rule_ID];
    const meta = this.getMeta(rule.rule_Name);
    if (val < meta.min || val > meta.max) {
      this.errorMessage = `Value for "${meta.label}" must be between ${meta.min} and ${meta.max}.`;
      return;
    }
    this.saving[rule.rule_ID] = true;
    this.errorMessage = '';
    this.http.put(`${this.apiUrl}/BusinessRules/${rule.rule_ID}`, { rule_Value: val }).subscribe({
      next: () => {
        this.saving[rule.rule_ID] = false;
        this.saved[rule.rule_ID] = true;
        rule.rule_Value = val;
        setTimeout(() => { this.saved[rule.rule_ID] = false; }, 2500);
      },
      error: (err) => {
        this.saving[rule.rule_ID] = false;
        this.errorMessage = extractErrorMessage(err, 'Failed to save. Please try again.');
      }
    });
  }

  isDirty(rule: BusinessRule): boolean {
    return this.editValues[rule.rule_ID] !== rule.rule_Value;
  }
}
