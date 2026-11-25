
using SchoolManagement.Application.Academics.DTOs;

namespace SchoolManagement.Application.Academics.Interfaces;

public interface IRoomService
{
    Task<IReadOnlyList<RoomDto>> GetAllAsync(
        string? search = null);

    Task<RoomDto?> GetByIdAsync(int id);

    Task<int> CreateAsync(SaveRoomRequest request);

    Task UpdateAsync(int id, SaveRoomRequest request);

    Task SetActiveAsync(int id, bool active);
}
