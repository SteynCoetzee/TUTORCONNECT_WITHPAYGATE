export interface NavItem {
  key: string;
  label: string;
  icon: string;
  route: string;
}

export interface NavSection {
  heading?: string;
  items: NavItem[];
}

/**
 * Single source of truth for Admin/Tutor/Student sidebar + top-navbar items — used both by the
 * real sidebar/topnav (to render + filter what's shown) and by the Business Logic "Navigation
 * Permissions" admin UI (to render the checkbox lists). AW-Tutor is NOT here — its nav is minimal
 * and stays hardcoded in sidebar.component.ts, never configurable.
 *
 * `key` is what gets stored in a role's hidden-items list (Role_Nav_Setting.Hidden_Items). Sidebar
 * item keys match their route (minus the /dashboard/ prefix); topnav items are prefixed with
 * "topnav:" so a sidebar entry and its topnav quick-link (same route, different surface) can be
 * hidden independently.
 *
 * Admin permissions ARE configurable (any admin who reaches Business Logic can edit them), but the
 * one hardcoded super-admin account seeded at startup (Program.cs) is always exempt from them — its
 * own nav never gets filtered, no matter what the saved Admin config says. This is that account's
 * safety net against locking itself out; every other Admin account is filtered normally.
 */
export const HARDCODED_ADMIN_EMAIL = 'TutorConnect00@gmail.com';
export const STUDENT_SIDEBAR: NavSection[] = [
  {
    heading: 'General',
    items: [
      { key: 'home', label: 'Home', icon: 'home', route: '/dashboard/home' },
      { key: 'user-info', label: 'My Profile', icon: 'person', route: '/dashboard/user-info' },
    ]
  },
  {
    heading: 'Learning',
    items: [
      { key: 'courses', label: 'Modules', icon: 'menu_book', route: '/dashboard/courses' },
      { key: 'announcements', label: 'Announcements', icon: 'campaign', route: '/dashboard/announcements' },
      { key: 'calendar', label: 'Calendar', icon: 'calendar_today', route: '/dashboard/calendar' },
    ]
  },
  {
    heading: 'Sessions',
    items: [
      { key: 'booking', label: 'Book a Session', icon: 'schedule', route: '/dashboard/booking' },
      { key: 'reviews', label: 'My Reviews', icon: 'star', route: '/dashboard/reviews' },
      { key: 'testimonials', label: 'Testimonials', icon: 'star_border', route: '/dashboard/testimonials' },
    ]
  },
  {
    heading: 'Support',
    items: [
      { key: 'faqs', label: 'FAQs', icon: 'help_outline', route: '/dashboard/faqs' },
      { key: 'wishlist', label: 'Module Wishlist', icon: 'favorite_border', route: '/dashboard/wishlist' },
    ]
  },
];

export const TUTOR_SIDEBAR: NavSection[] = [
  {
    heading: 'General',
    items: [
      { key: 'home', label: 'Home', icon: 'home', route: '/dashboard/home' },
      { key: 'user-info', label: 'My Profile', icon: 'person', route: '/dashboard/user-info' },
    ]
  },
  {
    heading: 'Teaching',
    items: [
      { key: 'courses', label: 'Modules', icon: 'menu_book', route: '/dashboard/courses' },
      { key: 'announcements', label: 'Announcements', icon: 'campaign', route: '/dashboard/announcements' },
      { key: 'calendar', label: 'Calendar', icon: 'calendar_today', route: '/dashboard/calendar' },
    ]
  },
  {
    heading: 'Sessions',
    items: [
      { key: 'slots', label: 'My Booking Slots', icon: 'event_available', route: '/dashboard/slots' },
      { key: 'log-hours', label: 'Log Hours', icon: 'timer', route: '/dashboard/log-hours' },
      { key: 'reviews', label: 'Reviews', icon: 'star', route: '/dashboard/reviews' },
    ]
  },
  {
    heading: 'Support',
    items: [
      { key: 'faqs', label: 'FAQs', icon: 'help_outline', route: '/dashboard/faqs' },
    ]
  },
];

export const ADMIN_SIDEBAR: NavSection[] = [
  {
    heading: 'General',
    items: [
      { key: 'home', label: 'Home', icon: 'home', route: '/dashboard/home' },
      { key: 'user-info', label: 'My Profile', icon: 'person', route: '/dashboard/user-info' },
    ]
  },
  {
    heading: 'Management',
    items: [
      { key: 'users', label: 'Users', icon: 'group', route: '/dashboard/users' },
      { key: 'courses', label: 'Modules', icon: 'menu_book', route: '/dashboard/courses' },
      { key: 'reports', label: 'Reports', icon: 'description', route: '/dashboard/reports' },
      { key: 'announcements', label: 'Announcements', icon: 'campaign', route: '/dashboard/announcements' },
    ]
  },
  {
    heading: 'Content',
    items: [
      { key: 'faq', label: 'FAQ', icon: 'help', route: '/dashboard/faq' },
      { key: 'media', label: 'Media', icon: 'perm_media', route: '/dashboard/media' },
      { key: 'help', label: 'Help Page', icon: 'support', route: '/dashboard/help' },
      { key: 'testimonials', label: 'Testimonials', icon: 'star_border', route: '/dashboard/testimonials' },
    ]
  },
  {
    heading: 'Reviews',
    items: [
      { key: 'log-hours-review', label: 'Hours Review', icon: 'timer', route: '/dashboard/log-hours-review' },
      { key: 'admin-reviews', label: 'Tutor & Session Reviews', icon: 'rate_review', route: '/dashboard/admin-reviews' },
    ]
  },
  {
    heading: 'Payments',
    items: [
      { key: 'admin-payments', label: 'Payments', icon: 'payment', route: '/dashboard/admin-payments' },
    ]
  },
  {
    heading: 'System',
    items: [
      { key: 'audit-log', label: 'Audit Log', icon: 'manage_search', route: '/dashboard/audit-log' },
      { key: 'business-logic', label: 'Business Logic', icon: 'tune', route: '/dashboard/business-logic' },
    ]
  },
];

export const ADMIN_TOPNAV: NavItem[] = [
  { key: 'topnav:user-info', label: 'User Info', icon: '', route: '/dashboard/user-info' },
  { key: 'topnav:courses', label: 'Courses', icon: '', route: '/dashboard/courses' },
  { key: 'topnav:calendar', label: 'Calendar', icon: '', route: '/dashboard/calendar' },
];

export const STUDENT_TOPNAV: NavItem[] = [
  { key: 'topnav:user-info', label: 'User Info', icon: '', route: '/dashboard/user-info' },
  { key: 'topnav:courses', label: 'Courses', icon: '', route: '/dashboard/courses' },
  { key: 'topnav:calendar', label: 'Calendar', icon: '', route: '/dashboard/calendar' },
  { key: 'topnav:booking', label: 'Booking', icon: '', route: '/dashboard/booking' },
];

export const TUTOR_TOPNAV: NavItem[] = [
  { key: 'topnav:user-info', label: 'User Info', icon: '', route: '/dashboard/user-info' },
  { key: 'topnav:courses', label: 'Courses', icon: '', route: '/dashboard/courses' },
  { key: 'topnav:calendar', label: 'Calendar', icon: '', route: '/dashboard/calendar' },
];
