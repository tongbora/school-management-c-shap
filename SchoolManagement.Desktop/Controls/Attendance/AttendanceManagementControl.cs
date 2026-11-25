
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SchoolManagement.Application.Attendance.DTOs;
using SchoolManagement.Application.Attendance.Interfaces;

namespace SchoolManagement.Desktop.Controls.Attendance;

public sealed class AttendanceManagementControl : UserControl
{
    private readonly IAttendanceService _service;

    private readonly TabControl tabs = new();
    private readonly ComboBox cboClass = new();
    private readonly ComboBox cboSession = new();
    private readonly DateTimePicker dtDate = new();

    private readonly DataGridView entryGrid = new();
    private readonly DataGridView historyGrid = new();

    private readonly Button btnLoad = new();
    private readonly Button btnAllPresent = new();
    private readonly Button btnSave = new();
    private readonly Button btnHistoryRefresh = new();

    private readonly TextBox txtHistorySearch = new();
    private readonly Label lblSummary = new();
    private readonly Label lblHistoryCount = new();

    private readonly BindingList<EntryRow> rows = new();

    private readonly System.Windows.Forms.Timer searchTimer =
        new() { Interval = 350 };

    private bool _busy;
    private bool _rosterLoaded;
    private int _historyVersion;

    public AttendanceManagementControl(
        IAttendanceService service)
    {
        _service = service;

        Dock = DockStyle.Fill;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.White;

        BuildUi();

        Load += async (_, _) =>
        {
            await LoadClassesAsync();
            await LoadHistoryAsync();
        };

        searchTimer.Tick += async (_, _) =>
        {
            searchTimer.Stop();
            await LoadHistoryAsync();
        };

        txtHistorySearch.TextChanged += (_, _) =>
        {
            ++_historyVersion;
            searchTimer.Stop();
            searchTimer.Start();
        };
    }

    private void BuildUi()
    {
        tabs.Dock = DockStyle.Fill;

        var entryPage = new TabPage("Mark Attendance");
        var historyPage = new TabPage("Attendance History");

        entryPage.Controls.Add(BuildEntryPage());
        historyPage.Controls.Add(BuildHistoryPage());

        tabs.TabPages.Add(entryPage);
        tabs.TabPages.Add(historyPage);

        Controls.Add(tabs);
    }

