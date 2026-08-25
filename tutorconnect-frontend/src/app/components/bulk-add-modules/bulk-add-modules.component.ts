import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ModuleService } from '../../services/module.service';
import { ModuleBulkImportResult } from '../../models/models';
import { extractErrorMessage } from '../../interceptors/error.interceptor';
import { ToastService } from '../../services/toast.service';

const MAX_FILE_BYTES = 5 * 1024 * 1024;

@Component({
  selector: 'app-bulk-add-modules',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './bulk-add-modules.component.html',
  styleUrl: './bulk-add-modules.component.css'
})
export class BulkAddModulesComponent implements OnInit {
  downloading = false;
  selectedFile: File | null = null;
  uploading = false;
  result: ModuleBulkImportResult | null = null;
  errorMessage = '';

  constructor(
    private authService: AuthService,
    private moduleService: ModuleService,
    private router: Router,
    private toastService: ToastService
  ) {}

  ngOnInit() {
    if (this.authService.getCurrentUserRole() !== 'Admin') {
      this.router.navigate(['/dashboard/courses']);
    }
  }

  downloadTemplate() {
    this.downloading = true;
    this.errorMessage = '';
    this.moduleService.downloadBulkTemplate().subscribe({
      next: (blob) => {
        this.downloading = false;
        this.triggerDownload(blob, 'Module_Bulk_Import_Template.xlsx');
      },
      error: (err) => {
        this.downloading = false;
        this.errorMessage = extractErrorMessage(err, 'Failed to download template.');
      }
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    if (!file.name.toLowerCase().endsWith('.xlsx')) {
      this.errorMessage = 'Only .xlsx files are supported.';
      this.toastService.error(this.errorMessage);
      input.value = '';
      return;
    }
    if (file.size > MAX_FILE_BYTES) {
      this.errorMessage = 'File must be under 5 MB.';
      this.toastService.error(this.errorMessage);
      input.value = '';
      return;
    }

    this.selectedFile = file;
    this.errorMessage = '';
    this.result = null;
  }

  clearFile() {
    this.selectedFile = null;
    this.result = null;
  }

  upload() {
    if (!this.selectedFile) return;
    this.uploading = true;
    this.errorMessage = '';
    this.result = null;
    this.moduleService.bulkCreateModules(this.selectedFile).subscribe({
      next: (res) => {
        this.uploading = false;
        this.result = res;
        this.selectedFile = null;
      },
      error: (err) => {
        this.uploading = false;
        this.errorMessage = extractErrorMessage(err, 'Bulk import failed. Please check your file and try again.');
      }
    });
  }

  startOver() {
    this.result = null;
    this.selectedFile = null;
    this.errorMessage = '';
  }

  goBack() {
    this.router.navigate(['/dashboard/courses']);
  }

  private triggerDownload(blob: Blob, filename: string) {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
}
