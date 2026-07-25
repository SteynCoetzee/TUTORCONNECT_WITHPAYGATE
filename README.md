# TutorConnect

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- SQL Server
- Angular CLI: `npm install -g @angular/cli`

## Setup

### 1. Clone the repository

```
git clone https://github.com/INF-370-2026/inf-370-2026-team29.git
cd inf-370-2026-team29
```

### 2. Configure the backend

Edit `TutorConnect.API/appsettings.json` and fill in your SQL Server connection string, PayFast merchant credentials, Cloudinary API keys, Google service account details, and email SMTP settings.

### 3. Run database migrations

```
cd TutorConnect.API
dotnet ef database update
```

### 4. Start the backend

```
dotnet run
```

Note the port printed in the console (e.g. `https://localhost:7xxx`) and update `tutorconnect-frontend/src/environments/environment.ts` if it differs.

### 5. Start the frontend

```
cd tutorconnect-frontend
npm install
ng serve
```

Open `http://localhost:4200` in your browser.
