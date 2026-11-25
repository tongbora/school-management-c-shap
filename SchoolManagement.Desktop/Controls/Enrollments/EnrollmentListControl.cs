
using System.Drawing;
using System.Windows.Forms;
using SchoolManagement.Application.Enrollments.DTOs;
using SchoolManagement.Application.Enrollments.Interfaces;

namespace SchoolManagement.Desktop.Controls.Enrollments;

public sealed class EnrollmentListControl : UserControl
{
    private readonly IEnrollmentService _service;

    private readonly DataGridView grid = new();
    private readonly TextBox txtSearch = new();
    private readonly Label lblCount = new();

    private readonly Button btnEnroll = new();
    private readonly Button btnTransfer = new();
    private readonly Button btnComplete = new();
    private readonly Button btnWithdraw = new();
    private readonly Button btnRefresh = new();

    private readonly System.Windows.Forms.Timer timer =
        new() { Interval = 300 };

    private int _loadVersion;
    private bool _busy;

    public EnrollmentListControl(IEnrollmentService service)
    {
        _service = service;

        Dock = DockStyle.Fill;
        BackColor = Color.White;

        BuildUi();

        timer.Tick += async (_, _) =>
        {
            timer.Stop();
            await LoadEnrollmentsAsync();
        };

        txtSearch.TextChanged += (_, _) =>
        {
            _loadVersion++;
            timer.Stop();
            timer.Start();
        };

        Load += async (_, _) =>
            await LoadEnrollmentsAsync();

        grid.SelectionChanged += (_, _) =>
            UpdateButtons();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(12),
            BackColor = Color.FromArgb(248, 250, 252)
        };

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 52));

        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 30));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = true
        };

        txtSearch.Width = 210;
        txtSearch.PlaceholderText = "Search student or class...";

        StyleButton(btnEnroll, "Enroll Student", 125);
        StyleButton(btnTransfer, "Transfer", 90);
        StyleButton(btnComplete, "Complete", 95);
        StyleButton(btnWithdraw, "Withdraw", 95);
        StyleButton(btnRefresh, "Refresh", 85);

        toolbar.Controls.AddRange(new Control[]
        {
            txtSearch, btnEnroll, btnTransfer,
            btnComplete, btnWithdraw, btnRefresh
        });

        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;
        grid.AutoGenerateColumns = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = Color.White;

        AddColumn("StudentCode", "Student Code");
        AddColumn("StudentName", "Student");
        AddColumn("AcademicYearName", "Academic Year");
        AddColumn("ClassName", "Class");
        AddColumn("EnrollmentDate", "Enrollment Date");
        AddColumn("Status", "Status");
        AddColumn("EndDate", "End Date");

        lblCount.Dock = DockStyle.Fill;
        lblCount.ForeColor = Color.DimGray;
        lblCount.TextAlign = ContentAlignment.MiddleLeft;

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(grid, 0, 1);
        root.Controls.Add(lblCount, 0, 2);
        Controls.Add(root);

        btnEnroll.Click += async (_, _) => await EnrollAsync();
        btnTransfer.Click += async (_, _) => await TransferAsync();
        btnComplete.Click += async (_, _) =>
            await CloseEnrollmentAsync("Completed");
        btnWithdraw.Click += async (_, _) =>
            await CloseEnrollmentAsync("Withdrawn");
        btnRefresh.Click += async (_, _) =>
            await LoadEnrollmentsAsync();

        UpdateButtons();
    }

    private static void StyleButton(
        Button button, string text, int width)
    {
        button.Text = text;
        button.Width = width;
        button.Height = 32;
        button.BackColor = Color.FromArgb(37, 99, 235);
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.UseVisualStyleBackColor = false;
        button.Cursor = Cursors.Hand;
    }

    private void AddColumn(
        string property, string heading)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = property,
            DataPropertyName = property,
            HeaderText = heading
        });
    }

    private EnrollmentDto? SelectedEnrollment()
    {
        return grid.CurrentRow?.DataBoundItem as EnrollmentDto;
    }

    private void UpdateButtons()
    {
        var selected = SelectedEnrollment();
        bool active = selected?.Status == "Active";

        btnEnroll.Enabled = !_busy;
        btnTransfer.Enabled = !_busy && active;
        btnComplete.Enabled = !_busy && active;
        btnWithdraw.Enabled = !_busy && active;
        btnRefresh.Enabled = !_busy;
    }

    private async Task LoadEnrollmentsAsync()
    {
        int version = ++_loadVersion;

        try
        {
            var data = await _service.GetAllAsync(
                txtSearch.Text.Trim());

            if (IsDisposed || version != _loadVersion)
                return;

            grid.DataSource = data.ToList();

            lblCount.Text =
                $"Total displayed enrollments: {data.Count}";

            UpdateButtons();
        }
        catch (Exception ex)
        {
            if (IsDisposed || version != _loadVersion)
                return;

            MessageBox.Show(
                this, ex.Message, "Load Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task EnrollAsync()
    {
        if (_busy) return;

        using var form = new EnrollmentEditForm(_service);

        if (form.ShowDialog(this) == DialogResult.OK)
            await LoadEnrollmentsAsync();
    }

    private async Task TransferAsync()
    {
        if (_busy) return;

        var selected = SelectedEnrollment();

        if (selected is null || selected.Status != "Active")
        {
            MessageBox.Show("Select an active enrollment.");
            return;
        }

        using var form = new TransferEnrollmentForm(
            _service, selected);

        if (form.ShowDialog(this) == DialogResult.OK)
            await LoadEnrollmentsAsync();
    }

    private async Task CloseEnrollmentAsync(string status)
    {
        if (_busy) return;

        var selected = SelectedEnrollment();

        if (selected is null || selected.Status != "Active")
        {
            MessageBox.Show("Select an active enrollment.");
            return;
        }

        string action = status == "Completed"
            ? "complete"
            : "withdraw";

        var result = MessageBox.Show(
            this,
            $"Are you sure you want to {action} " +
            $"{selected.StudentName}'s enrollment?",
            "Confirm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        _busy = true;
        UpdateButtons();

        try
        {
            DateOnly endDate =
                DateOnly.FromDateTime(DateTime.Today);

            await _service.SetStatusAsync(
                selected.EnrollmentId,
                status,
                endDate);

            await LoadEnrollmentsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this, ex.Message, "Update Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _busy = false;

            if (!IsDisposed)
                UpdateButtons();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Stop();
            timer.Dispose();
        }

        base.Dispose(disposing);
    }
}
