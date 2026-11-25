
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

using SchoolManagement.Application.Academics.DTOs;
using SchoolManagement.Application.Academics.Interfaces;

namespace SchoolManagement.Desktop.Controls.Academics;

public sealed class AcademicYearEditForm : Form
{
    private readonly IAcademicYearService _service;
    private readonly int? _yearId;

    private readonly TextBox txtName = new();
    private readonly DateTimePicker dtStart = new();
    private readonly DateTimePicker dtEnd = new();

    private readonly Button btnSave = new();
    private readonly Button btnCancel = new();

    private bool _isBusy;

    public AcademicYearEditForm(
        IAcademicYearService service,
        int? yearId = null)
    {
        _service = service ??
            throw new ArgumentNullException(nameof(service));

        _yearId = yearId;

        Text = yearId.HasValue
            ? "Edit Academic Year"
            : "Add Academic Year";

        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(500, 330);
        MinimumSize = new Size(500, 330);

        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;

        BackColor = Color.FromArgb(249, 250, 251);
        Font = new Font("Segoe UI", 10F);

        BuildForm();

        Shown += async (_, _) =>
        {
            if (_yearId.HasValue)
            {
                await LoadYearAsync();
            }
        };
    }

    private void BuildForm()
    {
        SuspendLayout();

        // ==========================================
        // Main layout
        // ==========================================

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = BackColor
        };

