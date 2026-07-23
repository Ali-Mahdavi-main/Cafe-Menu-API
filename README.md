<<<<<<< HEAD
# CafeMenu Backend Context (Current State)

## Project Overview

A multi-tenant SaaS platform for digital cafe menus built with:

* .NET 8 Web API
* Entity Framework Core
* SQL Server
* JWT Authentication
* React frontend planned

Goal:
Multiple cafes share one database and one backend while each cafe can only access its own data.

---

# Current Architecture

## Multi-Tenant Design

Main entities:

Cafe

* Id
* Name
* Address
* LogoUrl
* InstagramUrl
* ThemeConfigJson
* UserName
* PasswordHash

Category

* Id
* Name
* CafeId

MenuItem

* Id
* Title
* Description
* Price
* ImageUrl
* IsAvailable
* CafeId
* CategoryId

Relationships:

Cafe
├── Categories
└── MenuItems

Category
└── MenuItems

Every tenant is isolated using CafeId.

---

# Authentication

Implemented:

* Register endpoint
* Login endpoint
* BCrypt password hashing
* JWT token generation

JWT Claims:

* NameIdentifier
* Name
* CafeId
* CafeName

Token contains tenant identity.

Backend never trusts CafeId coming from frontend.

---

# Authorization

Protected endpoints use:

[Authorize]

Public endpoints use:

[AllowAnonymous]

Examples:

Public:

* GET /api/menu/{cafeId}
* GET /api/category/{cafeId}

Protected:

* Create menu item
* Update menu item
* Delete menu item
* Create category

---

# Tenant Isolation

Current authenticated cafe is determined from JWT claim:

CafeId

Example:

var cafeId = int.Parse(
User.FindFirstValue("CafeId")!
);

Ownership checks are performed before updates and deletes.

---

# CurrentCafeService

Implemented:

ICurrentCafeService

CurrentCafeService

Uses:

IHttpContextAccessor

Purpose:

Provides current authenticated CafeId anywhere in application without reading claims manually inside controllers.

---

# Global Query Filters

Implemented for:

* MenuItem
* Category

Purpose:

Prevent accidental cross-tenant data leaks.

Example:

_context.MenuItems.ToListAsync()

Automatically becomes:

SELECT *
FROM MenuItems
WHERE CafeId = CurrentCafeId

---

# Validation

DTO validation implemented using DataAnnotations.

Examples:

* Required
* MaxLength
* MinLength
* Range
* Url

Because ApiController attribute is used, invalid requests are automatically rejected before reaching controller actions.

---

# Global Exception Middleware

Implemented:

ExceptionMiddleware

Responsibilities:

* Catch unhandled exceptions
* Log errors
* Return standard JSON responses
* Prevent stack traces from reaching clients

Typical response:

{
"message": "An unexpected error occurred.",
"statusCode": 500
}

---

# Security Features

Implemented:

* BCrypt password hashing
* JWT authentication
* Authorization attributes
* Claim-based tenant resolution
* DTO validation
* Query filters
* Global exception handling

---

# Database

Technology:

* SQL Server
* Entity Framework Core

Important issue already solved:

Multiple cascade path error between:

Cafe
Category
MenuItem

Delete behavior configured manually to avoid SQL Server cascade conflicts.

---

# Image Strategy

Decision:

Images will initially be stored directly on the server.

Planned structure:

wwwroot/uploads/cafes/{cafeId}/

ImageUrl will store relative paths.

Example:

/uploads/cafes/15/abc123.jpg

Files will use Guid-based filenames to avoid collisions.

---

# Planned Frontend Stack

* React
* Tailwind CSS
* Axios
* React Router

---

# Planned Frontend Pages

Public:

* Public menu page
* Category list
* Product list

Admin:

* Login
* Register
* Dashboard
* Category management
* Menu management
* Settings page

---

# Development Philosophy

Priority order:

1. Build a working MVP
2. Get first paying cafes
3. Expand features based on real customer feedback

Avoid premature optimization and unnecessary architecture complexity.

---

# Suggested Next Backend Steps

Optional future improvements:

* Service Layer
* Refresh Tokens
* File Upload API
* Subscription System
* Payment Integration
* Analytics
* QR Ordering

Current recommendation:

Start frontend development and build the first usable version of the product.
=======
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
>>>>>>> f60375f9d563eed7e501b7b8e38d036afca3d3cd
