
using SchoolManagement.Application.Guardians.DTOs;
using SchoolManagement.Application.Guardians.Interfaces;
using System.Drawing;
using System.Windows.Forms;

namespace SchoolManagement.Desktop.Controls.Students;

public sealed class GuardianEditForm : Form
{
    private readonly IGuardianService _guardianService;
    private readonly int? _guardianId;

    private readonly TextBox txtFullName = new();
    private readonly ComboBox cboGender = new();
    private readonly TextBox txtPhone = new();
    private readonly TextBox txtEmail = new();
    private readonly TextBox txtOccupation = new();
    private readonly TextBox txtAddress = new();

    private readonly Button btnSave = new();
    private readonly Label lblError = new();

    private bool _saving;

    public int? SavedGuardianId { get; private set; }

    public GuardianEditForm(
        IGuardianService guardianService,
        int? guardianId = null)
    {
        _guardianService = guardianService;
        _guardianId = guardianId;

        Text = guardianId.HasValue
            ? "Edit Guardian"
            : "Register Guardian";

        ClientSize = new Size(520, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.White;

        BuildForm();

        if (guardianId.HasValue)
            Shown += GuardianEditForm_Shown;
    }

    private void BuildForm()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(25),
            ColumnCount = 1,
            AutoScroll = true
        };

        layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        void AddField(string label, Control input)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };

            panel.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 25));
            panel.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 35));

            panel.Controls.Add(new Label
            {
                Text = label,
                Dock = DockStyle.Fill
            }, 0, 0);

            input.Dock = DockStyle.Fill;
            panel.Controls.Add(input, 0, 1);

            int row = layout.RowCount++;
            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 70));
            layout.Controls.Add(panel, 0, row);
        }

        txtFullName.MaxLength = 150;
        txtPhone.MaxLength = 30;
        txtEmail.MaxLength = 150;
        txtOccupation.MaxLength = 120;
        txtAddress.MaxLength = 255;

        cboGender.DropDownStyle =
            ComboBoxStyle.DropDownList;

        cboGender.Items.AddRange(
            new object[] { "", "Male", "Female", "Other" });

        cboGender.SelectedIndex = 0;

        AddField("Full Name *", txtFullName);
        AddField("Gender", cboGender);
        AddField("Phone", txtPhone);
        AddField("Email", txtEmail);
        AddField("Occupation", txtOccupation);
        AddField("Address", txtAddress);

        lblError.ForeColor = Color.Firebrick;
        lblError.Dock = DockStyle.Fill;

        int errorRow = layout.RowCount++;
        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 55));
        layout.Controls.Add(lblError, 0, errorRow);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        btnSave.Text = "Save Guardian";
        btnSave.Size = new Size(140, 38);
        btnSave.BackColor = Color.FromArgb(37, 99, 235);
        btnSave.ForeColor = Color.White;
        btnSave.FlatStyle = FlatStyle.Flat;
        btnSave.Click += BtnSave_Click;

        var btnCancel = new Button
        {
            Text = "Cancel",
            Size = new Size(100, 38),
            DialogResult = DialogResult.Cancel
        };

        buttons.Controls.Add(btnSave);
        buttons.Controls.Add(btnCancel);

        int buttonRow = layout.RowCount++;
        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 55));
        layout.Controls.Add(buttons, 0, buttonRow);

        Controls.Add(layout);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private async void GuardianEditForm_Shown(
        object? sender, EventArgs e)
    {
        if (!_guardianId.HasValue)
            return;

        btnSave.Enabled = false;

        try
        {
            var guardians = await _guardianService.SearchAsync();

            var guardian = guardians.FirstOrDefault(
                g => g.GuardianId == _guardianId.Value);

            if (guardian is null)
            {
                MessageBox.Show(this, "Guardian not found.");
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            txtFullName.Text = guardian.FullName;
            cboGender.SelectedItem = guardian.Gender ?? "";
            txtPhone.Text = guardian.Phone ?? "";
            txtEmail.Text = guardian.Email ?? "";
            txtOccupation.Text = guardian.Occupation ?? "";
            txtAddress.Text = guardian.Address ?? "";
        }
        catch
        {
            lblError.Text = "Unable to load guardian information.";
        }
        finally
        {
            btnSave.Enabled = true;
        }
    }

    private async void BtnSave_Click(
        object? sender, EventArgs e)
    {
        if (_saving)
            return;

        lblError.Text = "";

        if (string.IsNullOrWhiteSpace(txtFullName.Text))
        {
            lblError.Text = "Full name is required.";
            return;
        }

        _saving = true;
        btnSave.Enabled = false;

        try
        {
            var request = new SaveGuardianRequest(
                txtFullName.Text.Trim(),
                cboGender.SelectedItem?.ToString(),
                txtPhone.Text.Trim(),
                txtEmail.Text.Trim(),
                txtOccupation.Text.Trim(),
                txtAddress.Text.Trim()
            );

            if (_guardianId.HasValue)
            {
                await _guardianService.UpdateAsync(
                    _guardianId.Value, request);

                SavedGuardianId = _guardianId.Value;
            }
            else
            {
                SavedGuardianId =
                    await _guardianService.CreateAsync(request);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ArgumentException ex)
        {
            lblError.Text = ex.Message;
        }
        catch (Exception)
        {
            lblError.Text = "Unable to save guardian.";
        }
        finally
        {
            _saving = false;
            btnSave.Enabled = true;
        }
    }
}
