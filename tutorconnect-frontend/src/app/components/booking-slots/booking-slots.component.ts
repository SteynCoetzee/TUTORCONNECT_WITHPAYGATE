import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';
import { extractErrorMessage } from '../../interceptors/error.interceptor';
import { HelpIconComponent } from '../help-icon/help-icon.component';

interface BookingSlot {
  booking_Slot_ID: number;
  slot_Date: string;
  slot_Time: string;
  session_Type: string;
  location: string;
  is_Booked: boolean;
  tutor_ID: number;
  max_Capacity?: number;
  module_Code?: string;
}

interface TutorModule {
  module_Code: string;
  module_Name: string;
}

@Component({
  selector: 'app-booking-slots',
  standalone: true,
  imports: [CommonModule, FormsModule, HelpIconComponent],
  templateUrl: './booking-slots.component.html',
  styleUrl: './booking-slots.component.css'
})
export class BookingSlotsComponent implements OnInit {
  slots: BookingSlot[] = [];
  tutorModules: TutorModule[] = [];
  loading = false;
  successMessage = '';
  errorMessage = '';

  showForm = false;
  formModuleCode = '';
  formDate = '';
  formTime = '';
  formSessionFormat = '';
  formSessionMode = '';
  formLocation = '';
  formCapacity: number | null = null;
  saving = false;

  deleteTargetId: number | null = null;
  editTargetId:   number | null = null;
  editModuleCode = '';
  editDate = '';
  editTime = '';
  editSessionFormat = '';
  editSessionMode = '';
  editLocation = '';
  editCapacity: number | null = null;
  today = new Date().toISOString().split('T')[0];
  formErrors: Record<string, string> = {};
  editErrors: Record<string, string> = {};

  private apiUrl = environment.apiUrl;
  private tutorId = 0;

  constructor(private http: HttpClient, private authService: AuthService) {}

  ngOnInit() {
    this.tutorId = this.authService.getCurrentUserId() ?? 0;
    this.loadTutorModules();
    this.loadSlots();
  }

  loadTutorModules() {
    this.http.get<any[]>(`${this.apiUrl}/TutorModule/tutor/${this.tutorId}`).subscribe({
      next: (data) => {
        this.tutorModules = data.map(m => ({
          module_Code: m.module_Code,
          module_Name: m.module_Name
        }));
      },
      error: (err) => { this.errorMessage = extractErrorMessage(err, 'Could not load your assigned modules. Module selection may be unavailable.'); }
    });
  }

  loadSlots() {
    this.loading = true;
    this.http.get<BookingSlot[]>(`${this.apiUrl}/BookingSlots/tutor/${this.tutorId}`).subscribe({
      next: (data) => { this.slots = data; this.loading = false; },
      error: (err) => { this.errorMessage = extractErrorMessage(err, 'Failed to load slots.'); this.loading = false; }
    });
  }

  openForm() {
    this.formModuleCode = '';
    this.formDate = '';
    this.formTime = '';
    this.formSessionFormat = '';
    this.formSessionMode = '';
    this.formLocation = '';
    this.formCapacity = null;
    this.formErrors = {};
    this.showForm = true;
    this.clearMessages();
  }

  cancelCreate() {
    this.showForm = false;
    this.formErrors = {};
    this.clearMessages();
  }

  validateFormCapacity(value: number | null) {
    if (this.formSessionFormat === 'Group') {
      if (!value || value < 2) {
        this.formErrors['capacity'] = 'Group capacity must be at least 2 students.';
      } else if (value > 100) {
        this.formErrors['capacity'] = 'Group capacity cannot exceed 100 students.';
      } else {
        delete this.formErrors['capacity'];
      }
    }
  }

  validateEditCapacity(value: number | null) {
    if (this.editSessionFormat === 'Group') {
      if (!value || value < 2) {
        this.editErrors['capacity'] = 'Group capacity must be at least 2 students.';
      } else if (value > 100) {
        this.editErrors['capacity'] = 'Group capacity cannot exceed 100 students.';
      } else {
        delete this.editErrors['capacity'];
      }
    }
  }

  saveSlot() {
    this.formErrors = {};
    if (!this.formModuleCode) this.formErrors['module'] = 'Please select a module.';
    if (!this.formDate) this.formErrors['date'] = 'Please select a date.';
    if (!this.formTime) this.formErrors['time'] = 'Please select a time.';
    if (!this.formSessionFormat) this.formErrors['format'] = 'Please choose a session format.';
    if (!this.formSessionMode) this.formErrors['mode'] = 'Please choose a session mode.';
    if (this.formSessionFormat === 'Group' && (!this.formCapacity || this.formCapacity < 2)) {
      this.formErrors['capacity'] = 'Group capacity must be at least 2 students.';
    }
    if (Object.keys(this.formErrors).length > 0) return;
    this.saving = true;
    this.clearMessages();
    this.http.post(`${this.apiUrl}/BookingSlots`, {
      slot_Date: this.formDate,
      slot_Time: this.formTime + ':00',
      session_Type: `${this.formSessionFormat} · ${this.formSessionMode}`,
      location: this.formLocation,
      tutor_ID: this.tutorId,
      max_Capacity: this.formSessionFormat === 'Group' ? this.formCapacity : null,
      module_Code: this.formModuleCode
    }).subscribe({
      next: () => {
        this.saving = false;
        this.successMessage = 'Booking slot created!';
        this.showForm = false;
        this.loadSlots();
      },
      error: (err) => { this.saving = false; this.errorMessage = extractErrorMessage(err, 'Failed to create slot. Please try again.'); }
    });
  }

