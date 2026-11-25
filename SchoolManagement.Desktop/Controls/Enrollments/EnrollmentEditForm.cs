
using System.Drawing;
using System.Windows.Forms;
using SchoolManagement.Application.Enrollments.DTOs;
using SchoolManagement.Application.Enrollments.Interfaces;

namespace SchoolManagement.Desktop.Controls.Enrollments;

public sealed class EnrollmentEditForm : Form
{
    private readonly IEnrollmentService _service;

    private readonly ComboBox cboStudent = new();
    private readonly ComboBox cboClass = new();
    private readonly DateTimePicker dtEnrollment = new();

    private readonly Button btnSave = new() { Text = "Enroll" };
    private readonly Button btnCancel = new() { Text = "Cancel" };

    private bool _busy = true;

    public EnrollmentEditForm(IEnrollmentService service)
    {
        _service = service;

        Text = "Enroll Student";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(570, 330);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(249, 250, 251);

        BuildUi();

        Shown += async (_, _) => await LoadOptionsAsync();
    }

    private void BuildUi()
    {
        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(24, 20, 24, 10)
        };

        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 160));

        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        for (int i = 0; i < 3; i++)
            fields.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 58));

        foreach (var combo in new[] { cboStudent, cboClass })
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.DisplayMember = nameof(EnrollmentOptionDto.Name);
            combo.ValueMember = nameof(EnrollmentOptionDto.Id);
        }

        dtEnrollment.Format = DateTimePickerFormat.Short;
        dtEnrollment.Value = DateTime.Today;

        AddField(fields, 0, "Student *", cboStudent);
        AddField(fields, 1, "Class *", cboClass);
        AddField(fields, 2, "Enrollment Date *", dtEnrollment);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 76,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(15, 14, 24, 10),
            BackColor = Color.White,
            WrapContents = false
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
        TableLayoutPanel panel, int row,
        string label, Control control)
    {
        panel.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, row);

        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(3, 9, 3, 9);
        panel.Controls.Add(control, 1, row);
    }

    private static void StyleButton(Button button, bool primary)
    {
        button.Size = new Size(110, 38);
        button.Margin = new Padding(8, 0, 0, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.BackColor = primary
            ? Color.FromArgb(37, 99, 235)
            : Color.White;
        button.ForeColor = primary ? Color.White : Color.Black;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
    }

    private async Task LoadOptionsAsync()
    {
        try
        {
            var students = await _service.GetStudentsAsync();
            var classes = await _service.GetClassesAsync();

            if (students.Count == 0 || classes.Count == 0)
                throw new InvalidOperationException(
                    "Create an active student and class first.");

            cboStudent.DataSource = students.ToList();
            cboClass.DataSource = classes.ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this, ex.Message, "Load Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

            Close();
        }
        finally
        {
            if (!IsDisposed) SetBusy(false);
        }
    }

    private async Task SaveAsync()
    {
        if (_busy) return;

        if (cboStudent.SelectedItem is not EnrollmentOptionDto student ||
            cboClass.SelectedItem is not EnrollmentOptionDto cls)
        {
            MessageBox.Show("Select a student and class.");
            return;
        }

        var request = new CreateEnrollmentRequest(
            student.Id,
            cls.Id,
            DateOnly.FromDateTime(dtEnrollment.Value));

        SetBusy(true);

        try
        {
            await _service.CreateAsync(request);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this, ex.Message, "Enrollment Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (!IsDisposed) SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;

        cboStudent.Enabled = !busy;
        cboClass.Enabled = !busy;
        dtEnrollment.Enabled = !busy;
        btnSave.Enabled = !busy;
        btnCancel.Enabled = !busy;

        btnSave.Text = busy ? "Saving..." : "Enroll";
    }
}
