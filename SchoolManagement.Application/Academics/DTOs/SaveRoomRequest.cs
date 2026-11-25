
namespace SchoolManagement.Application.Academics.DTOs;

public sealed record SaveRoomRequest(
    string RoomCode,
    string? RoomName,
    string? Building,
    int? FloorNumber,
    int Capacity
);
