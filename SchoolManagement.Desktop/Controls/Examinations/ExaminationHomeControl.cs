
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SchoolManagement.Application.Academics.Interfaces;
using SchoolManagement.Application.Examinations.DTOs;
using SchoolManagement.Application.Examinations.Interfaces;

namespace SchoolManagement.Desktop.Controls.Examinations;

public sealed class ExaminationHomeControl : UserControl
{
    // =========================================
    // Application services
    // =========================================

    private readonly IExamService _examService;
    private readonly IGradeService _gradeService;
    private readonly IAcademicYearService _yearService;

    // =========================================
    // Main controls
    // =========================================

    private readonly TabControl tabs = new();

    private readonly DataGridView examGrid = new();
    private readonly DataGridView gradeGrid = new();

    private readonly TextBox txtExamSearch = new();
    private readonly TextBox txtGradeSearch = new();

    // Examination buttons
    private readonly Button btnAddExam = new();
    private readonly Button btnEditExam = new();
    private readonly Button btnStatus = new();
    private readonly Button btnRefreshExams = new();

    // Grade buttons
    private readonly Button btnEnterGrade = new();
    private readonly Button btnRefreshGrades = new();

    // Summary labels
    private readonly Label lblExamCount = new();
    private readonly Label lblGradeCount = new();

    // Search debounce
    private readonly System.Windows.Forms.Timer examSearchTimer = new()
    {
        Interval = 350
    };

    private readonly System.Windows.Forms.Timer gradeSearchTimer = new()
    {
        Interval = 350
    };

    private int _examLoadVersion;
    private int _gradeLoadVersion;

    private bool _examActionBusy;

    // =========================================
    // Constructor
    // =========================================

    public ExaminationHomeControl(
        IExamService examService,
        IGradeService gradeService,
        IAcademicYearService yearService)
    {
        _examService = examService;
        _gradeService = gradeService;
        _yearService = yearService;

        Dock = DockStyle.Fill;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.White;

        BuildUi();

        ConfigureEvents();

        Load += async (_, _) =>
        {
            await LoadExamsAsync();
            await LoadGradesAsync();
        };
    }

    // =========================================
    // Build main UI
    // =========================================

    private void BuildUi()
    {
        tabs.Dock = DockStyle.Fill;

        // Tab 1: Examinations
        var examTab = new TabPage("Examinations")
        {
            BackColor = Color.White
        };

        // Tab 2: Student Grades
        var gradeTab = new TabPage("Student Grades")
        {
            BackColor = Color.White
        };

        // Tab 3: Bulk Grade Entry
        var bulkTab = new TabPage("Bulk Grade Entry")
        {
            BackColor = Color.White
        };

        examTab.Controls.Add(BuildExamPage());

        gradeTab.Controls.Add(BuildGradePage());

        // Uses the bulk grade control implemented
        // in the previous step.
        bulkTab.Controls.Add(
            new BulkGradeEntryControl(_gradeService));

        tabs.TabPages.Add(examTab);
        tabs.TabPages.Add(gradeTab);
        tabs.TabPages.Add(bulkTab);

        Controls.Add(tabs);
    }

    // =========================================
    // Configure events
    // =========================================

    private void ConfigureEvents()
    {
        // Examination search debounce
        examSearchTimer.Tick += async (_, _) =>
        {
            examSearchTimer.Stop();
            await LoadExamsAsync();
        };

        txtExamSearch.TextChanged += (_, _) =>
        {
            ++_examLoadVersion;

            examSearchTimer.Stop();
            examSearchTimer.Start();
        };

        // Grade search debounce
        gradeSearchTimer.Tick += async (_, _) =>
        {
            gradeSearchTimer.Stop();
            await LoadGradesAsync();
        };

        txtGradeSearch.TextChanged += (_, _) =>
        {
            ++_gradeLoadVersion;

            gradeSearchTimer.Stop();
            gradeSearchTimer.Start();
        };

        // Refresh grade history when returning
        // from bulk grade entry.
        tabs.SelectedIndexChanged += async (_, _) =>
        {
            if (tabs.SelectedIndex == 1)
            {
                await LoadGradesAsync();
            }
        };
    }

    // =========================================
    // Tab 1: Examinations
    // =========================================