    private Control BuildEntryPage()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 58));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = true
        };

        cboClass.Width = 260;
        cboClass.DropDownStyle =
            ComboBoxStyle.DropDownList;
        cboClass.DisplayMember =
            nameof(AttendanceOptionDto.Name);
        cboClass.ValueMember =
            nameof(AttendanceOptionDto.Id);

        dtDate.Width = 125;
        dtDate.Format = DateTimePickerFormat.Short;
        dtDate.Value = DateTime.Today;

        cboSession.Width = 125;
        cboSession.DropDownStyle =
            ComboBoxStyle.DropDownList;

        cboSession.Items.AddRange(new object[]
        {
            "FullDay", "Morning", "Afternoon"
        });

        cboSession.SelectedIndex = 0;

        StyleButton(btnLoad, "Load Students", 120);
        StyleButton(btnAllPresent, "Mark All Present", 145);
        StyleButton(btnSave, "Save Attendance", 145);

        toolbar.Controls.Add(cboClass);
        toolbar.Controls.Add(dtDate);
        toolbar.Controls.Add(cboSession);
        toolbar.Controls.Add(btnLoad);
        toolbar.Controls.Add(btnAllPresent);
        toolbar.Controls.Add(btnSave);

        entryGrid.Dock = DockStyle.Fill;
        entryGrid.BackgroundColor = Color.White;
        entryGrid.AutoGenerateColumns = false;
        entryGrid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;
        entryGrid.AllowUserToAddRows = false;
        entryGrid.AllowUserToDeleteRows = false;
        entryGrid.RowHeadersVisible = false;
        entryGrid.DataSource = rows;

        AddTextColumn(
            entryGrid, "StudentCode", "Student Code", true);

        AddTextColumn(
            entryGrid, "StudentName", "Student Name", true);

        var statusColumn = new DataGridViewComboBoxColumn
        {
            Name = "Status",
            HeaderText = "Attendance Status",
            DataPropertyName = "Status",
            FlatStyle = FlatStyle.Flat
        };

        statusColumn.Items.AddRange(
            "Present", "Absent", "Late", "Excused");

        entryGrid.Columns.Add(statusColumn);

        AddTextColumn(
            entryGrid, "Remark", "Remark", false);

        entryGrid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (entryGrid.IsCurrentCellDirty)
                entryGrid.CommitEdit(
                    DataGridViewDataErrorContexts.Commit);
        };

        entryGrid.CellValueChanged += (_, _) =>
            UpdateSummary();

        entryGrid.DataError += (_, e) =>
        {
            e.ThrowException = false;
        };

        var footer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(12)
        };

        lblSummary.Dock = DockStyle.Fill;
        lblSummary.TextAlign =
            ContentAlignment.MiddleLeft;
        lblSummary.Text = "Load students to record attendance.";

        footer.Controls.Add(lblSummary);

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(entryGrid, 0, 1);
        root.Controls.Add(footer, 0, 2);

        btnLoad.Click += async (_, _) =>
            await LoadRosterAsync();

        btnAllPresent.Click += (_, _) =>
            MarkAllPresent();

        btnSave.Click += async (_, _) =>
            await SaveAttendanceAsync();

        // Avoid saving stale rows after changing filters.
        cboClass.SelectedIndexChanged += (_, _) =>
            ClearRoster();

        cboSession.SelectedIndexChanged += (_, _) =>
            ClearRoster();

        dtDate.ValueChanged += (_, _) =>
            ClearRoster();

        UpdateButtons();

        return root;
    }

    private Control BuildHistoryPage()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 32));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false
        };

        txtHistorySearch.Width = 260;
        txtHistorySearch.PlaceholderText =
            "Search student, class or status...";

        StyleButton(btnHistoryRefresh, "Refresh", 90);

        toolbar.Controls.Add(txtHistorySearch);
        toolbar.Controls.Add(btnHistoryRefresh);

        historyGrid.Dock = DockStyle.Fill;
        historyGrid.BackgroundColor = Color.White;
        historyGrid.ReadOnly = true;
        historyGrid.AutoGenerateColumns = false;
        historyGrid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;
        historyGrid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;
        historyGrid.MultiSelect = false;
        historyGrid.RowHeadersVisible = false;
        historyGrid.AllowUserToAddRows = false;
        historyGrid.AllowUserToDeleteRows = false;

        AddTextColumn(
            historyGrid, "StudentCode", "Code", true);
        AddTextColumn(
            historyGrid, "StudentName", "Student", true);
        AddTextColumn(
            historyGrid, "ClassName", "Class", true);
        AddTextColumn(
            historyGrid, "AttendanceDate", "Date", true);
        AddTextColumn(
            historyGrid, "SessionName", "Session", true);
        AddTextColumn(
            historyGrid, "Status", "Status", true);
        AddTextColumn(
            historyGrid, "Remark", "Remark", true);

        lblHistoryCount.Dock = DockStyle.Fill;
        lblHistoryCount.ForeColor = Color.DimGray;

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(historyGrid, 0, 1);
        root.Controls.Add(lblHistoryCount, 0, 2);

        btnHistoryRefresh.Click += async (_, _) =>
            await LoadHistoryAsync();

        return root;
    }

    private static void AddTextColumn(
        DataGridView grid,
        string property,
        string header,
        bool readOnly)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = property,
            DataPropertyName = property,
            HeaderText = header,
            ReadOnly = readOnly
        });
    }

    private static void StyleButton(
        Button button,
        string label,
        int width)
    {
        button.Text = label;
        button.Width = width;
        button.Height = 32;
        button.BackColor =
            Color.FromArgb(37, 99, 235);
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.UseVisualStyleBackColor = false;
        button.Cursor = Cursors.Hand;
    }

    private async Task LoadClassesAsync()
    {
        try
        {
            var classes = await _service.GetClassesAsync();

            if (IsDisposed)
                return;

            cboClass.DataSource = classes.ToList();

            if (classes.Count == 0)
            {
                MessageBox.Show(
                    "Create a class and enroll students first.");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Load Classes Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ClearRoster()
    {
        rows.Clear();
        _rosterLoaded = false;

        UpdateSummary();
        UpdateButtons();
    }

    private async Task LoadRosterAsync()
    {
        if (_busy)
            return;

        if (cboClass.SelectedItem is not AttendanceOptionDto cls)
        {
            MessageBox.Show("Select a class.");
            return;
        }

        SetBusy(true);
        _rosterLoaded = false;

        try
        {
            var roster = await _service.GetRosterAsync(
                cls.Id,
                DateOnly.FromDateTime(dtDate.Value),
                cboSession.Text);

            if (IsDisposed)
                return;

            rows.Clear();

            foreach (var student in roster)
            {
                rows.Add(new EntryRow
                {
                    EnrollmentId = student.EnrollmentId,
                    StudentCode = student.StudentCode,
                    StudentName = student.StudentName,
                    Status = student.Status ?? "Present",
                    Remark = student.Remark ?? ""
                });
            }

            _rosterLoaded = roster.Count > 0;

            if (!_rosterLoaded)
            {
                MessageBox.Show(
                    "No enrolled students were found for " +
                    "the selected class and date.");
            }

            UpdateSummary();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Load Attendance Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            if (!IsDisposed)
                SetBusy(false);
        }
    }

    private void MarkAllPresent()
    {
        if (!_rosterLoaded || _busy)
            return;

        entryGrid.EndEdit();

        foreach (var row in rows)
            row.Status = "Present";

        entryGrid.Refresh();
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        lblSummary.Text =
            $"Students: {rows.Count}    " +
            $"Present: {rows.Count(x => x.Status == "Present")}    " +
            $"Absent: {rows.Count(x => x.Status == "Absent")}    " +
            $"Late: {rows.Count(x => x.Status == "Late")}    " +
            $"Excused: {rows.Count(x => x.Status == "Excused")}";
    }

    private async Task SaveAttendanceAsync()
    {
        if (_busy || !_rosterLoaded)
            return;

        if (cboClass.SelectedItem is not AttendanceOptionDto cls)
            return;

        // Commit pending edits before reading the row values.
        entryGrid.EndEdit();

        var request = new SaveAttendanceBatchRequest(
            cls.Id,
            DateOnly.FromDateTime(dtDate.Value),
            cboSession.Text,
            null,
            rows.Select(x => new AttendanceMarkDto(
                x.EnrollmentId,
                x.Status,
                x.Remark)).ToList());

        SetBusy(true);

        try
        {
            await _service.SaveBatchAsync(request);

            MessageBox.Show(
                "Attendance saved successfully.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            await LoadHistoryAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Save Attendance Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            if (!IsDisposed)
                SetBusy(false);
        }
    }

    private async Task LoadHistoryAsync()
    {
        int version = ++_historyVersion;

        try
        {
            var history = await _service.GetHistoryAsync(
                txtHistorySearch.Text.Trim());

            if (IsDisposed || version != _historyVersion)
                return;

            historyGrid.DataSource = history.ToList();

            lblHistoryCount.Text =
                $"Displayed records: {history.Count} " +
                "(maximum 500)";
        }
        catch (Exception ex)
        {
            if (IsDisposed || version != _historyVersion)
                return;

            MessageBox.Show(
                ex.Message,
                "History Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;

        cboClass.Enabled = !busy;
        cboSession.Enabled = !busy;
        dtDate.Enabled = !busy;
        btnLoad.Enabled = !busy;

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        btnSave.Enabled = !_busy && _rosterLoaded;
        btnAllPresent.Enabled = !_busy && _rosterLoaded;
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

    private sealed class EntryRow
    {
        public int EnrollmentId { get; set; }
        public string StudentCode { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string Status { get; set; } = "Present";
        public string Remark { get; set; } = "";
    }
}
