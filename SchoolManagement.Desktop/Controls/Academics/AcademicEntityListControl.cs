
using System.Drawing;
using System.Windows.Forms;

namespace SchoolManagement.Desktop.Controls.Academics;

public sealed class AcademicEntityListControl<T> : UserControl
    where T : class
{
    private readonly Func<string, Task<IReadOnlyList<T>>> _load;
    private readonly Func<T, int> _getId;
    private readonly Func<T, bool> _isActive;
    private readonly Func<T, string> _getName;
    private readonly Func<int?, Form> _createForm;
    private readonly Func<int, bool, Task> _setActive;

    private readonly DataGridView grid = new();
    private readonly TextBox search = new();
    private readonly Label count = new();
    private readonly Button add = new();
    private readonly Button edit = new();
    private readonly Button status = new();
    private readonly Button refresh = new();

    private readonly System.Windows.Forms.Timer timer =
        new() { Interval = 300 };

    private int _version;

    public AcademicEntityListControl(
        Func<string, Task<IReadOnlyList<T>>> load,
        Func<T, int> getId,
        Func<T, bool> isActive,
        Func<T, string> getName,
        Func<int?, Form> createForm,
        Func<int, bool, Task> setActive,
        params (string Property, string Header)[] columns)
    {
        _load = load;
        _getId = getId;
        _isActive = isActive;
        _getName = getName;
        _createForm = createForm;
        _setActive = setActive;

        Dock = DockStyle.Fill;
        BuildUi(columns);

        timer.Tick += async (_, _) =>
        {
            timer.Stop();
            await ReloadAsync();
        };

        search.TextChanged += (_, _) =>
        {
            ++_version;
            timer.Stop();
            timer.Start();
        };

        Load += async (_, _) => await ReloadAsync();
        grid.SelectionChanged += (_, _) => UpdateStatusButton();
    }

    private void BuildUi(
        (string Property, string Header)[] columns)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(12),
            BackColor = Color.FromArgb(248, 250, 252)
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = true
        };

        search.Width = 230;
        search.PlaceholderText = "Search...";

        Style(add, "Add");
        Style(edit, "Edit");
        Style(status, "Deactivate");
        Style(refresh, "Refresh");

        toolbar.Controls.AddRange(new Control[]
        {
            search, add, edit, status, refresh
        });

        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;
        grid.MultiSelect = false;
        grid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;
        grid.AutoGenerateColumns = false;
        grid.BackgroundColor = Color.White;

        foreach (var (property, header) in columns)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = property,
                DataPropertyName = property,
                HeaderText = header
            });
        }

        count.Dock = DockStyle.Fill;
        count.ForeColor = Color.DimGray;

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(grid, 0, 1);
        root.Controls.Add(count, 0, 2);
        Controls.Add(root);

        add.Click += async (_, _) => await OpenFormAsync(null);
        edit.Click += async (_, _) =>
        {
            var item = Selected();
            if (item != null)
                await OpenFormAsync(_getId(item));
        };

        status.Click += async (_, _) => await ToggleAsync();
        refresh.Click += async (_, _) => await ReloadAsync();

        grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex < 0) return;
            var item = Selected();
            if (item != null)
                await OpenFormAsync(_getId(item));
        };
    }

    private static void Style(Button button, string label)
    {
        button.Text = label;
        button.Width = label == "Deactivate" ? 115 : 85;
        button.Height = 32;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Color.FromArgb(37, 99, 235);
        button.ForeColor = Color.White;
        button.UseVisualStyleBackColor = false;
        button.Cursor = Cursors.Hand;
    }

    private T? Selected() =>
        grid.CurrentRow?.DataBoundItem as T;

    private void UpdateStatusButton()
    {
        var item = Selected();
        status.Enabled = item != null;
        status.Text = item != null && !_isActive(item)
            ? "Activate"
            : "Deactivate";
    }

    private async Task ReloadAsync()
    {
        int version = ++_version;

        try
        {
            var records = await _load(search.Text.Trim());

            if (IsDisposed || version != _version)
                return;

            grid.DataSource = records.ToList();
            count.Text = $"Total records: {records.Count}";
            UpdateStatusButton();
        }
        catch (Exception ex)
        {
            if (IsDisposed || version != _version)
                return;

            MessageBox.Show(
                this, ex.Message, "Load Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OpenFormAsync(int? id)
    {
        using var form = _createForm(id);

        if (form.ShowDialog(this) == DialogResult.OK)
            await ReloadAsync();
    }

    private async Task ToggleAsync()
    {
        var item = Selected();
        if (item == null) return;

        bool newState = !_isActive(item);
        string action = newState ? "activate" : "deactivate";

        if (MessageBox.Show(
            this,
            $"Do you want to {action} {_getName(item)}?",
            "Confirm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        status.Enabled = false;

        try
        {
            await _setActive(_getId(item), newState);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this, ex.Message, "Update Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
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