    private Control BuildExamPage()
    {
        var root = CreateRoot();
        var toolbar = CreateToolbar();

        txtExamSearch.Width = 230;
        txtExamSearch.PlaceholderText =
            "Search examinations...";

        StyleButton(
            btnAddExam,
            "Add Exam",
            105);

        StyleButton(
            btnEditExam,
            "Edit",
            85);

        StyleButton(
            btnStatus,
            "Open Exam",
            115);

        StyleButton(
            btnRefreshExams,
            "Refresh",
            95);

        toolbar.Controls.AddRange(new Control[]
        {
            txtExamSearch,
            btnAddExam,
            btnEditExam,
            btnStatus,
            btnRefreshExams
        });

        ConfigureGrid(examGrid);

        AddColumn(
            examGrid,
            "ExamName",
            "Examination");

        AddColumn(
            examGrid,
            "ExamType",
            "Exam Type");

        AddColumn(
            examGrid,
            "AcademicYearName",
            "Academic Year");

        AddColumn(
            examGrid,
            "StartDate",
            "Start Date");

        AddColumn(
            examGrid,
            "EndDate",
            "End Date");

        AddColumn(
            examGrid,
            "Status",
            "Status");

        lblExamCount.Dock = DockStyle.Fill;
        lblExamCount.ForeColor = Color.DimGray;
        lblExamCount.TextAlign =
            ContentAlignment.MiddleLeft;

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(examGrid, 0, 1);
        root.Controls.Add(lblExamCount, 0, 2);

        // Add examination
        btnAddExam.Click += async (_, _) =>
        {
            if (_examActionBusy)
                return;

            await OpenExamFormAsync(null);
        };

        // Edit examination
        btnEditExam.Click += async (_, _) =>
        {
            if (_examActionBusy)
                return;

            var exam = SelectedExam();

            if (exam is not null &&
                exam.Status == "Draft")
            {
                await OpenExamFormAsync(exam.ExamId);
            }
        };

        // Open or close examination
        btnStatus.Click += async (_, _) =>
        {
            if (_examActionBusy)
                return;

            await ChangeExamStatusAsync();
        };

        // Refresh list
        btnRefreshExams.Click += async (_, _) =>
        {
            await LoadExamsAsync();
        };

        examGrid.SelectionChanged += (_, _) =>
        {
            UpdateExamButtons();
        };

        UpdateExamButtons();

        return root;
    }

    // =========================================
    // Tab 2: Student Grades
    // =========================================

    private Control BuildGradePage()
    {
        var root = CreateRoot();
        var toolbar = CreateToolbar();

        txtGradeSearch.Width = 260;
        txtGradeSearch.PlaceholderText =
            "Search student, subject or exam...";

        StyleButton(
            btnEnterGrade,
            "Record Grade",
            130);

        StyleButton(
            btnRefreshGrades,
            "Refresh",
            95);

        toolbar.Controls.AddRange(new Control[]
        {
            txtGradeSearch,
            btnEnterGrade,
            btnRefreshGrades
        });

        ConfigureGrid(gradeGrid);

        AddColumn(
            gradeGrid,
            "StudentName",
            "Student");

        AddColumn(
            gradeGrid,
            "ClassName",
            "Class");

        AddColumn(
            gradeGrid,
            "SubjectName",
            "Subject");

        AddColumn(
            gradeGrid,
            "ExamName",
            "Examination");

        AddColumn(
            gradeGrid,
            "Score",
            "Score");

        AddColumn(
            gradeGrid,
            "MaxScore",
            "Maximum");

        AddColumn(
            gradeGrid,
            "Percentage",
            "Percentage");

        AddColumn(
            gradeGrid,
            "Remark",
            "Remark");

        lblGradeCount.Dock = DockStyle.Fill;
        lblGradeCount.ForeColor = Color.DimGray;
        lblGradeCount.TextAlign =
            ContentAlignment.MiddleLeft;

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(gradeGrid, 0, 1);
        root.Controls.Add(lblGradeCount, 0, 2);

        // Record individual grade
        btnEnterGrade.Click += async (_, _) =>
        {
            using var form =
                new GradeEntryForm(_gradeService);

            if (form.ShowDialog(this) ==
                DialogResult.OK)
            {
                await LoadGradesAsync();
            }
        };

        // Refresh grade history
        btnRefreshGrades.Click += async (_, _) =>
        {
            await LoadGradesAsync();
        };

        return root;
    }

    // =========================================
    // Shared layout helper
    // =========================================

