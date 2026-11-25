
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SchoolManagement.Application.Finance.DTOs;
using SchoolManagement.Application.Finance.Interfaces;

namespace SchoolManagement.Desktop.Controls.Finance;

public sealed class FinanceHomeControl : UserControl
{
    private readonly IFinanceService _finance;
    private readonly int? _currentUserId;

    private readonly TabControl tabs = new();

    private readonly DataGridView categoryGrid = new();
    private readonly DataGridView feeGrid = new();
    private readonly DataGridView paymentGrid = new();

    private readonly TextBox txtCategorySearch = new();
    private readonly TextBox txtFeeSearch = new();

    private readonly Label lblCategories = new();
    private readonly Label lblFees = new();
    private readonly Label lblPayments = new();
    private readonly Label lblTotals = new();

    private bool _busy;

    private static readonly Color Primary =
        Color.FromArgb(37, 99, 235);

    private static readonly Color Background =
        Color.FromArgb(245, 247, 251);

    private static readonly Color TextColor =
        Color.FromArgb(30, 41, 59);

    public FinanceHomeControl(
        IFinanceService finance,
        int? currentUserId = null)
    {
        _finance = finance;
        _currentUserId = currentUserId;

        Dock = DockStyle.Fill;
        BackColor = Background;
        Font = new Font("Segoe UI", 10);

        BuildUi();

        Load += async (_, _) =>
            await RefreshAllAsync();
    }

