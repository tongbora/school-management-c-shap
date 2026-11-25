/*
 School Management System - SQL Server 2022 fresh-install schema
 Run using SSMS in SQLCMD-compatible script window (GO batch separators).
 Creates SchoolManagementDB if absent. Does NOT drop tables or data.
 IMPORTANT: Run only against a new/empty SchoolManagementDB.
 Account passwords must be hashed in the C# application; no default admin account.
 Dates/times for audit use UTC; school calendar dates are local school dates.
*/
USE master;
GO
IF DB_ID(N'SchoolManagementDB') IS NULL
    CREATE DATABASE SchoolManagementDB;
GO
USE SchoolManagementDB;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.Users',N'U') IS NOT NULL
    THROW 51000, 'Schema already exists. This is a fresh-install script; do not run against an existing schema.', 1;
GO
BEGIN TRY
BEGIN TRANSACTION;

-- 1. Security
CREATE TABLE dbo.Users (
    UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL,
    Username NVARCHAR(100) NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL, -- store algorithm/version/salt/hash in one encoded value
    Email NVARCHAR(150) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    MustChangePassword BIT NOT NULL CONSTRAINT DF_Users_ChangePassword DEFAULT (0),
    LastLoginAt DATETIME2(3) NULL,
    CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(3) NULL
);

CREATE TABLE dbo.Roles (
    RoleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    RoleName NVARCHAR(100) NOT NULL CONSTRAINT UQ_Roles_Name UNIQUE,
    Description NVARCHAR(500) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Roles_Active DEFAULT (1),
    CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Roles_CreatedAt DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dbo.Permissions (
    PermissionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Permissions PRIMARY KEY,
    PermissionKey NVARCHAR(150) NOT NULL CONSTRAINT UQ_Permissions_Key UNIQUE,
    PermissionName NVARCHAR(150) NOT NULL,
    ModuleName NVARCHAR(100) NOT NULL
);

CREATE TABLE dbo.UserRoles (
    UserId INT NOT NULL,
    RoleId INT NOT NULL,
    CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId)
);

CREATE TABLE dbo.RolePermissions (
    RoleId INT NOT NULL,
    PermissionId INT NOT NULL,
    CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleId, PermissionId),
    CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId),
    CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions(PermissionId)
);

-- 2. Academic reference data
CREATE TABLE dbo.AcademicYears (
    AcademicYearId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AcademicYears PRIMARY KEY,
    YearName NVARCHAR(50) NOT NULL CONSTRAINT UQ_AcademicYears_Name UNIQUE,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_AcademicYears_Active DEFAULT (0),
    CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_AcademicYears_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_AcademicYears_Date CHECK (EndDate >= StartDate)
);

CREATE UNIQUE INDEX UX_AcademicYears_OneActive ON dbo.AcademicYears(IsActive) WHERE IsActive = 1;

CREATE TABLE dbo.Departments (
    DepartmentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Departments PRIMARY KEY,
    DepartmentName NVARCHAR(120) NOT NULL CONSTRAINT UQ_Departments_Name UNIQUE,
    Description NVARCHAR(500) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Departments_Active DEFAULT (1)
);

CREATE TABLE dbo.Rooms (
    RoomId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Rooms PRIMARY KEY,
    RoomCode NVARCHAR(50) NOT NULL CONSTRAINT UQ_Rooms_Code UNIQUE,
    RoomName NVARCHAR(100) NULL,
    Building NVARCHAR(100) NULL,
    FloorNumber INT NULL,
    Capacity INT NOT NULL CONSTRAINT DF_Rooms_Capacity DEFAULT (30),
    IsActive BIT NOT NULL CONSTRAINT DF_Rooms_Active DEFAULT (1),
    CONSTRAINT CK_Rooms_Capacity CHECK (Capacity > 0)
);

