
using System.Drawing;
using System.Windows.Forms;
using SchoolManagement.Application.Enrollments.DTOs;
using SchoolManagement.Application.Enrollments.Interfaces;

namespace SchoolManagement.Desktop.Controls.Enrollments;

public sealed class TransferEnrollmentForm : Form
{
    private readonly IEnrollmentService _service;
    private readonly EnrollmentDto _source;

    private readonly Label lblStudent = new();
    private readonly Label lblCurrentClass = new();
    private readonly ComboBox cboTargetClass = new();
    private readonly DateTimePicker dtTransfer = new();

    private readonly Button btnTransfer = new()
    {
        Text = "Transfer"
    };

    private readonly Button btnCancel = new()
    {
        Text = "Cancel"
    };

    private bool _busy = true;

    public TransferEnrollmentForm(
        IEnrollmentService service,
        EnrollmentDto source)
    {
        _service = service;
        _source = source;

        Text = "Transfer Student";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 365);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(249, 250, 251);

        BuildUi();

        Shown += async (_, _) => await LoadClassesAsync();
    }

    private void BuildUi()
    {
        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(24, 20, 24, 10)
        };

        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 160));

        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        for (int i = 0; i < 4; i++)
            fields.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 55));

        lblStudent.Text = _source.StudentName;
        lblCurrentClass.Text =
            $"{_source.ClassName} ({_source.AcademicYearName})";

        cboTargetClass.DropDownStyle =
            ComboBoxStyle.DropDownList;

        cboTargetClass.DisplayMember =
            nameof(EnrollmentOptionDto.Name);

        cboTargetClass.ValueMember =
            nameof(EnrollmentOptionDto.Id);

        dtTransfer.Format = DateTimePickerFormat.Short;
        dtTransfer.Value = DateTime.Today;

        AddField(fields, 0, "Student", lblStudent);
        AddField(fields, 1, "Current Class", lblCurrentClass);
        AddField(fields, 2, "Target Class *", cboTargetClass);
        AddField(fields, 3, "Transfer Date *", dtTransfer);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 76,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(15, 14, 24, 10),
            BackColor = Color.White
        };

        StyleButton(btnTransfer, true);
        StyleButton(btnCancel, false);

        btnCancel.DialogResult = DialogResult.Cancel;
        btnTransfer.Click += async (_, _) => await TransferAsync();

        footer.Controls.Add(btnTransfer);
        footer.Controls.Add(btnCancel);

        Controls.Add(fields);
        Controls.Add(footer);

        AcceptButton = btnTransfer;
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

    private async Task LoadClassesAsync()
    {
        try
        {
            var allClasses = await _service.GetClassesAsync();

            // Displays all active classes. The service enforces
            // matching academic years when saving a transfer.
            var options = allClasses
                .Where(x => x.Id != _source.ClassId &&
                    x.Name.EndsWith(
                        $"({_source.AcademicYearName})",
                        StringComparison.Ordinal))
                .ToList();

            if (options.Count == 0)
                throw new InvalidOperationException(
                    "No other active classes were found " +
                    "for this academic year.");

            cboTargetClass.DataSource = options;
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

    private async Task TransferAsync()
    {
        if (_busy) return;

        if (cboTargetClass.SelectedItem is not EnrollmentOptionDto target)
        {
            MessageBox.Show("Select a target class.");
            return;
        }

        var request = new TransferEnrollmentRequest(
            _source.EnrollmentId,
            target.Id,
            DateOnly.FromDateTime(dtTransfer.Value));

        SetBusy(true);

        try
        {
            await _service.TransferAsync(request);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this, ex.Message, "Transfer Error",
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
        cboTargetClass.Enabled = !busy;
        dtTransfer.Enabled = !busy;
        btnTransfer.Enabled = !busy;
        btnCancel.Enabled = !busy;
        btnTransfer.Text = busy ? "Transferring..." : "Transfer";
    }
}
