import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { FaqService, Faq, FaqCategory } from '../../services/faq.service';
import { HELP_PAGE_OPTIONS } from '../../shared/help-page-options';
import { HelpIconComponent } from '../help-icon/help-icon.component';

@Component({
  selector: 'app-faq-viewer',
  standalone: true,
  imports: [CommonModule, FormsModule, HelpIconComponent],
  templateUrl: './faq-viewer.component.html',
  styleUrl: './faq-viewer.component.css'
})
export class FaqViewerComponent implements OnInit {
  faqs: Faq[] = [];
  categories: FaqCategory[] = [];
  loading = false;
  expandedFaqId: number | null = null;
  searchTerm = '';

  activePageFilter: string | null = null;
  activePageLabel: string | null = null;

  constructor(
    private faqService: FaqService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit() {
    this.loadAll();
    // Subscribe (not just snapshot) so clicking a different page's help icon while already on
    // this route re-filters live instead of requiring a full navigation/reload.
    this.route.queryParamMap.subscribe(params => {
      const page = params.get('page');
      this.activePageFilter = page;
      this.activePageLabel = page ? (HELP_PAGE_OPTIONS.find(o => o.key === page)?.label ?? page) : null;
    });
  }

  loadAll() {
    this.loading = true;
    this.faqService.getCategories().subscribe({
      next: (cats) => {
        this.categories = cats;
        this.faqService.getFaqs().subscribe({
          next: (f) => { this.faqs = f; this.loading = false; },
          error: () => { this.loading = false; }
        });
      },
      error: () => { this.loading = false; }
    });
  }

  private matchesPageFilter(faq: Faq): boolean {
    return !this.activePageFilter || this.faqService.pagesOf(faq).includes(this.activePageFilter);
  }

  private matchesSearch(faq: Faq, categoryName: string): boolean {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) return true;
    return faq.question.toLowerCase().includes(term)
      || faq.answer.toLowerCase().includes(term)
      || categoryName.toLowerCase().includes(term);
  }

  getFaqsByCategory(categoryId: number, categoryName: string): Faq[] {
    return this.faqs.filter(f =>
      f.faq_Category_ID === categoryId && this.matchesPageFilter(f) && this.matchesSearch(f, categoryName)
    );
  }

  get hasVisibleFaqs(): boolean {
    return this.categories.some(c => this.getFaqsByCategory(c.faq_Category_ID, c.category_Name).length > 0);
  }

  clearSearch() { this.searchTerm = ''; }

  clearFilter() {
    this.router.navigate(['/dashboard/faqs']);
  }

  toggle(id: number) {
    this.expandedFaqId = this.expandedFaqId === id ? null : id;
  }
}