  openEdit(slot: BookingSlot) {
    this.editTargetId = slot.booking_Slot_ID;
    this.deleteTargetId = null;
    this.showForm = false;
    const parts = slot.session_Type.split('·').map(p => p.trim());
    this.editSessionFormat = parts[0] ?? '';
    this.editSessionMode   = parts[1] ?? '';
    this.editDate      = typeof slot.slot_Date === 'string' ? slot.slot_Date.split('T')[0] : String(slot.slot_Date);
    this.editTime      = slot.slot_Time.substring(0, 5);
    this.editLocation  = slot.location ?? '';
    this.editCapacity  = slot.max_Capacity ?? null;
    this.editModuleCode = slot.module_Code ?? '';
    this.clearMessages();
  }

  cancelEdit() { this.editTargetId = null; this.editErrors = {}; this.clearMessages(); }

  saveEdit() {
    this.editErrors = {};
    if (!this.editModuleCode) this.editErrors['module'] = 'Please select a module.';
    if (!this.editDate) this.editErrors['date'] = 'Please select a date.';
    if (!this.editTime) this.editErrors['time'] = 'Please select a time.';
    if (!this.editSessionFormat) this.editErrors['format'] = 'Please choose a session format.';
    if (!this.editSessionMode) this.editErrors['mode'] = 'Please choose a session mode.';
    if (this.editSessionFormat === 'Group' && (!this.editCapacity || this.editCapacity < 2)) {
      this.editErrors['capacity'] = 'Group capacity must be at least 2 students.';
    }
    if (Object.keys(this.editErrors).length > 0) return;
    this.saving = true;
    this.clearMessages();
    this.http.put(`${this.apiUrl}/BookingSlots/${this.editTargetId}`, {
      slot_Date:    this.editDate,
      slot_Time:    this.editTime + ':00',
      session_Type: `${this.editSessionFormat} · ${this.editSessionMode}`,
      location:     this.editLocation,
      tutor_ID:     this.tutorId,
      max_Capacity: this.editSessionFormat === 'Group' ? this.editCapacity : null,
      module_Code:  this.editModuleCode || null
    }).subscribe({
      next: () => {
        this.saving = false;
        this.successMessage = 'Slot updated!';
        this.editTargetId = null;
        this.loadSlots();
      },
      error: (err) => { this.saving = false; this.errorMessage = extractErrorMessage(err, 'Failed to update slot. Please try again.'); }
    });
  }

  confirmDelete(id: number) { this.deleteTargetId = id; this.editTargetId = null; this.clearMessages(); }
  cancelDelete() { this.deleteTargetId = null; }

  deleteSlot() {
    if (!this.deleteTargetId) return;
    this.http.delete(`${this.apiUrl}/BookingSlots/${this.deleteTargetId}`).subscribe({
      next: () => {
        this.successMessage = 'Slot deleted.';
        this.deleteTargetId = null;
        this.loadSlots();
      },
      error: (err) => {
        this.errorMessage = extractErrorMessage(err, 'Cannot delete a booked slot. It may already have bookings.');
        this.deleteTargetId = null;
      }
    });
  }

  get availableSlots(): BookingSlot[] { return this.slots.filter(s => !s.is_Booked); }
  get bookedSlots(): BookingSlot[]    { return this.slots.filter(s => s.is_Booked); }

  formatDate(d: string): string {
    try { return new Date(d).toLocaleDateString('en-ZA', { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' }); }
    catch { return d; }
  }

  formatTime(t: string): string {
    try {
      const [h, m] = t.split(':').map(Number);
      const d = new Date(); d.setHours(h, m);
      return d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true });
    } catch { return t; }
  }

  clearMessages() { this.errorMessage = ''; this.successMessage = ''; }
  clearFormError(key: string) { delete this.formErrors[key]; }
  clearEditError(key: string) { delete this.editErrors[key]; }
  setFormFormat(val: string) { this.formSessionFormat = val; delete this.formErrors['format']; }
  setFormMode(val: string)   { this.formSessionMode   = val; delete this.formErrors['mode'];   }
  setEditFormat(val: string) { this.editSessionFormat = val; delete this.editErrors['format']; }
  setEditMode(val: string)   { this.editSessionMode   = val; delete this.editErrors['mode'];   }
}
