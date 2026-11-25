
using System.Drawing;
using System.Windows.Forms;
using SchoolManagement.Application.Academics.DTOs;
using SchoolManagement.Application.Academics.Interfaces;

namespace SchoolManagement.Desktop.Controls.Academics;

public sealed class ClassSubjectListControl : UserControl
{
    private readonly IClassSubjectService _service;

    private readonly DataGridView grid = new();
    private readonly TextBox txtSearch = new();
    private readonly Label lblCount = new();

    private readonly Button btnAdd = new();
    private readonly Button btnEdit = new();
    private readonly Button btnRemove = new();
    private readonly Button btnRefresh = new();

    private readonly System.Windows.Forms.Timer searchTimer =
        new() { Interval = 300 };

    private int _loadVersion;
    private bool _busy;

    public ClassSubjectListControl(
        IClassSubjectService service)
    {
        _service = service;

        Dock = DockStyle.Fill;
        BackColor = Color.White;

        BuildUi();

        searchTimer.Tick += async (_, _) =>
        {
            searchTimer.Stop();
            await LoadAssignmentsAsync();
        };

        txtSearch.TextChanged += (_, _) =>
        {
            searchTimer.Stop();
            _loadVersion++;
            searchTimer.Start();
        };

        Load += async (_, _) =>
            await LoadAssignmentsAsync();

        grid.SelectionChanged += (_, _) =>
            UpdateButtons();
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

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 52));

        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 30));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        txtSearch.Width = 240;
        txtSearch.PlaceholderText =
            "Search class, subject, teacher...";

        ConfigureButton(btnAdd, "Assign Subject");
        ConfigureButton(btnEdit, "Edit");
        ConfigureButton(btnRemove, "Remove");
        ConfigureButton(btnRefresh, "Refresh");

        btnAdd.Width = 130;
        btnEdit.Width = 80;
        btnRemove.Width = 95;
        btnRefresh.Width = 90;

        btnRemove.BackColor =
            Color.FromArgb(220, 38, 38);

        toolbar.Controls.AddRange(new Control[]
        {
            txtSearch,
            btnAdd,
            btnEdit,
            btnRemove,
            btnRefresh
        });

        grid.Dock = DockStyle.Fill;
        grid.BackgroundColor = Color.White;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;

        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.ReadOnly = true;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;

        grid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;

        AddColumn("AcademicYearName", "Academic Year");
        AddColumn("ClassName", "Class");
        AddColumn("SubjectCode", "Subject Code");
        AddColumn("SubjectName", "Subject");
        AddColumn("TeacherName", "Teacher");

        lblCount.Text = "Total assignments: 0";
        lblCount.Dock = DockStyle.Fill;
        lblCount.ForeColor = Color.DimGray;
        lblCount.TextAlign = ContentAlignment.MiddleLeft;

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(grid, 0, 1);
        root.Controls.Add(lblCount, 0, 2);

        Controls.Add(root);

        btnAdd.Click += async (_, _) =>
            await AddAsync();

        btnEdit.Click += async (_, _) =>
            await EditAsync();

        btnRemove.Click += async (_, _) =>
            await RemoveAsync();

        btnRefresh.Click += async (_, _) =>
            await LoadAssignmentsAsync();

        grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
                await EditAsync();
        };

        UpdateButtons();
    }

    private static void ConfigureButton(
        Button button,
        string text)
    {
        button.Text = text;
        button.Height = 32;
        button.BackColor = Color.FromArgb(37, 99, 235);
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.UseVisualStyleBackColor = false;
        button.Cursor = Cursors.Hand;
    }

    private void AddColumn(
        string property,
        string header)
    {
        grid.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                Name = property,
                HeaderText = header,
                DataPropertyName = property
            });
    }

    private ClassSubjectDto? SelectedAssignment()
    {
        return grid.CurrentRow?.DataBoundItem
            as ClassSubjectDto;
    }

    private void UpdateButtons()
    {
        bool selected = SelectedAssignment() is not null;

        btnAdd.Enabled = !_busy;
        btnEdit.Enabled = !_busy && selected;
        btnRemove.Enabled = !_busy && selected;
        btnRefresh.Enabled = !_busy;
    }

    private async Task LoadAssignmentsAsync()
    {
        int version = ++_loadVersion;

        try
        {
            var data = await _service.GetAllAsync(
                txtSearch.Text.Trim());

            if (IsDisposed || version != _loadVersion)
                return;

            grid.DataSource = data.ToList();

            lblCount.Text =
                $"Total assignments: {data.Count}";

            UpdateButtons();
        }
        catch (Exception ex)
        {
            if (IsDisposed || version != _loadVersion)
                return;

            MessageBox.Show(
                this, ex.Message,
                "Load Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async Task AddAsync()
    {
        if (_busy)
            return;

        using var form = new ClassSubjectEditForm(_service);

        if (form.ShowDialog(this) == DialogResult.OK)
            await LoadAssignmentsAsync();
    }

    private async Task EditAsync()
    {
        if (_busy)
            return;

        var assignment = SelectedAssignment();

        if (assignment is null)
        {
            MessageBox.Show("Select an assignment first.");
            return;
        }

        using var form = new ClassSubjectEditForm(
            _service,
            assignment.ClassSubjectId);

        if (form.ShowDialog(this) == DialogResult.OK)
            await LoadAssignmentsAsync();
    }

    private async Task RemoveAsync()
    {
        if (_busy)
            return;

        var assignment = SelectedAssignment();

        if (assignment is null)
            return;

        var result = MessageBox.Show(
            this,
            $"Remove {assignment.SubjectName} from " +
            $"{assignment.ClassName}?\n\n" +
            "This action cannot be undone.",
            "Confirm Removal",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes)
            return;

        _busy = true;
        UpdateButtons();

        try
        {
            await _service.DeleteAsync(
                assignment.ClassSubjectId);

            await LoadAssignmentsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this, ex.Message,
                "Remove Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _busy = false;

            if (!IsDisposed)
                UpdateButtons();
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
