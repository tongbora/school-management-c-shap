using System;
using System.Collections.Generic;
using System.Text;


using SchoolManagement.Application.Teachers.DTOs;
using SchoolManagement.Application.Teachers.Interfaces;

namespace SchoolManagement.Desktop.Controls.Teachers;

public sealed class TeacherListControl : UserControl
{
    private readonly ITeacherService _service;

    private readonly DataGridView grid = new();
    private readonly TextBox txtSearch = new();
    private readonly Button btnAdd = new();
    private readonly Button btnEdit = new();
    private readonly Button btnDeactivate = new();
    private readonly Button btnRefresh = new();
    private readonly Label lblCount = new();

    private readonly System.Windows.Forms.Timer searchTimer = new()
    {
        Interval = 300
    };

    private int searchVersion;

    public TeacherListControl(ITeacherService service)
    {
        _service = service;

        Dock = DockStyle.Fill;
        BackColor = Color.White;

        BuildUi();

        searchTimer.Tick += async (_, _) =>
        {
            searchTimer.Stop();
            await LoadTeachersAsync();
        };

        txtSearch.TextChanged += (_, _) =>
        {
            searchTimer.Stop();
            searchTimer.Start();
        };

        Load += async (_, _) =>
            await LoadTeachersAsync();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(12),
            BackColor = Color.FromArgb(248, 250, 252)
        };

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 28));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        txtSearch.Width = 220;
        txtSearch.PlaceholderText =
            "Search teacher code or name...";

        ConfigureButton(btnAdd, "Add");
        ConfigureButton(btnEdit, "Edit");
        ConfigureButton(btnDeactivate, "Deactivate");
        ConfigureButton(btnRefresh, "Refresh");

        toolbar.Controls.Add(txtSearch);
        toolbar.Controls.Add(btnAdd);
        toolbar.Controls.Add(btnEdit);
        toolbar.Controls.Add(btnDeactivate);
        toolbar.Controls.Add(btnRefresh);

        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.MultiSelect = false;
        grid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowHeadersVisible = false;
        grid.BackgroundColor = Color.White;
        grid.AutoGenerateColumns = false;

        AddColumn("TeacherCode", "Teacher Code");
        AddColumn("FullName", "Full Name");
        AddColumn("Gender", "Gender");
        AddColumn("Phone", "Phone");
        AddColumn("Email", "Email");
        AddColumn("SubjectSpecialty", "Specialty");
        AddColumn("HireDate", "Hire Date");
        AddColumn("Status", "Status");

        lblCount.Text = "Total displayed teachers: 0";
        lblCount.ForeColor = Color.DimGray;
        lblCount.Dock = DockStyle.Fill;
        lblCount.TextAlign = ContentAlignment.MiddleLeft;

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(grid, 0, 1);
        root.Controls.Add(lblCount, 0, 2);

        Controls.Add(root);

        btnAdd.Click += async (_, _) =>
            await AddTeacherAsync();

        btnEdit.Click += async (_, _) =>
            await EditTeacherAsync();

        btnDeactivate.Click += async (_, _) =>
            await DeactivateTeacherAsync();

        btnRefresh.Click += async (_, _) =>
            await LoadTeachersAsync();

        grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
                await EditTeacherAsync();
        };
    }

    private static void ConfigureButton(
        Button button, string text)
    {
        button.Text = text;
        button.Width = text == "Deactivate" ? 100 : 78;
        button.Height = 30;
        button.BackColor = Color.FromArgb(37, 99, 235);
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
    }

    private void AddColumn(
        string property,
        string header)
    {
        grid.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                DataPropertyName = property,
                HeaderText = header,
                Name = property
            });
    }

    private async Task LoadTeachersAsync()
    {
        int version = ++searchVersion;
        string keyword = txtSearch.Text.Trim();

        try
        {
            var teachers =
                await _service.GetTeachersAsync(keyword);

            if (version != searchVersion || IsDisposed)
                return;

            grid.DataSource = teachers.ToList();

            lblCount.Text =
                $"Total displayed teachers: {teachers.Count}";
        }
        catch (Exception ex)
        {
            if (version != searchVersion || IsDisposed)
                return;

            MessageBox.Show(
                ex.Message,
                "Load Teachers Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private TeacherDto? SelectedTeacher()
    {
        return grid.CurrentRow?.DataBoundItem
            as TeacherDto;
    }

    private async Task AddTeacherAsync()
    {
        using var form = new TeacherEditForm(_service);

        if (form.ShowDialog(this) == DialogResult.OK)
            await LoadTeachersAsync();
    }

    private async Task EditTeacherAsync()
    {
        var teacher = SelectedTeacher();

        if (teacher is null)
        {
            MessageBox.Show("Please select a teacher.");
            return;
        }

        using var form = new TeacherEditForm(
            _service,
            teacher.TeacherId);

        if (form.ShowDialog(this) == DialogResult.OK)
            await LoadTeachersAsync();
    }

    private async Task DeactivateTeacherAsync()
    {
        var teacher = SelectedTeacher();

        if (teacher is null)
        {
            MessageBox.Show("Please select a teacher.");
            return;
        }

        if (teacher.Status == "Inactive")
        {
            MessageBox.Show("Teacher is already inactive.");
            return;
        }

        var confirm = MessageBox.Show(
            $"Deactivate {teacher.FullName}?",
            "Confirm Deactivation",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            await _service.DeactivateAsync(
                teacher.TeacherId);

            await LoadTeachersAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Deactivation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            searchTimer.Stop();
            searchTimer.Dispose();
        }

        base.Dispose(disposing);
    }
}
