
using System.Drawing;
using System.Windows.Forms;
using SchoolManagement.Application.Examinations.DTOs;
using SchoolManagement.Application.Examinations.Interfaces;

namespace SchoolManagement.Desktop.Controls.Examinations;

public sealed class GradeEntryForm : Form
{
    private readonly IGradeService _service;

    private readonly ComboBox cboExam = new();
    private readonly ComboBox cboSubject = new();
    private readonly ComboBox cboStudent = new();

    private readonly NumericUpDown numScore = new();
    private readonly NumericUpDown numMax = new();
    private readonly TextBox txtRemark = new();

    private readonly Button btnSave = new() { Text = "Save" };
    private readonly Button btnCancel = new() { Text = "Cancel" };

    private bool _busy;
    private bool _loading;

    public GradeEntryForm(IGradeService service)
    {
        _service = service;

        Text = "Record Student Grade";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(610, 490);
        Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        BuildUi();

        Shown += async (_, _) => await LoadExamsAsync();
    }

    private void BuildUi()
    {
        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(24, 20, 24, 12)
        };

        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 155));
        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        for (int i = 0; i < 6; i++)
            fields.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 59));

        foreach (var combo in
            new[] { cboExam, cboSubject, cboStudent })
        {
            combo.DropDownStyle =
                ComboBoxStyle.DropDownList;
            combo.DisplayMember = nameof(GradeOptionDto.Name);
            combo.ValueMember = nameof(GradeOptionDto.Id);
        }

        numScore.DecimalPlaces = 2;
        numScore.Maximum = 9999.99m;
        numScore.Minimum = 0;

        numMax.DecimalPlaces = 2;
        numMax.Minimum = 0.01m;
        numMax.Maximum = 9999.99m;
        numMax.Value = 100;

        txtRemark.MaxLength = 255;

        AddField(fields, 0, "Examination *", cboExam);
        AddField(fields, 1, "Class / Subject *", cboSubject);
        AddField(fields, 2, "Student *", cboStudent);
        AddField(fields, 3, "Score *", numScore);
        AddField(fields, 4, "Maximum Score *", numMax);
        AddField(fields, 5, "Remark", txtRemark);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 72,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(16, 14, 24, 8),
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

        cboExam.SelectedIndexChanged += async (_, _) =>
        {
            if (!_loading)
                await LoadSubjectsAsync();
        };

        cboSubject.SelectedIndexChanged += async (_, _) =>
        {
            if (!_loading)
                await LoadStudentsAsync();
        };

        SetBusy(true);
    }

    private static void AddField(
        TableLayoutPanel panel,
        int row, string label, Control input)
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
        button.ForeColor = primary ? Color.White : Color.Black;
        button.FlatAppearance.BorderSize =
            primary ? 0 : 1;
    }

    private async Task LoadExamsAsync()
    {
        SetBusy(true);
        _loading = true;

        try
        {
            var exams = await _service.GetOpenExamsAsync();

            if (exams.Count == 0)
                throw new InvalidOperationException(
                    "Open an examination before recording grades.");

            cboExam.DataSource = exams.ToList();
            _loading = false;

            await LoadSubjectsAsync();
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

    private async Task LoadSubjectsAsync()
    {
        if (cboExam.SelectedItem is not GradeOptionDto exam)
            return;

        SetBusy(true);
        _loading = true;

        try
        {
            cboSubject.DataSource = null;
            cboStudent.DataSource = null;

            var subjects =
                await _service.GetClassSubjectsAsync(exam.Id);

            if (IsDisposed)
                return;

            cboSubject.DataSource = subjects.ToList();
            _loading = false;

            await LoadStudentsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message,
                "Load Subjects Error");
        }
        finally
        {
            _loading = false;
            if (!IsDisposed)
                SetBusy(false);
        }
    }

    private async Task LoadStudentsAsync()
    {
        if (cboSubject.SelectedItem is not GradeOptionDto subject)
        {
            cboStudent.DataSource = null;
            return;
        }

        SetBusy(true);

        try
        {
            cboStudent.DataSource = null;

            var students =
                await _service.GetEnrollmentsAsync(subject.Id);

            if (IsDisposed)
                return;

            cboStudent.DataSource = students.ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message,
                "Load Students Error");
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

        if (cboExam.SelectedItem is not GradeOptionDto exam ||
            cboSubject.SelectedItem is not GradeOptionDto subject ||
            cboStudent.SelectedItem is not GradeOptionDto student)
        {
            MessageBox.Show(this,
                "Select an examination, subject, and student.");
            return;
        }

        var request = new SaveGradeRequest(
            student.Id,
            subject.Id,
            exam.Id,
            numScore.Value,
            numMax.Value,
            txtRemark.Text);

        SetBusy(true);

        try
        {
            await _service.SaveAsync(request);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message,
                "Grade Error",
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

        cboExam.Enabled = !busy;
        cboSubject.Enabled = !busy;
        cboStudent.Enabled = !busy;
        numScore.Enabled = !busy;
        numMax.Enabled = !busy;
        txtRemark.Enabled = !busy;
        btnSave.Enabled = !busy;
        btnCancel.Enabled = !busy;
    }
}
