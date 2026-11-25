
using System.Drawing;
using System.Windows.Forms;
using SchoolManagement.Application.Academics.DTOs;
using SchoolManagement.Application.Academics.Interfaces;

namespace SchoolManagement.Desktop.Controls.Academics;

public sealed class SubjectEditForm : Form
{
    private readonly ISubjectService _service;
    private readonly int? _subjectId;

    private readonly TextBox txtCode = new();
    private readonly TextBox txtName = new();
    private readonly TextBox txtGrade = new();
    private readonly Button btnSave = new();
    private readonly Button btnCancel = new();

    private bool _busy;

    public SubjectEditForm(
        ISubjectService service,
        int? subjectId = null)
    {
        _service = service;
        _subjectId = subjectId;

        Text = subjectId.HasValue ? "Edit Subject" : "Add Subject";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(500, 340);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(249, 250, 251);

        BuildForm();

        Shown += async (_, _) =>
        {
            if (_subjectId.HasValue)
                await LoadSubjectAsync();
        };
    }

    private void BuildForm()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };

        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(24, 20, 24, 10)
        };

        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 135));
        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        for (int i = 0; i < 3; i++)
            fields.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 60));

        txtCode.MaxLength = 50;
        txtName.MaxLength = 120;
        txtGrade.MaxLength = 50;

        AddField(fields, 0, "Subject Code *", txtCode);
        AddField(fields, 1, "Subject Name *", txtName);
        AddField(fields, 2, "Grade Level", txtGrade);

        var footer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(20, 12, 24, 12)
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 255,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0)
        };

        ConfigureButton(btnSave, "Save", true);
        ConfigureButton(btnCancel, "Cancel", false);

        btnSave.Click += async (_, _) => await SaveAsync();
        btnCancel.DialogResult = DialogResult.Cancel;

        buttons.Controls.Add(btnSave);
        buttons.Controls.Add(btnCancel);
        footer.Controls.Add(buttons);

        root.Controls.Add(fields, 0, 0);
        root.Controls.Add(footer, 0, 1);
        Controls.Add(root);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private static void AddField(
        TableLayoutPanel panel, int row,
        string label, Control input)
    {
        panel.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, row);

        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(0, 10, 0, 10);
        panel.Controls.Add(input, 1, row);
    }

    private static void ConfigureButton(
        Button button, string label, bool primary)
    {
        button.Text = label;
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

    private async Task LoadSubjectAsync()
    {
        SetBusy(true);

        try
        {
            var subject = await _service.GetByIdAsync(
                _subjectId!.Value);

            if (subject is null)
            {
                MessageBox.Show("Subject not found.");
                Close();
                return;
            }

            txtCode.Text = subject.SubjectCode;
            txtName.Text = subject.SubjectName;
            txtGrade.Text = subject.GradeLevel ?? "";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message,
                "Load Error", MessageBoxButtons.OK,
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
        if (_busy)
            return;

        if (string.IsNullOrWhiteSpace(txtCode.Text) ||
            string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show(
                "Subject code and subject name are required.");
            return;
        }

        var request = new SaveSubjectRequest(
            txtCode.Text.Trim(),
            txtName.Text.Trim(),
            txtGrade.Text);

        SetBusy(true);

        try
        {
            if (_subjectId.HasValue)
                await _service.UpdateAsync(_subjectId.Value, request);
            else
                await _service.CreateAsync(request);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message,
                "Save Error", MessageBoxButtons.OK,
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
        btnSave.Enabled = !busy;
        btnCancel.Enabled = !busy;
        txtCode.Enabled = !busy;
        txtName.Enabled = !busy;
        txtGrade.Enabled = !busy;
        btnSave.Text = busy ? "Saving..." : "Save";
    }
}
