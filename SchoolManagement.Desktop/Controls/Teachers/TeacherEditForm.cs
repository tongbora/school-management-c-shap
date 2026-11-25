
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using SchoolManagement.Application.Teachers.DTOs;
using SchoolManagement.Application.Teachers.Interfaces;

namespace SchoolManagement.Desktop.Controls.Teachers;

public sealed class TeacherEditForm : Form
{
    private readonly ITeacherService _service;
    private readonly int? _teacherId;

    private readonly TextBox txtCode = new();
    private readonly TextBox txtName = new();
    private readonly ComboBox cboGender = new();
    private readonly DateTimePicker dtBirth = new();
    private readonly DateTimePicker dtHire = new();
    private readonly TextBox txtPhone = new();
    private readonly TextBox txtEmail = new();
    private readonly TextBox txtSpecialty = new();
    private readonly ComboBox cboDepartment = new();

    private readonly Button btnSave = new();
    private readonly Button btnCancel = new();

    private bool _departmentsLoaded;

    public TeacherEditForm(
        ITeacherService service,
        int? teacherId = null)
    {
        _service = service;
        _teacherId = teacherId;

        Text = teacherId.HasValue
            ? "Edit Teacher"
            : "Add Teacher";

        StartPosition = FormStartPosition.CenterParent;
        Width = 560;
        Height = 630;
        MinimumSize = new Size(520, 600);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        BuildForm();

        Shown += async (_, _) =>
        {
            try
            {
                btnSave.Enabled = false;

                await LoadDepartmentsAsync();

                if (_teacherId.HasValue)
                    await LoadTeacherAsync();

                btnSave.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Load Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                Close();
            }
        };
    }

