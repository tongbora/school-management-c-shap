
using SchoolManagement.Application.Academics.DTOs;
using SchoolManagement.Application.Academics.Interfaces;

namespace SchoolManagement.Desktop.Controls.Academics;

public sealed class AcademicYearListControl : UserControl
{
    private readonly IAcademicYearService _service;

    private readonly DataGridView grid = new();
    private readonly TextBox txtSearch = new();
    private readonly Button btnAdd = new();
    private readonly Button btnEdit = new();
    private readonly Button btnActivate = new();
    private readonly Button btnRefresh = new();
    private readonly Label lblCount = new();

    private readonly System.Windows.Forms.Timer searchTimer = new()
    {
        Interval = 300
    };

    private int _searchVersion;

    public AcademicYearListControl(IAcademicYearService service)
    {
        _service = service;

        Dock = DockStyle.Fill;
        BackColor = Color.White;

        BuildUI();

        searchTimer.Tick += async (_, _) =>
        {
            searchTimer.Stop();
            await LoadYearsAsync();
        };

        txtSearch.TextChanged += (_, _) =>
        {
            searchTimer.Stop();
            searchTimer.Start();
        };

        Load += async (_, _) => await LoadYearsAsync();
    }

    private void BuildUI()
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
            new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 28));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false
        };

        txtSearch.Width = 220;
        txtSearch.PlaceholderText = "Search academic year...";

        ConfigureButton(btnAdd, "Add");
        ConfigureButton(btnEdit, "Edit");
        ConfigureButton(btnActivate, "Set Active");
        ConfigureButton(btnRefresh, "Refresh");

        toolbar.Controls.AddRange(new Control[]
        {
            txtSearch, btnAdd, btnEdit, btnActivate, btnRefresh
        });

        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;
        grid.AutoGenerateColumns = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = Color.White;

        AddColumn("YearName", "Academic Year");
        AddColumn("StartDate", "Start Date");
        AddColumn("EndDate", "End Date");
        AddColumn("IsActive", "Active");

        lblCount.Dock = DockStyle.Fill;
        lblCount.ForeColor = Color.DimGray;
        lblCount.TextAlign = ContentAlignment.MiddleLeft;

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(grid, 0, 1);
        root.Controls.Add(lblCount, 0, 2);
        Controls.Add(root);

        btnAdd.Click += async (_, _) => await AddAsync();
        btnEdit.Click += async (_, _) => await EditAsync();
        btnActivate.Click += async (_, _) => await ActivateAsync();
        btnRefresh.Click += async (_, _) => await LoadYearsAsync();

        grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
                await EditAsync();
        };
    }

    private static void ConfigureButton(Button button, string label)
    {
        button.Text = label;
        button.Width = label == "Set Active" ? 105 : 85;
        button.Height = 30;
        button.BackColor = Color.FromArgb(37, 99, 235);
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
    }

    private void AddColumn(string property, string header)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = property,
            HeaderText = header,
            DataPropertyName = property
        });
    }

    private async Task LoadYearsAsync()
    {
        int version = ++_searchVersion;

        try
        {
            var data = await _service.GetAllAsync(txtSearch.Text);

            if (IsDisposed || version != _searchVersion)
                return;

            grid.DataSource = data.ToList();
            lblCount.Text = $"Total academic years: {data.Count}";
        }
        catch (Exception ex)
        {
            if (IsDisposed || version != _searchVersion)
                return;

            MessageBox.Show(
                ex.Message, "Load Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private AcademicYearDto? SelectedYear()
    {
        return grid.CurrentRow?.DataBoundItem as AcademicYearDto;
    }

    private async Task AddAsync()
    {
        using var form = new AcademicYearEditForm(_service);

        if (form.ShowDialog(this) == DialogResult.OK)
            await LoadYearsAsync();
    }

    private async Task EditAsync()
    {
        var year = SelectedYear();

        if (year is null)
        {
            MessageBox.Show("Select an academic year.");
            return;
        }

        using var form = new AcademicYearEditForm(
            _service, year.AcademicYearId);

        if (form.ShowDialog(this) == DialogResult.OK)
            await LoadYearsAsync();
    }

    private async Task ActivateAsync()
    {
        var year = SelectedYear();

        if (year is null)
        {
            MessageBox.Show("Select an academic year.");
            return;
        }

        if (year.IsActive)
        {
            MessageBox.Show("This academic year is already active.");
            return;
        }

        var result = MessageBox.Show(
            $"Set {year.YearName} as the active academic year?",
            "Confirm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
            return;

        btnActivate.Enabled = false;

        try
        {
            await _service.SetActiveAsync(year.AcademicYearId);
            await LoadYearsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message, "Activation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnActivate.Enabled = true;
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
