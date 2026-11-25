
using System.Drawing;
using System.Windows.Forms;
using SchoolManagement.Application.Academics.DTOs;
using SchoolManagement.Application.Academics.Interfaces;

namespace SchoolManagement.Desktop.Controls.Academics;

public sealed class DepartmentListControl : UserControl
{
    private readonly IDepartmentService _service;

    private readonly DataGridView grid = new();
    private readonly TextBox txtSearch = new();
    private readonly Label lblCount = new();

    private readonly Button btnAdd = new();
    private readonly Button btnEdit = new();
    private readonly Button btnStatus = new();
    private readonly Button btnRefresh = new();

    private readonly System.Windows.Forms.Timer timer =
        new() { Interval = 300 };

    private int _loadVersion;

    public DepartmentListControl(IDepartmentService service)
    {
        _service = service;

        Dock = DockStyle.Fill;

        BuildUi();

        timer.Tick += async (_, _) =>
        {
            timer.Stop();
            await LoadDepartmentsAsync();
        };

        txtSearch.TextChanged += (_, _) =>
        {
            timer.Stop();
            _loadVersion++;
            timer.Start();
        };

        Load += async (_, _) =>
            await LoadDepartmentsAsync();

        grid.SelectionChanged += (_, _) =>
            UpdateStatusButton();
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
            new RowStyle(SizeType.Absolute, 50));
        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 30));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false
        };

        txtSearch.Width = 230;
        txtSearch.PlaceholderText =
            "Search department name...";

        ConfigureButton(btnAdd, "Add");
        ConfigureButton(btnEdit, "Edit");
        ConfigureButton(btnStatus, "Deactivate");
        ConfigureButton(btnRefresh, "Refresh");

        toolbar.Controls.AddRange(new Control[]
        {
            txtSearch, btnAdd, btnEdit,
            btnStatus, btnRefresh
        });

        grid.Dock = DockStyle.Fill;
        grid.BackgroundColor = Color.White;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;

        AddColumn("DepartmentName", "Department Name");
        AddColumn("Description", "Description");

        grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "IsActive",
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
        btnStatus.Click += async (_, _) =>
            await ChangeStatusAsync();
        btnRefresh.Click += async (_, _) =>
            await LoadDepartmentsAsync();

        grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
                await EditAsync();
        };
    }

    private static void ConfigureButton(
        Button button, string label)
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
            Name = property,
            DataPropertyName = property,
            HeaderText = heading
        });
    }

    private async Task LoadDepartmentsAsync()
    {
        int version = ++_loadVersion;

        try
        {
            var data = await _service.GetAllAsync(
                txtSearch.Text);

            if (IsDisposed || version != _loadVersion)
                return;

            grid.DataSource = data.ToList();
            lblCount.Text =
                $"Total displayed departments: {data.Count}";

            UpdateStatusButton();
        }
        catch (Exception ex)
        {
            if (IsDisposed || version != _loadVersion)
                return;

            MessageBox.Show(
                ex.Message,
                "Load Departments Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private DepartmentDto? SelectedDepartment()
    {
        return grid.CurrentRow?.DataBoundItem
            as DepartmentDto;
    }

    private void UpdateStatusButton()
    {
        var selected = SelectedDepartment();

        btnStatus.Enabled = selected is not null;
        btnStatus.Text = selected?.IsActive == false
            ? "Activate"
            : "Deactivate";
    }

    private async Task AddAsync()
    {
        using var form = new DepartmentEditForm(_service);

        if (form.ShowDialog(this) == DialogResult.OK)
            await LoadDepartmentsAsync();
    }

    private async Task EditAsync()
    {
        var selected = SelectedDepartment();

        if (selected is null)
        {
            MessageBox.Show("Select a department first.");
            return;
        }

        using var form = new DepartmentEditForm(
            _service, selected.DepartmentId);

        if (form.ShowDialog(this) == DialogResult.OK)
            await LoadDepartmentsAsync();
    }

    private async Task ChangeStatusAsync()
    {
        var selected = SelectedDepartment();

        if (selected is null)
            return;

        bool newStatus = !selected.IsActive;
        string action = newStatus ? "activate" : "deactivate";

        var result = MessageBox.Show(
            $"Do you want to {action} {selected.DepartmentName}?",
            "Confirm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        btnStatus.Enabled = false;

        try
        {
            await _service.SetActiveAsync(
                selected.DepartmentId, newStatus);

            await LoadDepartmentsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Update Status Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
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
