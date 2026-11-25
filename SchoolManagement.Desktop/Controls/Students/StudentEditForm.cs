
using SchoolManagement.Application.Students.DTOs;
using SchoolManagement.Application.Students.Interfaces;
using System.Drawing;
using System.Net.Mail;
using System.Windows.Forms;

namespace SchoolManagement.Desktop.Controls.Students;

public sealed class StudentEditForm : Form
{
    private readonly IStudentService _studentService;
    private readonly int? _studentId;

    private readonly TextBox txtCode = new();
    private readonly TextBox txtName = new();
    private readonly ComboBox cboGender = new();
    private readonly DateTimePicker dtpBirth = new();
    private readonly TextBox txtPhone = new();
    private readonly TextBox txtEmail = new();
    private readonly TextBox txtAddress = new();

    private readonly PictureBox picStudent = new();
    private readonly Button btnUpload = new();
    private readonly Button btnSave = new();
    private readonly Button btnCancel = new();
    private readonly Label lblError = new();

    private string? _selectedPhotoPath;
    private string? _existingPhotoName;
    private bool _saving;

    public StudentEditForm(
        IStudentService studentService,
        int? studentId = null)
    {
        _studentService = studentService;
        _studentId = studentId;

        Text = studentId.HasValue
            ? "Edit Student"
            : "Register Student";

        ClientSize = new Size(640, 680);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 10F);

        BuildForm();

        if (_studentId.HasValue)
            Shown += StudentEditForm_Shown;