        root.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 78));

        // ==========================================
        // Input fields
        // ==========================================

        var inputPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(24, 24, 24, 12),
            BackColor = BackColor
        };

        inputPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 145));

        inputPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        for (int i = 0; i < 3; i++)
        {
            inputPanel.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 58));
        }

        // Year name
        txtName.MaxLength = 50;
        txtName.BorderStyle = BorderStyle.FixedSingle;
        txtName.Font = new Font("Segoe UI", 10F);

        // Date controls
        dtStart.Format = DateTimePickerFormat.Short;
        dtEnd.Format = DateTimePickerFormat.Short;

        dtStart.Font = new Font("Segoe UI", 10F);
        dtEnd.Font = new Font("Segoe UI", 10F);

        int currentYear = DateTime.Today.Year;

        dtStart.Value = new DateTime(
            currentYear, 9, 1);

        dtEnd.Value = new DateTime(
            currentYear + 1, 8, 31);

        txtName.Text =
            $"{currentYear}-{currentYear + 1}";

        AddField(
            inputPanel,
            0,
            "Year Name *",
            txtName);

        AddField(
            inputPanel,
            1,
            "Start Date *",
            dtStart);

        AddField(
            inputPanel,
            2,
            "End Date *",
            dtEnd);

        // ==========================================
        // Footer
        // ==========================================

        var footer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(20, 12, 24, 12)
        };

        // Divider line at top of footer
        var separator = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = Color.FromArgb(229, 231, 235)
        };

        footer.Controls.Add(separator);

        // Buttons are positioned using a dedicated
        // right-aligned layout instead of table cells.
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 255,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = new Padding(0, 4, 0, 0)
        };

        // ==========================================
        // Save button
        // ==========================================

        btnSave.Text = "Save";
        btnSave.Size = new Size(110, 38);

        btnSave.BackColor =
            Color.FromArgb(37, 99, 235);

        btnSave.ForeColor = Color.White;
        btnSave.FlatStyle = FlatStyle.Flat;
        btnSave.FlatAppearance.BorderSize = 0;

        btnSave.Font = new Font(
            "Segoe UI", 10F, FontStyle.Bold);

        btnSave.Cursor = Cursors.Hand;
        btnSave.Margin = new Padding(8, 0, 0, 0);
        btnSave.UseVisualStyleBackColor = false;

        btnSave.Click += async (_, _) =>
            await SaveAsync();

        // ==========================================
        // Cancel button
        // ==========================================

        btnCancel.Text = "Cancel";
        btnCancel.Size = new Size(110, 38);

        btnCancel.BackColor = Color.White;

        btnCancel.ForeColor =
            Color.FromArgb(55, 65, 81);

        btnCancel.FlatStyle = FlatStyle.Flat;

        btnCancel.FlatAppearance.BorderColor =
            Color.FromArgb(209, 213, 219);

        btnCancel.FlatAppearance.BorderSize = 1;

        btnCancel.Font = new Font(
            "Segoe UI", 10F);

        btnCancel.Cursor = Cursors.Hand;
        btnCancel.Margin = new Padding(8, 0, 0, 0);
        btnCancel.UseVisualStyleBackColor = false;
        btnCancel.DialogResult = DialogResult.Cancel;

        // RightToLeft: first control is on the right.
        buttonPanel.Controls.Add(btnSave);
        buttonPanel.Controls.Add(btnCancel);

        footer.Controls.Add(buttonPanel);

        // ==========================================
        // Assemble layout
        // ==========================================

        root.Controls.Add(inputPanel, 0, 0);
        root.Controls.Add(footer, 0, 1);

        Controls.Add(root);

        AcceptButton = btnSave;
        CancelButton = btnCancel;

        ResumeLayout(true);
    }

    // ==========================================
    // Helper: input field
    // ==========================================

    private static void AddField(
        TableLayoutPanel panel,
        int row,
        string label,
        Control input)
    {
        var title = new Label
        {
            Text = label,
            AutoSize = true,
            ForeColor = Color.FromArgb(55, 65, 81),
            Font = new Font("Segoe UI", 10F),
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 8, 0)
        };

        input.Dock = DockStyle.Fill;

        input.Margin =
            new Padding(0, 10, 0, 10);

        panel.Controls.Add(title, 0, row);
        panel.Controls.Add(input, 1, row);
    }

    // ==========================================
    // Load academic year for editing
    // ==========================================

    private async Task LoadYearAsync()
    {
        SetBusy(true);

        try
        {
            var year = await _service.GetByIdAsync(
                _yearId!.Value);

            if (year is null)
            {
                MessageBox.Show(
                    this,
                    "Academic year not found.",
                    "Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                Close();
                return;
            }

            txtName.Text = year.YearName;

            dtStart.Value =
                year.StartDate.ToDateTime(
                    TimeOnly.MinValue);

            dtEnd.Value =
                year.EndDate.ToDateTime(
                    TimeOnly.MinValue);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Load Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            Close();
        }
        finally
        {
            SetBusy(false);
        }
    }

    // ==========================================
    // Save academic year
    // ==========================================

    private async Task SaveAsync()
    {
        if (_isBusy)
            return;

        string yearName = txtName.Text.Trim();

        if (string.IsNullOrWhiteSpace(yearName))
        {
            MessageBox.Show(
                this,
                "Academic year name is required.",
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            txtName.Focus();
            return;
        }

        DateOnly startDate =
            DateOnly.FromDateTime(dtStart.Value);

        DateOnly endDate =
            DateOnly.FromDateTime(dtEnd.Value);

        if (endDate < startDate)
        {
            MessageBox.Show(
                this,
                "End date cannot be earlier than start date.",
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            dtEnd.Focus();
            return;
        }

        var request = new SaveAcademicYearRequest(
            yearName,
            startDate,
            endDate);

        SetBusy(true);

        try
        {
            if (_yearId.HasValue)
            {
                await _service.UpdateAsync(
                    _yearId.Value,
                    request);
            }
            else
            {
                await _service.CreateAsync(request);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Save Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    // ==========================================
    // Loading state
    // ==========================================

    private void SetBusy(bool busy)
    {
        _isBusy = busy;

        btnSave.Enabled = !busy;
        btnCancel.Enabled = !busy;

        txtName.Enabled = !busy;
        dtStart.Enabled = !busy;
        dtEnd.Enabled = !busy;

        btnSave.Text = busy ? "Saving..." : "Save";
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }
}
