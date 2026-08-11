import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { ToastService, Toast } from '../../services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast.component.html',
  styleUrl: './toast.component.css'
})
export class ToastComponent implements OnInit, OnDestroy {
  toasts: (Toast & { removing?: boolean })[] = [];
  private sub!: Subscription;

  constructor(private toastService: ToastService) {}

  ngOnInit() {
    this.sub = this.toastService.toast$.subscribe(toast => {
      this.toasts.push({ ...toast });
      setTimeout(() => this.dismiss(toast.id), 5000);
    });
  }

  dismiss(id: number) {
    const t = this.toasts.find(t => t.id === id);
    if (!t || t.removing) return;
    t.removing = true;
    setTimeout(() => {
      this.toasts = this.toasts.filter(t => t.id !== id);
    }, 350);
  }

  icon(type: string): string {
    if (type === 'error')   return 'error_outline';
    if (type === 'success') return 'check_circle_outline';
    if (type === 'warning') return 'warning_amber';
    return 'info_outline';
  }

  ngOnDestroy() { this.sub?.unsubscribe(); }
}
