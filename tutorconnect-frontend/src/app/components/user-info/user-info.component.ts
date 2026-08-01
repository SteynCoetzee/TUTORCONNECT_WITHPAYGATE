import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
import { UserProfile, UserProfileUpdate } from '../../models/models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-user-info',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-info.component.html',
  styleUrl: './user-info.component.css'
})
export class UserInfoComponent implements OnInit {
  profile: UserProfile | null = null;
  editMode = false;
  saving = false;
  successMessage = '';
  errorMessage = '';

  editData: UserProfileUpdate = { firstName: '', lastName: '' };

  // Role-specific profile
  roleProfile: any = null;
  editRoleProfile: any = {};
  savingRole = false;

  // Profile picture
  uploadingPicture = false;
  pictureError = '';
  pictureSuccess = '';

  // Camera modal
  showCameraModal = false;
  cameraStream: MediaStream | null = null;
  @ViewChild('cameraVideo') cameraVideoRef!: ElementRef<HTMLVideoElement>;
  @ViewChild('cameraCanvas') cameraCanvasRef!: ElementRef<HTMLCanvasElement>;

  // Change password
  showPasswordForm = false;
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  savingPassword = false;
  passwordError = '';
  passwordSuccess = '';
  pwFieldErrors: Record<string, string> = {};

  private apiUrl = environment.apiUrl;

  constructor(
    private authService: AuthService,
    private userService: UserService,
    private http: HttpClient
  ) {}

  ngOnInit() {
    const userId = this.authService.getCurrentUserId();
    if (userId) {
      this.userService.getUser(userId).subscribe({
        next: (data) => {
          this.profile = data;
          this.loadRoleProfile(userId);
        },
        error: () => { this.errorMessage = 'Failed to load profile.'; }
      });
    }
  }

  loadRoleProfile(userId: number) {
    this.http.get<any>(`${this.apiUrl}/Users/${userId}/role-profile`).subscribe({
      next: (data) => {
        this.roleProfile = data;
        this.editRoleProfile = data ? { ...data } : {};
      },
      error: () => { this.errorMessage = 'Could not load your role details. Some fields may be empty.'; }
    });
  }

  // ─── Address autocomplete ────────────────────────────────────────────────────
  addressSuggestions: string[] = [];
  private addressTimer: any;

  onAddressInput(value: string) {
    this.validateAddress(value);
    clearTimeout(this.addressTimer);
    if (!value || value.trim().length < 4) { this.addressSuggestions = []; return; }
    this.addressTimer = setTimeout(() => {
      this.http.get<any[]>('https://nominatim.openstreetmap.org/search', {
        params: { q: value, format: 'json', limit: '6', countrycodes: 'za', addressdetails: '1', featuretype: 'address' }
      }).subscribe({
        next: (res) => { this.addressSuggestions = res.map(r => this.formatNominatimAddress(r)); },
        error: () => { this.addressSuggestions = []; }
      });
    }, 450);
  }

  private formatNominatimAddress(r: any): string {
    const a = r.address ?? {};
    const parts = [
      a.house_number && a.road ? `${a.house_number} ${a.road}` : (a.road ?? null),
      a.suburb ?? a.neighbourhood ?? null,
      a.city ?? a.town ?? a.village ?? a.municipality ?? null,
      a.postcode ?? null
    ].filter(Boolean);
    return parts.length ? parts.join(', ') : r.display_name;
  }

  selectAddress(address: string) {
    this.editData.address = address;
    this.addressSuggestions = [];
    this.validateAddress(address);
  }

  hideAddressSuggestions() {
    setTimeout(() => { this.addressSuggestions = []; }, 180);
  }

  // ─── Validation ──────────────────────────────────────────────────────────────
  fieldErrors: Record<string, string> = {};

