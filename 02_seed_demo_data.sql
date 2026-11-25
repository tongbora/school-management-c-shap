/*
 SchoolManagementDB demonstration data, SQL Server 2022.
 RUN ONLY IN A DEVELOPMENT/TEST DATABASE. DO NOT RUN IN PRODUCTION.
 Non-destructive and rerunnable: records identified by DEMO prefixes or codes.
 Does not create users or passwords. Sample payments require an existing active
 Administrator user; otherwise payments are skipped.
 Execute the ENTIRE SCRIPT as a single batch (no GO inside the transaction).
*/
USE SchoolManagementDB;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Students',N'U') IS NULL OR
       OBJECT_ID(N'dbo.Fees',N'U') IS NULL OR
       OBJECT_ID(N'dbo.Grades',N'U') IS NULL OR
       OBJECT_ID(N'dbo.Timetables',N'U') IS NULL
        THROW 51001, 'Run 01_create_database.sql on a fresh database first.', 1;

    -- Academic reference data. Never replace existing dates or active-year choice.
    INSERT dbo.AcademicYears(YearName,StartDate,EndDate,IsActive)
    SELECT v.n,v.s,v.e,0 FROM (VALUES
      (N'2025-2026',CONVERT(date,'20250901'),CONVERT(date,'20260831')),
      (N'2026-2027',CONVERT(date,'20260901'),CONVERT(date,'20270831')),
      (N'2027-2028',CONVERT(date,'20270901'),CONVERT(date,'20280831'))
    )v(n,s,e) WHERE NOT EXISTS(SELECT 1 FROM dbo.AcademicYears a WHERE a.YearName=v.n);

    DECLARE @YearId int, @StartDate date, @EndDate date;
    SELECT @YearId=AcademicYearId,@StartDate=StartDate,@EndDate=EndDate
      FROM dbo.AcademicYears WHERE YearName=N'2026-2027';
    IF @YearId IS NULL THROW 51002, 'Academic year 2026-2027 was not found.',1;
    DECLARE @EnrollDate date=DATEADD(day,10,@StartDate),
            @AttendDate date=DATEADD(day,20,@StartDate),
            @ExamDate date=DATEADD(day,40,@StartDate),
            @DueDate date=DATEADD(day,60,@StartDate);
    IF DATEADD(day,92,@ExamDate)>@EndDate THROW 51003,'Academic year date range too short.',1;

    INSERT dbo.Departments(DepartmentName,Description,IsActive)
    SELECT v.n,v.d,1 FROM (VALUES
      (N'DEMO - Mathematics',N'Demonstration mathematics department'),
      (N'DEMO - English',N'Demonstration English department'),
      (N'DEMO - Science',N'Demonstration science department'),
      (N'DEMO - Administration',N'Demonstration administration department')
    )v(n,d) WHERE NOT EXISTS(SELECT 1 FROM dbo.Departments d WHERE d.DepartmentName=v.n);

    INSERT dbo.Rooms(RoomCode,RoomName,Building,FloorNumber,Capacity,IsActive)
    SELECT v.c,v.n,N'DEMO Building',1,30,1 FROM (VALUES
      (N'DEMO-R101',N'DEMO Room 101'),(N'DEMO-R102',N'DEMO Room 102')
    )v(c,n) WHERE NOT EXISTS(SELECT 1 FROM dbo.Rooms r WHERE r.RoomCode=v.c);

    INSERT dbo.Subjects(SubjectCode,SubjectName,GradeLevel,IsActive)
    SELECT v.c,v.n,N'Grade 10',1 FROM (VALUES
      (N'DEMO-MATH',N'Demo Mathematics'),
      (N'DEMO-ENG',N'Demo English'),
      (N'DEMO-SCI',N'Demo Science'),
      (N'DEMO-KH',N'Demo Khmer Literature')
    )v(c,n) WHERE NOT EXISTS(SELECT 1 FROM dbo.Subjects s WHERE s.SubjectCode=v.c);

    INSERT dbo.Teachers(TeacherCode,FullName,Gender,Phone,SubjectSpecialty,DepartmentId,HireDate,Status)
    SELECT v.c,v.n,v.g,v.p,v.specialty,d.DepartmentId,CONVERT(date,'20250110'),N'Active'
    FROM (VALUES
      (N'DEMO-T001',N'Demo Teacher Sokha',N'Male',N'010000101',N'Mathematics',N'DEMO - Mathematics'),
      (N'DEMO-T002',N'Demo Teacher Sreymom',N'Female',N'010000102',N'English',N'DEMO - English'),
      (N'DEMO-T003',N'Demo Teacher Dara',N'Male',N'010000103',N'Science',N'DEMO - Science')
    )v(c,n,g,p,specialty,dept)
    JOIN dbo.Departments d ON d.DepartmentName=v.dept
    WHERE NOT EXISTS(SELECT 1 FROM dbo.Teachers t WHERE t.TeacherCode=v.c);

    INSERT dbo.Students(StudentCode,FullName,Gender,DateOfBirth,Phone,Address,Status)
    SELECT v.c,v.n,v.g,v.d,v.p,N'Demo address, Phnom Penh',N'Active'
    FROM (VALUES
      (N'DEMO-S001',N'Demo Student Chan Dara',N'Male',CONVERT(date,'20100215'),N'011000101'),
      (N'DEMO-S002',N'Demo Student Sok Sreypov',N'Female',CONVERT(date,'20100520'),N'011000102'),
      (N'DEMO-S003',N'Demo Student Lim Vanna',N'Male',CONVERT(date,'20100810'),N'011000103'),
      (N'DEMO-S004',N'Demo Student Kim Sreyneang',N'Female',CONVERT(date,'20101103'),N'011000104'),
      (N'DEMO-S005',N'Demo Student Heng Visal',N'Male',CONVERT(date,'20100312'),N'011000105'),
      (N'DEMO-S006',N'Demo Student Touch Sopheak',N'Female',CONVERT(date,'20100725'),N'011000106')
    )v(c,n,g,d,p) WHERE NOT EXISTS(SELECT 1 FROM dbo.Students s WHERE s.StudentCode=v.c);

    INSERT dbo.Guardians(FullName,Gender,Phone,Occupation)
    SELECT v.n,v.g,v.p,v.o FROM (VALUES
      (N'DEMO Guardian Chan Sophal',N'Male',N'012000101',N'Business Owner'),
      (N'DEMO Guardian Sok Mony',N'Female',N'012000102',N'Teacher'),
      (N'DEMO Guardian Lim Sothy',N'Male',N'012000103',N'Employee')
    )v(n,g,p,o) WHERE NOT EXISTS(SELECT 1 FROM dbo.Guardians x WHERE x.Phone=v.p);

    INSERT dbo.StudentGuardians(StudentId,GuardianId,Relationship,IsPrimary)
    SELECT s.StudentId,g.GuardianId,v.rel,1
    FROM (VALUES
      (N'DEMO-S001',N'012000101',N'Father'),
      (N'DEMO-S002',N'012000102',N'Mother'),
      (N'DEMO-S003',N'012000103',N'Father')
    )v(student,guardianPhone,rel)
    JOIN dbo.Students s ON s.StudentCode=v.student
    JOIN dbo.Guardians g ON g.Phone=v.guardianPhone
    WHERE NOT EXISTS(SELECT 1 FROM dbo.StudentGuardians sg WHERE sg.StudentId=s.StudentId AND sg.GuardianId=g.GuardianId)
      AND NOT EXISTS(SELECT 1 FROM dbo.StudentGuardians sg WHERE sg.StudentId=s.StudentId AND sg.IsPrimary=1);

    INSERT dbo.Classes(ClassName,GradeLevel,AcademicYearId,RoomId,HomeroomTeacherId,Capacity,IsActive)
    SELECT v.n,N'Grade 10',@YearId,r.RoomId,t.TeacherId,30,1
    FROM (VALUES
      (N'DEMO Grade 10A',N'DEMO-R101',N'DEMO-T001'),
      (N'DEMO Grade 10B',N'DEMO-R102',N'DEMO-T002')
    )v(n,room,teacher)
    JOIN dbo.Rooms r ON r.RoomCode=v.room
    JOIN dbo.Teachers t ON t.TeacherCode=v.teacher
    WHERE NOT EXISTS(SELECT 1 FROM dbo.Classes c WHERE c.AcademicYearId=@YearId AND c.ClassName=v.n);

    INSERT dbo.ClassSubjects(ClassId,SubjectId,TeacherId,AcademicYearId)
    SELECT c.ClassId,s.SubjectId,t.TeacherId,@YearId
    FROM dbo.Classes c
    CROSS JOIN (VALUES
      (N'DEMO-MATH',N'DEMO-T001'),
      (N'DEMO-ENG',N'DEMO-T002'),
      (N'DEMO-SCI',N'DEMO-T003')
    )v(subjectCode,teacherCode)
    JOIN dbo.Subjects s ON s.SubjectCode=v.subjectCode
    JOIN dbo.Teachers t ON t.TeacherCode=v.teacherCode
    WHERE c.AcademicYearId=@YearId
      AND c.ClassName IN(N'DEMO Grade 10A',N'DEMO Grade 10B')
      AND NOT EXISTS(SELECT 1 FROM dbo.ClassSubjects cs
          WHERE cs.ClassId=c.ClassId AND cs.SubjectId=s.SubjectId AND cs.AcademicYearId=@YearId);

    INSERT dbo.Timetables(ClassSubjectId,RoomId,DayOfWeek,StartTime,EndTime)
    SELECT cs.ClassSubjectId,c.RoomId,v.daynum,v.startTime,v.endTime
    FROM dbo.ClassSubjects cs
    JOIN dbo.Classes c ON c.ClassId=cs.ClassId AND c.AcademicYearId=@YearId
    JOIN dbo.Subjects s ON s.SubjectId=cs.SubjectId
    JOIN (VALUES
      (N'DEMO-MATH',CONVERT(tinyint,1),CONVERT(time,'08:00'),CONVERT(time,'09:00')),
      (N'DEMO-ENG',CONVERT(tinyint,2),CONVERT(time,'09:00'),CONVERT(time,'10:00')),
      (N'DEMO-SCI',CONVERT(tinyint,3),CONVERT(time,'10:00'),CONVERT(time,'11:00'))
    )v(subjectCode,daynum,startTime,endTime) ON v.subjectCode=s.SubjectCode
    WHERE c.ClassName IN(N'DEMO Grade 10A',N'DEMO Grade 10B')
      AND NOT EXISTS(SELECT 1 FROM dbo.Timetables x
          WHERE x.ClassSubjectId=cs.ClassSubjectId AND x.DayOfWeek=v.daynum AND x.StartTime=v.startTime);

    INSERT dbo.Enrollments(StudentId,ClassId,AcademicYearId,EnrollmentDate,Status)
    SELECT s.StudentId,c.ClassId,@YearId,@EnrollDate,N'Active'
    FROM (VALUES
      (N'DEMO-S001',N'DEMO Grade 10A'),(N'DEMO-S002',N'DEMO Grade 10A'),
      (N'DEMO-S003',N'DEMO Grade 10A'),(N'DEMO-S004',N'DEMO Grade 10A'),
      (N'DEMO-S005',N'DEMO Grade 10B'),(N'DEMO-S006',N'DEMO Grade 10B')
    )v(student,className)
    JOIN dbo.Students s ON s.StudentCode=v.student
    JOIN dbo.Classes c ON c.ClassName=v.className AND c.AcademicYearId=@YearId
    WHERE NOT EXISTS(SELECT 1 FROM dbo.Enrollments e
        WHERE e.StudentId=s.StudentId AND e.ClassId=c.ClassId AND e.AcademicYearId=@YearId)
      AND NOT EXISTS(SELECT 1 FROM dbo.Enrollments e
        WHERE e.StudentId=s.StudentId AND e.AcademicYearId=@YearId AND e.Status=N'Active');

    INSERT dbo.Attendance(EnrollmentId,AttendanceDate,SessionName,Status,Remark)
    SELECT e.EnrollmentId,@AttendDate,N'FullDay',
      CASE s.StudentCode WHEN N'DEMO-S002' THEN N'Absent'
        WHEN N'DEMO-S003' THEN N'Late' WHEN N'DEMO-S004' THEN N'Excused'
        ELSE N'Present' END,N'DEMO attendance - test record'
    FROM dbo.Enrollments e JOIN dbo.Students s ON s.StudentId=e.StudentId
    WHERE s.StudentCode IN(N'DEMO-S001',N'DEMO-S002',N'DEMO-S003',N'DEMO-S004',N'DEMO-S005',N'DEMO-S006')
      AND e.AcademicYearId=@YearId AND e.Status=N'Active' AND e.EnrollmentDate<=@AttendDate
      AND NOT EXISTS(SELECT 1 FROM dbo.Attendance a WHERE a.EnrollmentId=e.EnrollmentId
          AND a.AttendanceDate=@AttendDate AND a.SessionName=N'FullDay');

    INSERT dbo.Exams(ExamName,ExamType,AcademicYearId,StartDate,EndDate,Status)
    SELECT v.n,v.t,@YearId,DATEADD(day,v.offsetDays,@ExamDate),DATEADD(day,v.offsetDays+2,@ExamDate),v.status
    FROM (VALUES
      (N'DEMO Midterm 2026-2027',N'Midterm',0,N'Open'),
      (N'DEMO Final 2026-2027',N'Final',90,N'Draft')
    )v(n,t,offsetDays,status)
    WHERE NOT EXISTS(SELECT 1 FROM dbo.Exams e WHERE e.ExamName=v.n AND e.AcademicYearId=@YearId);

    DECLARE @MidtermId int;
    SELECT TOP(1) @MidtermId=ExamId FROM dbo.Exams
      WHERE ExamName=N'DEMO Midterm 2026-2027' AND AcademicYearId=@YearId ORDER BY ExamId;

    INSERT dbo.Grades(EnrollmentId,ClassSubjectId,ExamId,AcademicYearId,Score,MaxScore,Remark)
    SELECT e.EnrollmentId,cs.ClassSubjectId,@MidtermId,@YearId,v.score,100,N'DEMO grade - test record'
    FROM (VALUES
      (N'DEMO-S001',CONVERT(decimal(6,2),85.00)),
      (N'DEMO-S002',CONVERT(decimal(6,2),92.00)),
      (N'DEMO-S003',CONVERT(decimal(6,2),75.50)),
      (N'DEMO-S004',CONVERT(decimal(6,2),88.00)),
      (N'DEMO-S005',CONVERT(decimal(6,2),80.00)),
      (N'DEMO-S006',CONVERT(decimal(6,2),95.00))
    )v(student,score)
    JOIN dbo.Students s ON s.StudentCode=v.student
    JOIN dbo.Enrollments e ON e.StudentId=s.StudentId AND e.AcademicYearId=@YearId AND e.Status=N'Active'
    JOIN dbo.ClassSubjects cs ON cs.ClassId=e.ClassId AND cs.AcademicYearId=@YearId
    JOIN dbo.Subjects sub ON sub.SubjectId=cs.SubjectId AND sub.SubjectCode=N'DEMO-MATH'
    WHERE @MidtermId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Grades g
       WHERE g.EnrollmentId=e.EnrollmentId AND g.ClassSubjectId=cs.ClassSubjectId AND g.ExamId=@MidtermId);

    INSERT dbo.FeeCategories(CategoryName,Description,IsActive)
    SELECT v.n,v.d,1 FROM (VALUES
      (N'DEMO Tuition',N'Demonstration tuition fee'),
      (N'DEMO Registration',N'Demonstration registration fee'),
      (N'DEMO Examination',N'Demonstration examination fee'),
      (N'DEMO Materials',N'Demonstration learning materials fee')
    )v(n,d) WHERE NOT EXISTS(SELECT 1 FROM dbo.FeeCategories c WHERE c.CategoryName=v.n);

    INSERT dbo.Fees(EnrollmentId,FeeCategoryId,Description,Amount,CurrencyCode,DueDate)
    SELECT e.EnrollmentId,fc.FeeCategoryId,v.descr,v.amount,v.ccy,@DueDate
    FROM (VALUES
      (N'DEMO-S001',N'DEMO Tuition',N'DEMO Tuition S001',CONVERT(decimal(18,2),150.00),'USD'),
      (N'DEMO-S002',N'DEMO Tuition',N'DEMO Tuition S002',CONVERT(decimal(18,2),150.00),'USD'),
      (N'DEMO-S003',N'DEMO Registration',N'DEMO Registration S003',CONVERT(decimal(18,2),30.00),'USD'),
      (N'DEMO-S004',N'DEMO Examination',N'DEMO Examination S004',CONVERT(decimal(18,2),20.00),'USD'),
      (N'DEMO-S005',N'DEMO Tuition',N'DEMO Tuition S005 KHR',CONVERT(decimal(18,2),600000.00),'KHR'),
      (N'DEMO-S006',N'DEMO Materials',N'DEMO Materials S006 KHR',CONVERT(decimal(18,2),80000.00),'KHR')
    )v(student,category,descr,amount,ccy)
    JOIN dbo.Students s ON s.StudentCode=v.student
    JOIN dbo.Enrollments e ON e.StudentId=s.StudentId AND e.AcademicYearId=@YearId AND e.Status=N'Active'
    JOIN dbo.FeeCategories fc ON fc.CategoryName=v.category
    WHERE NOT EXISTS(SELECT 1 FROM dbo.Fees f WHERE f.EnrollmentId=e.EnrollmentId
        AND f.FeeCategoryId=fc.FeeCategoryId AND f.Description=v.descr);

    -- Demo payments intentionally need an existing authorized user, no default credentials.
    DECLARE @Receiver int;
    SELECT TOP(1) @Receiver=u.UserId FROM dbo.Users u
    JOIN dbo.UserRoles ur ON ur.UserId=u.UserId
    JOIN dbo.Roles r ON r.RoleId=ur.RoleId
    WHERE u.IsActive=1 AND r.RoleName=N'Administrator' ORDER BY u.UserId;
    IF @Receiver IS NOT NULL
    BEGIN
        INSERT dbo.FeePayments(FeeId,Amount,PaymentMethod,ReferenceNo,Note,ReceivedBy)
        SELECT f.FeeId,v.amount,N'Cash',v.ref,N'DEMO TEST PAYMENT - NOT REAL MONEY',@Receiver
        FROM (VALUES
          (N'DEMO Tuition S001',N'DEMO-PAY-S001',CONVERT(decimal(18,2),100.00)),
          (N'DEMO Registration S003',N'DEMO-PAY-S003',CONVERT(decimal(18,2),30.00)),
          (N'DEMO Tuition S005 KHR',N'DEMO-PAY-S005',CONVERT(decimal(18,2),200000.00))
        )v(descr,ref,amount)
        JOIN dbo.Fees f ON f.Description=v.descr
        JOIN dbo.Enrollments e ON e.EnrollmentId=f.EnrollmentId AND e.AcademicYearId=@YearId
        WHERE NOT EXISTS(SELECT 1 FROM dbo.FeePayments p WHERE p.ReferenceNo=v.ref)
          AND COALESCE((SELECT SUM(p.Amount) FROM dbo.FeePayments p WHERE p.FeeId=f.FeeId),0)+v.amount<=f.Amount;
    END
    ELSE PRINT 'Demo payments skipped: create/login an active Administrator first.';

    COMMIT TRANSACTION;
    PRINT 'Demo seed completed. Reruns will not duplicate matched demo records.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

