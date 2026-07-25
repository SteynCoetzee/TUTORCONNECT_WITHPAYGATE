# TutorConnect

A web-based tutoring management platform built for Smith's Tutoring. TutorConnect connects students with tutors, handles module enrolments, session bookings, payments, quizzes, assignments, and a range of admin tools.

## Tech Stack

- **Backend:** ASP.NET Core 8 Web API with Entity Framework Core (SQL Server)
- **Frontend:** Angular 19 standalone components
- **Payments:** PayFast gateway integration
- **Media:** Cloudinary for uploaded content
- **Calendar:** Google Calendar API for session scheduling
- **Auth:** JWT bearer tokens

## Getting Started

### Prerequisites

- .NET 8 SDK
- Node.js 18+
- SQL Server (local or remote)
- Angular CLI (`npm install -g @angular/cli`)

### Backend Setup

1. Clone the repo and open `TutorConnect.API/` in your terminal.
2. Copy `appsettings.json` and fill in your own values for the database connection string, PayFast credentials, Cloudinary keys, and Google service account.
3. Run migrations:
   ```
   dotnet ef database update
   ```
4. Start the API:
   ```
   dotnet run
   ```
   The API will start on `https://localhost:7xxx` — check the console for the exact port.

### Frontend Setup

1. Navigate to `tutorconnect-frontend/`.
2. Install dependencies:
   ```
   npm install
   ```
3. Update `src/environments/environment.ts` with the correct API base URL if needed.
4. Run the dev server:
   ```
   ng serve
   ```
   Open `http://localhost:4200` in your browser.

## Project Structure

```
TutorConnect_Workspace/
├── TutorConnect.API/          # .NET 8 Web API
│   ├── Controllers/           # API controllers
│   ├── Models/                # EF Core entities
│   ├── Migrations/            # EF Core migrations
│   └── appsettings.json       # Config (fill in your own values)
└── tutorconnect-frontend/     # Angular 19 app
    └── src/app/
        ├── components/        # Feature components
        ├── services/          # HTTP + auth services
        ├── models/            # TypeScript interfaces
        └── guards/            # Route guards
```

## Features by Module

| Use Case Range | Feature Area | Developer |
|---|---|---|
| UC 1.1–1.8 | Authentication, registration, profile management | Steyn Coetzee |
| UC 2.1–2.8 | Student enrolment and module browsing | Modiri Thobane |
| UC 3.9–3.24 | Quizzes, assignments, grades, announcements | Jean-Jac du Toit |
| UC 3.25–3.28 | Testimonials and FAQ management | Steyn Coetzee |
| UC 3.29–3.40 | Tutor reviews and session attendance | Lutendo Singo |
| UC 4.8–4.11 | Tutor log hours and student hour views | Modiri Thobane |
| UC 4.16–4.23 | Media content and help resource management | Steyn Coetzee |
| UC 4.28–4.31 | Help resources viewer | Xander Steyn |
| UC 4.32–4.35 | Module wishlist and admin configurations | Steyn Coetzee |
| UC 5.1–5.10 | Booking system and PayFast payment integration | Xander Steyn |
| UC 6.5 | Admin reports and system audit log | Modiri Thobane |

## Team

- Steyn Coetzee
- Modiri Thobane
- Jean-Jac du Toit
- Lutendo Singo
- Xander Steyn
