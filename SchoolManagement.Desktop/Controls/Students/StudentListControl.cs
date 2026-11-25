using System;
using System.Collections.Generic;
using System.Text;


using SchoolManagement.Application.Students.DTOs;
using SchoolManagement.Application.Students.Interfaces;
using System.Drawing;
using System.Windows.Forms;

namespace SchoolManagement.Desktop.Controls.Students;

public sealed class StudentListControl : UserControl
{
    private readonly IStudentService _studentService;

    private readonly DataGridView dgvStudents = new();
    private readonly TextBox txtSearch = new();
    private readonly Button btnAdd = new();
    private readonly Button btnEdit = new();
    private readonly Button btnDeactivate = new();
    private readonly Button btnRefresh = new();
    private readonly Label lblStatus = new();

    private bool _loading;

    public StudentListControl(IStudentService studentService)
    {
        _studentService = studentService;

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(245, 247, 251);
        Font = new Font("Segoe UI", 10);

        BuildLayout();
        Disposer();


        Load += StudentListControl_Load;
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            ColumnCount = 1,
            RowCount = 3
        };

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 70));
        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 40));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.White,
            Padding = new Padding(10)
        };

        txtSearch.Width = 250;
        txtSearch.PlaceholderText = "Search code or name...";
        txtSearch.Margin = new Padding(5, 8, 10, 5);

        ConfigureButton(btnAdd, "Add Student", 120);
        ConfigureButton(btnEdit, "Edit", 85);
        ConfigureButton(btnDeactivate, "Deactivate", 115);
        ConfigureButton(btnRefresh, "Refresh", 90);

        toolbar.Controls.AddRange(new Control[]
        {
            txtSearch,
            btnAdd,
            btnEdit,
            btnDeactivate,
            btnRefresh
        });

        ConfigureGrid();

        lblStatus.Text = "Ready";
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        lblStatus.ForeColor = Color.Gray;

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(dgvStudents, 0, 1);
        root.Controls.Add(lblStatus, 0, 2);

        Controls.Add(root);

        btnAdd.Click += BtnAdd_Click;
        btnEdit.Click += BtnEdit_Click;
        btnDeactivate.Click += BtnDeactivate_Click;
        btnRefresh.Click += async (_, _) => await LoadStudentsAsync();


        _searchTimer.Tick += async (_, _) =>
        {
            _searchTimer.Stop();
            await LoadStudentsAsync();
        };

        txtSearch.TextChanged += (_, _) =>
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        };


        dgvStudents.CellDoubleClick += (_, _) =>
        {
            EditSelectedStudent();
        };
    }

    private static void ConfigureButton(
        Button button,
        string text,
        int width)
    {
        button.Text = text;
        button.Size = new Size(width, 36);
        button.Margin = new Padding(4);
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = Color.FromArgb(37, 99, 235);
        button.ForeColor = Color.White;
        button.FlatAppearance.BorderSize = 0;
        button.Cursor = Cursors.Hand;
    }

    private void ConfigureGrid()
    {
        dgvStudents.Dock = DockStyle.Fill;
        dgvStudents.BackgroundColor = Color.White;
        dgvStudents.BorderStyle = BorderStyle.None;

        dgvStudents.AllowUserToAddRows = false;
        dgvStudents.AllowUserToDeleteRows = false;
        dgvStudents.ReadOnly = true;

        dgvStudents.MultiSelect = false;
        dgvStudents.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;

        dgvStudents.AutoGenerateColumns = false;
        dgvStudents.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;

        dgvStudents.RowHeadersVisible = false;
        dgvStudents.RowTemplate.Height = 35;

        dgvStudents.ColumnHeadersHeight = 42;
        dgvStudents.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        dgvStudents.EnableHeadersVisualStyles = false;
        dgvStudents.ColumnHeadersDefaultCellStyle.BackColor =
            Color.FromArgb(30, 58, 95);
        dgvStudents.ColumnHeadersDefaultCellStyle.ForeColor =
            Color.White;

        AddColumn("Student Code", "StudentCode");
        AddColumn("Full Name", "FullName");
        AddColumn("Gender", "Gender");
        AddColumn("Date of Birth", "DateOfBirth");
        AddColumn("Phone", "Phone");
        AddColumn("Email", "Email");
        AddColumn("Status", "Status");
    }

    private void AddColumn(string header, string property)
    {
        dgvStudents.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                DataPropertyName = property,
                Name = property
            });
    }

    private async void StudentListControl_Load(
        object? sender, EventArgs e)
    {
        await LoadStudentsAsync();
    }


    private int _searchVersion = 0;

    private async Task LoadStudentsAsync()
    {
        int currentVersion = ++_searchVersion;
        string keyword = txtSearch.Text.Trim();

        lblStatus.Text = "Searching...";

        try
        {
            var students = await _studentService
                .GetStudentsAsync(keyword);

            if (IsDisposed || currentVersion != _searchVersion)
                return;

            dgvStudents.DataSource = students.ToList();

            lblStatus.Text =
                $"Total displayed students: {students.Count}";
        }
        catch (Exception)
        {
            if (!IsDisposed && currentVersion == _searchVersion)
            {
                lblStatus.Text = "Unable to load students.";
            }
        }
    }


    private StudentDto? GetSelectedStudent()
    {
        return dgvStudents.CurrentRow?.DataBoundItem
            as StudentDto;
    }

    private async void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var form = new StudentEditForm(_studentService);

        if (form.ShowDialog(FindForm()) == DialogResult.OK)
            await LoadStudentsAsync();
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        EditSelectedStudent();
    }

    private void EditSelectedStudent()
    {
        var selected = GetSelectedStudent();

        if (selected is null)
        {
            MessageBox.Show(
                "Please select a student.");
            return;
        }

        using var form = new StudentEditForm(
            _studentService,
            selected.StudentId);

        if (form.ShowDialog(FindForm()) == DialogResult.OK)
            _ = LoadStudentsAsync();
    }

    private async void BtnDeactivate_Click(
        object? sender,
        EventArgs e)
    {
        var selected = GetSelectedStudent();

        if (selected is null)
        {
            MessageBox.Show(
                "Please select a student.");
            return;
        }

        if (selected.Status == "Inactive")
        {
            MessageBox.Show(
                "This student is already inactive.");
            return;
        }

        var confirmation = MessageBox.Show(
            $"Deactivate student {selected.FullName}?",
            "Confirm Deactivation",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirmation != DialogResult.Yes)
            return;

        btnDeactivate.Enabled = false;

        try
        {
            await _studentService.DeactivateAsync(
                selected.StudentId);

            await LoadStudentsAsync();
        }
        catch (Exception)
        {
            MessageBox.Show(
                "Unable to deactivate the student.",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            btnDeactivate.Enabled = true;
        }
    }


    private readonly System.Windows.Forms.Timer _searchTimer = new()
    {
        Interval = 300
    };


    private void Disposer()
    {
        Disposed += (_, _) =>
        {
            _searchTimer.Stop();
            _searchTimer.Dispose();
        };
    }

}
