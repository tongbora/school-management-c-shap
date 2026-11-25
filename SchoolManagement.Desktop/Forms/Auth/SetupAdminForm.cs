
using SchoolManagement.Application.Authentication.DTOs;
using SchoolManagement.Infrastructure.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SchoolManagement.Desktop.Forms.Auth;

public partial class SetupAdminForm : Form
{
    private readonly AdminSetupService _adminSetupService;
    private bool _isSubmitting = false;

    public SetupAdminForm(AdminSetupService adminSetupService)
    {
        InitializeComponent();

        _adminSetupService = adminSetupService
            ?? throw new ArgumentNullException(
                nameof(adminSetupService));

        // Form configuration
        Text = "School Management - Administrator Setup";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        // Password configuration
        txtPassword.UseSystemPasswordChar = true;
        txtConfirmPassword.UseSystemPasswordChar = true;

        // Error label
        lblError.Text = string.Empty;
        lblError.ForeColor = Color.Red;
        lblError.AutoSize = true;

        // Default button
        AcceptButton = btnCreateAdmin;

        // Events
        // Connect btnCreateAdmin_Click through the Designer,
        // not here, to avoid duplicate subscriptions.
    }

    private void SetupAdminForm_Load(
        object? sender,
        EventArgs e)
    {
        txtFullName.Focus();
        lblError.Text = string.Empty;
    }

    private async void btnCreateAdmin_Click(
        object? sender,
        EventArgs e)
    {
        if (_isSubmitting)
            return;

        lblError.Text = string.Empty;

        string fullName = txtFullName.Text.Trim();
        string username = txtUsername.Text.Trim();
        string email = txtEmail.Text.Trim();
        string password = txtPassword.Text;
        string confirmPassword = txtConfirmPassword.Text;

        // Validate full name
        if (string.IsNullOrWhiteSpace(fullName))
        {
            ShowError("Full name is required.");
            txtFullName.Focus();
            return;
        }

        if (fullName.Length > 150)
        {
            ShowError("Full name cannot exceed 150 characters.");
            return;
        }

        // Validate username
        if (string.IsNullOrWhiteSpace(username))
        {
            ShowError("Username is required.");
            txtUsername.Focus();
            return;
        }

        if (username.Length > 100)
        {
            ShowError("Username cannot exceed 100 characters.");
            return;
        }

        // Validate email
        if (!string.IsNullOrWhiteSpace(email) &&
            !System.Net.Mail.MailAddress.TryCreate(
                email, out _))
        {
            ShowError("Please enter a valid email address.");
            txtEmail.Focus();
            return;
        }

        if (email.Length > 150)
        {
            ShowError("Email cannot exceed 150 characters.");
            return;
        }

        // Validate password
        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError("Password is required.");
            txtPassword.Focus();
            return;
        }

        if (password.Length < 12 || password.Length > 128)
        {
            ShowError(
                "Password must contain 12 to 128 characters.");
            txtPassword.Focus();
            return;
        }

        // Confirm password
        if (password != confirmPassword)
        {
            ShowError("Passwords do not match.");
            txtConfirmPassword.Focus();
            return;
        }

        _isSubmitting = true;
        btnCreateAdmin.Enabled = false;

        try
        {
            var request = new CreateAdminRequest(
                FullName: fullName,
                Username: username,
                Email: string.IsNullOrWhiteSpace(email)
                    ? null
                    : email,
                Password: password
            );

            await _adminSetupService
                .CreateAdminAsync(request);

            MessageBox.Show(
                this,
                "Administrator account created successfully.",
                "Setup Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ArgumentException ex)
        {
            ShowError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception)
        {
            ShowError(
                "Unable to create the administrator account. " +
                "Please check the database connection.");
        }
        finally
        {
            txtPassword.Clear();
            txtConfirmPassword.Clear();

            _isSubmitting = false;
            btnCreateAdmin.Enabled = true;
        }
    }

    private void ShowError(string message)
    {
        lblError.Text = message;
        lblError.ForeColor = Color.Red;
    }
}
