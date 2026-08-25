import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { CommonModule } from '@angular/common';
import { extractErrorMessage } from '../../interceptors/error.interceptor';
import { ToastService } from '../../services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink, CommonModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  loginObj = { email: '', password: '' };
  rememberMe = false;
  showPassword = false;
  loading = false;
  errorMessage = '';
  fieldErrors: Record<string, string> = {};

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      if (params['sessionExpired'] === 'true') {
        this.errorMessage = 'Your session has expired. Please sign in again.';
        this.toastService.error(this.errorMessage);
      }
    });
  }

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

  validatePassword(value: string) {
    if (!value) {
      this.fieldErrors['password'] = 'Password is required.';
    } else {
      delete this.fieldErrors['password'];
    }
  }

  onLogin() {
    this.validateEmail(this.loginObj.email);
    this.validatePassword(this.loginObj.password);
    if (Object.keys(this.fieldErrors).length > 0) return;

    this.errorMessage = '';
    this.loading = true;
    this.authService.login(this.loginObj).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err: any) => {
        this.loading = false;
        if (err.status === 401) {
          this.errorMessage = 'Incorrect email or password. Please try again.';
        } else {
          this.errorMessage = extractErrorMessage(err, 'Sign in failed. Please check your details and try again.');
        }
      }
    });
  }
}
