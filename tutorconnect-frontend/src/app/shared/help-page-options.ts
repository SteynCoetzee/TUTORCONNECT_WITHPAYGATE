export interface HelpPageOption {
  key: string;
  label: string;
}

/**
 * Canonical list of dashboard "page keys" that an FAQ can be tagged with, used both by:
 *  - the admin FAQ form's "Applicable Pages" checkbox list, and
 *  - the dev-mode sanity check in HelpIconComponent (warns if a pageKey typo doesn't match anything here).
 *
 * Each `key` must exactly match the pageKey="..." passed to <app-help-icon> on the corresponding
 * page's template. Keep this list and those templates in sync by hand.
 */
export const HELP_PAGE_OPTIONS: HelpPageOption[] = [
  { key: 'home',              label: 'Home' },
  { key: 'user-info',         label: 'User Information' },
  { key: 'courses',           label: 'Modules' },
  { key: 'announcements',     label: 'Announcements' },
  { key: 'calendar',          label: 'Calendar / Booking Slots' },
  { key: 'booking',           label: 'Sessions' },
  { key: 'log-hours',         label: 'Log Hours' },
  { key: 'reviews',           label: 'My Reviews' },
  { key: 'slots',             label: 'My Booking Slots' },
  { key: 'faqs',              label: 'FAQ Viewer' },
  { key: 'wishlist',          label: 'Module Wishlist' },
  { key: 'reports',           label: 'Reports (Admin)' },
  { key: 'users',             label: 'User Management (Admin)' },
  { key: 'faq',               label: 'FAQ Management (Admin)' },
  { key: 'media',             label: 'Media Management (Admin)' },
  { key: 'help',              label: 'Help Centre (Admin)' },
  { key: 'testimonials',      label: 'Testimonials' },
  { key: 'log-hours-review',  label: 'Hours Review (Admin)' },
  { key: 'audit-log',         label: 'Audit Log (Admin)' },
  { key: 'admin-reviews',     label: 'Tutor & Session Reviews (Admin)' },
  { key: 'business-logic',    label: 'Business Logic (Admin)' },
  { key: 'admin-payments',    label: 'Payments (Admin)' },
];
