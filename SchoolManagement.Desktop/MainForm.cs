
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

// =========================================
// Application service interfaces
// =========================================

using SchoolManagement.Application.Academics.Interfaces;
using SchoolManagement.Application.Attendance.Interfaces;
using SchoolManagement.Application.Dashboard.Interfaces;
using SchoolManagement.Application.Enrollments.Interfaces;
using SchoolManagement.Application.Examinations.Interfaces;
using SchoolManagement.Application.Finance.Interfaces;
using SchoolManagement.Application.Students.Interfaces;
using SchoolManagement.Application.Teachers.Interfaces;

// =========================================
// Desktop controls
// =========================================

using SchoolManagement.Desktop.Controls.Academics;
using SchoolManagement.Desktop.Controls.Attendance;
using SchoolManagement.Desktop.Controls.Dashboard;
using SchoolManagement.Desktop.Controls.Enrollments;
using SchoolManagement.Desktop.Controls.Examinations;
using SchoolManagement.Desktop.Controls.Finance;
using SchoolManagement.Desktop.Controls.Students;
using SchoolManagement.Desktop.Controls.Teachers;

namespace SchoolManagement.Desktop;

public partial class MainForm : Form
{
    // =========================================
    // Application services
    // =========================================

    private readonly IDashboardService _dashboardService;
    private readonly IStudentService _studentService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly ITeacherService _teacherService;

    private readonly IAcademicYearService _academicYearService;
    private readonly IDepartmentService _departmentService;
    private readonly ISubjectService _subjectService;
    private readonly IRoomService _roomService;
    private readonly IClassService _classService;
    private readonly IClassSubjectService _classSubjectService;

    private readonly IAttendanceService _attendanceService;
    private readonly IExamService _examService;
    private readonly IGradeService _gradeService;
    private readonly IFinanceService _financeService;

    // =========================================
    // Main layout controls
    // =========================================

    private readonly Panel sidebar = new();
    private readonly Panel header = new();
    private readonly Panel contentPanel = new();

    private readonly Label pageTitle = new();

    // =========================================
    // Constructor
    // =========================================

    public MainForm(
        IDashboardService dashboardService,
        IStudentService studentService,
        IEnrollmentService enrollmentService,
        ITeacherService teacherService,
        IAcademicYearService academicYearService,
        IDepartmentService departmentService,
        ISubjectService subjectService,
        IRoomService roomService,
        IClassService classService,
        IClassSubjectService classSubjectService,
        IAttendanceService attendanceService,
        IExamService examService,
        IGradeService gradeService,
        IFinanceService financeService)
    {
        InitializeComponent();

        // Dashboard
        _dashboardService = dashboardService;

        // Students
        _studentService = studentService;

        // Enrollments
        _enrollmentService = enrollmentService;

        // Teachers
        _teacherService = teacherService;

        // Academics
        _academicYearService = academicYearService;
        _departmentService = departmentService;
        _subjectService = subjectService;
        _roomService = roomService;
        _classService = classService;
        _classSubjectService = classSubjectService;

        // Attendance
        _attendanceService = attendanceService;

        // Examinations
        _examService = examService;
        _gradeService = gradeService;

        // Finance
        _financeService = financeService;

        // =========================================
        // Main form configuration
        // =========================================

        Text = "School Management System";

        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;

        MinimumSize = new Size(1000, 650);

        BuildLayout();
        ShowDashboard();
    }

    // =========================================
    // Build application layout
    // =========================================

    private void BuildLayout()
    {
        SuspendLayout();

        Controls.Clear();

        // =========================================
        // Sidebar
        // =========================================

        sidebar.Dock = DockStyle.Left;
        sidebar.Width = 230;

        sidebar.BackColor =
            Color.FromArgb(24, 52, 86);

        // =========================================
        // Header
        // =========================================

        header.Dock = DockStyle.Top;
        header.Height = 70;
        header.BackColor = Color.White;

        pageTitle.Text = "Dashboard";

        pageTitle.Font = new Font(
            "Segoe UI",
            18,
            FontStyle.Bold);

        pageTitle.ForeColor =
            Color.FromArgb(30, 58, 95);

        pageTitle.Dock = DockStyle.Fill;

        pageTitle.Padding =
            new Padding(24, 15, 0, 0);

        header.Controls.Add(pageTitle);

        // =========================================
        // Content area
        // =========================================

        contentPanel.Dock = DockStyle.Fill;

        contentPanel.BackColor =
            Color.FromArgb(245, 247, 251);

        // =========================================
        // Sidebar menu
        // =========================================

        var menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,

            FlowDirection =
                FlowDirection.TopDown,

            WrapContents = false,
            AutoScroll = true,

            Padding = new Padding(
                10, 20, 10, 10)
        };

