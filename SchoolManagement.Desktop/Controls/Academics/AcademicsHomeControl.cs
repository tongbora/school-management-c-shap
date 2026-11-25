
using System.Drawing;
using System.Windows.Forms;

using SchoolManagement.Application.Academics.DTOs;
using SchoolManagement.Application.Academics.Interfaces;

namespace SchoolManagement.Desktop.Controls.Academics;

public sealed class AcademicsHomeControl : UserControl
{
    private readonly TabControl tabs = new();

    public AcademicsHomeControl(
        IAcademicYearService yearService,
        IDepartmentService departmentService,
        ISubjectService subjectService,
        IRoomService roomService,
        IClassService classService,
        IClassSubjectService classSubjectService)
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(248, 250, 252);

        tabs.Dock = DockStyle.Fill;
        tabs.Font = new Font("Segoe UI", 10);
        tabs.Padding = new Point(18, 8);

        AddTab("Academic Years",
            new AcademicYearListControl(yearService));

        AddTab("Departments",
            new DepartmentListControl(departmentService));

        AddTab("Subjects",
            new SubjectListControl(subjectService));

        AddTab("Rooms",
            new AcademicEntityListControl<RoomDto>(
                search => roomService.GetAllAsync(search),
                x => x.RoomId,
                x => x.IsActive,
                x => x.RoomCode,
                id => new RoomEditForm(roomService, id),
                (id, active) => roomService.SetActiveAsync(id, active),
                ("RoomCode", "Room Code"),
                ("RoomName", "Room Name"),
                ("Building", "Building"),
                ("FloorNumber", "Floor"),
                ("Capacity", "Capacity"),
                ("IsActive", "Active")));

        AddTab("Classes",
            new AcademicEntityListControl<ClassDto>(
                search => classService.GetAllAsync(search),
                x => x.ClassId,
                x => x.IsActive,
                x => x.ClassName,
                id => new ClassEditForm(classService, id),
                (id, active) => classService.SetActiveAsync(id, active),
                ("ClassName", "Class Name"),
                ("GradeLevel", "Grade"),
                ("AcademicYearName", "Academic Year"),
                ("RoomName", "Room"),
                ("HomeroomTeacherName", "Homeroom Teacher"),
                ("Capacity", "Capacity"),
                ("IsActive", "Active")));

        AddTab(
            "Class Subjects",
            new ClassSubjectListControl(classSubjectService));


        Controls.Add(tabs);
    }

    private void AddTab(string title, Control content)
    {
        var page = new TabPage(title)
        {
            BackColor = Color.White
        };

        content.Dock = DockStyle.Fill;
        page.Controls.Add(content);
        tabs.TabPages.Add(page);
    }
}
