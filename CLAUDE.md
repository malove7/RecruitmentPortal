# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run (HTTP on localhost:5011)
dotnet run --launch-profile http

# Run (HTTPS on localhost:7054)
dotnet run --launch-profile https

# EF Core migrations
dotnet ef migrations add <MigrationName>
dotnet ef database update

# Run tests (xUnit + Moq + FluentAssertions + EF Core InMemory)
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~CandidatesControllerTests.Index_ShouldReturnAllCandidates_WhenNoFiltersProvided"
```

`RecruitmentPortal.Tests` (referenced by `RecruitmentPortal.sln`) tests controllers directly against an EF Core InMemory `ApplicationDbContext` — no HTTP/integration harness.

## Architecture

**ASP.NET Core MVC** on .NET 10, targeting an Azure SQL Database via EF Core 10.

### Authentication & Authorization

JWT tokens are issued on login and stored in secure HTTP-only cookies (`jwtToken`). A custom JWT Bearer handler reads the token from the cookie on each request — not from the Authorization header.

**Two authorization layers run in tandem:**
- **Role-based:** `[Authorize(Roles = "Admin")]` — three roles: `Admin`, `HR`, `Interviewer`
- **Permission-based:** `[Authorize(Policy = "Permissions.ViewCandidates")]` — 23 fine-grained permissions stored as role claims

The permission system uses a dynamic policy provider (`PermissionPolicyProvider`) that handles any policy name matching the `Permissions.*` pattern, delegating evaluation to `PermissionAuthorizationHandler` which checks claims on the current user.

Roles and their permission claims are seeded at startup via `DatabaseSeeder.SeedRolesAndAdminAsync()`. Admin gets all permissions; HR and Interviewer get their respective subsets. A default admin account (`admin@recruitmentportal.com` / `Admin@123!`) is also seeded.

### Data Model

`ApplicationDbContext` extends `IdentityDbContext<ApplicationUser, ApplicationRole, string>`. Core entities:

- **Candidate** → has many **Interview** (via `CandidateId`), has many **CandidateNote**
- **JobPosition** → has many **Candidate**
- **Interview** → belongs to one **Candidate**, one **ApplicationUser** (Interviewer), has one **Feedback**
- **Feedback** → belongs to one **Interview**

`ApplicationUser` (extends `IdentityUser`) adds `FirstName` and `LastName`. Interviewers are `ApplicationUser` records with the Interviewer role — there is no separate Interviewer table (it was removed in migration `20260223072457_RemoveInterviewerModel`).

Status enums: `CandidateStatus` (Applied → Hired/Rejected), `InterviewRound` (Screening, Technical, Practical, Managerial, HR, Final).

### Key Services

| Service | Responsibility |
|---|---|
| `AuthService` | Login (returns JWT), ForgotPassword (placeholder) |
| `JwtTokenGenerator` | Builds JWT with user claims + role + permission claims |
| `PermissionPolicyProvider` | Dynamically generates `IAuthorizationPolicy` for `Permissions.*` |
| `PermissionAuthorizationHandler` | Checks permission claims against requirement |

### Configuration

JWT settings (`Key`, `Issuer`, `Audience`, `ExpireHours`) are in `appsettings.json`. The JWT key in that file is a development placeholder — use User Secrets or Azure Key Vault for production. Database connection string is also in `appsettings.json` pointing to the Azure SQL instance.

`Program.cs` applies EF migrations automatically on startup (`context.Database.Migrate()`).

### UI

Razor Views with a Bootstrap sidebar layout (`Views/Shared/_Layout.cshtml`). Static assets (Bootstrap, Font Awesome, custom CSS) are under `wwwroot/`. Admin-only nav items (Users, Roles & Permissions) are conditionally rendered based on the user's role claim.
