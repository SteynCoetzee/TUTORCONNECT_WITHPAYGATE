import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { extractErrorMessage } from '../../interceptors/error.interceptor';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.css'
})
export class ForgotPasswordComponent {
  step: 'email' | 'code' | 'password' = 'email';
  email = '';
  resetCode = '';
  newPassword = '';
  confirmPassword = '';
  loading = false;
  errorMessage = '';
  successMessage = '';
  resetCodeExpiryMinutes = 15;
  showPassword = false;
  showConfirmPassword = false;
  fieldErrors: Record<string, string> = {};
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient, private router: Router) {}

  private emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;

  validateEmail(value: string) {
    if (!value.trim()) {
      this.fieldErrors['email'] = 'Email address is required.';
    } else if (!this.emailRegex.test(value.trim())) {
      this.fieldErrors['email'] = 'Please enter a valid email address (e.g. you@university.edu).';
    } else {
      delete this.fieldErrors['email'];
    }
  }

  validateNewPassword(value: string) {
    if (!value) {
      this.fieldErrors['password'] = 'New password is required.';
    } else if (value.length < 8) {
      this.fieldErrors['password'] = 'Password must be at least 8 characters long.';
    } else if (!/[A-Z]/.test(value)) {
      this.fieldErrors['password'] = 'Password must contain at least one uppercase letter.';
    } else if (!/[0-9]/.test(value)) {
      this.fieldErrors['password'] = 'Password must contain at least one number.';
    } else if (!/[^a-zA-Z0-9]/.test(value)) {
      this.fieldErrors['password'] = 'Password must contain at least one special character (e.g. !@#$%).';
    } else {
      delete this.fieldErrors['password'];
    }
    if (this.confirmPassword) this.validateConfirm(this.confirmPassword);
  }

  validateConfirm(value: string) {
    if (!value) {
      this.fieldErrors['confirm'] = 'Please confirm your new password.';
    } else if (value !== this.newPassword) {
      this.fieldErrors['confirm'] = 'Passwords do not match.';
    } else {
      delete this.fieldErrors['confirm'];
    }
  }

  requestReset() {
    this.validateEmail(this.email);
    if (this.fieldErrors['email']) return;

    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.http.post(`${this.apiUrl}/Auth/forgot-password`, { email: this.email }).subscribe({
      next: (response: any) => {
        this.loading = false;
        this.successMessage = response.message || 'If this email is registered, a reset code has been sent.';
        if (response.expiresInMinutes) this.resetCodeExpiryMinutes = response.expiresInMinutes;
        this.step = 'code';
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = extractErrorMessage(err, 'Something went wrong. Please try again later.');
      }
    });
  }

  verifyCode() {
    if (!this.resetCode || this.resetCode.length < 6) {
      this.fieldErrors['code'] = 'Please enter the 6-character code from your email.';
      return;
    }
    delete this.fieldErrors['code'];

    this.loading = true;
    this.errorMessage = '';

    this.http.post(`${this.apiUrl}/Auth/verify-reset-code`, {
      email: this.email,
      resetCode: this.resetCode
    }, { responseType: 'text' }).subscribe({
      next: () => {
        this.loading = false;
        this.step = 'password';
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = extractErrorMessage(err, 'Incorrect or expired code. Please check your email and try again.');
      }
    });
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

  resetPassword() {
    this.validateNewPassword(this.newPassword);
    this.validateConfirm(this.confirmPassword);
    if (this.fieldErrors['password'] || this.fieldErrors['confirm']) return;

    this.loading = true;
    this.errorMessage = '';

    this.http.post(`${this.apiUrl}/Auth/reset-password`, {
      email: this.email,
      resetCode: this.resetCode,
      newPassword: this.newPassword
    }).subscribe({
      next: () => {
        this.loading = false;
        this.successMessage = 'Password reset successfully! Redirecting to login...';
        setTimeout(() => this.router.navigate(['/login']), 2000);
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = extractErrorMessage(err, 'Failed to reset password. Your code may have expired — please start again.');
      }
    });
  }

  goBack() {
    if (this.step === 'code') {
      this.step = 'email';
      this.resetCode = '';
    } else if (this.step === 'password') {
      this.step = 'code';
      this.newPassword = '';
      this.confirmPassword = '';
    }
    this.errorMessage = '';
    this.fieldErrors = {};
  }

  onResetCodeChange(val: string) {
    this.resetCode = val.toUpperCase();
    if (this.resetCode.length >= 6) delete this.fieldErrors['code'];
  }
}
