import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AnnouncementService } from '../../services/announcement.service';
import { AuthService } from '../../services/auth.service';
import { Announcement, AnnouncementCreate, AnnouncementUpdate } from '../../models/models';
import { extractErrorMessage } from '../../interceptors/error.interceptor';
import { HelpIconComponent } from '../help-icon/help-icon.component';

@Component({
  selector: 'app-announcements',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe, NgClass, HelpIconComponent],
  templateUrl: './announcements.component.html',
  styleUrl: './announcements.component.css'
})
export class AnnouncementsComponent implements OnInit {
  activeTab: 'website' | 'module' = 'website';

  websiteAnnouncements: Announcement[] = [];
  moduleAnnouncements: Announcement[] = [];
  loading = false;
  isAdmin = false;

  // Error modal
  showErrorModal = false;
  errorModalMessage = '';

  // Create form (website announcements, inline at top)
  showCreateForm = false;
  createData: AnnouncementCreate = { announcement_Name: '', announcement_Details: '', announcement_Type: 'Update', module_Code: '' };
  creating = false;
  createErrors: Record<string, string> = {};

  // Edit (inline)
  editingAnnouncement: Announcement | null = null;
  editData: AnnouncementUpdate = { announcement_Name: '', announcement_Details: '', announcement_Type: 'Update', module_Code: '' };
  updating = false;
  editErrors: Record<string, string> = {};

  // Delete (inline)
  deletingId: number | null = null;
  deleting = false;

  types = ['Update', 'Deadline', 'Event', 'Resource'];

  constructor(
    private announcementService: AnnouncementService,
    private authService: AuthService
  ) {}

  ngOnInit() {
    const role = this.authService.getCurrentUserRole();
    this.isAdmin = role === 'Admin';
    this.loadWebsite();
    this.loadModule();
  }

  loadWebsite() {
    this.loading = true;
    this.announcementService.getWebsiteAnnouncements().subscribe({
      next: (data) => { this.websiteAnnouncements = data; this.loading = false; },
      error: (err) => { this.loading = false; this.showError(extractErrorMessage(err, 'Failed to load website announcements.')); }
    });
  }

  loadModule() {
    this.announcementService.getAnnouncements().subscribe({
      next: (data) => { this.moduleAnnouncements = data.filter(a => a.module_Code && a.module_Code.trim() !== ''); },
      error: (err) => {
        this.errorModalMessage = extractErrorMessage(err, 'Could not load module announcements. Please refresh the page.');
        this.showErrorModal = true;
      }
    });
  }

  validateCreateTitle(val: string) {
    if (!val?.trim()) {
      this.createErrors['title'] = 'Title is required.';
    } else if (val.trim().length < 3) {
      this.createErrors['title'] = 'Title must be at least 3 characters.';
    } else if (val.trim().length > 150) {
      this.createErrors['title'] = 'Title cannot exceed 150 characters.';
    } else {
      delete this.createErrors['title'];
    }
  }

  validateEditTitle(val: string) {
    if (!val?.trim()) {
      this.editErrors['title'] = 'Title is required.';
    } else if (val.trim().length < 3) {
      this.editErrors['title'] = 'Title must be at least 3 characters.';
    } else if (val.trim().length > 150) {
      this.editErrors['title'] = 'Title cannot exceed 150 characters.';
    } else {
      delete this.editErrors['title'];
    }
  }

  // ── Create ────────────────────────────────────────────────────────
  openCreate() {
    this.createData = { announcement_Name: '', announcement_Details: '', announcement_Type: 'Update', module_Code: '' };
    this.createErrors = {};
    this.showCreateForm = true;
    this.editingAnnouncement = null;
  }

  submitCreate() {
    this.validateCreateTitle(this.createData.announcement_Name);
    if (Object.keys(this.createErrors).length > 0) return;
    const userId = this.authService.getCurrentUserId();
    this.createData.admin_ID = userId ?? undefined;
    this.createData.module_Code = '';
    this.creating = true;
    this.announcementService.createAnnouncement(this.createData).subscribe({
      next: () => { this.creating = false; this.showCreateForm = false; this.loadWebsite(); },
      error: (err) => { this.creating = false; this.showError(extractErrorMessage(err, 'Failed to post announcement. Please try again.')); }
    });
  }

  // ── Edit ──────────────────────────────────────────────────────────
  openEdit(a: Announcement) {
    this.showCreateForm = false;
    this.editingAnnouncement = a;
    this.editErrors = {};
    this.editData = {
      announcement_Name: a.announcement_Name,
      announcement_Details: a.announcement_Details,
      announcement_Type: a.announcement_Type,
      module_Code: a.module_Code
    };
  }

  cancelEdit() { this.editingAnnouncement = null; this.editErrors = {}; }

  submitEdit() {
    if (!this.editingAnnouncement) return;
    this.validateEditTitle(this.editData.announcement_Name);
    if (Object.keys(this.editErrors).length > 0) return;
    this.updating = true;
    this.announcementService.updateAnnouncement(this.editingAnnouncement.announcement_ID, this.editData).subscribe({
      next: () => { this.updating = false; this.editingAnnouncement = null; this.loadWebsite(); },
      error: (err) => { this.updating = false; this.showError(extractErrorMessage(err, 'Failed to update announcement. Please try again.')); }
    });
  }

  // ── Delete ────────────────────────────────────────────────────────
  openDelete(id: number) { this.deletingId = id; this.editingAnnouncement = null; this.showCreateForm = false; }
  cancelDelete() { this.deletingId = null; }

  confirmDelete() {
    if (this.deletingId === null) return;
    this.deleting = true;
    this.announcementService.deleteAnnouncement(this.deletingId).subscribe({
      next: () => { this.deleting = false; this.deletingId = null; this.loadWebsite(); },
      error: (err) => { this.deleting = false; this.showError(extractErrorMessage(err, 'Failed to delete announcement.')); }
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────
  showError(msg: string) { this.errorModalMessage = msg; this.showErrorModal = true; }

  getBadgeClass(type: string): string {
    const map: Record<string, string> = {
      'Update': 'badge badge-teal',
      'Deadline': 'badge badge-warning',
      'Event': 'badge badge-info',
      'Resource': 'badge badge-purple'
    };
    return map[type] || 'badge badge-teal';
  }
}
