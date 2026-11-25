using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Infrastructure.Persistence.Entities;

namespace SchoolManagement.Infrastructure.Persistence;

public partial class SchoolDbContext : DbContext
{
    public SchoolDbContext(DbContextOptions<SchoolDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AcademicYear> AcademicYears { get; set; }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassSubject> ClassSubjects { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Enrollment> Enrollments { get; set; }

    public virtual DbSet<Exam> Exams { get; set; }

    public virtual DbSet<Fee> Fees { get; set; }

    public virtual DbSet<FeeCategory> FeeCategories { get; set; }

    public virtual DbSet<FeePayment> FeePayments { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<Guardian> Guardians { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentGuardian> StudentGuardians { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<Timetable> Timetables { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<VwFeeBalance> VwFeeBalances { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcademicYear>(entity =>
        {
            entity.HasIndex(e => e.YearName, "UQ_AcademicYears_Name").IsUnique();

            entity.HasIndex(e => e.IsActive, "UX_AcademicYears_OneActive")
                .IsUnique()
                .HasFilter("([IsActive]=(1))");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_AcademicYears_CreatedAt");
            entity.Property(e => e.YearName).HasMaxLength(50);
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.ToTable("Attendance");

            entity.HasIndex(e => e.AttendanceDate, "IX_Attendance_Date");

            entity.HasIndex(e => new { e.EnrollmentId, e.AttendanceDate, e.SessionName }, "UQ_Attendance_EnrollmentDateSession").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Attendance_CreatedAt");
            entity.Property(e => e.Remark).HasMaxLength(255);
            entity.Property(e => e.SessionName)
                .HasMaxLength(20)
                .HasDefaultValue("FullDay", "DF_Attendance_Session");
            entity.Property(e => e.Status).HasMaxLength(30);

            entity.HasOne(d => d.Enrollment).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.EnrollmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attendance_Enrollments");

            entity.HasOne(d => d.RecordedByNavigation).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.RecordedBy)
                .HasConstraintName("FK_Attendance_Users");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.OccurredAt }, "IX_AuditLogs_UserTime").IsDescending(false, true);

            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.Property(e => e.EntityName).HasMaxLength(100);
            entity.Property(e => e.OccurredAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_AuditLogs_OccurredAt");

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_AuditLogs_Users");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasIndex(e => e.RoomId, "IX_Classes_Room");

            entity.HasIndex(e => e.HomeroomTeacherId, "IX_Classes_Teacher");

            entity.HasIndex(e => new { e.ClassId, e.AcademicYearId }, "UQ_Classes_IdYear").IsUnique();

            entity.HasIndex(e => new { e.AcademicYearId, e.ClassName }, "UQ_Classes_YearName").IsUnique();

            entity.Property(e => e.Capacity).HasDefaultValue(30, "DF_Classes_Capacity");
            entity.Property(e => e.ClassName).HasMaxLength(100);
            entity.Property(e => e.GradeLevel).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Classes_Active");

            entity.HasOne(d => d.AcademicYear).WithMany(p => p.Classes)
                .HasForeignKey(d => d.AcademicYearId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_AcademicYears");

            entity.HasOne(d => d.HomeroomTeacher).WithMany(p => p.Classes)
                .HasForeignKey(d => d.HomeroomTeacherId)
                .HasConstraintName("FK_Classes_Teachers");

            entity.HasOne(d => d.Room).WithMany(p => p.Classes)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK_Classes_Rooms");
        });

        modelBuilder.Entity<ClassSubject>(entity =>
        {
            entity.HasIndex(e => e.SubjectId, "IX_ClassSubjects_Subject");

            entity.HasIndex(e => e.TeacherId, "IX_ClassSubjects_Teacher");

            entity.HasIndex(e => new { e.ClassId, e.SubjectId, e.AcademicYearId }, "UQ_ClassSubjects_ClassSubjectYear").IsUnique();

            entity.HasIndex(e => new { e.ClassSubjectId, e.AcademicYearId }, "UQ_ClassSubjects_IdYear").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_ClassSubjects_CreatedAt");

            entity.HasOne(d => d.Subject).WithMany(p => p.ClassSubjects)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClassSubjects_Subjects");

            entity.HasOne(d => d.Teacher).WithMany(p => p.ClassSubjects)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("FK_ClassSubjects_Teachers");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassSubjects)
                .HasPrincipalKey(p => new { p.ClassId, p.AcademicYearId })
                .HasForeignKey(d => new { d.ClassId, d.AcademicYearId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClassSubjects_ClassYear");
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasIndex(e => e.DepartmentName, "UQ_Departments_Name").IsUnique();

            entity.Property(e => e.DepartmentName).HasMaxLength(120);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Departments_Active");
        });

        modelBuilder.Entity<Enrollment>(entity =>
        {
            entity.HasIndex(e => new { e.ClassId, e.AcademicYearId }, "IX_Enrollments_ClassYear");

            entity.HasIndex(e => new { e.StudentId, e.ClassId, e.AcademicYearId }, "UQ_Enrollments_StudentClassYear").IsUnique();

            entity.HasIndex(e => new { e.StudentId, e.AcademicYearId }, "UX_Enrollments_ActiveStudentYear")
                .IsUnique()
                .HasFilter("([Status]=N'Active')");

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Enrollments_CreatedAt");
            entity.Property(e => e.EnrollmentDate).HasDefaultValueSql("(CONVERT([date],getdate()))", "DF_Enrollments_Date");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Active", "DF_Enrollments_Status");

            entity.HasOne(d => d.Student).WithMany(p => p.Enrollments)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Enrollments_Students");

            entity.HasOne(d => d.Class).WithMany(p => p.Enrollments)
                .HasPrincipalKey(p => new { p.ClassId, p.AcademicYearId })
                .HasForeignKey(d => new { d.ClassId, d.AcademicYearId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Enrollments_ClassYear");
        });

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.HasIndex(e => new { e.ExamId, e.AcademicYearId }, "UQ_Exams_IdYear").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Exams_CreatedAt");
            entity.Property(e => e.ExamName).HasMaxLength(120);
            entity.Property(e => e.ExamType).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Draft", "DF_Exams_Status");

            entity.HasOne(d => d.AcademicYear).WithMany(p => p.Exams)
                .HasForeignKey(d => d.AcademicYearId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Exams_AcademicYears");
        });

        modelBuilder.Entity<Fee>(entity =>
        {
            entity.HasIndex(e => e.DueDate, "IX_Fees_DueDate");

            entity.HasIndex(e => e.EnrollmentId, "IX_Fees_Enrollment");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Fees_CreatedAt");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength()
                .HasDefaultValue("USD", "DF_Fees_Currency");
            entity.Property(e => e.Description).HasMaxLength(255);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Fees)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Fees_Users");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.Fees)
                .HasForeignKey(d => d.EnrollmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Fees_Enrollments");

            entity.HasOne(d => d.FeeCategory).WithMany(p => p.Fees)
                .HasForeignKey(d => d.FeeCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Fees_Categories");
        });

        modelBuilder.Entity<FeeCategory>(entity =>
        {
            entity.HasIndex(e => e.CategoryName, "UQ_FeeCategories_Name").IsUnique();

            entity.Property(e => e.CategoryName).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_FeeCategories_Active");
        });

        modelBuilder.Entity<FeePayment>(entity =>
        {
            entity.HasKey(e => e.PaymentId);

            entity.HasIndex(e => e.FeeId, "IX_FeePayments_Fee");

            entity.HasIndex(e => e.ReferenceNo, "UX_FeePayments_ReferenceNo")
                .IsUnique()
                .HasFilter("([ReferenceNo] IS NOT NULL)");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_FeePayments_CreatedAt");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.PaymentDate)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_FeePayments_PaymentDate");
            entity.Property(e => e.PaymentMethod).HasMaxLength(30);
            entity.Property(e => e.ReferenceNo).HasMaxLength(100);

            entity.HasOne(d => d.Fee).WithMany(p => p.FeePayments)
                .HasForeignKey(d => d.FeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FeePayments_Fees");

            entity.HasOne(d => d.ReceivedByNavigation).WithMany(p => p.FeePayments)
                .HasForeignKey(d => d.ReceivedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FeePayments_Users");
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasIndex(e => new { e.ExamId, e.AcademicYearId }, "IX_Grades_ExamYear");

            entity.HasIndex(e => new { e.EnrollmentId, e.ClassSubjectId, e.ExamId }, "UQ_Grades_OneScore").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Grades_CreatedAt");
            entity.Property(e => e.MaxScore)
                .HasDefaultValue(100m, "DF_Grades_MaxScore")
                .HasColumnType("decimal(6, 2)");
            entity.Property(e => e.Remark).HasMaxLength(255);
            entity.Property(e => e.Score).HasColumnType("decimal(6, 2)");
            entity.Property(e => e.UpdatedAt).HasPrecision(3);

            entity.HasOne(d => d.Enrollment).WithMany(p => p.Grades)
                .HasForeignKey(d => d.EnrollmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Grades_Enrollments");

            entity.HasOne(d => d.RecordedByNavigation).WithMany(p => p.Grades)
                .HasForeignKey(d => d.RecordedBy)
                .HasConstraintName("FK_Grades_Users");

            entity.HasOne(d => d.ClassSubject).WithMany(p => p.Grades)
                .HasPrincipalKey(p => new { p.ClassSubjectId, p.AcademicYearId })
                .HasForeignKey(d => new { d.ClassSubjectId, d.AcademicYearId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Grades_ClassSubjectsYear");

            entity.HasOne(d => d.Exam).WithMany(p => p.Grades)
                .HasPrincipalKey(p => new { p.ExamId, p.AcademicYearId })
                .HasForeignKey(d => new { d.ExamId, d.AcademicYearId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Grades_ExamsYear");
        });

        modelBuilder.Entity<Guardian>(entity =>
        {
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Guardians_CreatedAt");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.Occupation).HasMaxLength(120);
            entity.Property(e => e.Phone).HasMaxLength(30);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(e => e.PermissionKey, "UQ_Permissions_Key").IsUnique();

            entity.Property(e => e.ModuleName).HasMaxLength(100);
            entity.Property(e => e.PermissionKey).HasMaxLength(150);
            entity.Property(e => e.PermissionName).HasMaxLength(150);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.RoleName, "UQ_Roles_Name").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Roles_CreatedAt");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Roles_Active");
            entity.Property(e => e.RoleName).HasMaxLength(100);

            entity.HasMany(d => d.Permissions).WithMany(p => p.Roles)
                .UsingEntity<Dictionary<string, object>>(
                    "RolePermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_RolePermissions_Permissions"),
                    l => l.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_RolePermissions_Roles"),
                    j =>
                    {
                        j.HasKey("RoleId", "PermissionId");
                        j.ToTable("RolePermissions");
                    });
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasIndex(e => e.RoomCode, "UQ_Rooms_Code").IsUnique();

            entity.Property(e => e.Building).HasMaxLength(100);
            entity.Property(e => e.Capacity).HasDefaultValue(30, "DF_Rooms_Capacity");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Rooms_Active");
            entity.Property(e => e.RoomCode).HasMaxLength(50);
            entity.Property(e => e.RoomName).HasMaxLength(100);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasIndex(e => e.StudentCode, "UQ_Students_Code").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Students_CreatedAt");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.PhotoPath).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Active", "DF_Students_Status");
            entity.Property(e => e.StudentCode).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasPrecision(3);
        });

        modelBuilder.Entity<StudentGuardian>(entity =>
        {
            entity.HasKey(e => new { e.StudentId, e.GuardianId });

            entity.HasIndex(e => e.GuardianId, "IX_StudentGuardians_Guardian");

            entity.HasIndex(e => e.StudentId, "UX_StudentGuardians_OnePrimary")
                .IsUnique()
                .HasFilter("([IsPrimary]=(1))");

            entity.Property(e => e.Relationship).HasMaxLength(50);

            entity.HasOne(d => d.Guardian).WithMany(p => p.StudentGuardians)
                .HasForeignKey(d => d.GuardianId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentGuardians_Guardians");

            entity.HasOne(d => d.Student)
                .WithMany(p => p.StudentGuardians)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentGuardians_Students");

        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasIndex(e => e.SubjectCode, "UQ_Subjects_Code").IsUnique();

            entity.Property(e => e.GradeLevel).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Subjects_Active");
            entity.Property(e => e.SubjectCode).HasMaxLength(50);
            entity.Property(e => e.SubjectName).HasMaxLength(120);
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasIndex(e => e.DepartmentId, "IX_Teachers_Department");

            entity.HasIndex(e => e.TeacherCode, "UQ_Teachers_Code").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Teachers_CreatedAt");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Active", "DF_Teachers_Status");
            entity.Property(e => e.SubjectSpecialty).HasMaxLength(120);
            entity.Property(e => e.TeacherCode).HasMaxLength(50);

            entity.HasOne(d => d.Department).WithMany(p => p.Teachers)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK_Teachers_Departments");
        });

        modelBuilder.Entity<Timetable>(entity =>
        {
            entity.HasIndex(e => new { e.RoomId, e.DayOfWeek, e.StartTime }, "IX_Timetables_RoomDay");

            entity.HasIndex(e => new { e.ClassSubjectId, e.DayOfWeek, e.StartTime }, "UQ_Timetables_ClassSubjectSlot").IsUnique();

            entity.Property(e => e.EndTime).HasPrecision(0);
            entity.Property(e => e.StartTime).HasPrecision(0);

            entity.HasOne(d => d.ClassSubject).WithMany(p => p.Timetables)
                .HasForeignKey(d => d.ClassSubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Timetables_ClassSubjects");

            entity.HasOne(d => d.Room).WithMany(p => p.Timetables)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK_Timetables_Rooms");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username, "UQ_Users_Username").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())", "DF_Users_CreatedAt");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Users_IsActive");
            entity.Property(e => e.LastLoginAt).HasPrecision(3);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.UpdatedAt).HasPrecision(3);
            entity.Property(e => e.Username).HasMaxLength(100);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UserRoles_Roles"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UserRoles_Users"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("UserRoles");
                    });
        });

        modelBuilder.Entity<VwFeeBalance>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwFeeBalances");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BalanceAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CurrencyCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.PaidAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PaymentStatus).HasMaxLength(7);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
