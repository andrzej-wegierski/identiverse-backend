# identiverse-backend

## 1. Project overview
Identiverse is an identity and profile management system designed to provide a secure and flexible way to handle user information. The backend is built as a RESTful API that manages authentication, authorization, and contextual identity profiles. It allows users to create different profiles for various contexts (e.g., social, professional, legal) while maintaining a single underlying identity.

## 2. Architecture overview
The project follows a layered architecture to ensure clean separation of concerns:
- **PublicApi**: Contains controllers, middleware, and authentication logic. It serves as the entry point for all HTTP requests.
- **Domain**: Encapsulates business logic, domain models, and service abstractions. Authorization is strictly enforced in this layer.
- **Database**: Handles EF Core entities, migrations, and repository implementations. It also includes ASP.NET Core Identity for user management.

## 3. Prerequisites
- **.NET SDK 10**: The project is built using the latest .NET version.
- **Docker & Docker Compose**: Required for running the PostgreSQL database.
- **Node.js**: Not required for the backend, but may be needed for the frontend.

## 4. Database setup (PostgreSQL via Docker)
Setting up the database is automated using a script that initializes a PostgreSQL container.

1. Navigate to the `Database` directory:
   ```bash
   cd Database
   ```
2. Run the initialization script:
   ```bash
   ./create-identiverse-db.sh
   ```
3. The script will:
   - Start a PostgreSQL 16 container in Docker.
   - Create the `identiverse-db` database and a `local` user with `psql` password.
   - Expose port `5432` for local development.

This script only needs to be run once.

## 5. Running the backend
Once the database is running, you can start the backend from the root directory:

```bash
dotnet restore
dotnet run --project PublicApi
```

- **Database migrations**: Applied automatically on startup.
- **Local access**: The backend runs on `http://localhost:5248/swagger/index.html` 

## 6. Configuration
Configuration is managed via `appsettings.json` and can be overridden by environment variables or User Secrets.

### Required Settings
- **IdentiverseDatabase:ConnectionString**: PostgreSQL connection string.
- **Jwt**: Settings for token issuance (Issuer, Audience, SigningKey).
- **Resend:ApiKey**: API key for email sending services.


## 6. Authentication & security notes
- **JWT Authentication**: Secure token-based access for all protected endpoints.
- **Email Confirmation**: Users must confirm their email before they can log in.
- **Password Reset**: Secure reset flow via email tokens.
- **Role-Based Authorization**: Access is controlled based on roles (`User`, `Admin`).
- **Domain-Level Security**: Ownership checks (Self-or-Admin) are performed in the Domain layer for person and profile-scoped data.