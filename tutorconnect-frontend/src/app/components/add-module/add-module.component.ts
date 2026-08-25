import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { Module } from '../../models/models';
import { environment } from '../../../environments/environment';
import { extractErrorMessage } from '../../interceptors/error.interceptor';

@Component({
  selector: 'app-add-module',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './add-module.component.html',
  styleUrl: './add-module.component.css'
})
export class AddModuleComponent implements OnInit {
  isEditMode = false;
  editingModuleCode = '';
  
  formCode = '';
  formName = '';
  formDescription = '';
  formPriceOneOnOne: number | null = null;
  formPriceGroup: number | null = null;

  maxPriceOneOnOne = 10000;
  maxPriceGroup = 10000;

  loading = false;
  saving = false;
  errorMessage = '';
  successMessage = '';
  fieldErrors: Record<string, string> = {};
  
  private apiUrl = environment.apiUrl;

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    // Check if user is admin
    const role = this.authService.getCurrentUserRole();
    if (role !== 'Admin') {
      this.router.navigate(['/dashboard/courses']);
      return;
    }

    // Check if we're in edit mode
    this.route.queryParams.subscribe(params => {
      if (params['moduleCode']) {
        this.isEditMode = true;
        this.editingModuleCode = params['moduleCode'];
        this.loadModule();
      }
    });

    this.loadPriceCaps();
  }

  private loadPriceCaps() {
    this.http.get<any[]>(`${this.apiUrl}/BusinessRules`).subscribe({
      next: (rules) => {
        const oneOnOne = rules.find(r => r.rule_Name === 'module_max_price_oneonone');
        const group = rules.find(r => r.rule_Name === 'module_max_price_group');
        if (oneOnOne) this.maxPriceOneOnOne = Number(oneOnOne.rule_Value);
        if (group) this.maxPriceGroup = Number(group.rule_Value);
      },
      error: () => { /* fall back to the defaults already set above */ }
    });
  }

  loadModule() {
    this.loading = true;
    this.http.get<Module[]>(`${this.apiUrl}/Modules`).subscribe({
      next: (modules) => {
        const module = modules.find(m => m.module_Code === this.editingModuleCode);
        if (module) {
          this.formCode = module.module_Code;
          this.formName = module.module_Name;
          this.formDescription = module.module_Description;
          this.formPriceOneOnOne = module.module_Price_OneOnOne;
          this.formPriceGroup = module.module_Price_Group;
        } else {
          this.errorMessage = 'Module not found.';
          setTimeout(() => this.goBack(), 2000);
        }
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = extractErrorMessage(err, 'Failed to load module details.');
        this.loading = false;
      }
    });
  }

  validateCode(val: string) {
    if (!val?.trim()) {
      this.fieldErrors['code'] = 'Module code is required (e.g. CS101).';
    } else if (!/^[a-zA-Z0-9\-_]{2,20}$/.test(val.trim())) {
      this.fieldErrors['code'] = 'Code must be 2–20 alphanumeric characters (e.g. CS101, MATH201).';
    } else {
      delete this.fieldErrors['code'];
    }
  }

  validateName(val: string) {
    if (!val?.trim()) {
      this.fieldErrors['name'] = 'Module name is required.';
    } else if (val.trim().length < 3) {
      this.fieldErrors['name'] = 'Name must be at least 3 characters.';
    } else if (val.trim().length > 100) {
      this.fieldErrors['name'] = 'Name cannot exceed 100 characters.';
    } else {
      delete this.fieldErrors['name'];
    }
  }

  validateDescription(val: string) {
    if (!val?.trim()) {
      this.fieldErrors['desc'] = 'Description is required.';
    } else if (val.trim().length < 10) {
      this.fieldErrors['desc'] = 'Please provide a more detailed description (at least 10 characters).';
    } else if (val.trim().length > 500) {
      this.fieldErrors['desc'] = 'Description cannot exceed 500 characters.';
    } else {
      delete this.fieldErrors['desc'];
    }
  }

  validatePrice(val: number | null, field: string, label: string, max: number) {
    if (val === null || val === undefined || (val as any) === '') {
      this.fieldErrors[field] = `${label} is required.`;
    } else if (val < 0) {
      this.fieldErrors[field] = `${label} cannot be negative.`;
    } else if (val > max) {
      this.fieldErrors[field] = `${label} seems too high — maximum is R${max.toLocaleString()}.`;
    } else {
      delete this.fieldErrors[field];
    }
  }

  saveModule() {
    this.validateCode(this.formCode);
    this.validateName(this.formName);
    this.validateDescription(this.formDescription);
    this.validatePrice(this.formPriceOneOnOne, 'price1on1', 'One-on-One price', this.maxPriceOneOnOne);
    this.validatePrice(this.formPriceGroup, 'priceGroup', 'Group price', this.maxPriceGroup);
    if (Object.keys(this.fieldErrors).length > 0) return;

    this.saving = true;
    this.errorMessage = '';
    this.successMessage = '';

    const payload = {
      module_Code: this.formCode,
      module_Name: this.formName,
      module_Description: this.formDescription,
      module_Price_OneOnOne: this.formPriceOneOnOne,
      module_Price_Group: this.formPriceGroup
    };

    if (this.isEditMode) {
      // Update existing module
      this.http.put(`${this.apiUrl}/Modules/${this.editingModuleCode}`, payload).subscribe({
        next: () => {
          this.successMessage = 'Module updated successfully!';
          setTimeout(() => this.goBack(), 1500);
        },
        error: (err) => {
          this.errorMessage = extractErrorMessage(err, 'Failed to update module. Please try again.');
          this.saving = false;
        }
      });
    } else {
      // Create new module
      this.http.post(`${this.apiUrl}/Modules`, payload).subscribe({
        next: () => {
          this.successMessage = 'Module created successfully!';
          setTimeout(() => this.goBack(), 1500);
        },
        error: (err) => {
          this.errorMessage = extractErrorMessage(err, 'Failed to create module. Please try again.');
          this.saving = false;
        }
      });
    }
  }

  goBack() {
    this.router.navigate(['/dashboard/courses']);
  }
}
