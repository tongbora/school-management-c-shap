# School Management System

A Windows desktop school management application built with **C# / .NET 10 WinForms**, **Entity Framework Core**, and **SQL Server 2022**. SQL Server can run locally using Docker Compose.

## Features

- Dashboard
- Student and guardian management
- Teacher management
- Academic years, departments, rooms, subjects, classes, and class-subject assignments
- Student enrollments and transfers
- Attendance tracking and history
- Examinations, individual grades, and bulk grade entry
- Finance: fee categories, student fees, outstanding balances, and payment history
- Authentication and first-run administrator setup

> **Development status:** This is a learning/development project. Review authentication, authorization, audit logging, finance controls, and concurrency handling before using real school or payment data. Payment recording requires integration with the authenticated user's ID.

## Requirements

- **Windows 10/11** (the desktop UI uses WinForms)
- **.NET 10 SDK** — [Download .NET](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker Desktop** with Linux containers — [Download Docker](https://www.docker.com/products/docker-desktop/)
- Visual Studio 2022 or later with Windows desktop development workload, or a compatible IDE / terminal
- **DataGrip** or **SQL Server Management Studio** (SSMS) to run the SQL scripts

## 1. Clone the repository

Open PowerShell:

```powershell
git clone https://github.com/tongbora/school-management-c-shap.git
cd school-management-c-shap
```

## 2. Start SQL Server with Docker Compose (Option 1: password in YAML)

The repository contains `docker-compose.yml`, which configures **SQL Server 2022 Developer Edition**. This setup stores the SQL Server administrator password **directly in the Compose file**:

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: school-sqlserver
    restart: unless-stopped
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: YourStrongPassword123!
      MSSQL_PID: Developer
    ports:
      - "127.0.0.1:1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
```

**Before starting:** Open `docker-compose.yml` and replace `YourStrongPassword123!` with a **strong, unique password of your own** that satisfies SQL Server password complexity requirements. **No `.env` file is needed for this option.** Keep the existing health-check and named-volume sections of the Compose file.

> **Security warning for this public repository:** A password stored in a committed YAML file is visible to everyone and remains recoverable from Git history. Use this configuration **only for disposable, local development databases**. Never reuse this password for another account or a production database. For any nonlocal/shared environment, use secret or environment-based configuration instead. If a real password was previously published, rotate it; editing the file does not erase the old commit.

Open PowerShell in the repository root and start Docker Desktop, then run:

```powershell
docker compose config
docker compose up -d
docker compose ps
```

Check logs if SQL Server does not become ready:

```powershell
docker compose logs --tail 100 sqlserver
```

The container is mapped to **`127.0.0.1:1433`** (only your computer) and database files are preserved in the Docker named volume `sqlserver_data`.

> If the container is marked unhealthy, check the logs. Some SQL Server 2022 image versions may not contain `/opt/mssql-tools18/bin/sqlcmd` at the path used by the health check. That can cause a health-check failure even when the server is running.

> **Existing Docker volume:** Updating the Compose password **does not automatically change the password of an existing `sa` login**. For an existing SQL Server instance, change the SQL login password through an authorized SQL connection and keep the Compose and application settings consistent. Do not delete the volume to change a password.

## 3. Initialize the database (first installation only)

Connect with DataGrip or SSMS using:

| Setting | Value |
| --- | --- |
| Server / Host | `localhost` |
| Port | `1433` |
| Authentication | SQL Server authentication |
| User | `sa` |
| Password | `MSSQL_SA_PASSWORD` value configured in `docker-compose.yml` |
| Initial database | `master` |
| Encryption | Trust server certificate for **local development only** |

Execute these files **in order**:

1. `01_create_database.sql` — creates `SchoolManagementDB`, tables, foreign keys, indexes, roles, permissions and the fee-balance view. **Run only for a new/empty database.**
2. `02_seed_demo_data.sql` — **optional** sample records for students, teachers, academic years, attendance, examinations, grades, and finance. Use **development/test databases only**.

**Important execution details:** Run the **entire** script each time, not a selected `INSERT` statement. `01_create_database.sql` uses SQL Server `GO` batch separators, so use a client or script-execution mode that understands `GO`. The demo seed must run as one complete batch so its variables and transaction remain in scope.

The seed script does not create login accounts. Its sample payment rows are inserted only when an active Administrator user already exists; otherwise those payments are skipped. After creating your administrator, you may rerun the seed script to add the demo payments.

## 4. Configure the application connection

The desktop app connects to SQL Server on `127.0.0.1:1433`, database `SchoolManagementDB`. Configure the existing `SchoolManagement.Desktop/appsettings.json` connection settings for your local environment **without committing credentials**.

The application setup used in this project reads its database password from the Windows environment variable `SCHOOL_DB_PASSWORD`. Set it in the PowerShell session **before running**:

```powershell
$env:SCHOOL_DB_PASSWORD = Read-Host "SQL Server password"
```

Enter the same password specified under `MSSQL_SA_PASSWORD` in `docker-compose.yml`. This variable affects only the current PowerShell session. If you launch from Visual Studio instead, make the variable available to the Visual Studio process (or configure it securely in local-only settings).

> The WinForms application does **not** automatically read the SQL Server password from `docker-compose.yml`. You must separately set `SCHOOL_DB_PASSWORD` to the same value before launching the desktop application.

## 5. Build and run

From the repository root:

```powershell
dotnet restore SchoolManagement.slnx
dotnet build SchoolManagement.slnx
dotnet run --project SchoolManagement.Desktop
```

Alternatively, open `SchoolManagement.slnx` in Visual Studio, select `SchoolManagement.Desktop` as the startup project, and run it. Ensure the database is running and the application can access `SCHOOL_DB_PASSWORD`.

On the **first application launch**, follow the secure administrator setup flow to create the initial account. Subsequent launches use the login screen. **No default administrator username/password is provided in this repository.**

## 6. Main project layout

```text
school-management-c-shap/
├── SchoolManagement.Application/    # Application contracts and DTOs
├── SchoolManagement.Desktop/        # Windows Forms UI and startup
├── SchoolManagement.Domain/         # Domain layer
├── SchoolManagement.Infrastructure/ # EF Core models and services
├── 01_create_database.sql           # Fresh-install database schema
├── 02_seed_demo_data.sql            # Optional demo/test data
├── docker-compose.yml               # SQL Server 2022 container (local-demo password)
├── .gitignore
├── README.md
└── SchoolManagement.slnx
```

## Common commands

```powershell
# Check SQL Server container
docker compose ps

# Stop the container (keeps database data)
docker compose stop

# Start it again
docker compose start

# Follow database logs
docker compose logs -f sqlserver

# Build the application
dotnet build SchoolManagement.slnx
```

**Warning:** `docker compose down -v` deletes the named database volume and can permanently remove your SQL Server data. Do not use it unless you intentionally want a fresh database and have verified backups.

## Troubleshooting

**`Login failed for user 'sa'`** — Confirm that the actual `sa` password, the password in `docker-compose.yml`, and the desktop app’s `SCHOOL_DB_PASSWORD` match. Changing the Compose password after initializing a SQL Server data volume does **not** automatically reset the existing `sa` password.

**`Cannot connect to SQL Server`** — Check `docker compose ps`, the Docker logs, whether port 1433 is occupied, and your local connection settings.

**`Must declare the scalar variable @YearId`** — You ran only part of the demo script. Execute the entire `02_seed_demo_data.sql` as a single batch.

**`GO` syntax error** — The SQL tool ran the schema as a single SQL statement. Use script execution / SQLCMD-compatible batch handling.

**Project does not run on Linux/macOS** — The SQL Server container can run on supported Docker hosts, but the WinForms desktop client requires Windows.

## Security and contributing

- This **Option 1** setup has a password in `docker-compose.yml`; treat it as an **insecure local-demo convention**, not a production default. Change the example password, do not reuse it, and avoid committing any real or shared credential. Never commit production connection strings, database backups, or personal student records.
- Demo students and payments are synthetic and should never be treated as real school records.
- Review and exclude temporary utilities such as `SchoolManagement.PasswordReset` before publishing; do not expose account recovery shortcuts.
- The SQL scripts are currently **manual setup** files; automatic non-destructive database migrations have not yet been implemented.

## License

No open-source license has been included yet. If you want others to reuse or modify the code, add an appropriate `LICENSE` file.
