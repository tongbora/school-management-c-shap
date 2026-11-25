
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using SchoolManagement.Application.Examinations.DTOs;
using SchoolManagement.Application.Examinations.Interfaces;

namespace SchoolManagement.Desktop.Controls.Examinations;

public sealed class BulkGradeEntryControl : UserControl
{
    private readonly IGradeService _service;

    private readonly ComboBox cboExam = new();
    private readonly ComboBox cboSubject = new();
    private readonly NumericUpDown numMaxScore = new();

    private readonly Button btnLoad = new();
    private readonly Button btnSave = new();
    private readonly Button btnRefresh = new();

    private readonly DataGridView grid = new();
    private readonly Label lblSummary = new();

    private bool _busy;
    private bool _loadingOptions;
    private bool _rosterLoaded;

    private int _loadedExamId;
    private int _loadedSubjectId;
    private int _optionsVersion;

    public BulkGradeEntryControl(IGradeService service)
    {
        _service = service;

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(248, 250, 252);
        Font = new Font("Segoe UI", 10);

        BuildUi();

        Load += async (_, _) => await LoadExamsAsync();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 60));

        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 50));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 8, 0, 0)
        };

        foreach (var combo in new[] { cboExam, cboSubject })
        {
            combo.DropDownStyle =
                ComboBoxStyle.DropDownList;

            combo.DisplayMember =
                nameof(GradeOptionDto.Name);

            combo.ValueMember =
                nameof(GradeOptionDto.Id);
        }

        cboExam.Width = 210;
        cboSubject.Width = 265;

        numMaxScore.Width = 105;
        numMaxScore.DecimalPlaces = 2;
        numMaxScore.Minimum = 0.01m;
        numMaxScore.Maximum = 9999.99m;
        numMaxScore.Value = 100;

        StyleButton(btnLoad, "Load Students", 125);
        StyleButton(btnSave, "Save All", 110);
        StyleButton(btnRefresh, "Refresh Exams", 125);

        toolbar.Controls.Add(cboExam);
        toolbar.Controls.Add(cboSubject);
        toolbar.Controls.Add(new Label
        {
            Text = "Max Score",
            AutoSize = true,
            Padding = new Padding(5, 8, 0, 0)
        });
        toolbar.Controls.Add(numMaxScore);
        toolbar.Controls.Add(btnLoad);
        toolbar.Controls.Add(btnSave);
        toolbar.Controls.Add(btnRefresh);

        ConfigureGrid();

        var footer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12, 8, 12, 8)
        };

        lblSummary.Dock = DockStyle.Fill;
        lblSummary.ForeColor = Color.FromArgb(55, 65, 81);
        lblSummary.TextAlign = ContentAlignment.MiddleLeft;
        lblSummary.Text =
            "Select an exam and class subject, then load students.";

        footer.Controls.Add(lblSummary);

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(grid, 0, 1);
        root.Controls.Add(footer, 0, 2);

        Controls.Add(root);

        cboExam.SelectedIndexChanged += async (_, _) =>
        {
            if (!_loadingOptions)
                await LoadSubjectsAsync();
        };

        cboSubject.SelectedIndexChanged += (_, _) =>
        {
            if (!_loadingOptions)
                ClearRoster();
        };

        btnLoad.Click += async (_, _) =>
            await LoadStudentsAsync();

        btnSave.Click += async (_, _) =>
            await SaveAllAsync();

        btnRefresh.Click += async (_, _) =>
            await LoadExamsAsync();

        UpdateButtons();
    }

    private void ConfigureGrid()
    {
        grid.Dock = DockStyle.Fill;
        grid.BackgroundColor = Color.White;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;
        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;
        grid.SelectionMode =
            DataGridViewSelectionMode.CellSelect;

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "StudentCode",
            HeaderText = "Student Code",
            ReadOnly = true,
            FillWeight = 75
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "StudentName",
            HeaderText = "Student Name",
            ReadOnly = true,
            FillWeight = 140
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Score",
            HeaderText = "Score",
            ValueType = typeof(string),
            ReadOnly = false,
            FillWeight = 65
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Remark",
            HeaderText = "Remark",
            ReadOnly = false,
            MaxInputLength = 255,
            FillWeight = 150
        });

        grid.CellValueChanged += (_, _) =>
            UpdateSummary();

        grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (grid.IsCurrentCellDirty)
                grid.CommitEdit(
                    DataGridViewDataErrorContexts.Commit);
        };
    }

    private static void StyleButton(
        Button button,
        string text,
        int width)
    {
        button.Text = text;
        button.Size = new Size(width, 32);
        button.BackColor = Color.FromArgb(37, 99, 235);
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.UseVisualStyleBackColor = false;
        button.Cursor = Cursors.Hand;
    }

    private async Task LoadExamsAsync()
    {
        if (_busy) return;

        int version = ++_optionsVersion;

        ClearRoster();
        SetBusy(true);

        try
        {
            var exams = await _service.GetOpenExamsAsync();

            if (IsDisposed || version != _optionsVersion)
                return;

            _loadingOptions = true;

            try
            {
                cboSubject.DataSource = null;
                cboExam.DataSource = exams.ToList();
            }
            finally
            {
                _loadingOptions = false;
            }

            await LoadSubjectsCoreAsync(version);
        }
        catch (Exception ex)
        {
            ShowError("Load Exams Error", ex);
        }
        finally
        {
            if (!IsDisposed)
                SetBusy(false);
        }
    }

    private async Task LoadSubjectsAsync()
    {
        if (_loadingOptions || _busy) return;

        int version = ++_optionsVersion;

        ClearRoster();
        SetBusy(true);

        try
        {
            await LoadSubjectsCoreAsync(version);
        }
        catch (Exception ex)
        {
            ShowError("Load Subjects Error", ex);
        }
        finally
        {
            if (!IsDisposed)
                SetBusy(false);
        }
    }

    private async Task LoadSubjectsCoreAsync(int version)
    {
        var exam = cboExam.SelectedItem as GradeOptionDto;

        if (exam is null)
        {
            cboSubject.DataSource = null;
            return;
        }

        var subjects =
            await _service.GetClassSubjectsAsync(exam.Id);

        if (IsDisposed || version != _optionsVersion)
            return;

        _loadingOptions = true;

        try
        {
            cboSubject.DataSource = subjects.ToList();
        }
        finally
        {
            _loadingOptions = false;
        }
    }

    private async Task LoadStudentsAsync()
    {
        if (_busy) return;

        if (cboExam.SelectedItem is not GradeOptionDto exam ||
            cboSubject.SelectedItem is not GradeOptionDto subject)
        {
            MessageBox.Show(
                this,
                "Select an open exam and class subject.",
                "Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ClearRoster();
        SetBusy(true);

        try
        {
            var data = await _service.GetBulkRowsAsync(
                exam.Id, subject.Id);

            if (IsDisposed) return;

            // Grid starts with no rows.
            foreach (var item in data)
            {
                int rowIndex = grid.Rows.Add(
                    item.StudentCode,
                    item.StudentName,
                    item.Score?.ToString(
                        "0.##", CultureInfo.CurrentCulture) ?? "",
                    item.Remark ?? "");

                // EnrollmentId is not editable or displayed.
                grid.Rows[rowIndex].Tag = item.EnrollmentId;
            }

            if (data.Count > 0)
            {
                _loadedExamId = exam.Id;
                _loadedSubjectId = subject.Id;
                _rosterLoaded = true;

                // Use an existing maximum score if
                // previously recorded grades have one.
                var existingMax = data
                    .Where(x => x.MaxScore.HasValue)
                    .Select(x => x.MaxScore!.Value)
                    .Distinct()
                    .ToList();

                if (existingMax.Count == 1 &&
                    existingMax[0] >= numMaxScore.Minimum &&
                    existingMax[0] <= numMaxScore.Maximum)
                {
                    numMaxScore.Value = existingMax[0];
                }
                else if (existingMax.Count > 1)
                {
                    MessageBox.Show(
                        this,
                        "Existing grades have different maximum " +
                        "scores. Saving this batch will use " +
                        "the selected maximum for all scored rows.",
                        "Maximum Score Warning",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(
                    this,
                    "No active student enrollments found.",
                    "No Students",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }

            UpdateSummary();
        }
        catch (Exception ex)
        {
            ShowError("Load Students Error", ex);
        }
        finally
        {
            if (!IsDisposed)
                SetBusy(false);
        }
    }

    private async Task SaveAllAsync()
    {
        if (_busy || !_rosterLoaded) return;

        if (cboExam.SelectedItem is not GradeOptionDto exam ||
            cboSubject.SelectedItem is not GradeOptionDto subject ||
            exam.Id != _loadedExamId ||
            subject.Id != _loadedSubjectId)
        {
            MessageBox.Show(this,
                "Selection changed. Reload the student roster.");
            return;
        }

        grid.EndEdit();

        var marks = new List<BulkGradeMarkDto>();
        int scoredCount = 0;

        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.Tag is not int enrollmentId)
                continue;

            string rawScore =
                Convert.ToString(row.Cells["Score"].Value)?
                    .Trim() ?? "";

            decimal? score = null;

            if (rawScore.Length > 0)
            {
                if (!decimal.TryParse(
                    rawScore,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out var parsed))
                {
                    MessageBox.Show(
                        this,
                        $"Invalid score on row {row.Index + 1}.",
                        "Validation Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (parsed < 0 ||
                    parsed > numMaxScore.Value ||
                    decimal.Round(parsed, 2) != parsed)
                {
                    MessageBox.Show(
                        this,
                        $"Score on row {row.Index + 1} " +
                        "must be within the allowed range " +
                        "and have at most two decimal places.",
                        "Validation Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                score = parsed;
                scoredCount++;
            }

            string? remark =
                Convert.ToString(row.Cells["Remark"].Value);

            marks.Add(new BulkGradeMarkDto(
                enrollmentId,
                score,
                remark));
        }

        if (scoredCount == 0)
        {
            MessageBox.Show(this,
                "Enter at least one student score.");
            return;
        }

        var request = new SaveBulkGradesRequest(
            exam.Id,
            subject.Id,
            numMaxScore.Value,
            marks);

        var confirm = MessageBox.Show(
            this,
            $"Save {scoredCount} scored student record(s)? " +
            "Existing grades for these rows will be updated.",
            "Confirm Bulk Grade Save",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        SetBusy(true);

        try
        {
            int saved = await _service.SaveBulkAsync(request);

            if (IsDisposed) return;

            MessageBox.Show(
                this,
                $"{saved} student grade(s) saved successfully.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            SetBusy(false);
            await LoadStudentsAsync();
        }
        catch (Exception ex)
        {
            ShowError("Save Grades Error", ex);
        }
        finally
        {
            if (!IsDisposed)
                SetBusy(false);
        }
    }

    private void ClearRoster()
    {
        grid.Rows.Clear();
        _rosterLoaded = false;
        _loadedExamId = 0;
        _loadedSubjectId = 0;

        UpdateSummary();
        UpdateButtons();
    }

    private void UpdateSummary()
    {
        int scored = 0;

        foreach (DataGridViewRow row in grid.Rows)
        {
            string value =
                Convert.ToString(row.Cells["Score"].Value)?
                    .Trim() ?? "";

            if (value.Length > 0)
                scored++;
        }

        lblSummary.Text =
            $"Students: {grid.Rows.Count}    " +
            $"Scores entered: {scored}    " +
            $"Blank: {grid.Rows.Count - scored}";
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;

        cboExam.Enabled = !busy;
        cboSubject.Enabled = !busy;
        numMaxScore.Enabled = !busy;

        btnRefresh.Enabled = !busy;
        btnLoad.Enabled = !busy;

        grid.ReadOnly = busy;

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        btnSave.Enabled = !_busy && _rosterLoaded;
    }

    private void ShowError(string title, Exception ex)
    {
        if (!IsDisposed)
        {
            MessageBox.Show(
                this,
                ex.Message,
                title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
