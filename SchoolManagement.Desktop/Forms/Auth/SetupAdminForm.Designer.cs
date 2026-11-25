
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SchoolManagement.Desktop.Forms.Auth;

partial class SetupAdminForm
{
    private IContainer? components = null;

    private Label lblTitle = null!;
    private Label lblFullName = null!;
    private Label lblUsername = null!;
    private Label lblEmail = null!;
    private Label lblPassword = null!;
    private Label lblConfirmPassword = null!;
    private Label lblError = null!;

    private TextBox txtFullName = null!;
    private TextBox txtUsername = null!;
    private TextBox txtEmail = null!;
    private TextBox txtPassword = null!;
    private TextBox txtConfirmPassword = null!;

    private Button btnCreateAdmin = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblTitle = new Label();
        lblFullName = new Label();
        lblUsername = new Label();
        lblEmail = new Label();
        lblPassword = new Label();
        lblConfirmPassword = new Label();
        lblError = new Label();

        txtFullName = new TextBox();
        txtUsername = new TextBox();
        txtEmail = new TextBox();
        txtPassword = new TextBox();
        txtConfirmPassword = new TextBox();

        btnCreateAdmin = new Button();

        SuspendLayout();

        // Main form configuration
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(520, 610);

        Name = "SetupAdminForm";
        Text = "School Management - Administrator Setup";
        BackColor = Color.White;
        Font = new Font("Segoe UI", 10F);

        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        // Title
        lblTitle.Name = "lblTitle";
        lblTitle.Text = "Create Administrator Account";
        lblTitle.Font = new Font(
            "Segoe UI",
            17F,
            FontStyle.Bold
        );
        lblTitle.ForeColor = Color.FromArgb(30, 58, 95);
        lblTitle.Location = new Point(45, 30);
        lblTitle.Size = new Size(430, 45);
        lblTitle.TextAlign = ContentAlignment.MiddleCenter;

        // Full name label
        lblFullName.Name = "lblFullName";
        lblFullName.Text = "Full Name";
        lblFullName.Location = new Point(55, 100);
        lblFullName.AutoSize = true;

        // Full name textbox
        txtFullName.Name = "txtFullName";
        txtFullName.Location = new Point(55, 125);
        txtFullName.Size = new Size(410, 30);
        txtFullName.TabIndex = 0;
        txtFullName.MaxLength = 150;

        // Username label
        lblUsername.Name = "lblUsername";
        lblUsername.Text = "Username";
        lblUsername.Location = new Point(55, 170);
        lblUsername.AutoSize = true;

        // Username textbox
        txtUsername.Name = "txtUsername";
        txtUsername.Location = new Point(55, 195);
        txtUsername.Size = new Size(410, 30);
        txtUsername.TabIndex = 1;
        txtUsername.MaxLength = 100;

        // Email label
        lblEmail.Name = "lblEmail";
        lblEmail.Text = "Email (Optional)";
        lblEmail.Location = new Point(55, 240);
        lblEmail.AutoSize = true;

        // Email textbox
        txtEmail.Name = "txtEmail";
        txtEmail.Location = new Point(55, 265);
        txtEmail.Size = new Size(410, 30);
        txtEmail.TabIndex = 2;
        txtEmail.MaxLength = 150;

        // Password label
        lblPassword.Name = "lblPassword";
        lblPassword.Text = "Password";
        lblPassword.Location = new Point(55, 310);
        lblPassword.AutoSize = true;

        // Password textbox
        txtPassword.Name = "txtPassword";
        txtPassword.Location = new Point(55, 335);
        txtPassword.Size = new Size(410, 30);
        txtPassword.TabIndex = 3;
        txtPassword.MaxLength = 128;
        txtPassword.UseSystemPasswordChar = true;

        // Confirm password label
        lblConfirmPassword.Name = "lblConfirmPassword";
        lblConfirmPassword.Text = "Confirm Password";
        lblConfirmPassword.Location = new Point(55, 380);
        lblConfirmPassword.AutoSize = true;

        // Confirm password textbox
        txtConfirmPassword.Name = "txtConfirmPassword";
        txtConfirmPassword.Location = new Point(55, 405);
        txtConfirmPassword.Size = new Size(410, 30);
        txtConfirmPassword.TabIndex = 4;
        txtConfirmPassword.MaxLength = 128;
        txtConfirmPassword.UseSystemPasswordChar = true;

        // Error label
        lblError.Name = "lblError";
        lblError.Text = string.Empty;
        lblError.ForeColor = Color.Firebrick;
        lblError.Location = new Point(55, 455);
        lblError.Size = new Size(410, 45);
        lblError.TextAlign = ContentAlignment.MiddleLeft;

        // Create Administrator button
        btnCreateAdmin.Name = "btnCreateAdmin";
        btnCreateAdmin.Text = "Create Administrator";
        btnCreateAdmin.Location = new Point(55, 520);
        btnCreateAdmin.Size = new Size(410, 45);
        btnCreateAdmin.TabIndex = 5;

        btnCreateAdmin.BackColor =
            Color.FromArgb(37, 99, 235);

        btnCreateAdmin.ForeColor = Color.White;
        btnCreateAdmin.FlatStyle = FlatStyle.Flat;
        btnCreateAdmin.FlatAppearance.BorderSize = 0;
        btnCreateAdmin.Cursor = Cursors.Hand;

        btnCreateAdmin.Font = new Font(
            "Segoe UI",
            11F,
            FontStyle.Bold
        );

        // Event handlers
        Load += SetupAdminForm_Load;
        btnCreateAdmin.Click += btnCreateAdmin_Click;

        // Add controls
        Controls.Add(lblTitle);

        Controls.Add(lblFullName);
        Controls.Add(txtFullName);

        Controls.Add(lblUsername);
        Controls.Add(txtUsername);

        Controls.Add(lblEmail);
        Controls.Add(txtEmail);

        Controls.Add(lblPassword);
        Controls.Add(txtPassword);

        Controls.Add(lblConfirmPassword);
        Controls.Add(txtConfirmPassword);

        Controls.Add(lblError);
        Controls.Add(btnCreateAdmin);

        ResumeLayout(false);
        PerformLayout();
    }
}