    private void BuildForm()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            ColumnCount = 2,
            RowCount = 11,
            AutoScroll = true
        };

        panel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 145));

        panel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        for (int i = 0; i < 10; i++)
        {
            panel.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 45));
        }

        panel.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 60));

        txtCode.MaxLength = 50;
        txtName.MaxLength = 150;
        txtPhone.MaxLength = 30;
        txtEmail.MaxLength = 150;
        txtSpecialty.MaxLength = 120;

        cboGender.DropDownStyle =
            ComboBoxStyle.DropDownList;

        cboGender.Items.AddRange(
            new object[]
            {
                "None",
                "Male",
                "Female",
                "Other"
            });

        cboGender.SelectedIndex = 0;

        dtBirth.Format = DateTimePickerFormat.Short;
        dtBirth.ShowCheckBox = true;
        dtBirth.Checked = false;

        dtHire.Format = DateTimePickerFormat.Short;
        dtHire.Value = DateTime.Today;

        // Department dropdown
        cboDepartment.DropDownStyle =
            ComboBoxStyle.DropDownList;

        cboDepartment.DisplayMember =
            nameof(DepartmentOptionDto.DepartmentName);

        cboDepartment.ValueMember =
            nameof(DepartmentOptionDto.DepartmentId);

        cboDepartment.Enabled = false;

        AddField(panel, 0, "Teacher Code *", txtCode);
        AddField(panel, 1, "Full Name *", txtName);
        AddField(panel, 2, "Gender", cboGender);
        AddField(panel, 3, "Date of Birth", dtBirth);
        AddField(panel, 4, "Phone", txtPhone);
        AddField(panel, 5, "Email", txtEmail);
        AddField(panel, 6, "Specialty", txtSpecialty);
        AddField(panel, 7, "Department", cboDepartment);
        AddField(panel, 8, "Hire Date *", dtHire);

        var note = new Label
        {
            Text = "Select a department or leave it unassigned.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Anchor = AnchorStyles.Left
        };

        panel.Controls.Add(note, 1, 9);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        btnSave.Text = "Save";
        btnSave.Width = 100;
        btnSave.Height = 32;

        btnCancel.Text = "Cancel";
        btnCancel.Width = 100;
        btnCancel.Height = 32;
        btnCancel.DialogResult = DialogResult.Cancel;

        btnSave.Click += async (_, _) =>
            await SaveTeacherAsync();

        buttons.Controls.Add(btnCancel);
        buttons.Controls.Add(btnSave);

        panel.Controls.Add(buttons, 1, 10);

        Controls.Add(panel);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private static void AddField(
        TableLayoutPanel panel,
        int row,
        string label,
        Control control)
    {
        panel.Controls.Add(
            new Label
            {
                Text = label,
                Anchor = AnchorStyles.Left,
                AutoSize = true
            },
            0,
            row);

        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(3, 5, 3, 5);

        panel.Controls.Add(control, 1, row);
    }

    // =========================================
    // Load departments from database
    // =========================================

    private async Task LoadDepartmentsAsync()
    {
        var departments =
            await _service.GetDepartmentsAsync();

        var options = new List<DepartmentOptionDto>
        {
            new(0, "-- No Department --")
        };

        options.AddRange(departments);

        cboDepartment.DataSource = options;
        cboDepartment.SelectedValue = 0;
        cboDepartment.Enabled = true;

        _departmentsLoaded = true;
    }

    // =========================================
    // Load existing teacher
    // =========================================

    private async Task LoadTeacherAsync()
    {
        var teacher = await _service.GetByIdAsync(
            _teacherId!.Value);

        if (teacher is null)
        {
            throw new InvalidOperationException(
                "Teacher not found.");
        }

        txtCode.Text = teacher.TeacherCode;
        txtName.Text = teacher.FullName;

        string gender = teacher.Gender ?? "None";

        int genderIndex =
            cboGender.FindStringExact(gender);

        cboGender.SelectedIndex =
            genderIndex >= 0 ? genderIndex : 0;

        if (teacher.DateOfBirth.HasValue)
        {
            dtBirth.Value =
                teacher.DateOfBirth.Value.ToDateTime(
                    TimeOnly.MinValue);

            dtBirth.Checked = true;
        }
        else
        {
            dtBirth.Checked = false;
        }

        txtPhone.Text = teacher.Phone ?? "";
        txtEmail.Text = teacher.Email ?? "";
        txtSpecialty.Text =
            teacher.SubjectSpecialty ?? "";

        // Select department based on stored ID
        int departmentId = teacher.DepartmentId ?? 0;

        bool departmentExists =
            ((IEnumerable<DepartmentOptionDto>)
                cboDepartment.DataSource!)
            .Any(x => x.DepartmentId == departmentId);

        if (!departmentExists)
        {
            throw new InvalidOperationException(
                "The teacher's assigned department " +
                "was not found. Please check the database.");
        }

        cboDepartment.SelectedValue = departmentId;

        dtHire.Value =
            teacher.HireDate.ToDateTime(
                TimeOnly.MinValue);
    }

    // =========================================
    // Save teacher
    // =========================================

    private async Task SaveTeacherAsync()
    {
        if (!_departmentsLoaded)
        {
            MessageBox.Show(
                "Departments have not finished loading.");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtCode.Text) ||
            string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show(
                "Teacher code and full name are required.",
                "Validation",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        int? departmentId = null;

        if (cboDepartment.SelectedItem
            is DepartmentOptionDto selectedDepartment &&
            selectedDepartment.DepartmentId > 0)
        {
            departmentId = selectedDepartment.DepartmentId;
        }

        var request = new SaveTeacherRequest(
            txtCode.Text.Trim(),
            txtName.Text.Trim(),
            cboGender.SelectedIndex == 0
                ? null
                : cboGender.SelectedItem?.ToString(),
            dtBirth.Checked
                ? DateOnly.FromDateTime(dtBirth.Value)
                : null,
            txtPhone.Text,
            txtEmail.Text,
            txtSpecialty.Text,
            departmentId,
            DateOnly.FromDateTime(dtHire.Value)
        );

        btnSave.Enabled = false;

        try
        {
            if (_teacherId.HasValue)
            {
                await _service.UpdateAsync(
                    _teacherId.Value,
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
                ex.Message,
                "Save Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            btnSave.Enabled = true;
        }
    }
}
