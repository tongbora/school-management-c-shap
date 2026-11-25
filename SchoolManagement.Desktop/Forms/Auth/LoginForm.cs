
using SchoolManagement.Application.Authentication.DTOs;
using SchoolManagement.Application.Authentication.Interfaces;
using System.Drawing;
using System.Windows.Forms;

namespace SchoolManagement.Desktop.Forms.Auth;

public sealed class LoginForm : Form
{
    private readonly IAuthService _authService;

    private readonly TextBox txtUsername = new();
    private readonly TextBox txtPassword = new();
    private readonly Label lblError = new();
    private readonly Button btnLogin = new();

    private bool _isSubmitting;

    public LoginResult? AuthenticatedUser { get; private set; }

    public LoginForm(IAuthService authService)
    {
        _authService = authService
            ?? throw new ArgumentNullException(nameof(authService));

        Text = "School Management - Login";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(460, 390);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.White;

        var title = new Label
        {
            Text = "School Management System",
            Font = new Font("Segoe UI", 17F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 58, 95),
            Location = new Point(35, 30),
            Size = new Size(390, 50),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var usernameLabel = new Label
        {
            Text = "Username",
            Location = new Point(50, 105),
            AutoSize = true
        };

        txtUsername.Location = new Point(50, 135);
        txtUsername.Size = new Size(360, 30);
        txtUsername.MaxLength = 100;

        var passwordLabel = new Label
        {
            Text = "Password",
            Location = new Point(50, 185),
            AutoSize = true
        };

        txtPassword.Location = new Point(50, 215);
        txtPassword.Size = new Size(360, 30);
        txtPassword.UseSystemPasswordChar = true;

        var showPassword = new CheckBox
        {
            Text = "Show password",
            Location = new Point(50, 260),
            AutoSize = true
        };

        showPassword.CheckedChanged += (_, _) =>
        {
            txtPassword.UseSystemPasswordChar =
                !showPassword.Checked;
        };

        lblError.Location = new Point(50, 290);
        lblError.Size = new Size(360, 35);
        lblError.ForeColor = Color.Firebrick;

        btnLogin.Text = "Login";
        btnLogin.Location = new Point(50, 335);
        btnLogin.Size = new Size(360, 42);
        btnLogin.BackColor = Color.FromArgb(37, 99, 235);
        btnLogin.ForeColor = Color.White;
        btnLogin.FlatStyle = FlatStyle.Flat;
        btnLogin.FlatAppearance.BorderSize = 0;

        btnLogin.Click += BtnLogin_Click;

        Controls.AddRange(new Control[]
        {
            title,
            usernameLabel,
            txtUsername,
            passwordLabel,
            txtPassword,
            showPassword,
            lblError,
            btnLogin
        });

        AcceptButton = btnLogin;
    }

    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        if (_isSubmitting)
            return;

        lblError.Text = "";

        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            lblError.Text = "Enter username and password.";
            return;
        }

        _isSubmitting = true;
        btnLogin.Enabled = false;

        try
        {
            var request = new LoginRequest(username, password);

            var result = await _authService.LoginAsync(request);

            if (!result.IsSuccess)
            {
                lblError.Text =
                    result.ErrorMessage ?? "Login failed.";
                return;
            }

            AuthenticatedUser = result;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception)
        {
            lblError.Text =
                "Unable to sign in. Please try again.";
        }
        finally
        {
            txtPassword.Clear();
            _isSubmitting = false;
            btnLogin.Enabled = true;
        }
    }
}
