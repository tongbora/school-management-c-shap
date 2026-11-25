using System;
using System.Collections.Generic;

namespace SchoolManagement.Infrastructure.Persistence.Entities;

public partial class Room
{
    public int RoomId { get; set; }

    public string RoomCode { get; set; } = null!;

    public string? RoomName { get; set; }

    public string? Building { get; set; }

    public int? FloorNumber { get; set; }

    public int Capacity { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<Timetable> Timetables { get; set; } = new List<Timetable>();
}
