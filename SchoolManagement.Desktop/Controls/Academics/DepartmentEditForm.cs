
using System.Drawing;
using System.Windows.Forms;
using SchoolManagement.Application.Academics.DTOs;
using SchoolManagement.Application.Academics.Interfaces;

namespace SchoolManagement.Desktop.Controls.Academics;

public sealed class DepartmentEditForm : Form
{
    private readonly IDepartmentService _service;
    private readonly int? _departmentId;

    private readonly TextBox txtName = new();
    private readonly TextBox txtDescription = new();
    private readonly Button btnSave = new();
    private readonly Button btnCancel = new();

    private bool _busy;

    public DepartmentEditForm(
        IDepartmentService service,
        int? departmentId = null)
    {
        _service = service;
        _departmentId = departmentId;

        Text = departmentId.HasValue
            ? "Edit Department"
            : "Add Department";

        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(530, 340);
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
            if (_departmentId.HasValue)
                await LoadDepartmentAsync();
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

        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 75));

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(24, 20, 24, 10)
        };

        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 140));

        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        fields.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 55));

        fields.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 115));

        txtName.MaxLength = 120;
        txtName.BorderStyle = BorderStyle.FixedSingle;

        txtDescription.MaxLength = 500;
        txtDescription.Multiline = true;
        txtDescription.ScrollBars = ScrollBars.Vertical;

        AddField(fields, 0, "Department Name *", txtName);
        AddField(fields, 1, "Description", txtDescription);

        var footer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(15)
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 250,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
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
        TableLayoutPanel panel,
        int row,
        string text,
        Control control)
    {
        panel.Controls.Add(new Label
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, row);

        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(4, 8, 4, 8);

        panel.Controls.Add(control, 1, row);
    }

    private static void ConfigureButton(
        Button button, string text, bool primary)
    {
        button.Text = text;
        button.Size = new Size(105, 38);
        button.Margin = new Padding(7, 0, 0, 0);
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

    private async Task LoadDepartmentAsync()
    {
        SetBusy(true);

        try
        {
            var department = await _service.GetByIdAsync(
                _departmentId!.Value);

            if (department is null)
            {
                MessageBox.Show("Department not found.");
                Close();
                return;
            }

            txtName.Text = department.DepartmentName;
            txtDescription.Text = department.Description ?? "";
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

        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show("Department name is required.");
            txtName.Focus();
            return;
        }

        var request = new SaveDepartmentRequest(
            txtName.Text.Trim(),
            txtDescription.Text);

        SetBusy(true);

        try
        {
            if (_departmentId.HasValue)
            {
                await _service.UpdateAsync(
                    _departmentId.Value, request);
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
            if (!IsDisposed)
                SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        btnSave.Enabled = !busy;
        btnCancel.Enabled = !busy;
        txtName.Enabled = !busy;
        txtDescription.Enabled = !busy;
        btnSave.Text = busy ? "Saving..." : "Save";
    }
}
