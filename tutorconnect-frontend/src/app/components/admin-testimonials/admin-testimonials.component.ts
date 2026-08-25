import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';
import { Testimonial, TestimonialCreate, TestimonialCategory } from '../../models/models';
import { extractErrorMessage } from '../../interceptors/error.interceptor';
import { HelpIconComponent } from '../help-icon/help-icon.component';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-admin-testimonials',
  standalone: true,
  imports: [CommonModule, FormsModule, HelpIconComponent],
  templateUrl: './admin-testimonials.component.html',
  styleUrl: './admin-testimonials.component.css'
})
export class AdminTestimonialsComponent implements OnInit {
  activeTab: 'pending' | 'approved' | 'categories' | 'submit' = 'pending';

  pendingTestimonials: Testimonial[] = [];
  approvedTestimonials: Testimonial[] = [];
  categories: TestimonialCategory[] = [];

  loading = false;
  successMessage = '';
  errorMessage = '';

  // Submit form (for students)
  submitDescription = '';
  submitCategoryId: number | null = null;
  submitting = false;
  submitErrors: Record<string, string> = {};

  // Category form (for admins)
  showCatForm = false;
  editingCat: TestimonialCategory | null = null;
  catName = '';
  savingCat = false;
  deleteCatId: number | null = null;
  catErrors: Record<string, string> = {};

  // Edit testimonial
  editingTestimonial: Testimonial | null = null;
  editDescription = '';
  editCategoryId: number | null = null;
  saving = false;
  editErrors: Record<string, string> = {};

  deleteTargetId: number | null = null;

