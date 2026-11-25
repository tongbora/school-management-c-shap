
using System.Drawing;
using System.Windows.Forms;
using SchoolManagement.Application.Academics.DTOs;
using SchoolManagement.Application.Academics.Interfaces;

namespace SchoolManagement.Desktop.Controls.Academics;

public sealed class SubjectListControl : UserControl
{
    private readonly ISubjectService _service;

    private readonly DataGridView grid = new();
    private readonly TextBox txtSearch = new();
    private readonly Label lblCount = new();

    private readonly Button btnAdd = new();
    private readonly Button btnEdit = new();
    private readonly Button btnStatus = new();
    private readonly Button btnRefresh = new();

    private readonly System.Windows.Forms.Timer timer =
        new() { Interval = 300 };

    private int _version;

    public SubjectListControl(ISubjectService service)
    {
        _service = service;
        Dock = DockStyle.Fill;

        BuildUi();

        timer.Tick += async (_, _) =>
        {
            timer.Stop();
            await LoadSubjectsAsync();
        };

        txtSearch.TextChanged += (_, _) =>
        {
            timer.Stop();
            _version++;
            timer.Start();
        };

        Load += async (_, _) => await LoadSubjectsAsync();
        grid.SelectionChanged += (_, _) => UpdateStatusButton();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12),
            BackColor = Color.FromArgb(248, 250, 252)
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false
        };

        txtSearch.Width = 220;
        txtSearch.PlaceholderText = "Search code or name...";

        ConfigureButton(btnAdd, "Add");
        ConfigureButton(btnEdit, "Edit");
        ConfigureButton(btnStatus, "Deactivate");
        ConfigureButton(btnRefresh, "Refresh");

        toolbar.Controls.AddRange(new Control[]
        {
            txtSearch, btnAdd, btnEdit, btnStatus, btnRefresh
        });

        grid.Dock = DockStyle.Fill;
        grid.BackgroundColor = Color.White;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;
        grid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;

        AddColumn("SubjectCode", "Subject Code");
        AddColumn("SubjectName", "Subject Name");
        AddColumn("GradeLevel", "Grade Level");

        grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Active",
            DataPropertyName = "IsActive",
            FillWeight = 35
        });

        lblCount.Dock = DockStyle.Fill;
        lblCount.ForeColor = Color.DimGray;

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(grid, 0, 1);
        root.Controls.Add(lblCount, 0, 2);
        Controls.Add(root);

        btnAdd.Click += async (_, _) => await AddAsync();
        btnEdit.Click += async (_, _) => await EditAsync();
        btnStatus.Click += async (_, _) => await ToggleStatusAsync();
        btnRefresh.Click += async (_, _) => await LoadSubjectsAsync();

        grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
                await EditAsync();
        };
    }

    private static void ConfigureButton(Button button, string label)
    {
        button.Text = label;
        button.Width = label == "Deactivate" ? 115 : 85;
        button.Height = 32;
        button.BackColor = Color.FromArgb(37, 99, 235);
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.Cursor = Cursors.Hand;
    }

    private void AddColumn(string property, string heading)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = heading,
            DataPropertyName = property
        });
    }

    private async Task LoadSubjectsAsync()
    {
        int version = ++_version;

        try
        {
            var data = await _service.GetAllAsync(txtSearch.Text);

            if (IsDisposed || version != _version)
                return;

            grid.DataSource = data.ToList();
            lblCount.Text = $"Total displayed subjects: {data.Count}";
            UpdateStatusButton();
        }
        catch (Exception ex)
        {
            if (IsDisposed || version != _version)
                return;

            MessageBox.Show(ex.Message,
                "Load Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private SubjectDto? SelectedSubject() =>
        grid.CurrentRow?.DataBoundItem as SubjectDto;

    private void UpdateStatusButton()
    {
        var subject = SelectedSubject();
        btnStatus.Enabled = subject is not null;
        btnStatus.Text = subject?.IsActive == false
            ? "Activate"
            : "Deactivate";
    }

    private async Task AddAsync()
    {
        using var form = new SubjectEditForm(_service);

        if (form.ShowDialog(this) == DialogResult.OK)
            await LoadSubjectsAsync();
    }

    private async Task EditAsync()
    {
        var subject = SelectedSubject();

        if (subject is null)
        {
            MessageBox.Show("Select a subject first.");
            return;
        }

        using var form = new SubjectEditForm(
            _service, subject.SubjectId);

        if (form.ShowDialog(this) == DialogResult.OK)
            await LoadSubjectsAsync();
    }

    private async Task ToggleStatusAsync()
    {
        var subject = SelectedSubject();

        if (subject is null)
            return;

        bool active = !subject.IsActive;
        string action = active ? "activate" : "deactivate";

        if (MessageBox.Show(
            $"Do you want to {action} {subject.SubjectName}?",
            "Confirm", MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        btnStatus.Enabled = false;

        try
        {
            await _service.SetActiveAsync(subject.SubjectId, active);
            await LoadSubjectsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message,
                "Update Status Error", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            if (!IsDisposed)
                UpdateStatusButton();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            timer.Stop();
            timer.Dispose();
        }

        base.Dispose(disposing);
    }
}