    private static TableLayoutPanel CreateRoot()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12),
            BackColor = Color.FromArgb(
                248, 250, 252)
        };

        root.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                50));

        root.RowStyles.Add(
            new RowStyle(
                SizeType.Percent,
                100));

        root.RowStyles.Add(
            new RowStyle(
                SizeType.Absolute,
                30));

        return root;
    }

    private static FlowLayoutPanel CreateToolbar()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = true
        };
    }

    // =========================================
    // DataGridView helpers
    // =========================================

    private static void ConfigureGrid(
        DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;

        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;

        grid.AutoGenerateColumns = false;

        grid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;

        grid.ReadOnly = true;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;

        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;

        grid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;

        grid.ColumnHeadersHeight = 38;
        grid.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        grid.RowTemplate.Height = 36;

        grid.EnableHeadersVisualStyles = false;

        grid.ColumnHeadersDefaultCellStyle.BackColor =
            Color.FromArgb(241, 245, 249);

        grid.ColumnHeadersDefaultCellStyle.ForeColor =
            Color.FromArgb(30, 41, 59);

        grid.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(219, 234, 254);

        grid.DefaultCellStyle.SelectionForeColor =
            Color.FromArgb(30, 41, 59);
    }

    private static void AddColumn(
        DataGridView grid,
        string property,
        string header)
    {
        grid.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                Name = property,
                DataPropertyName = property,
                HeaderText = header
            });
    }

    // =========================================
    // Button style
    // =========================================

    private static void StyleButton(
        Button button,
        string text,
        int width)
    {
        button.Text = text;

        button.Size = new Size(width, 32);

        button.BackColor =
            Color.FromArgb(37, 99, 235);

        button.ForeColor = Color.White;

        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;

        button.UseVisualStyleBackColor = false;
        button.Cursor = Cursors.Hand;

        button.Margin =
            new Padding(4, 3, 4, 3);
    }

    // =========================================
    // Selected examination
    // =========================================

    private ExamDto? SelectedExam()
    {
        return examGrid.CurrentRow?.DataBoundItem
            as ExamDto;
    }

    // =========================================
    // Update examination buttons
    // =========================================

    private void UpdateExamButtons()
    {
        var exam = SelectedExam();

        btnAddExam.Enabled = !_examActionBusy;

        btnEditExam.Enabled =
            !_examActionBusy &&
            exam?.Status == "Draft";

        bool canAdvance =
            exam?.Status is "Draft" or "Open";

        btnStatus.Enabled =
            !_examActionBusy && canAdvance;

        btnStatus.Text = exam?.Status switch
        {
            "Draft" => "Open Exam",
            "Open" => "Close Exam",
            "Closed" => "Closed",
            _ => "Change Status"
        };
    }

    // =========================================
    // Load examinations
    // =========================================

    private async Task LoadExamsAsync()
    {
        int version = ++_examLoadVersion;

        try
        {
            string search =
                txtExamSearch.Text.Trim();

            var exams =
                await _examService.GetAllAsync(search);

            if (IsDisposed ||
                version != _examLoadVersion)
            {
                return;
            }

            examGrid.DataSource = exams.ToList();

            lblExamCount.Text =
                $"Displayed examinations: {exams.Count}";

            UpdateExamButtons();
        }
        catch (Exception ex)
        {
            if (!IsDisposed &&
                version == _examLoadVersion)
            {
                MessageBox.Show(
                    this,
                    ex.Message,
                    "Examination Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }

    // =========================================
    // Load grade history
    // =========================================

    private async Task LoadGradesAsync()
    {
        int version = ++_gradeLoadVersion;

        try
        {
            string search =
                txtGradeSearch.Text.Trim();

            var grades =
                await _gradeService.GetAllAsync(search);

            if (IsDisposed ||
                version != _gradeLoadVersion)
            {
                return;
            }

            gradeGrid.DataSource = grades.ToList();

            lblGradeCount.Text =
                $"Displayed grades: {grades.Count} " +
                "(maximum 500)";
        }
        catch (Exception ex)
        {
            if (!IsDisposed &&
                version == _gradeLoadVersion)
            {
                MessageBox.Show(
                    this,
                    ex.Message,
                    "Grade Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }

    // =========================================
    // Add / Edit examination
    // =========================================

    private async Task OpenExamFormAsync(
        int? examId)
    {
        if (_examActionBusy)
            return;

        _examActionBusy = true;
        UpdateExamButtons();

        try
        {
            using var form = new ExamEditForm(
                _examService,
                _yearService,
                examId);

            if (form.ShowDialog(this) ==
                DialogResult.OK)
            {
                await LoadExamsAsync();
            }
        }
        finally
        {
            _examActionBusy = false;

            if (!IsDisposed)
                UpdateExamButtons();
        }
    }

    // =========================================
    // Change examination status
    // =========================================

    private async Task ChangeExamStatusAsync()
    {
        if (_examActionBusy)
            return;

        var exam = SelectedExam();

        if (exam is null)
            return;

        string nextStatus = exam.Status switch
        {
            "Draft" => "Open",
            "Open" => "Closed",
            _ => ""
        };

        if (string.IsNullOrEmpty(nextStatus))
            return;

        var confirm = MessageBox.Show(
            this,
            $"Change '{exam.ExamName}' from " +
            $"{exam.Status} to {nextStatus}?",
            "Confirm Examination Status",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        _examActionBusy = true;
        UpdateExamButtons();

        try
        {
            await _examService.SetStatusAsync(
                exam.ExamId,
                nextStatus);

            await LoadExamsAsync();

            MessageBox.Show(
                this,
                $"Examination changed to {nextStatus}.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Status Update Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _examActionBusy = false;

            if (!IsDisposed)
                UpdateExamButtons();
        }
    }

    // =========================================
    // Cleanup
    // =========================================

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            examSearchTimer.Stop();
            examSearchTimer.Dispose();

            gradeSearchTimer.Stop();
            gradeSearchTimer.Dispose();
        }

        base.Dispose(disposing);
    }
}
