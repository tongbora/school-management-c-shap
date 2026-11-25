
using SchoolManagement.Application.Dashboard.Interfaces;
using System.Drawing;
using System.Windows.Forms;

namespace SchoolManagement.Desktop.Controls.Dashboard;

public sealed class DashboardControl : UserControl
{
    private readonly IDashboardService _dashboardService;

    private readonly Label lblStudents = new();
    private readonly Label lblTeachers = new();
    private readonly Label lblClasses = new();
    private readonly Label lblEnrollments = new();

    private readonly Label lblStatus = new();

    public DashboardControl(
        IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(245, 247, 251);

        BuildLayout();

        Load += DashboardControl_Load;
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            ColumnCount = 4,
            RowCount = 3
        };

        for (int i = 0; i < 4; i++)
        {
            layout.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent, 25));
        }

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 65));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 160));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        var heading = new Label
        {
            Text = "Dashboard Overview",
            Font = new Font(
                "Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 58, 95),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        layout.Controls.Add(heading, 0, 0);
        layout.SetColumnSpan(heading, 4);

        layout.Controls.Add(
            CreateStatCard("Total Students", lblStudents),
            0, 1);

        layout.Controls.Add(
            CreateStatCard("Total Teachers", lblTeachers),
            1, 1);

        layout.Controls.Add(
            CreateStatCard("Total Classes", lblClasses),
            2, 1);

        layout.Controls.Add(
            CreateStatCard("Total Enrollments", lblEnrollments),
            3, 1);

        lblStatus.Text = "Loading dashboard...";
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.ForeColor = Color.Gray;
        lblStatus.TextAlign = ContentAlignment.TopLeft;
        lblStatus.Padding = new Padding(8, 24, 0, 0);

        layout.Controls.Add(lblStatus, 0, 2);
        layout.SetColumnSpan(lblStatus, 4);

        Controls.Add(layout);
    }

    private static Panel CreateStatCard(
        string title,
        Label valueLabel)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(8),
            Padding = new Padding(16)
        };

        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 40,
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.Gray
        };

        valueLabel.Text = "—";
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.Font = new Font(
            "Segoe UI", 26, FontStyle.Bold);
        valueLabel.ForeColor =
            Color.FromArgb(37, 99, 235);

        card.Controls.Add(valueLabel);
        card.Controls.Add(titleLabel);

        return card;
    }

    private async void DashboardControl_Load(
        object? sender,
        EventArgs e)
    {
        try
        {
            var summary =
                await _dashboardService.GetSummaryAsync();

            if (IsDisposed)
                return;

            lblStudents.Text =
                summary.TotalStudents.ToString("N0");

            lblTeachers.Text =
                summary.TotalTeachers.ToString("N0");

            lblClasses.Text =
                summary.TotalClasses.ToString("N0");

            lblEnrollments.Text =
                summary.TotalEnrollments.ToString("N0");

            lblStatus.Text = "Dashboard loaded successfully.";
        }
        catch (Exception)
        {
            lblStatus.Text =
                "Unable to load dashboard statistics.";
        }
    }
}
