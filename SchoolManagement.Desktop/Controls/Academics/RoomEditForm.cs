
using System.Drawing;
using System.Windows.Forms;
using SchoolManagement.Application.Academics.DTOs;
using SchoolManagement.Application.Academics.Interfaces;

namespace SchoolManagement.Desktop.Controls.Academics;

public sealed class RoomEditForm : Form
{
    private readonly IRoomService _service;
    private readonly int? _id;

    private readonly TextBox code = new();
    private readonly TextBox name = new();
    private readonly TextBox building = new();
    private readonly NumericUpDown floor = new();
    private readonly NumericUpDown capacity = new();
    private readonly Button save = new() { Text = "Save" };
    private readonly Button cancel = new() { Text = "Cancel" };

    public RoomEditForm(IRoomService service, int? id = null)
    {
        _service = service;
        _id = id;
        Text = id.HasValue ? "Edit Room" : "Add Room";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(500, 395);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(249, 250, 251);

        BuildUi();

        Shown += async (_, _) =>
        {
            if (!_id.HasValue) return;
            save.Enabled = false;
            try
            {
                var room = await _service.GetByIdAsync(_id.Value)
                    ?? throw new InvalidOperationException("Room not found.");

                code.Text = room.RoomCode;
                name.Text = room.RoomName ?? "";
                building.Text = room.Building ?? "";
                floor.Value = room.FloorNumber ?? 0;
                capacity.Value = room.Capacity;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Load Error");
                Close();
            }
            finally
            {
                if (!IsDisposed) save.Enabled = true;
            }
        };
    }

    private void BuildUi()
    {
        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(20, 20, 20, 8)
        };
        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 145));
        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        for (int i = 0; i < 5; i++)
            fields.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 53));

        code.MaxLength = 50;
        name.MaxLength = 100;
        building.MaxLength = 100;
        floor.Minimum = -20;
        floor.Maximum = 200;
        capacity.Minimum = 1;
        capacity.Maximum = 100000;
        capacity.Value = 30;

        AddField(fields, 0, "Room Code *", code);
        AddField(fields, 1, "Room Name", name);
        AddField(fields, 2, "Building", building);
        AddField(fields, 3, "Floor (0 = none)", floor);
        AddField(fields, 4, "Capacity *", capacity);

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

    private async Task SaveAsync()
    {
        var request = new SaveRoomRequest(
            code.Text.Trim(),
            name.Text,
            building.Text,
            floor.Value == 0 ? null : (int)floor.Value,
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
