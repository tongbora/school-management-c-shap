
using System.Drawing;
using System.Windows.Forms;
using SchoolManagement.Application.Examinations.DTOs;
using SchoolManagement.Application.Examinations.Interfaces;
using SchoolManagement.Application.Academics.Interfaces;

namespace SchoolManagement.Desktop.Controls.Examinations;

public sealed class ExamEditForm : Form
{
    private readonly IExamService _service;
    private readonly IAcademicYearService _yearService;
    private readonly int? _id;

    private readonly TextBox txtName = new();
    private readonly TextBox txtType = new();
    private readonly ComboBox cboYear = new();
    private readonly DateTimePicker dtStart = new();
    private readonly DateTimePicker dtEnd = new();
    private readonly Button btnSave = new() { Text = "Save" };
    private readonly Button btnCancel = new() { Text = "Cancel" };

    private bool _ready;
    private bool _busy;

    public ExamEditForm(
        IExamService service,
        IAcademicYearService yearService,
        int? id = null)
    {
        _service = service;
        _yearService = yearService;
        _id = id;

        Text = id.HasValue ? "Edit Examination"
                           : "Add Examination";

        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(570, 430);
        Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        BuildUi();

        Shown += async (_, _) => await LoadAsync();
    }

    private void BuildUi()
    {
        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(22)
        };

        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 150));
        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        for (int i = 0; i < 5; i++)
            fields.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 56));

        txtName.MaxLength = 120;
        txtType.MaxLength = 50;

        cboYear.DropDownStyle =
            ComboBoxStyle.DropDownList;
        cboYear.DisplayMember = "Name";
        cboYear.ValueMember = "Id";

        foreach (var picker in new[] { dtStart, dtEnd })
        {
            picker.Format = DateTimePickerFormat.Short;
            picker.ShowCheckBox = true;
            picker.Checked = false;
        }

        AddField(fields, 0, "Exam Name *", txtName);
        AddField(fields, 1, "Exam Type", txtType);
        AddField(fields, 2, "Academic Year *", cboYear);
        AddField(fields, 3, "Start Date", dtStart);
        AddField(fields, 4, "End Date", dtEnd);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 72,
            Padding = new Padding(16, 14, 24, 8),
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.White
        };

        StyleButton(btnSave, true);
        StyleButton(btnCancel, false);

        btnCancel.DialogResult = DialogResult.Cancel;
        btnSave.Click += async (_, _) => await SaveAsync();

        footer.Controls.Add(btnSave);
        footer.Controls.Add(btnCancel);

        Controls.Add(fields);
        Controls.Add(footer);

        AcceptButton = btnSave;
        CancelButton = btnCancel;

        SetBusy(true);
    }

    private static void AddField(
        TableLayoutPanel panel,
        int row,
        string label,
        Control input)
    {
        panel.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, row);

        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(3, 10, 3, 10);
        panel.Controls.Add(input, 1, row);
    }

    private static void StyleButton(
        Button button, bool primary)
    {
        button.Size = new Size(110, 38);
        button.Margin = new Padding(8, 0, 0, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.BackColor = primary
            ? Color.FromArgb(37, 99, 235)
            : Color.White;
        button.ForeColor = primary
            ? Color.White
            : Color.FromArgb(55, 65, 81);
        button.FlatAppearance.BorderSize =
            primary ? 0 : 1;
    }

    private async Task LoadAsync()
    {
        SetBusy(true);

        try
        {
            var years = await _yearService.GetAllAsync();

            var options = years
                .Select(x => new
                {
                    Id = x.AcademicYearId,
                    Name = x.YearName
                })
                .ToList();

            if (options.Count == 0)
                throw new InvalidOperationException(
                    "Create an academic year first.");

            cboYear.DataSource = options;

            if (_id.HasValue)
            {
                var exam = await _service.GetByIdAsync(
                    _id.Value)
                    ?? throw new InvalidOperationException(
                        "Examination not found.");

                txtName.Text = exam.ExamName;
                txtType.Text = exam.ExamType ?? "";
                cboYear.SelectedValue = exam.AcademicYearId;

                if (exam.StartDate.HasValue)
                {
                    dtStart.Value = exam.StartDate.Value
                        .ToDateTime(TimeOnly.MinValue);
                    dtStart.Checked = true;
                }

                if (exam.EndDate.HasValue)
                {
                    dtEnd.Value = exam.EndDate.Value
                        .ToDateTime(TimeOnly.MinValue);
                    dtEnd.Checked = true;
                }
            }

            _ready = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message,
                "Load Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Close();
        }
        finally
        {
            if (!IsDisposed)
                SetBusy(false);
        }
    }

    private async Task SaveAsync()
    {
        if (_busy || !_ready)
            return;

        if (cboYear.SelectedValue is not int yearId)
            return;

        var request = new SaveExamRequest(
            txtName.Text.Trim(),
            txtType.Text,
            yearId,
            dtStart.Checked
                ? DateOnly.FromDateTime(dtStart.Value)
                : null,
            dtEnd.Checked
                ? DateOnly.FromDateTime(dtEnd.Value)
                : null);

        SetBusy(true);

        try
        {
            if (_id.HasValue)
                await _service.UpdateAsync(_id.Value, request);
            else
                await _service.CreateAsync(request);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message,
                "Save Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            if (!IsDisposed)
                SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        txtName.Enabled = !busy;
        txtType.Enabled = !busy;
        cboYear.Enabled = !busy;
        dtStart.Enabled = !busy;
        dtEnd.Enabled = !busy;
        btnSave.Enabled = !busy;
        btnCancel.Enabled = !busy;
    }
}
