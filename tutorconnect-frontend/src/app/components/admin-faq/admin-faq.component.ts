import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { extractErrorMessage } from '../../interceptors/error.interceptor';
import { FaqService, Faq as FAQ } from '../../services/faq.service';
import { HELP_PAGE_OPTIONS } from '../../shared/help-page-options';
import { HelpIconComponent } from '../help-icon/help-icon.component';

interface FAQCategory { faq_Category_ID: number; category_Name: string; }

@Component({
  selector: 'app-admin-faq',
  standalone: true,
  imports: [CommonModule, FormsModule, HelpIconComponent],
  templateUrl: './admin-faq.component.html',
  styleUrl: './admin-faq.component.css'
})
export class AdminFaqComponent implements OnInit {
  faqs: FAQ[] = [];
  categories: FAQCategory[] = [];
  loading = false;
  successMessage = '';
  errorMessage = '';

  // FAQ form
  showFaqForm = false;
  editingFaq: FAQ | null = null;
  faqQuestion = '';
  faqAnswer = '';
  faqCategoryId: number | null = null;
  faqApplicablePages: string[] = [];
  helpPageOptions = HELP_PAGE_OPTIONS;
  savingFaq = false;
  deleteFaqId: number | null = null;
  faqErrors: Record<string, string> = {};

  // Category form
  showCatForm = false;
  editingCat: FAQCategory | null = null;
  catName = '';
  savingCat = false;
  deleteCatId: number | null = null;
  catErrors: Record<string, string> = {};

