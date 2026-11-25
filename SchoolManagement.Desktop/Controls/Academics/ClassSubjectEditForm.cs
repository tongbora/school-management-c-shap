
using System.Drawing;
using System.Windows.Forms;
using SchoolManagement.Application.Academics.DTOs;
using SchoolManagement.Application.Academics.Interfaces;

namespace SchoolManagement.Desktop.Controls.Academics;

public sealed class ClassSubjectEditForm : Form
{
    private readonly IClassSubjectService _service;
    private readonly int? _id;

    private readonly ComboBox cboClass = new();
    private readonly ComboBox cboSubject = new();
    private readonly ComboBox cboTeacher = new();

    private readonly Button btnSave = new()
    {
        Text = "Save"
    };

    private readonly Button btnCancel = new()
    {
        Text = "Cancel"
    };

    private bool _busy;
    private bool _loaded;

    public ClassSubjectEditForm(
        IClassSubjectService service,
        int? id = null)
    {
        _service = service;
        _id = id;

        Text = id.HasValue
            ? "Edit Class Subject Assignment"
            : "Assign Subject to Class";

        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(570, 330);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(249, 250, 251);

        BuildUi();

        Shown += async (_, _) => await LoadDataAsync();
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
            new ColumnStyle(SizeType.Absolute, 145));

        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        for (int i = 0; i < 3; i++)
        {
            fields.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 58));
        }

        ConfigureCombo(cboClass);
        ConfigureCombo(cboSubject);
        ConfigureCombo(cboTeacher);

        AddField(fields, 0, "Class *", cboClass);
        AddField(fields, 1, "Subject *", cboSubject);
        AddField(fields, 2, "Teacher", cboTeacher);

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 76,
            BackColor = Color.White,
            Padding = new Padding(15, 12, 24, 12)
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 260,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0)
        };

        ConfigureButton(btnSave, true);
        ConfigureButton(btnCancel, false);

        btnCancel.DialogResult = DialogResult.Cancel;

        btnSave.Click += async (_, _) => await SaveAsync();

        buttons.Controls.Add(btnSave);
        buttons.Controls.Add(btnCancel);

        footer.Controls.Add(buttons);

        Controls.Add(fields);
        Controls.Add(footer);

        AcceptButton = btnSave;
        CancelButton = btnCancel;

        SetBusy(true);
    }

    private static void ConfigureCombo(ComboBox combo)
    {
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.DisplayMember = nameof(AcademicOptionDto.Name);
        combo.ValueMember = nameof(AcademicOptionDto.Id);
    }

    private static void ConfigureButton(
        Button button, bool primary)
    {
        button.Size = new Size(110, 38);
        button.Margin = new Padding(8, 0, 0, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;

        button.BackColor = primary
            ? Color.FromArgb(37, 99, 235)
            : Color.White;

        button.ForeColor = primary
            ? Color.White
            : Color.FromArgb(55, 65, 81);

        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor =
            Color.FromArgb(209, 213, 219);
    }

    private static void AddField(
        TableLayoutPanel panel,
        int row,
        string label,
        Control control)
    {
        panel.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, row);

        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(3, 10, 3, 10);

        panel.Controls.Add(control, 1, row);
    }

    private static void BindOptions(
        ComboBox combo,
        IReadOnlyList<AcademicOptionDto> items,
        bool optional)
    {
        var options = items.ToList();

        if (optional)
        {
            options.Insert(
                0,
                new AcademicOptionDto(
                    0, "-- No Teacher Assigned --"));
        }

        combo.DataSource = options;

        if (options.Count > 0)
            combo.SelectedIndex = 0;
    }

    private static void SelectValue(
        ComboBox combo,
        int? selectedId)
    {
        int id = selectedId ?? 0;

        bool exists = combo.Items
            .Cast<AcademicOptionDto>()
            .Any(x => x.Id == id);

        if (!exists)
        {
            throw new InvalidOperationException(
                "An existing assignment references an unavailable " +
                "class, subject, or teacher. Review its status first.");
        }

        combo.SelectedValue = id;
    }

    private async Task LoadDataAsync()
    {
        SetBusy(true);

        try
        {
            var classes = await _service.GetClassesAsync();
            var subjects = await _service.GetSubjectsAsync();
            var teachers = await _service.GetTeachersAsync();

            BindOptions(cboClass, classes, false);
            BindOptions(cboSubject, subjects, false);
            BindOptions(cboTeacher, teachers, true);

            if (!_id.HasValue)
            {
                if (classes.Count == 0)
                    throw new InvalidOperationException(
                        "Create an active class first.");

                if (subjects.Count == 0)
                    throw new InvalidOperationException(
                        "Create an active subject first.");
            }
            else
            {
                var assignment = await _service.GetByIdAsync(
                    _id.Value);

                if (assignment is null)
                    throw new InvalidOperationException(
                        "Assignment not found.");

                SelectValue(cboClass, assignment.ClassId);
                SelectValue(cboSubject, assignment.SubjectId);
                SelectValue(cboTeacher, assignment.TeacherId);
            }

            _loaded = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this, ex.Message,
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
        if (_busy || !_loaded)
            return;

        if (cboClass.SelectedItem is not AcademicOptionDto cls ||
            cboSubject.SelectedItem is not AcademicOptionDto subject)
        {
            MessageBox.Show(
                this, "Select a class and subject.");
            return;
        }

        var selectedTeacher =
            cboTeacher.SelectedItem as AcademicOptionDto;

        int? teacherId = selectedTeacher?.Id > 0
            ? selectedTeacher.Id
            : null;

        var request = new SaveClassSubjectRequest(
            cls.Id,
            subject.Id,
            teacherId);

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
            MessageBox.Show(
                this, ex.Message,
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

        cboClass.Enabled = !busy;
        cboSubject.Enabled = !busy;
        cboTeacher.Enabled = !busy;

        btnSave.Enabled = !busy;
        btnCancel.Enabled = !busy;

        btnSave.Text = busy ? "Saving..." : "Save";

        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }
}
