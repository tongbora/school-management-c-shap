
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using SchoolManagement.Application.Authentication.Interfaces;
using SchoolManagement.Application.Dashboard.Interfaces;
using SchoolManagement.Application.Students.Interfaces;
using SchoolManagement.Application.Guardians.Interfaces;
using SchoolManagement.Application.Teachers.Interfaces;
using SchoolManagement.Application.Academics.Interfaces;
using SchoolManagement.Application.Enrollments.Interfaces;
using SchoolManagement.Application.Attendance.Interfaces;
using SchoolManagement.Application.Examinations.Interfaces;
using SchoolManagement.Application.Finance.Interfaces;
using SchoolManagement.Infrastructure.Services;







using SchoolManagement.Desktop.Forms.Auth;

using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Security;
using SchoolManagement.Infrastructure.Services;


using WinFormsApp = System.Windows.Forms.Application;

namespace SchoolManagement.Desktop;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            // =========================================
            // 1. Initialize application host
            // =========================================

            var builder = Host.CreateApplicationBuilder();

            // =========================================
            // 2. Load appsettings.json
            // =========================================

            builder.Configuration.AddJsonFile(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "appsettings.json"),
                optional: false,
                reloadOnChange: false
            );

            // =========================================
            // 3. Get SQL Server connection settings
            // =========================================

            string? configuredConnection =
                builder.Configuration.GetConnectionString(
                    "SchoolDatabase");

            if (string.IsNullOrWhiteSpace(configuredConnection))
            {
                MessageBox.Show(
                    "SchoolDatabase connection is missing from appsettings.json.",
                    "Configuration Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return;
            }

            // =========================================
            // 4. Get SQL Server password
            // =========================================

            string? dbPassword =
                Environment.GetEnvironmentVariable(
                    "SCHOOL_DB_PASSWORD");

            if (string.IsNullOrWhiteSpace(dbPassword))
            {
                MessageBox.Show(
                    "SCHOOL_DB_PASSWORD is not configured.\n\n" +
                    "Please configure the SQL Server password.",
                    "Configuration Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return;
            }

            // =========================================
            // 5. Build SQL Server connection string
            // =========================================

            var sqlBuilder = new SqlConnectionStringBuilder(
                configuredConnection)
            {
                Password = dbPassword
            };

            string connectionString = sqlBuilder.ConnectionString;

            // =========================================
            // 6. Register Entity Framework Core
            // =========================================

            builder.Services.AddDbContextFactory<SchoolDbContext>(
                options =>
                {
                    options.UseSqlServer(
                        connectionString,
                        sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(
                                maxRetryCount: 3,
                                maxRetryDelay:
                                    TimeSpan.FromSeconds(5),
                                errorNumbersToAdd: null
                            );
                        });
                });

            builder.Services.AddScoped<SchoolDbContext>(sp =>
                sp.GetRequiredService<
                    IDbContextFactory<SchoolDbContext>>()
                  .CreateDbContext());

            // =========================================
            // 7. Register authentication services
            // =========================================

            builder.Services.AddSingleton<PasswordService>();

            builder.Services.AddScoped<AdminSetupService>();

            builder.Services.AddScoped<
                IAuthService,
                AuthService>();

            // =========================================
            // 8. Register dashboard service
            // =========================================

            builder.Services.AddScoped<
                IDashboardService,
                DashboardService>();

            // =========================================
            // 9. Register student service
            // =========================================

            builder.Services.AddScoped<
                IStudentService,
                StudentService>();


            builder.Services.AddScoped<
                ITeacherService,
                TeacherService>();

            builder.Services.AddScoped<
                IAcademicYearService,
                AcademicYearService>();

            builder.Services.AddScoped<
    IDepartmentService,
    DepartmentService>();

            builder.Services.AddScoped<ISubjectService, SubjectService>();


            builder.Services.AddScoped<
                IRoomService,
                RoomService>();

            builder.Services.AddScoped<
                IClassService,
                ClassService>();


            builder.Services.AddScoped<
                IClassSubjectService,
                ClassSubjectService>();

            builder.Services.AddScoped<
                IEnrollmentService,
                EnrollmentService>();

            builder.Services.AddScoped<
                IAttendanceService,
                AttendanceService>();


            builder.Services.AddScoped<IExamService, ExamService>();
            builder.Services.AddScoped<IGradeService, GradeService>();


            builder.Services.AddScoped<IFinanceService, FinanceService>();


            // =========================================
            // 10. Register guardian service
            // =========================================

            builder.Services.AddScoped<
                IGuardianService,
                GuardianService>();

            // =========================================
            // 11. Register WinForms
            // =========================================

            builder.Services.AddTransient<SetupAdminForm>();

            builder.Services.AddTransient<LoginForm>();

            builder.Services.AddTransient<MainForm>();

            // =========================================
            // 12. Build dependency injection container
            // =========================================

            using var host = builder.Build();

            using var scope = host.Services.CreateScope();

            var services = scope.ServiceProvider;

            // =========================================
            // 13. Verify SQL Server connection
            // =========================================

            var dbFactory = services.GetRequiredService<
                IDbContextFactory<SchoolDbContext>>();

            using (var db = dbFactory.CreateDbContext())
            {
                db.Database.OpenConnection();
                db.Database.CloseConnection();
            }

            // =========================================
            // 14. Check administrator setup
            // =========================================

            var setupService =
                services.GetRequiredService<AdminSetupService>();

            bool setupRequired = setupService
                .IsSetupRequiredAsync()
                .GetAwaiter()
                .GetResult();

            if (setupRequired)
            {
                using var setupForm =
                    services.GetRequiredService<SetupAdminForm>();

                if (setupForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
            }

            // =========================================
            // 15. Open login form
            // =========================================

            using var loginForm =
                services.GetRequiredService<LoginForm>();

            if (loginForm.ShowDialog() != DialogResult.OK ||
                loginForm.AuthenticatedUser?.IsSuccess != true)
            {
                return;
            }

            var currentUser = loginForm.AuthenticatedUser;

            // =========================================
            // 16. Open main dashboard
            // =========================================

            using var mainForm =
                services.GetRequiredService<MainForm>();

            mainForm.Text =
                $"School Management System - {currentUser.FullName}";

            WinFormsApp.Run(mainForm);
        }

        // =========================================
        // SQL Server errors
        // =========================================

        catch (SqlException ex)
        {
            MessageBox.Show(
                $"Unable to connect to SQL Server.\n\n" +
                $"Error Number: {ex.Number}\n\n" +
                "Please verify:\n" +
                "1. Docker SQL Server is running.\n" +
                "2. SQL Server port 1433 is available.\n" +
                "3. Database credentials are correct.\n" +
                "4. SchoolManagementDB exists.",
                "Database Connection Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        // =========================================
        // Other application errors
        // =========================================

        catch (Exception ex)
        {
#if DEBUG
            string message = ex.ToString();
#else
            string message =
                "Unable to start School Management System. " +
                "Please contact the administrator.";
#endif

            MessageBox.Show(
                message,
                "Application Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}