-- Read-only verification. Counts include any non-demo records already present.
SELECT 'AcademicYears' Module,COUNT(*) Total FROM dbo.AcademicYears
UNION ALL SELECT 'Departments',COUNT(*) FROM dbo.Departments
UNION ALL SELECT 'Rooms',COUNT(*) FROM dbo.Rooms
UNION ALL SELECT 'Subjects',COUNT(*) FROM dbo.Subjects
UNION ALL SELECT 'Teachers',COUNT(*) FROM dbo.Teachers
UNION ALL SELECT 'Students',COUNT(*) FROM dbo.Students
UNION ALL SELECT 'Guardians',COUNT(*) FROM dbo.Guardians
UNION ALL SELECT 'Classes',COUNT(*) FROM dbo.Classes
UNION ALL SELECT 'ClassSubjects',COUNT(*) FROM dbo.ClassSubjects
UNION ALL SELECT 'Enrollments',COUNT(*) FROM dbo.Enrollments
UNION ALL SELECT 'Attendance',COUNT(*) FROM dbo.Attendance
UNION ALL SELECT 'Exams',COUNT(*) FROM dbo.Exams
UNION ALL SELECT 'Grades',COUNT(*) FROM dbo.Grades
UNION ALL SELECT 'FeeCategories',COUNT(*) FROM dbo.FeeCategories
UNION ALL SELECT 'Fees',COUNT(*) FROM dbo.Fees
UNION ALL SELECT 'FeePayments',COUNT(*) FROM dbo.FeePayments;

SELECT b.FeeId,s.StudentCode,s.FullName,b.Amount,b.CurrencyCode,
       b.PaidAmount,b.BalanceAmount,b.PaymentStatus
FROM dbo.vwFeeBalances b
JOIN dbo.Enrollments e ON e.EnrollmentId=b.EnrollmentId
JOIN dbo.Students s ON s.StudentId=e.StudentId
WHERE s.StudentCode LIKE N'DEMO-S%'
ORDER BY s.StudentCode;
