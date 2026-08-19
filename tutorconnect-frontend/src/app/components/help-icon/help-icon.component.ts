import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FaqService, Faq } from '../../services/faq.service';
import { HELP_PAGE_OPTIONS } from '../../shared/help-page-options';
import { environment } from '../../../environments/environment';

/**
 * Small "?" icon meant to sit inline inside a page's <h1 class="page-title">. Hovering it previews
 * the FAQs tagged (by an admin) for that page; clicking it jumps to the FAQ viewer pre-filtered to
 * just those.
 */
@Component({
  selector: 'app-help-icon',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './help-icon.component.html',
  styleUrl: './help-icon.component.css'
})
export class HelpIconComponent implements OnInit {
  @Input({ required: true }) pageKey!: string;
  /** 'light' renders a lighter-toned icon, for use on dark backgrounds (e.g. the Home banner). */
  @Input() variant: 'dark' | 'light' = 'dark';

  faqs: Faq[] = [];
  loading = true;

  constructor(private faqService: FaqService, private router: Router) {}

  ngOnInit() {
    if (!environment.production && !HELP_PAGE_OPTIONS.some(o => o.key === this.pageKey)) {
      console.warn(`[HelpIconComponent] pageKey "${this.pageKey}" isn't in HELP_PAGE_OPTIONS — check for a typo.`);
    }
    this.faqService.getFaqsForPage(this.pageKey).subscribe({
      next: (faqs) => { this.faqs = faqs; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  goToFaqs(event: MouseEvent) {
    event.stopPropagation();
    this.router.navigate(['/dashboard/faqs'], { queryParams: { page: this.pageKey } });
  }
}