CREATE TABLE dbo.Teachers (
    TeacherId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Teachers PRIMARY KEY,
    TeacherCode NVARCHAR(50) NOT NULL CONSTRAINT UQ_Teachers_Code UNIQUE,
    FullName NVARCHAR(150) NOT NULL,
    Gender NVARCHAR(20) NULL,
    DateOfBirth DATE NULL,
    Phone NVARCHAR(30) NULL,
    Email NVARCHAR(150) NULL,
    SubjectSpecialty NVARCHAR(120) NULL,
    DepartmentId INT NULL,
    HireDate DATE NOT NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_Teachers_Status DEFAULT (N'Active'),
    CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Teachers_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Teachers_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(DepartmentId),
    CONSTRAINT CK_Teachers_Status CHECK (Status IN (N'Active',N'Inactive',N'OnLeave'))
);

CREATE TABLE dbo.Classes (
    ClassId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Classes PRIMARY KEY,
    ClassName NVARCHAR(100) NOT NULL,
    GradeLevel NVARCHAR(50) NOT NULL,
    AcademicYearId INT NOT NULL,
    RoomId INT NULL,
    HomeroomTeacherId INT NULL,
    Capacity INT NOT NULL CONSTRAINT DF_Classes_Capacity DEFAULT (30),
    IsActive BIT NOT NULL CONSTRAINT DF_Classes_Active DEFAULT (1),
    CONSTRAINT FK_Classes_AcademicYears FOREIGN KEY (AcademicYearId) REFERENCES dbo.AcademicYears(AcademicYearId),
    CONSTRAINT FK_Classes_Rooms FOREIGN KEY (RoomId) REFERENCES dbo.Rooms(RoomId),
    CONSTRAINT FK_Classes_Teachers FOREIGN KEY (HomeroomTeacherId) REFERENCES dbo.Teachers(TeacherId),
    CONSTRAINT UQ_Classes_YearName UNIQUE (AcademicYearId, ClassName),
    CONSTRAINT UQ_Classes_IdYear UNIQUE (ClassId, AcademicYearId),
    CONSTRAINT CK_Classes_Capacity CHECK (Capacity > 0)
);

CREATE TABLE dbo.Subjects (
    SubjectId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Subjects PRIMARY KEY,
    SubjectCode NVARCHAR(50) NOT NULL CONSTRAINT UQ_Subjects_Code UNIQUE,
    SubjectName NVARCHAR(120) NOT NULL,
    GradeLevel NVARCHAR(50) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_Subjects_Active DEFAULT (1)
);

-- 3. Student records
CREATE TABLE dbo.Students (
    StudentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Students PRIMARY KEY,
    StudentCode NVARCHAR(50) NOT NULL CONSTRAINT UQ_Students_Code UNIQUE,
    FullName NVARCHAR(150) NOT NULL,
    Gender NVARCHAR(20) NOT NULL,
    DateOfBirth DATE NOT NULL,
    Phone NVARCHAR(30) NULL,
    Email NVARCHAR(150) NULL,
    Address NVARCHAR(255) NULL,
    PhotoPath NVARCHAR(500) NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_Students_Status DEFAULT (N'Active'),
    CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Students_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(3) NULL,
    CONSTRAINT CK_Students_Status CHECK (Status IN (N'Active',N'Inactive',N'Graduated',N'Transferred'))
);

