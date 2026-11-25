
using System.Drawing;
using System.Windows.Forms;
using SchoolManagement.Application.Academics.DTOs;
using SchoolManagement.Application.Academics.Interfaces;

namespace SchoolManagement.Desktop.Controls.Academics;

public sealed class ClassEditForm : Form
{
    private readonly IClassService _service;
    private readonly int? _id;

    private readonly TextBox name = new();
    private readonly TextBox grade = new();
    private readonly ComboBox year = new();
    private readonly ComboBox room = new();
    private readonly ComboBox teacher = new();
    private readonly NumericUpDown capacity = new();
    private readonly Button save = new() { Text = "Save" };
    private readonly Button cancel = new() { Text = "Cancel" };

    public ClassEditForm(IClassService service, int? id = null)
    {
        _service = service;
        _id = id;

        Text = id.HasValue ? "Edit Class" : "Add Class";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 460);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(249, 250, 251);

        BuildUi();

        Shown += async (_, _) => await LoadAsync();
    }

    private void BuildUi()
    {
        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(20, 20, 20, 8)
        };

        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 170));
        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        for (int i = 0; i < 6; i++)
            fields.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 53));

        name.MaxLength = 100;
        grade.MaxLength = 50;
        capacity.Minimum = 1;
        capacity.Maximum = 100000;
        capacity.Value = 30;

        foreach (ComboBox combo in new[] { year, room, teacher })
        {
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.DisplayMember = nameof(AcademicOptionDto.Name);
            combo.ValueMember = nameof(AcademicOptionDto.Id);
        }

        AddField(fields, 0, "Class Name *", name);
        AddField(fields, 1, "Grade Level *", grade);
        AddField(fields, 2, "Academic Year *", year);
        AddField(fields, 3, "Room", room);
        AddField(fields, 4, "Homeroom Teacher", teacher);
        AddField(fields, 5, "Capacity *", capacity);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 68,
            Padding = new Padding(15, 14, 20, 10),
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.White
        };

        StyleButton(save, true);
        StyleButton(cancel, false);
        cancel.DialogResult = DialogResult.Cancel;
        save.Click += async (_, _) => await SaveAsync();

        footer.Controls.Add(save);
        footer.Controls.Add(cancel);

        Controls.Add(fields);
        Controls.Add(footer);

        AcceptButton = save;
        CancelButton = cancel;
    }

    private static void AddField(
        TableLayoutPanel layout, int row, string label, Control input)
    {
        layout.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, row);

        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(3, 8, 3, 8);
        layout.Controls.Add(input, 1, row);
    }

    private static void StyleButton(Button button, bool primary)
    {
        button.Size = new Size(108, 36);
        button.Margin = new Padding(8, 0, 0, 0);
        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.BackColor = primary
            ? Color.FromArgb(37, 99, 235)
            : Color.White;
        button.ForeColor = primary ? Color.White : Color.Black;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
    }

    private static void BindOptions(
        ComboBox combo,
        IReadOnlyList<AcademicOptionDto> values,
        bool optional)
    {
        var options = values.ToList();

        if (optional)
            options.Insert(0, new AcademicOptionDto(
                0, "-- None --"));

        combo.DataSource = options;
    }

    private static void SelectOption(ComboBox combo, int? id)
    {
        int target = id ?? 0;
        bool found = combo.Items.Cast<AcademicOptionDto>()
            .Any(x => x.Id == target);

        if (!found)
            throw new InvalidOperationException(
                "A previously assigned option is unavailable. " +
                "Review its status before editing this class.");

        combo.SelectedValue = target;
    }

    private async Task LoadAsync()
    {
        save.Enabled = false;

        try
        {
            var years = await _service.GetYearsAsync();
            var rooms = await _service.GetRoomsAsync();
            var teachers = await _service.GetTeachersAsync();

            BindOptions(year, years, false);
            BindOptions(room, rooms, true);
            BindOptions(teacher, teachers, true);

            if (!_id.HasValue)
            {
                if (years.Count == 0)
                    throw new InvalidOperationException(
                        "Create an academic year first.");

                return;
            }

            var entity = await _service.GetByIdAsync(_id.Value)
                ?? throw new InvalidOperationException(
                    "Class not found.");

            name.Text = entity.ClassName;
            grade.Text = entity.GradeLevel;
            capacity.Value = entity.Capacity;

            SelectOption(year, entity.AcademicYearId);
            SelectOption(room, entity.RoomId);
            SelectOption(teacher, entity.HomeroomTeacherId);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Load Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }
        finally
        {
            if (!IsDisposed) save.Enabled = true;
        }
    }

    private async Task SaveAsync()
    {
        if (year.SelectedItem is not AcademicOptionDto y)
        {
            MessageBox.Show(this, "Choose an academic year.");
            return;
        }

        var selectedRoom = room.SelectedItem as AcademicOptionDto;
        var selectedTeacher = teacher.SelectedItem as AcademicOptionDto;

        var request = new SaveClassRequest(
            name.Text.Trim(),
            grade.Text.Trim(),
            y.Id,
            selectedRoom?.Id > 0 ? selectedRoom.Id : null,
            selectedTeacher?.Id > 0 ? selectedTeacher.Id : null,
            (int)capacity.Value);

        save.Enabled = false;
        cancel.Enabled = false;

        try
        {
            if (_id.HasValue)
                await _service.UpdateAsync(_id.Value, request);
            else
                await _service.CreateAsync(request);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Save Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (!IsDisposed)
            {
                save.Enabled = true;
                cancel.Enabled = true;
            }
        }
    }
}