  activeTab: 'faqs' | 'categories' = 'faqs';
  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient, private faqService: FaqService) {}

  ngOnInit() { this.loadAll(); }

  loadAll() {
    this.loading = true;
    this.http.get<FAQCategory[]>(`${this.apiUrl}/AdminContent/faq-categories`).subscribe({
      next: (cats) => {
        this.categories = cats;
        this.faqService.getFaqs(true).subscribe({
          next: (f) => { this.faqs = f; this.loading = false; },
          error: () => { this.loading = false; }
        });
      },
      error: () => { this.loading = false; }
    });
  }

  getCategoryName(id: number): string {
    return this.categories.find(c => c.faq_Category_ID === id)?.category_Name ?? 'Unknown';
  }

  getFaqCountByCategory(categoryId: number): number {
    return this.faqs.filter(f => f.faq_Category_ID === categoryId).length;
  }

  validateFaqQuestion(val: string) {
    if (!val?.trim()) { this.faqErrors['question'] = 'Question is required.'; }
    else if (val.trim().length < 5) { this.faqErrors['question'] = 'Question must be at least 5 characters.'; }
    else if (val.trim().length > 500) { this.faqErrors['question'] = 'Question cannot exceed 500 characters.'; }
    else { delete this.faqErrors['question']; }
  }

  validateFaqAnswer(val: string) {
    if (!val?.trim()) { this.faqErrors['answer'] = 'Answer is required.'; }
    else if (val.trim().length < 5) { this.faqErrors['answer'] = 'Answer must be at least 5 characters.'; }
    else if (val.trim().length > 2000) { this.faqErrors['answer'] = 'Answer cannot exceed 2000 characters.'; }
    else { delete this.faqErrors['answer']; }
  }

  validateCatName(val: string) {
    if (!val?.trim()) { this.catErrors['name'] = 'Category name is required.'; }
    else if (val.trim().length < 2) { this.catErrors['name'] = 'Name must be at least 2 characters.'; }
    else if (val.trim().length > 100) { this.catErrors['name'] = 'Name cannot exceed 100 characters.'; }
    else { delete this.catErrors['name']; }
  }

  closeFaqForm(): void {
    this.showFaqForm = false;
    this.editingFaq = null;
    this.faqQuestion = '';
    this.faqAnswer = '';
    this.faqCategoryId = null;
    this.faqApplicablePages = [];
    this.faqErrors = {};
  }

  closeCatForm(): void {
    this.showCatForm = false;
    this.editingCat = null;
    this.catName = '';
    this.catErrors = {};
  }

  // ─── FAQ CRUD ───────────────────────────────────────────────────────────────
  openFaqForm(faq?: FAQ) {
    this.editingFaq = faq ?? null;
    this.faqQuestion = faq?.question ?? '';
    this.faqAnswer = faq?.answer ?? '';
    this.faqCategoryId = faq?.faq_Category_ID ?? null;
    this.faqApplicablePages = faq ? this.faqService.pagesOf(faq) : [];
    this.faqErrors = {};
    this.showFaqForm = true;
    this.clearMessages();
  }

  togglePage(key: string) {
    const i = this.faqApplicablePages.indexOf(key);
    if (i > -1) this.faqApplicablePages.splice(i, 1);
    else this.faqApplicablePages.push(key);
  }

  saveFaq() {
    this.validateFaqQuestion(this.faqQuestion);
    this.validateFaqAnswer(this.faqAnswer);
    if (!this.faqCategoryId) this.faqErrors['category'] = 'Please select a category.';
    else delete this.faqErrors['category'];
    if (Object.keys(this.faqErrors).length > 0) return;
    this.savingFaq = true;
    const payload = {
      question: this.faqQuestion,
      answer: this.faqAnswer,
      FAQ_Category_ID: this.faqCategoryId!,
      Applicable_Pages: this.faqApplicablePages.join(',')
    };
    const obs = this.editingFaq
      ? this.faqService.updateFaq(this.editingFaq.faq_ID, payload)
      : this.faqService.createFaq(payload);
    obs.subscribe({
      next: () => { this.savingFaq = false; this.successMessage = 'FAQ saved.'; this.showFaqForm = false; this.loadAll(); },
      error: (err) => { this.savingFaq = false; this.errorMessage = extractErrorMessage(err, 'Failed to save FAQ.'); }
    });
  }

  deleteFaq() {
    if (!this.deleteFaqId) return;
    this.faqService.deleteFaq(this.deleteFaqId).subscribe({
      next: () => { this.successMessage = 'FAQ deleted.'; this.deleteFaqId = null; this.loadAll(); },
      error: (err) => { this.errorMessage = extractErrorMessage(err, 'Failed to delete FAQ.'); this.deleteFaqId = null; }
    });
  }

  // ─── CATEGORY CRUD ──────────────────────────────────────────────────────────
  openCatForm(cat?: FAQCategory) {
    this.editingCat = cat ?? null;
    this.catName = cat?.category_Name ?? '';
    this.catErrors = {};
    this.showCatForm = true;
    this.clearMessages();
  }

  saveCat() {
    this.validateCatName(this.catName);
    if (Object.keys(this.catErrors).length > 0) return;
    this.savingCat = true;
    const payload = { category_Name: this.catName };
    const obs = this.editingCat
      ? this.http.put(`${this.apiUrl}/AdminContent/faq-categories/${this.editingCat.faq_Category_ID}`, payload)
      : this.http.post(`${this.apiUrl}/AdminContent/faq-categories`, payload);
    obs.subscribe({
      next: () => { this.savingCat = false; this.successMessage = 'Category saved.'; this.showCatForm = false; this.loadAll(); },
      error: (err) => { this.savingCat = false; this.errorMessage = extractErrorMessage(err, 'Failed to save category.'); }
    });
  }

  deleteCat() {
    if (!this.deleteCatId) return;
    this.http.delete(`${this.apiUrl}/AdminContent/faq-categories/${this.deleteCatId}`).subscribe({
      next: () => { this.successMessage = 'Category deleted.'; this.deleteCatId = null; this.loadAll(); },
      error: (err) => { this.errorMessage = extractErrorMessage(err, 'Failed to delete category.'); this.deleteCatId = null; }
    });
  }

  clearMessages() { this.errorMessage = ''; this.successMessage = ''; }
  clearFaqError(key: string) { delete this.faqErrors[key]; }
}