CREATE TABLE dbo.Guardians (
    GuardianId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Guardians PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL,
    Gender NVARCHAR(20) NULL,
    Phone NVARCHAR(30) NULL,
    Email NVARCHAR(150) NULL,
    Occupation NVARCHAR(120) NULL,
    Address NVARCHAR(255) NULL,
    CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Guardians_CreatedAt DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dbo.StudentGuardians (
    StudentId INT NOT NULL,
    GuardianId INT NOT NULL,
    Relationship NVARCHAR(50) NOT NULL,
    IsPrimary BIT NOT NULL CONSTRAINT DF_StudentGuardians_Primary DEFAULT (0),
    CONSTRAINT PK_StudentGuardians PRIMARY KEY (StudentId, GuardianId),
    CONSTRAINT FK_StudentGuardians_Students FOREIGN KEY (StudentId) REFERENCES dbo.Students(StudentId),
    CONSTRAINT FK_StudentGuardians_Guardians FOREIGN KEY (GuardianId) REFERENCES dbo.Guardians(GuardianId)
);
CREATE UNIQUE INDEX UX_StudentGuardians_OnePrimary ON dbo.StudentGuardians(StudentId) WHERE IsPrimary = 1;

CREATE TABLE dbo.Enrollments (
    EnrollmentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Enrollments PRIMARY KEY,
    StudentId INT NOT NULL,
    ClassId INT NOT NULL,
    AcademicYearId INT NOT NULL,
    EnrollmentDate DATE NOT NULL CONSTRAINT DF_Enrollments_Date DEFAULT CONVERT(date,GETDATE()),
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_Enrollments_Status DEFAULT (N'Active'),
    EndDate DATE NULL,
    CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Enrollments_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Enrollments_Students FOREIGN KEY (StudentId) REFERENCES dbo.Students(StudentId),
    CONSTRAINT FK_Enrollments_ClassYear FOREIGN KEY (ClassId, AcademicYearId)
        REFERENCES dbo.Classes(ClassId, AcademicYearId),
    CONSTRAINT UQ_Enrollments_StudentClassYear UNIQUE (StudentId, ClassId, AcademicYearId),
    CONSTRAINT CK_Enrollments_Status CHECK (Status IN (N'Active',N'Completed',N'Transferred',N'Withdrawn')),
    CONSTRAINT CK_Enrollments_Dates CHECK (EndDate IS NULL OR EndDate >= EnrollmentDate)
);
CREATE UNIQUE INDEX UX_Enrollments_ActiveStudentYear
    ON dbo.Enrollments(StudentId, AcademicYearId) WHERE Status = N'Active';

-- 4. Class subject assignments and timetable
CREATE TABLE dbo.ClassSubjects (
    ClassSubjectId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ClassSubjects PRIMARY KEY,
    ClassId INT NOT NULL,
    SubjectId INT NOT NULL,
    TeacherId INT NULL,
    AcademicYearId INT NOT NULL,
    CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_ClassSubjects_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ClassSubjects_ClassYear FOREIGN KEY (ClassId, AcademicYearId)
        REFERENCES dbo.Classes(ClassId, AcademicYearId),
    CONSTRAINT FK_ClassSubjects_Subjects FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects(SubjectId),
    CONSTRAINT FK_ClassSubjects_Teachers FOREIGN KEY (TeacherId) REFERENCES dbo.Teachers(TeacherId),
    CONSTRAINT UQ_ClassSubjects_ClassSubjectYear UNIQUE (ClassId, SubjectId, AcademicYearId),
    CONSTRAINT UQ_ClassSubjects_IdYear UNIQUE (ClassSubjectId, AcademicYearId)
);

CREATE TABLE dbo.Timetables (
    TimetableId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Timetables PRIMARY KEY,
    ClassSubjectId INT NOT NULL,
    RoomId INT NULL,
    DayOfWeek TINYINT NOT NULL, -- 1=Monday, ... 7=Sunday
    StartTime TIME(0) NOT NULL,
    EndTime TIME(0) NOT NULL,
    CONSTRAINT FK_Timetables_ClassSubjects FOREIGN KEY (ClassSubjectId) REFERENCES dbo.ClassSubjects(ClassSubjectId),
    CONSTRAINT FK_Timetables_Rooms FOREIGN KEY (RoomId) REFERENCES dbo.Rooms(RoomId),
    CONSTRAINT CK_Timetables_Weekday CHECK (DayOfWeek BETWEEN 1 AND 7),
    CONSTRAINT CK_Timetables_Time CHECK (EndTime > StartTime),
    CONSTRAINT UQ_Timetables_ClassSubjectSlot UNIQUE (ClassSubjectId,DayOfWeek,StartTime)
);

-- 5. Attendance and assessments
CREATE TABLE dbo.Attendance (
    AttendanceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Attendance PRIMARY KEY,
    EnrollmentId INT NOT NULL,
    AttendanceDate DATE NOT NULL,
    SessionName NVARCHAR(20) NOT NULL CONSTRAINT DF_Attendance_Session DEFAULT (N'FullDay'),
    Status NVARCHAR(30) NOT NULL,
    Remark NVARCHAR(255) NULL,
    RecordedBy INT NULL,
    CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Attendance_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Attendance_Enrollments FOREIGN KEY (EnrollmentId) REFERENCES dbo.Enrollments(EnrollmentId),
    CONSTRAINT FK_Attendance_Users FOREIGN KEY (RecordedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT UQ_Attendance_EnrollmentDateSession UNIQUE (EnrollmentId,AttendanceDate,SessionName),
    CONSTRAINT CK_Attendance_Session CHECK (SessionName IN (N'FullDay',N'Morning',N'Afternoon')),
    CONSTRAINT CK_Attendance_Status CHECK (Status IN (N'Present',N'Absent',N'Late',N'Excused'))
);

CREATE TABLE dbo.Exams (
    ExamId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Exams PRIMARY KEY,
    ExamName NVARCHAR(120) NOT NULL,
    ExamType NVARCHAR(50) NULL,
    AcademicYearId INT NOT NULL,
    StartDate DATE NULL,
    EndDate DATE NULL,
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_Exams_Status DEFAULT (N'Draft'),
    CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Exams_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Exams_AcademicYears FOREIGN KEY (AcademicYearId) REFERENCES dbo.AcademicYears(AcademicYearId),
    CONSTRAINT UQ_Exams_IdYear UNIQUE (ExamId,AcademicYearId),
    CONSTRAINT CK_Exams_Dates CHECK (StartDate IS NULL OR EndDate IS NULL OR EndDate >= StartDate),
    CONSTRAINT CK_Exams_Status CHECK (Status IN (N'Draft',N'Open',N'Closed'))
);

CREATE TABLE dbo.Grades (
    GradeId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Grades PRIMARY KEY,
    EnrollmentId INT NOT NULL,
    ClassSubjectId INT NOT NULL,
    ExamId INT NOT NULL,
    AcademicYearId INT NOT NULL,
    Score DECIMAL(6,2) NOT NULL,
    MaxScore DECIMAL(6,2) NOT NULL CONSTRAINT DF_Grades_MaxScore DEFAULT (100),
    Remark NVARCHAR(255) NULL,
    RecordedBy INT NULL,
    CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Grades_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(3) NULL,
    CONSTRAINT FK_Grades_Enrollments FOREIGN KEY (EnrollmentId) REFERENCES dbo.Enrollments(EnrollmentId),
    CONSTRAINT FK_Grades_ClassSubjectsYear FOREIGN KEY (ClassSubjectId,AcademicYearId)
        REFERENCES dbo.ClassSubjects(ClassSubjectId,AcademicYearId),
    CONSTRAINT FK_Grades_ExamsYear FOREIGN KEY (ExamId,AcademicYearId)
        REFERENCES dbo.Exams(ExamId,AcademicYearId),
    CONSTRAINT FK_Grades_Users FOREIGN KEY (RecordedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT UQ_Grades_OneScore UNIQUE (EnrollmentId,ClassSubjectId,ExamId),
    CONSTRAINT CK_Grades_Scores CHECK (Score >= 0 AND MaxScore > 0 AND Score <= MaxScore)
);

-- 6. Finance: amount due in Fees, actual transactions in FeePayments
CREATE TABLE dbo.FeeCategories (
    FeeCategoryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FeeCategories PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL CONSTRAINT UQ_FeeCategories_Name UNIQUE,
    Description NVARCHAR(500) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_FeeCategories_Active DEFAULT (1)
);

CREATE TABLE dbo.Fees (
    FeeId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Fees PRIMARY KEY,
    EnrollmentId INT NOT NULL,
    FeeCategoryId INT NOT NULL,
    Description NVARCHAR(255) NULL,
    Amount DECIMAL(18,2) NOT NULL,
    CurrencyCode CHAR(3) NOT NULL CONSTRAINT DF_Fees_Currency DEFAULT ('USD'),
    DueDate DATE NOT NULL,
    CreatedBy INT NULL,
    CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Fees_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Fees_Enrollments FOREIGN KEY (EnrollmentId) REFERENCES dbo.Enrollments(EnrollmentId),
    CONSTRAINT FK_Fees_Categories FOREIGN KEY (FeeCategoryId) REFERENCES dbo.FeeCategories(FeeCategoryId),
    CONSTRAINT FK_Fees_Users FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_Fees_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Fees_Currency CHECK (CurrencyCode IN ('USD','KHR'))
);

CREATE TABLE dbo.FeePayments (
    PaymentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FeePayments PRIMARY KEY,
    FeeId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaymentDate DATETIME2(3) NOT NULL CONSTRAINT DF_FeePayments_PaymentDate DEFAULT SYSUTCDATETIME(),
    PaymentMethod NVARCHAR(30) NOT NULL,
    ReferenceNo NVARCHAR(100) NULL,
    Note NVARCHAR(500) NULL,
    ReceivedBy INT NOT NULL,
    CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_FeePayments_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_FeePayments_Fees FOREIGN KEY (FeeId) REFERENCES dbo.Fees(FeeId),
    CONSTRAINT FK_FeePayments_Users FOREIGN KEY (ReceivedBy) REFERENCES dbo.Users(UserId),
    CONSTRAINT CK_FeePayments_Amount CHECK (Amount > 0),
    CONSTRAINT CK_FeePayments_Method CHECK (PaymentMethod IN (N'Cash',N'BankTransfer',N'Card',N'Other'))
);
CREATE UNIQUE INDEX UX_FeePayments_ReferenceNo ON dbo.FeePayments(ReferenceNo) WHERE ReferenceNo IS NOT NULL;

-- 7. Audit log (append only at application level; restrict database permissions)
CREATE TABLE dbo.AuditLogs (
    AuditLogId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
    UserId INT NULL,
    Action NVARCHAR(100) NOT NULL,
    EntityName NVARCHAR(100) NOT NULL,
    EntityId NVARCHAR(100) NULL,
    Details NVARCHAR(MAX) NULL, -- never store passwords or sensitive secrets
    OccurredAt DATETIME2(3) NOT NULL CONSTRAINT DF_AuditLogs_OccurredAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
);

-- 8. Supporting indexes (FK indexes are not created automatically by SQL Server)
CREATE INDEX IX_Teachers_Department ON dbo.Teachers(DepartmentId);
CREATE INDEX IX_Classes_Room ON dbo.Classes(RoomId);
CREATE INDEX IX_Classes_Teacher ON dbo.Classes(HomeroomTeacherId);
CREATE INDEX IX_StudentGuardians_Guardian ON dbo.StudentGuardians(GuardianId);
CREATE INDEX IX_Enrollments_ClassYear ON dbo.Enrollments(ClassId,AcademicYearId);
CREATE INDEX IX_ClassSubjects_Teacher ON dbo.ClassSubjects(TeacherId);
CREATE INDEX IX_ClassSubjects_Subject ON dbo.ClassSubjects(SubjectId);
CREATE INDEX IX_Timetables_RoomDay ON dbo.Timetables(RoomId,DayOfWeek,StartTime);
CREATE INDEX IX_Attendance_Date ON dbo.Attendance(AttendanceDate);
CREATE INDEX IX_Grades_ExamYear ON dbo.Grades(ExamId,AcademicYearId);
CREATE INDEX IX_Fees_Enrollment ON dbo.Fees(EnrollmentId);
CREATE INDEX IX_Fees_DueDate ON dbo.Fees(DueDate);
CREATE INDEX IX_FeePayments_Fee ON dbo.FeePayments(FeeId);
CREATE INDEX IX_AuditLogs_UserTime ON dbo.AuditLogs(UserId,OccurredAt DESC);

-- 9. Role seed data (no user accounts / passwords seeded)
INSERT dbo.Roles(RoleName,Description) VALUES
 (N'Administrator',N'Full system access'),
 (N'Teacher',N'Teaching and academic records'),
 (N'Registrar',N'Student registration and enrollment'),
 (N'Cashier',N'Fees and payments'),
 (N'Viewer',N'Read-only access');

INSERT dbo.Permissions(PermissionKey,PermissionName,ModuleName) VALUES
 (N'dashboard.view',N'View dashboard',N'Dashboard'),
 (N'students.view',N'View students',N'Students'),
 (N'students.create',N'Create students',N'Students'),
 (N'students.edit',N'Edit students',N'Students'),
 (N'students.deactivate',N'Deactivate students',N'Students'),
 (N'guardians.manage',N'Manage guardians',N'Students'),
 (N'teachers.view',N'View teachers',N'Teachers'),
 (N'teachers.manage',N'Manage teachers',N'Teachers'),
 (N'classes.view',N'View classes',N'Classes'),
 (N'classes.manage',N'Manage classes',N'Classes'),
 (N'enrollments.manage',N'Manage enrollments',N'Enrollments'),
 (N'attendance.view',N'View attendance',N'Attendance'),
 (N'attendance.manage',N'Manage attendance',N'Attendance'),
 (N'academics.view',N'View academic records',N'Academics'),
 (N'academics.manage',N'Manage academic records',N'Academics'),
 (N'grades.view',N'View grades',N'Grades'),
 (N'grades.manage',N'Manage grades',N'Grades'),
 (N'fees.view',N'View fees',N'Finance'),
 (N'fees.manage',N'Manage fees',N'Finance'),
 (N'payments.receive',N'Record payments',N'Finance'),
 (N'reports.view',N'View reports',N'Reports'),
 (N'users.manage',N'Manage users',N'Administration'),
 (N'roles.manage',N'Manage roles',N'Administration'),
 (N'audit.view',N'View audit log',N'Administration');

INSERT dbo.RolePermissions(RoleId,PermissionId)
SELECT r.RoleId,p.PermissionId
FROM dbo.Roles AS r CROSS JOIN dbo.Permissions AS p
WHERE r.RoleName = N'Administrator';

COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-- 10. Read-only fee summary; due/paid/balance cannot silently get out of sync
CREATE VIEW dbo.vwFeeBalances
AS
SELECT f.FeeId, f.EnrollmentId, f.FeeCategoryId, f.Amount, f.CurrencyCode,
       f.DueDate,
       CAST(COALESCE(SUM(p.Amount),0) AS DECIMAL(18,2)) AS PaidAmount,
       CAST(f.Amount - COALESCE(SUM(p.Amount),0) AS DECIMAL(18,2)) AS BalanceAmount,
       CASE WHEN COALESCE(SUM(p.Amount),0) >= f.Amount THEN N'Paid'
            WHEN COALESCE(SUM(p.Amount),0) > 0 THEN N'Partial'
            WHEN f.DueDate < CONVERT(date,GETDATE()) THEN N'Overdue'
            ELSE N'Pending' END AS PaymentStatus
FROM dbo.Fees f LEFT JOIN dbo.FeePayments p ON p.FeeId = f.FeeId
GROUP BY f.FeeId,f.EnrollmentId,f.FeeCategoryId,f.Amount,f.CurrencyCode,f.DueDate;
GO

PRINT N'SchoolManagementDB successfully created. Create initial administrator via secure C# setup flow.';
GO
