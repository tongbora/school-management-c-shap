
namespace SchoolManagement.Application.Academics.DTOs;

public sealed record RoomDto(
    int RoomId,
    string RoomCode,
    string? RoomName,
    string? Building,
    int? FloorNumber,
    int Capacity,
    bool IsActive
);
