# Cafe Menu SaaS – Backend API

A multi-tenant REST API for managing digital cafe menus, built with ASP.NET Core 8 and Entity Framework Core. Provides authentication, menu management, image uploads, public menu endpoints, and admin controls.

## Tech Stack
- **Framework:** ASP.NET Core 8 Web API
- **Database:** SQL Server (via Entity Framework Core)
- **Authentication:** JWT with BCrypt password hashing
- **Image Processing:** SkiaSharp (resize & compress uploads)
- **Architecture:** Multi-tenant, shared database with tenant isolation via CafeId claim

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- SQL Server (local, Docker, or cloud)
- [Docker](https://www.docker.com/) (optional, for containerised deployment)