  private nameRegex   = /^[a-zA-Z\s'\-]+$/;
  private phoneRegex  = /^(\+27|0)[6-8][0-9]{8}$/;
  private lettersOnly = /^[a-zA-Z\s,.\-()&]+$/;
  private alphaNum    = /^[a-zA-Z0-9\s\-]+$/;

  validateName(value: string, field: string) {
    if (!value?.trim()) { this.fieldErrors[field] = 'This field is required.'; return; }
    if (value.trim().length < 2) { this.fieldErrors[field] = 'Must be at least 2 characters.'; return; }
    if (value.trim().length > 50) { this.fieldErrors[field] = 'Cannot exceed 50 characters.'; return; }
    if (!this.nameRegex.test(value)) { this.fieldErrors[field] = 'Only letters, spaces, hyphens and apostrophes allowed.'; return; }
    delete this.fieldErrors[field];
  }

  validatePhone(value: string) {
    if (!value?.trim()) { delete this.fieldErrors['phone']; return; } // optional field
    const clean = value.trim().replace(/\s/g, '');
    if (!this.phoneRegex.test(clean)) {
      this.fieldErrors['phone'] = 'Use format: +27XXXXXXXXX or 0XXXXXXXXX (10–12 digits, SA mobile).';
    } else { delete this.fieldErrors['phone']; }
  }

  validateAddress(value: string) {
    if (!value?.trim()) { delete this.fieldErrors['address']; return; }
    if (value.trim().length < 5) { this.fieldErrors['address'] = 'Address is too short.'; return; }
    if (value.trim().length > 200) { this.fieldErrors['address'] = 'Cannot exceed 200 characters.'; return; }
    delete this.fieldErrors['address'];
  }

  validateBio(value: string) {
    if (!value?.trim()) { delete this.fieldErrors['bio']; return; }
    if (value.length > 500) { this.fieldErrors['bio'] = `${value.length}/500 — too long.`; return; }
    delete this.fieldErrors['bio'];
  }

  validateStudentNumber(value: string) {
    if (!value?.trim()) { delete this.fieldErrors['studentNumber']; return; }
    if (!/^[uU][0-9]{8}$/.test(value.trim()) && !/^[a-zA-Z0-9]{4,20}$/.test(value.trim())) {
      this.fieldErrors['studentNumber'] = 'Use format uXXXXXXXX (e.g. u12345678) or your institution number.';
    } else { delete this.fieldErrors['studentNumber']; }
  }

  validateFaculty(value: string) {
    if (!value?.trim()) { delete this.fieldErrors['faculty']; return; }
    if (!this.lettersOnly.test(value)) { this.fieldErrors['faculty'] = 'Only letters and spaces allowed.'; return; }
    delete this.fieldErrors['faculty'];
  }

  validateYearOfStudy(value: number | null) {
    if (value === null || value === undefined) { delete this.fieldErrors['yearOfStudy']; return; }
    if (!Number.isInteger(Number(value)) || value < 1 || value > 7) {
      this.fieldErrors['yearOfStudy'] = 'Year must be between 1 and 7.';
    } else { delete this.fieldErrors['yearOfStudy']; }
  }

  validateYearsExp(value: number | null) {
    if (value === null || value === undefined) { delete this.fieldErrors['yearsExp']; return; }
    if (!Number.isInteger(Number(value)) || value < 0 || value > 50) {
      this.fieldErrors['yearsExp'] = 'Must be between 0 and 50 years.';
    } else { delete this.fieldErrors['yearsExp']; }
  }

  validateTextField(value: string, field: string, min = 2, max = 200) {
    if (!value?.trim()) { delete this.fieldErrors[field]; return; }
    if (value.trim().length < min) { this.fieldErrors[field] = `Must be at least ${min} characters.`; return; }
    if (value.trim().length > max) { this.fieldErrors[field] = `Cannot exceed ${max} characters.`; return; }
    delete this.fieldErrors[field];
  }

  get hasFieldErrors(): boolean { return Object.keys(this.fieldErrors).length > 0; }

  private validateAll(): boolean {
    this.fieldErrors = {};
    this.validateName(this.editData.firstName ?? '', 'firstName');
    this.validateName(this.editData.lastName ?? '', 'lastName');
    this.validatePhone(this.editData.phone ?? '');
    this.validateAddress(this.editData.address ?? '');
    this.validateBio(this.editData.bio ?? '');
    const role = this.roleName;
    if (role === 'Student') {
      this.validateStudentNumber(this.editRoleProfile.student_Number ?? '');
      this.validateFaculty(this.editRoleProfile.faculty ?? '');
      this.validateYearOfStudy(this.editRoleProfile.year_Of_Study);
      this.validateTextField(this.editRoleProfile.degree_Program ?? '', 'degreeProgram');
    } else if (role === 'Tutor' || role === 'AW-Tutor') {
      this.validateTextField(this.editRoleProfile.qualifications ?? '', 'qualifications');
      this.validateTextField(this.editRoleProfile.specialization ?? '', 'specialization');
      this.validateYearsExp(this.editRoleProfile.years_Of_Experience);
    } else if (role === 'Admin') {
      this.validateTextField(this.editRoleProfile.job_Title ?? '', 'jobTitle');
    }
    return !this.hasFieldErrors;
  }

  // ─── Edit flow ───────────────────────────────────────────────────────────────

  startEdit() {
    if (!this.profile) return;
    this.editData = {
      firstName: this.profile.firstName,
      lastName: this.profile.lastName,
      phone: this.profile.phone,
      address: this.profile.address,
      bio: this.profile.bio
    };
    this.editRoleProfile = this.roleProfile ? { ...this.roleProfile } : {};
    this.fieldErrors = {};
    this.editMode = true;
  }

  cancelEdit() {
    this.editMode = false;
    this.fieldErrors = {};
    this.errorMessage = '';
  }

  saveProfile() {
    if (!this.validateAll()) {
      this.errorMessage = 'Please fix the errors below before saving.';
      return;
    }
    const userId = this.authService.getCurrentUserId();
    if (!userId) return;
    this.saving = true;
    this.errorMessage = '';
    this.userService.updateUser(userId, this.editData).subscribe({
      next: () => {
        this.saving = false;
        this.editMode = false;
        this.successMessage = 'Profile updated successfully!';
        this.userService.getUser(userId).subscribe({ next: (d) => { this.profile = d; } });
        this.userService.notifyProfileChanged();
        this.saveRoleProfile(userId);
        setTimeout(() => { this.successMessage = ''; }, 3000);
      },
      error: () => { this.saving = false; this.errorMessage = 'Failed to save profile.'; }
    });
  }

  saveRoleProfile(userId: number) {
    const role = this.profile?.roleName;
    let endpoint = '';
    if (role === 'Student') endpoint = `${this.apiUrl}/Users/${userId}/student-profile`;
    else if (role === 'Tutor' || role === 'AW-Tutor') endpoint = `${this.apiUrl}/Users/${userId}/tutor-profile`;
    else if (role === 'Admin') endpoint = `${this.apiUrl}/Users/${userId}/admin-profile`;
    if (!endpoint) return;
    this.http.put(endpoint, this.editRoleProfile, { responseType: 'text' }).subscribe({
      next: () => { this.roleProfile = { ...this.editRoleProfile }; },
      error: () => { this.errorMessage = 'Profile saved, but some role-specific details could not be saved. Please try again.'; }
    });
  }

  validateCurrentPw(value: string) {
    if (!value) {
      this.pwFieldErrors['currentPw'] = 'Please enter your current password.';
    } else {
      delete this.pwFieldErrors['currentPw'];
    }
  }

  validateNewPw(value: string) {
    if (!value) {
      this.pwFieldErrors['newPw'] = 'New password is required.';
    } else if (value.length < 8) {
      this.pwFieldErrors['newPw'] = 'Password must be at least 8 characters long.';
    } else if (!/[A-Z]/.test(value)) {
      this.pwFieldErrors['newPw'] = 'Password must include at least one uppercase letter.';
    } else if (!/[0-9]/.test(value)) {
      this.pwFieldErrors['newPw'] = 'Password must include at least one number.';
    } else if (!/[^a-zA-Z0-9]/.test(value)) {
      this.pwFieldErrors['newPw'] = 'Password must include at least one special character (e.g. !@#$%).';
    } else {
      delete this.pwFieldErrors['newPw'];
    }
    if (this.confirmPassword) this.validateConfirmPw(this.confirmPassword);
  }

  validateConfirmPw(value: string) {
    if (!value) {
      this.pwFieldErrors['confirmPw'] = 'Please confirm your new password.';
    } else if (value !== this.newPassword) {
      this.pwFieldErrors['confirmPw'] = 'Passwords do not match.';
    } else {
      delete this.pwFieldErrors['confirmPw'];
    }
  }

  openPasswordForm() {
    this.showPasswordForm = true;
    this.currentPassword = '';
    this.newPassword = '';
    this.confirmPassword = '';
    this.passwordError = '';
    this.passwordSuccess = '';
    this.pwFieldErrors = {};
  }

  cancelPasswordForm() {
    this.showPasswordForm = false;
    this.passwordError = '';
    this.passwordSuccess = '';
    this.pwFieldErrors = {};
  }

  get pwStrength(): 0 | 1 | 2 | 3 {
    if (!this.newPassword) return 0;
    const len     = this.newPassword.length >= 8;
    const upper   = /[A-Z]/.test(this.newPassword);
    const num     = /[0-9]/.test(this.newPassword);
    const special = /[^a-zA-Z0-9]/.test(this.newPassword);
    const score   = [len, upper, num, special].filter(Boolean).length;
    if (score <= 1) return 1;
    if (score <= 3) return 2;
    return 3;
  }

  get pwStrengthLabel(): string {
    return ['', 'Weak', 'Fair', 'Strong'][this.pwStrength];
  }

  changePassword() {
    this.validateCurrentPw(this.currentPassword);
    this.validateNewPw(this.newPassword);
    this.validateConfirmPw(this.confirmPassword);
    if (Object.keys(this.pwFieldErrors).length > 0) return;
    if (this.newPassword === this.currentPassword) {
      this.pwFieldErrors['newPw'] = 'New password must be different from your current password.';
      return;
    }
    const userId = this.authService.getCurrentUserId();
    if (!userId) return;
    this.savingPassword = true;
    this.passwordError = '';
    this.http.put(`${this.apiUrl}/Auth/change-password/${userId}`, {
      currentPassword: this.currentPassword,
      newPassword: this.newPassword,
      confirmPassword: this.confirmPassword
    }, { responseType: 'text' }).subscribe({
      next: () => {
        this.savingPassword = false;
        this.passwordSuccess = 'Password updated successfully!';
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
        setTimeout(() => { this.showPasswordForm = false; this.passwordSuccess = ''; }, 2000);
      },
      error: (err) => {
        this.savingPassword = false;
        if (err.status === 400) {
          this.pwFieldErrors['currentPw'] = 'Current password is incorrect.';
        } else {
          this.passwordError = typeof err.error === 'string' && err.error ? err.error : 'Failed to update password. Please try again.';
        }
      }
    });
  }

  onProfilePictureSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    const allowed = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];
    if (!allowed.includes(file.type)) {
      this.pictureError = 'Only JPEG, PNG, WebP, or GIF images are allowed.';
      input.value = '';
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this.pictureError = 'Profile picture must be under 5 MB.';
      input.value = '';
      return;
    }
    this.uploadPictureFile(file, () => { input.value = ''; });
  }

  async openCamera() {
    this.pictureError = '';
    try {
      this.cameraStream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'user' } });
      this.showCameraModal = true;
      setTimeout(() => {
        if (this.cameraVideoRef?.nativeElement) {
          this.cameraVideoRef.nativeElement.srcObject = this.cameraStream;
        }
      }, 80);
    } catch {
      this.pictureError = 'Camera access denied or not available on this device.';
    }
  }

  capturePhoto() {
    const video = this.cameraVideoRef.nativeElement;
    const canvas = this.cameraCanvasRef.nativeElement;
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    canvas.getContext('2d')!.drawImage(video, 0, 0);
    canvas.toBlob((blob) => {
      if (!blob) return;
      const file = new File([blob], 'camera-photo.jpg', { type: 'image/jpeg' });
      this.closeCamera();
      this.uploadPictureFile(file, () => {});
    }, 'image/jpeg', 0.92);
  }

  closeCamera() {
    this.cameraStream?.getTracks().forEach(t => t.stop());
    this.cameraStream = null;
    this.showCameraModal = false;
  }

  private uploadPictureFile(file: File, reset: () => void) {
    const userId = this.authService.getCurrentUserId();
    if (!userId) return;
    this.uploadingPicture = true;
    this.pictureError = '';
    this.pictureSuccess = '';
    const fd = new FormData();
    fd.append('file', file);
    this.http.post(`${this.apiUrl}/Users/${userId}/profile-picture`, fd, { responseType: 'text' }).subscribe({
      next: (url) => {
        this.uploadingPicture = false;
        const cleanUrl = url.replace(/^"|"$/g, '').trim();
        if (this.profile) this.profile.profile_Picture_Url = cleanUrl;
        this.userService.notifyProfileChanged();
        this.pictureSuccess = 'Profile picture updated!';
        reset();
        setTimeout(() => { this.pictureSuccess = ''; }, 3000);
      },
      error: (err) => {
        this.uploadingPicture = false;
        this.pictureError = typeof err.error === 'string' ? err.error : 'Upload failed. Try again.';
        reset();
      }
    });
  }

  get roleName(): string { return this.profile?.roleName ?? this.authService.getCurrentUserRole(); }

  getStudentId(): string {
    if (!this.profile) return '';
    return `STU${new Date().getFullYear()}${String(this.profile.user_ID).padStart(3, '0')}`;
  }
}
