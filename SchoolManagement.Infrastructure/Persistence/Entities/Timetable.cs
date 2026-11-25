using System;
using System.Collections.Generic;

namespace SchoolManagement.Infrastructure.Persistence.Entities;

public partial class Timetable
{
    public int TimetableId { get; set; }

    public int ClassSubjectId { get; set; }

    public int? RoomId { get; set; }

    public byte DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public virtual ClassSubject ClassSubject { get; set; } = null!;

    public virtual Room? Room { get; set; }
}