    // =========================================
    // Main layout
    // =========================================

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Background
        };

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 78));

        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        var summary = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(18)
        };

        lblTotals.Dock = DockStyle.Fill;
        lblTotals.TextAlign =
            ContentAlignment.MiddleLeft;
        lblTotals.ForeColor = TextColor;
        lblTotals.Font = new Font(
            "Segoe UI", 11, FontStyle.Bold);
        lblTotals.Text = "Loading finance summary...";

        summary.Controls.Add(lblTotals);

        tabs.Dock = DockStyle.Fill;
        tabs.Font = new Font("Segoe UI", 10);

        tabs.TabPages.Add(
            BuildCategoryTab());

        tabs.TabPages.Add(
            BuildFeeTab());

        tabs.TabPages.Add(
            BuildPaymentTab());

        root.Controls.Add(summary, 0, 0);
        root.Controls.Add(tabs, 0, 1);

        Controls.Add(root);
    }

    // =========================================
    // Fee Categories tab
    // =========================================

    private TabPage BuildCategoryTab()
    {
        var page = CreateTab("Fee Categories");
        var layout = CreatePageLayout();

        var toolbar = CreateToolbar();

        txtCategorySearch.Width = 250;
        txtCategorySearch.PlaceholderText =
            "Search fee categories...";

        var add = CreateButton(
            "Add Category", true, 140);

        var edit = CreateButton(
            "Edit Category", false, 130);

        var toggle = CreateButton(
            "Activate / Deactivate", false, 180);

        var refresh = CreateButton(
            "Refresh", false, 100);

        toolbar.Controls.AddRange(new Control[]
        {
            txtCategorySearch,
            add,
            edit,
            toggle,
            refresh
        });

        ConfigureGrid(categoryGrid);

        AddColumn(categoryGrid,
            "CategoryName", "Category Name", 30);

        AddColumn(categoryGrid,
            "Description", "Description", 55);

        AddColumn(categoryGrid,
            "IsActive", "Active", 15);

        ConfigureFooter(lblCategories);

        layout.Controls.Add(toolbar, 0, 0);
        layout.Controls.Add(categoryGrid, 0, 1);
        layout.Controls.Add(lblCategories, 0, 2);

        page.Controls.Add(layout);

        add.Click += async (_, _) =>
            await EditCategoryAsync(null);

        edit.Click += async (_, _) =>
            await EditCategoryAsync(
                Selected<FeeCategoryDto>(categoryGrid));

        toggle.Click += async (_, _) =>
            await ToggleCategoryAsync();

        refresh.Click += async (_, _) =>
            await RefreshAllAsync();

        txtCategorySearch.TextChanged += (_, _) =>
            ApplyCategoryFilter();

        return page;
    }

    // =========================================
    // Student Fees tab
    // =========================================

    private TabPage BuildFeeTab()
    {
        var page = CreateTab("Student Fees & Balances");
        var layout = CreatePageLayout();
        var toolbar = CreateToolbar();

        txtFeeSearch.Width = 250;
        txtFeeSearch.PlaceholderText =
            "Search student, category or status...";

        var assign = CreateButton(
            "Assign Fee", true, 125);

        var payment = CreateButton(
            "Record Payment", false, 150);

        var refresh = CreateButton(
            "Refresh", false, 100);

        toolbar.Controls.AddRange(new Control[]
        {
            txtFeeSearch,
            assign,
            payment,
            refresh
        });

        ConfigureGrid(feeGrid);

        AddColumn(feeGrid,
            "StudentName", "Student", 20);

        AddColumn(feeGrid,
            "CategoryName", "Category", 15);

        AddColumn(feeGrid,
            "Amount", "Assigned", 12);

        AddColumn(feeGrid,
            "CurrencyCode", "Currency", 9);

        AddColumn(feeGrid,
            "PaidAmount", "Paid", 12);

        AddColumn(feeGrid,
            "BalanceAmount", "Balance", 12);

        AddColumn(feeGrid,
            "PaymentStatus", "Status", 12);

        AddColumn(feeGrid,
            "DueDate", "Due Date", 12);

        ConfigureFooter(lblFees);

        layout.Controls.Add(toolbar, 0, 0);
        layout.Controls.Add(feeGrid, 0, 1);
        layout.Controls.Add(lblFees, 0, 2);

        page.Controls.Add(layout);

        payment.Enabled = _currentUserId.HasValue;

        if (!_currentUserId.HasValue)
        {
            payment.Text = "Login Required";
        }

        assign.Click += async (_, _) =>
            await AssignFeeAsync();

        payment.Click += async (_, _) =>
            await RecordPaymentAsync();

        refresh.Click += async (_, _) =>
            await RefreshAllAsync();

        txtFeeSearch.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await RefreshAllAsync();
            }
        };

        return page;
    }

    // =========================================
    // Payment History tab
    // =========================================

    private TabPage BuildPaymentTab()
    {
        var page = CreateTab("Payment History");
        var layout = CreatePageLayout();
        var toolbar = CreateToolbar();

        var refresh = CreateButton(
            "Refresh Payments", false, 160);

        toolbar.Controls.Add(refresh);

        ConfigureGrid(paymentGrid);

        AddColumn(paymentGrid,
            "PaymentId", "Payment ID", 10);

        AddColumn(paymentGrid,
            "StudentName", "Student", 20);

        AddColumn(paymentGrid,
            "Amount", "Amount", 12);

        AddColumn(paymentGrid,
            "CurrencyCode", "Currency", 10);

        AddColumn(paymentGrid,
            "PaymentDate", "Date", 18);

        AddColumn(paymentGrid,
            "PaymentMethod", "Method", 15);

        AddColumn(paymentGrid,
            "ReferenceNo", "Reference", 15);

        ConfigureFooter(lblPayments);

        layout.Controls.Add(toolbar, 0, 0);
        layout.Controls.Add(paymentGrid, 0, 1);
        layout.Controls.Add(lblPayments, 0, 2);

        page.Controls.Add(layout);

        refresh.Click += async (_, _) =>
            await RefreshAllAsync();

        return page;
    }

    // =========================================
    // Load Finance data
    // =========================================

    private async Task RefreshAllAsync()
    {
        if (_busy) return;

        _busy = true;

        try
        {
            var categories =
                await _finance.GetCategoriesAsync();

            var fees =
                await _finance.GetFeesAsync(
                    txtFeeSearch.Text.Trim());

            var payments =
                await _finance.GetPaymentsAsync();

            var totals =
                await _finance.GetTotalsAsync();

            if (IsDisposed) return;

            categoryGrid.Tag = categories.ToList();
            ApplyCategoryFilter();

            feeGrid.DataSource = fees.ToList();
            paymentGrid.DataSource = payments.ToList();

            lblFees.Text =
                $"{fees.Count} fee record(s)";

            lblPayments.Text =
                $"{payments.Count} payment record(s)";

            lblTotals.Text = totals.Count == 0
                ? "No fees assigned yet"
                : string.Join(
                    "     |     ",
                    totals.Select(x =>
                        $"{x.CurrencyCode}   " +
                        $"Assigned: {x.Assigned:N2}   " +
                        $"Paid: {x.Paid:N2}   " +
                        $"Outstanding: {x.Outstanding:N2}"));
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            _busy = false;
        }
    }

    private void ApplyCategoryFilter()
    {
        if (categoryGrid.Tag is not
            System.Collections.Generic.List<FeeCategoryDto> all)
            return;

        string search = txtCategorySearch.Text.Trim();

        var filtered = all
            .Where(x =>
                search.Length == 0 ||
                x.CategoryName.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase) ||
                (x.Description?.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        categoryGrid.DataSource = filtered;

        lblCategories.Text =
            $"{filtered.Count} fee categor(ies)";
    }

    // =========================================
    // Create / Edit category
    // =========================================

    private async Task EditCategoryAsync(
        FeeCategoryDto? selected)
    {
        using var dialog = CreateDialog(
            selected is null
                ? "Add Fee Category"
                : "Edit Fee Category",
            out var fields);

        var name = new TextBox
        {
            Text = selected?.CategoryName ?? "",
            MaxLength = 100
        };

        var description = new TextBox
        {
            Text = selected?.Description ?? "",
            MaxLength = 500,
            Multiline = true,
            Height = 75
        };

        AddDialogField(fields,
            "Category Name *", name, 48);

        AddDialogField(fields,
            "Description", description, 90);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            await _finance.SaveCategoryAsync(
                new SaveFeeCategoryRequest(
                    name.Text,
                    description.Text),
                selected?.FeeCategoryId);

            await RefreshAllAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async Task ToggleCategoryAsync()
    {
        var category =
            Selected<FeeCategoryDto>(categoryGrid);

        if (category is null)
        {
            MessageBox.Show(this,
                "Select a category first.");
            return;
        }

        bool newStatus = !category.IsActive;

        var confirm = MessageBox.Show(
            this,
            $"{(newStatus ? "Activate" : "Deactivate")} " +
            $"'{category.CategoryName}'?",
            "Confirm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            await _finance.SetCategoryActiveAsync(
                category.FeeCategoryId,
                newStatus);

            await RefreshAllAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    // =========================================
    // Assign student fee
    // =========================================

    private async Task AssignFeeAsync()
    {
        try
        {
            var enrollments =
                await _finance.GetActiveEnrollmentsAsync();

            var categories =
                (await _finance.GetCategoriesAsync())
                .Where(x => x.IsActive)
                .Select(x => new FinanceOptionDto(
                    x.FeeCategoryId,
                    x.CategoryName))
                .ToList();

            if (enrollments.Count == 0 ||
                categories.Count == 0)
            {
                throw new InvalidOperationException(
                    "Create active enrollments and " +
                    "fee categories first.");
            }

            using var dialog = CreateDialog(
                "Assign Student Fee",
                out var fields);

            var enrollment = CreateCombo(enrollments);
            var category = CreateCombo(categories);
            var amount = CreateMoneyInput();

            var currency = new ComboBox
            {
                DropDownStyle =
                    ComboBoxStyle.DropDownList
            };

            currency.Items.AddRange(new object[]
            {
                "USD", "KHR"
            });

            currency.SelectedIndex = 0;

            var due = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short
            };

            var description = new TextBox
            {
                MaxLength = 255
            };

            AddDialogField(fields,
                "Student Enrollment *",
                enrollment, 48);

            AddDialogField(fields,
                "Fee Category *",
                category, 48);

            AddDialogField(fields,
                "Amount *",
                amount, 48);

            AddDialogField(fields,
                "Currency *",
                currency, 48);

            AddDialogField(fields,
                "Due Date *",
                due, 48);

            AddDialogField(fields,
                "Description",
                description, 48);

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            await _finance.CreateFeeAsync(
                new CreateStudentFeeRequest(
                    ((FinanceOptionDto)enrollment.SelectedItem!).Id,
                    ((FinanceOptionDto)category.SelectedItem!).Id,
                    description.Text,
                    amount.Value,
                    currency.Text,
                    DateOnly.FromDateTime(due.Value)),
                _currentUserId);

            await RefreshAllAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    // =========================================
    // Record payment
    // =========================================

    private async Task RecordPaymentAsync()
    {
        if (!_currentUserId.HasValue) return;

        var fee = Selected<StudentFeeDto>(feeGrid);

        if (fee is null || fee.BalanceAmount <= 0)
        {
            MessageBox.Show(this,
                "Select a fee with an outstanding balance.");
            return;
        }

        using var dialog = CreateDialog(
            "Record Fee Payment",
            out var fields);

        var amount = CreateMoneyInput();

        amount.Maximum = fee.BalanceAmount;
        amount.Value = fee.BalanceAmount;

        var method = new ComboBox
        {
            DropDownStyle =
                ComboBoxStyle.DropDownList
        };

        method.Items.AddRange(new object[]
        {
            "Cash", "Bank Transfer", "KHQR", "Other"
        });

        method.SelectedIndex = 0;

        var reference = new TextBox
        {
            MaxLength = 100
        };

        var note = new TextBox
        {
            MaxLength = 500
        };

        AddDialogField(fields,
            "Payment Amount *", amount, 48);

        AddDialogField(fields,
            "Payment Method *", method, 48);

        AddDialogField(fields,
            "Reference No", reference, 48);

        AddDialogField(fields,
            "Note", note, 48);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var confirm = MessageBox.Show(
            this,
            $"Record {amount.Value:N2} " +
            $"{fee.CurrencyCode} for {fee.StudentName}?",
            "Confirm Payment",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        try
        {
            await _finance.RecordPaymentAsync(
                new RecordFeePaymentRequest(
                    fee.FeeId,
                    amount.Value,
                    method.Text,
                    reference.Text,
                    note.Text),
                _currentUserId.Value);

            MessageBox.Show(this,
                "Payment recorded successfully.");

            await RefreshAllAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    // =========================================
    // UI helpers
    // =========================================

    private static TabPage CreateTab(string title)
    {
        return new TabPage(title)
        {
            Padding = new Padding(12),
            BackColor = Color.White
        };
    }

    private static TableLayoutPanel CreatePageLayout()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };

        panel.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 64));

        panel.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        panel.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 42));

        return panel;
    }

    private static FlowLayoutPanel CreateToolbar()
    {
        return new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(6, 12, 6, 6),
            WrapContents = false,
            AutoScroll = true
        };
    }

    private static Button CreateButton(
        string text, bool primary, int width)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 36,
            Margin = new Padding(4, 0, 4, 0),
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            BackColor = primary ? Primary : Color.White,
            ForeColor = primary ? Color.White : TextColor,
            Cursor = Cursors.Hand
        };

        button.FlatAppearance.BorderSize =
            primary ? 0 : 1;

        button.FlatAppearance.BorderColor =
            Color.FromArgb(203, 213, 225);

        return button;
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;

        grid.AutoGenerateColumns = false;
        grid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;

        grid.ReadOnly = true;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;

        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;

        grid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;

        grid.ColumnHeadersHeight = 44;
        grid.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        grid.RowTemplate.Height = 40;
        grid.EnableHeadersVisualStyles = false;

        grid.ColumnHeadersDefaultCellStyle.BackColor =
            Color.FromArgb(241, 245, 249);

        grid.ColumnHeadersDefaultCellStyle.ForeColor =
            TextColor;

        grid.DefaultCellStyle.SelectionBackColor =
            Color.FromArgb(219, 234, 254);

        grid.DefaultCellStyle.SelectionForeColor =
            TextColor;
    }

    private static void AddColumn(
        DataGridView grid,
        string property,
        string heading,
        float fillWeight)
    {
        grid.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                Name = property,
                DataPropertyName = property,
                HeaderText = heading,
                FillWeight = fillWeight,
                MinimumWidth = 80
            });
    }

    private static void ConfigureFooter(Label label)
    {
        label.Dock = DockStyle.Fill;
        label.Padding = new Padding(8);
        label.ForeColor = Color.DimGray;
        label.TextAlign = ContentAlignment.MiddleLeft;
    }

    // =========================================
    // Responsive form/dialog
    // =========================================

    private static Form CreateDialog(
        string title,
        out TableLayoutPanel fields)
    {
        var dialog = new Form
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(620, 500),
            MinimumSize = new Size(560, 400),
            FormBorderStyle = FormBorderStyle.Sizable,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.White
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(24, 20, 24, 16)
        };

        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 66));

        fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            ColumnCount = 2,
            RowCount = 0
        };

        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 180));

        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 12, 0, 0)
        };

        var save = CreateButton("Save", true, 110);
        var cancel = CreateButton("Cancel", false, 110);

        save.DialogResult = DialogResult.OK;
        cancel.DialogResult = DialogResult.Cancel;

        footer.Controls.Add(save);
        footer.Controls.Add(cancel);

        root.Controls.Add(fields, 0, 0);
        root.Controls.Add(footer, 0, 1);

        dialog.Controls.Add(root);

        dialog.AcceptButton = save;
        dialog.CancelButton = cancel;

        return dialog;
    }

    private static void AddDialogField(
        TableLayoutPanel fields,
        string caption,
        Control input,
        int height)
    {
        int row = fields.RowCount++;

        fields.RowStyles.Add(
            new RowStyle(SizeType.Absolute, height + 12));

        var label = new Label
        {
            Text = caption,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = TextColor,
            AutoEllipsis = false
        };

        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(4, 8, 4, 8);

        fields.Controls.Add(label, 0, row);
        fields.Controls.Add(input, 1, row);
    }

    private static ComboBox CreateCombo(
        System.Collections.Generic.IReadOnlyList<
            FinanceOptionDto> items)
    {
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            DisplayMember = nameof(FinanceOptionDto.Name),
            ValueMember = nameof(FinanceOptionDto.Id)
        };

        combo.DataSource = items.ToList();
        return combo;
    }

    private static NumericUpDown CreateMoneyInput()
    {
        return new NumericUpDown
        {
            DecimalPlaces = 2,
            Minimum = 0.01m,
            Maximum = 9999999999999999.99m,
            Value = 1m,
            ThousandsSeparator = true
        };
    }

    private static T? Selected<T>(
        DataGridView grid) where T : class
    {
        return grid.CurrentRow?.DataBoundItem as T;
    }

    private void ShowError(Exception ex)
    {
        if (IsDisposed) return;

        MessageBox.Show(
            this,
            ex.Message,
            "Finance Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