  userRole = '';
  userId = 0;
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient, private authService: AuthService, private toastService: ToastService) {}

  ngOnInit() {
    this.userRole = this.authService.getCurrentUserRole();
    this.userId = this.authService.getCurrentUserId() ?? 0;
    this.loadAll();
    if (this.userRole === 'Student') {
      this.activeTab = 'approved';
    }
  }

  loadAll() {
    this.loading = true;
    this.http.get<TestimonialCategory[]>(`${this.apiUrl}/Testimonials/categories`).subscribe({
      next: (cats) => {
        this.categories = cats;
        this.loadPending();
        this.loadApproved();
      },
      error: () => { this.loading = false; }
    });
  }

  loadPending() {
    this.http.get<Testimonial[]>(`${this.apiUrl}/Testimonials/pending`).subscribe({
      next: (data) => { this.pendingTestimonials = data; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  loadApproved() {
    this.http.get<Testimonial[]>(`${this.apiUrl}/Testimonials/approved`).subscribe({
      next: (data) => { this.approvedTestimonials = data; },
      error: () => {}
    });
  }

  getCategoryName(id: number): string {
    return this.categories.find(c => c.testimonial_Category_ID === id)?.test_Category_Name ?? 'Uncategorised';
  }

  // ─── APPROVE ────────────────────────────────────────────────────────────────
  approve(id: number) {
    this.clearMessages();
    this.http.put(`${this.apiUrl}/Testimonials/${id}/approve`, {}).subscribe({
      next: () => { this.successMessage = 'Testimonial approved.'; this.loadAll(); },
      error: (err) => { this.errorMessage = extractErrorMessage(err, 'Failed to approve testimonial.'); }
    });
  }

  // ─── DELETE ──────────────────────────────────────────────────────────────────
  confirmDelete(id: number) { this.deleteTargetId = id; }
  cancelDelete() { this.deleteTargetId = null; }

  deleteTestimonial() {
    if (!this.deleteTargetId) return;
    this.http.delete(`${this.apiUrl}/Testimonials/${this.deleteTargetId}`).subscribe({
      next: () => {
        this.successMessage = 'Testimonial deleted.';
        this.deleteTargetId = null;
        this.loadAll();
      },
      error: (err) => { this.errorMessage = extractErrorMessage(err, 'Failed to delete testimonial.'); this.deleteTargetId = null; }
    });
  }

  validateTestimonialDesc(val: string, errObj: Record<string, string>) {
    if (!val?.trim()) { errObj['desc'] = 'Testimonial text is required.'; }
    else if (val.trim().length < 10) { errObj['desc'] = 'Please write at least 10 characters.'; }
    else if (val.trim().length > 1000) { errObj['desc'] = 'Testimonial cannot exceed 1000 characters.'; }
    else { delete errObj['desc']; }
  }

  validateCatName(val: string) {
    if (!val?.trim()) { this.catErrors['name'] = 'Category name is required.'; }
    else if (val.trim().length < 2) { this.catErrors['name'] = 'Name must be at least 2 characters.'; }
    else if (val.trim().length > 100) { this.catErrors['name'] = 'Name cannot exceed 100 characters.'; }
    else { delete this.catErrors['name']; }
  }

  // ─── EDIT ────────────────────────────────────────────────────────────────────
  openEdit(t: Testimonial) {
    this.editingTestimonial = t;
    this.editDescription = t.testimonial_Description;
    this.editCategoryId = t.testimonial_Category_ID;
    this.editErrors = {};
    this.clearMessages();
  }

  cancelEdit() {
    this.editingTestimonial = null;
    this.editErrors = {};
  }

  saveEdit() {
    this.validateTestimonialDesc(this.editDescription, this.editErrors);
    if (Object.keys(this.editErrors).length > 0) return;
    if (!this.editingTestimonial || !this.editCategoryId) {
      this.errorMessage = 'Please fill in all fields.';
      this.toastService.error(this.errorMessage);
      return;
    }
    this.saving = true;
    this.http.put(`${this.apiUrl}/Testimonials/${this.editingTestimonial.testimonial_ID}`, {
      description: this.editDescription,
      testimonial_Category_ID: this.editCategoryId
    }).subscribe({
      next: () => {
        this.saving = false;
        this.successMessage = 'Testimonial updated. Awaiting admin approval.';
        this.editingTestimonial = null;
        this.loadAll();
      },
      error: (err) => { this.saving = false; this.errorMessage = extractErrorMessage(err, 'Failed to update testimonial.'); }
    });
  }

  // ─── SUBMIT NEW (STUDENT) ────────────────────────────────────────────────────
  submitTestimonial() {
    this.validateTestimonialDesc(this.submitDescription, this.submitErrors);
    if (!this.submitCategoryId) this.submitErrors['category'] = 'Please select a category.';
    else delete this.submitErrors['category'];
    if (Object.keys(this.submitErrors).length > 0) return;
    this.submitting = true;
    this.clearMessages();
    this.http.post(`${this.apiUrl}/Testimonials`, {
      description: this.submitDescription,
      student_ID: this.userId,
      testimonial_Category_ID: this.submitCategoryId
    }).subscribe({
      next: () => {
        this.submitting = false;
        this.successMessage = 'Testimonial submitted! It will appear once approved by an admin.';
        this.submitDescription = '';
        this.submitCategoryId = null;
        this.submitErrors = {};
        this.loadAll();
      },
      error: (err) => { this.submitting = false; this.errorMessage = extractErrorMessage(err, 'Failed to submit testimonial.'); }
    });
  }

  // ─── CATEGORIES (ADMIN) ─────────────────────────────────────────────────────
  openCatForm(cat?: TestimonialCategory) {
    this.editingCat = cat ?? null;
    this.catName = cat?.test_Category_Name ?? '';
    this.catErrors = {};
    this.showCatForm = true;
    this.clearMessages();
  }

  saveCat() {
    this.validateCatName(this.catName);
    if (Object.keys(this.catErrors).length > 0) return;
    this.savingCat = true;
    const payload = { test_Category_Name: this.catName };
    const obs = this.editingCat
      ? this.http.put(`${this.apiUrl}/AdminContent/testimonial-categories/${this.editingCat.testimonial_Category_ID}`, payload)
      : this.http.post(`${this.apiUrl}/AdminContent/testimonial-categories`, payload);
    obs.subscribe({
      next: () => { this.savingCat = false; this.successMessage = 'Category saved.'; this.showCatForm = false; this.loadAll(); },
      error: (err) => { this.savingCat = false; this.errorMessage = extractErrorMessage(err, 'Failed to save category.'); }
    });
  }

  confirmDeleteCat(id: number) { this.deleteCatId = id; }
  cancelDeleteCat() { this.deleteCatId = null; }

  deleteCat() {
    if (!this.deleteCatId) return;
    this.http.delete(`${this.apiUrl}/AdminContent/testimonial-categories/${this.deleteCatId}`).subscribe({
      next: () => { this.successMessage = 'Category deleted.'; this.deleteCatId = null; this.loadAll(); },
      error: (err) => { this.errorMessage = extractErrorMessage(err, 'Failed to delete category.'); this.deleteCatId = null; }
    });
  }

  clearMessages() { this.errorMessage = ''; this.successMessage = ''; }
  clearSubmitError(key: string) { delete this.submitErrors[key]; }
}