        var logo = new Label
        {
            Text = "SCHOOL SYSTEM",

            ForeColor = Color.White,

            Font = new Font(
                "Segoe UI",
                15,
                FontStyle.Bold),

            Width = 195,
            Height = 65,

            TextAlign =
                ContentAlignment.MiddleCenter
        };

        menu.Controls.Add(logo);

        // =========================================
        // Enabled modules
        // =========================================

        AddMenuButton(
            menu,
            "Dashboard",
            true,
            ShowDashboard);

        AddMenuButton(
            menu,
            "Students",
            true,
            ShowStudents);

        AddMenuButton(
            menu,
            "Enrollments",
            true,
            ShowEnrollments);

        AddMenuButton(
            menu,
            "Teachers",
            true,
            ShowTeachers);

        AddMenuButton(
            menu,
            "Academics",
            true,
            ShowAcademics);

        AddMenuButton(
            menu,
            "Attendance",
            true,
            ShowAttendance);

        AddMenuButton(
            menu,
            "Examinations",
            true,
            ShowExaminations);

        AddMenuButton(
            menu,
            "Finance",
            true,
            ShowFinance);

        // =========================================
        // Add sidebar
        // =========================================

        sidebar.Controls.Add(menu);

        // =========================================
        // Add layout panels
        // =========================================

        Controls.Add(contentPanel);
        Controls.Add(header);
        Controls.Add(sidebar);

        ResumeLayout(true);
    }

    // =========================================
    // Sidebar menu button helper
    // =========================================

    private static void AddMenuButton(
        FlowLayoutPanel menu,
        string title,
        bool enabled,
        Action? onClick)
    {
        var button = new Button
        {
            Text = title,

            Width = 195,
            Height = 45,

            Margin =
                new Padding(0, 4, 0, 4),

            FlatStyle = FlatStyle.Flat,

            TextAlign =
                ContentAlignment.MiddleLeft,

            Padding =
                new Padding(15, 0, 0, 0),

            BackColor =
                Color.FromArgb(30, 65, 105),

            ForeColor = Color.White,

            Cursor = enabled
                ? Cursors.Hand
                : Cursors.Default,

            Enabled = enabled
        };

        button.FlatAppearance.BorderSize = 0;

        if (onClick is not null)
        {
            button.Click += (_, _) =>
                onClick();
        }

        menu.Controls.Add(button);
    }

    // =========================================
    // Shared navigation helper
    // =========================================

    private void NavigateTo(
        string title,
        Control nextControl)
    {
        pageTitle.Text = title;

        contentPanel.SuspendLayout();

        try
        {
            // Remove and dispose previous module.
            foreach (Control oldControl in
                contentPanel.Controls
                    .Cast<Control>()
                    .ToArray())
            {
                contentPanel.Controls.Remove(
                    oldControl);

                oldControl.Dispose();
            }

            nextControl.Dock = DockStyle.Fill;

            contentPanel.Controls.Add(
                nextControl);
        }
        finally
        {
            contentPanel.ResumeLayout(true);
        }
    }

    // =========================================
    // Dashboard
    // =========================================

    private void ShowDashboard()
    {
        NavigateTo(
            "Dashboard",
            new DashboardControl(
                _dashboardService));
    }

    // =========================================
    // Student Management
    // =========================================

    private void ShowStudents()
    {
        NavigateTo(
            "Student Management",
            new StudentListControl(
                _studentService));
    }

    // =========================================
    // Enrollment Management
    // =========================================

    private void ShowEnrollments()
    {
        NavigateTo(
            "Student Enrollment Management",
            new EnrollmentListControl(
                _enrollmentService));
    }

    // =========================================
    // Teacher Management
    // =========================================

    private void ShowTeachers()
    {
        NavigateTo(
            "Teacher Management",
            new TeacherListControl(
                _teacherService));
    }

    // =========================================
    // Academics Management
    // =========================================

    private void ShowAcademics()
    {
        NavigateTo(
            "Academics Management",
            new AcademicsHomeControl(
                _academicYearService,
                _departmentService,
                _subjectService,
                _roomService,
                _classService,
                _classSubjectService));
    }

    // =========================================
    // Attendance Management
    // =========================================

    private void ShowAttendance()
    {
        NavigateTo(
            "Attendance Management",
            new AttendanceManagementControl(
                _attendanceService));
    }

    // =========================================
    // Examinations Management
    // =========================================

    private void ShowExaminations()
    {
        NavigateTo(
            "Examinations Management",
            new ExaminationHomeControl(
                _examService,
                _gradeService,
                _academicYearService));
    }

    // =========================================
    // Finance Management
    // =========================================

    private void ShowFinance()
    {
        NavigateTo(
            "Finance Management",
            new FinanceHomeControl(
                _financeService));
    }

    // =========================================
    // Designer event handler
    // =========================================

    private void MainForm_Load(
        object? sender,
        EventArgs e)
    {
    }
}