        FormClosed += (_, _) =>
        {
            picStudent.Image?.Dispose();
        };
    }

    private void BuildForm()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20)
        };

        var title = new Label
        {
            Text = _studentId.HasValue
                ? "Edit Student Information"
                : "Student Registration",
            Location = new Point(30, 20),
            Size = new Size(570, 45),
            Font = new Font(
                "Segoe UI", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 58, 95)
        };

        panel.Controls.Add(title);

        // Student photo preview
        picStudent.Location = new Point(410, 95);
        picStudent.Size = new Size(165, 175);
        picStudent.BackColor = Color.FromArgb(240, 242, 245);
        picStudent.BorderStyle = BorderStyle.FixedSingle;
        picStudent.SizeMode = PictureBoxSizeMode.Zoom;

        btnUpload.Text = "Upload Photo";
        btnUpload.Location = new Point(410, 285);
        btnUpload.Size = new Size(165, 38);
        btnUpload.Click += BtnUpload_Click;

        panel.Controls.Add(picStudent);
        panel.Controls.Add(btnUpload);

        int y = 95;

        AddField(panel, "Student Code *", txtCode, ref y);
        AddField(panel, "Full Name *", txtName, ref y);

        cboGender.DropDownStyle =
            ComboBoxStyle.DropDownList;

        cboGender.Items.AddRange(
            new object[] { "Male", "Female", "Other" });

        cboGender.SelectedIndex = 0;

        AddField(panel, "Gender *", cboGender, ref y);

        dtpBirth.Format = DateTimePickerFormat.Short;
        dtpBirth.MaxDate = DateTime.Today;
        dtpBirth.Value = DateTime.Today;

        AddField(panel, "Date of Birth *", dtpBirth, ref y);
        AddField(panel, "Phone", txtPhone, ref y);
        AddField(panel, "Email", txtEmail, ref y);
        AddField(panel, "Address", txtAddress, ref y);

        txtCode.MaxLength = 50;
        txtName.MaxLength = 150;
        txtPhone.MaxLength = 30;
        txtEmail.MaxLength = 150;
        txtAddress.MaxLength = 255;

        lblError.Location = new Point(30, y + 5);
        lblError.Size = new Size(570, 45);
        lblError.ForeColor = Color.Firebrick;

        btnCancel.Text = "Cancel";
        btnCancel.Location = new Point(345, y + 65);
        btnCancel.Size = new Size(105, 40);
        btnCancel.DialogResult = DialogResult.Cancel;

        btnSave.Text = "Save Student";
        btnSave.Location = new Point(465, y + 65);
        btnSave.Size = new Size(130, 40);
        btnSave.BackColor = Color.FromArgb(37, 99, 235);
        btnSave.ForeColor = Color.White;
        btnSave.FlatStyle = FlatStyle.Flat;
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += BtnSave_Click;

        panel.Controls.Add(lblError);
        panel.Controls.Add(btnCancel);
        panel.Controls.Add(btnSave);

        panel.AutoScrollMinSize = new Size(610, y + 130);

        Controls.Add(panel);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private static void AddField(
        Panel panel,
        string label,
        Control input,
        ref int y)
    {
        var title = new Label
        {
            Text = label,
            Location = new Point(30, y),
            Size = new Size(340, 24)
        };

        input.Location = new Point(30, y + 25);
        input.Size = new Size(340, 30);

        panel.Controls.Add(title);
        panel.Controls.Add(input);

        y += 74;
    }

    private void BtnUpload_Click(
        object? sender,
        EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select Student Photo",
            Filter =
                "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
            Multiselect = false,
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            if (new FileInfo(dialog.FileName).Length >
                5 * 1024 * 1024)
            {
                throw new InvalidOperationException(
                    "Photo must be smaller than 5 MB.");
            }

            using var original =
                Image.FromFile(dialog.FileName);

            if (original.RawFormat.Guid !=
                    System.Drawing.Imaging.ImageFormat.Jpeg.Guid &&
                original.RawFormat.Guid !=
                    System.Drawing.Imaging.ImageFormat.Png.Guid)
            {
                throw new InvalidOperationException(
                    "Only JPG and PNG photos are supported.");
            }

            var preview = new Bitmap(original);

            var oldImage = picStudent.Image;
            picStudent.Image = preview;
            oldImage?.Dispose();

            _selectedPhotoPath = dialog.FileName;

            lblError.Text = "";
        }
        catch (Exception)
        {
            lblError.Text =
                "Unable to load photo. Select a valid JPG or PNG.";
        }
    }

    private async void StudentEditForm_Shown(
        object? sender,
        EventArgs e)
    {
        if (!_studentId.HasValue)
            return;

        btnSave.Enabled = false;

        try
        {
            var student = await _studentService
                .GetByIdAsync(_studentId.Value);

            if (student is null)
            {
                MessageBox.Show(this, "Student not found.");
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            txtCode.Text = student.StudentCode;
            txtName.Text = student.FullName;

            cboGender.SelectedItem = student.Gender;

            if (cboGender.SelectedIndex < 0)
                cboGender.SelectedIndex = 0;

            var birthDate =
                student.DateOfBirth.ToDateTime(
                    TimeOnly.MinValue);

            if (birthDate >= dtpBirth.MinDate &&
                birthDate <= dtpBirth.MaxDate)
            {
                dtpBirth.Value = birthDate;
            }

            txtPhone.Text = student.Phone ?? "";
            txtEmail.Text = student.Email ?? "";
            txtAddress.Text = student.Address ?? "";

            _existingPhotoName = student.PhotoPath;

            try
            {
                picStudent.Image =
                    StudentPhotoStorage.LoadPhoto(
                        _existingPhotoName);
            }
            catch
            {
                lblError.Text =
                    "Existing student photo could not be loaded.";
            }
        }
        catch
        {
            lblError.Text =
                "Unable to load student information.";
        }
        finally
        {
            btnSave.Enabled = true;
        }
    }

    private async void BtnSave_Click(
        object? sender,
        EventArgs e)
    {
        if (_saving)
            return;

        lblError.Text = "";

        string code = txtCode.Text.Trim();
        string name = txtName.Text.Trim();
        string gender = cboGender.SelectedItem?.ToString() ?? "";
        string email = txtEmail.Text.Trim();

        if (string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(name))
        {
            lblError.Text =
                "Student code and full name are required.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(email) &&
            !MailAddress.TryCreate(email, out _))
        {
            lblError.Text = "Invalid email address.";
            return;
        }

        _saving = true;
        btnSave.Enabled = false;

        string? newlySavedPhoto = null;

        try
        {
            string? photoName = _existingPhotoName;

            if (!string.IsNullOrWhiteSpace(
                _selectedPhotoPath))
            {
                newlySavedPhoto =
                    StudentPhotoStorage.SavePhoto(
                        _selectedPhotoPath);

                photoName = newlySavedPhoto;
            }

            var request = new SaveStudentRequest(
                StudentCode: code,
                FullName: name,
                Gender: gender,
                DateOfBirth: DateOnly.FromDateTime(
                    dtpBirth.Value.Date),
                Phone: txtPhone.Text,
                Email: email,
                Address: txtAddress.Text,
                PhotoPath: photoName
            );

            if (_studentId.HasValue)
            {
                await _studentService.UpdateAsync(
                    _studentId.Value, request);
            }
            else
            {
                await _studentService.CreateAsync(request);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            // Remove a newly copied file if saving failed.
            if (newlySavedPhoto is not null)
            {
                try
                {
                    StudentPhotoStorage.DeletePhoto(
                        newlySavedPhoto);
                }
                catch
                {
                    // Log cleanup failures in a future logging service.
                }
            }

            lblError.Text = ex is ArgumentException
                or InvalidOperationException
                ? ex.Message
                : "Unable to save student information.";
        }
        finally
        {
            _saving = false;
            btnSave.Enabled = true;
        }
    }
}
