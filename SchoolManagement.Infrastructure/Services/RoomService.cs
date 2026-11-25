
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Application.Academics.DTOs;
using SchoolManagement.Application.Academics.Interfaces;
using SchoolManagement.Infrastructure.Persistence;
using SchoolManagement.Infrastructure.Persistence.Entities;

namespace SchoolManagement.Infrastructure.Services;

public sealed class RoomService : IRoomService
{
    private readonly IDbContextFactory<SchoolDbContext> _factory;

    public RoomService(IDbContextFactory<SchoolDbContext> factory)
        => _factory = factory;

    public async Task<IReadOnlyList<RoomDto>> GetAllAsync(
        string? search = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Rooms.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(x =>
                x.RoomCode.Contains(keyword) ||
                (x.RoomName != null &&
                 x.RoomName.Contains(keyword)) ||
                (x.Building != null &&
                 x.Building.Contains(keyword)));
        }

        return await query
            .OrderBy(x => x.RoomCode)
            .Select(x => new RoomDto(
                x.RoomId, x.RoomCode, x.RoomName,
                x.Building, x.FloorNumber, x.Capacity,
                x.IsActive))
            .ToListAsync();
    }

    public async Task<RoomDto?> GetByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Rooms.AsNoTracking()
            .Where(x => x.RoomId == id)
            .Select(x => new RoomDto(
                x.RoomId, x.RoomCode, x.RoomName,
                x.Building, x.FloorNumber, x.Capacity,
                x.IsActive))
            .FirstOrDefaultAsync();
    }

    public async Task<int> CreateAsync(SaveRoomRequest request)
    {
        Validate(request);
        await using var db = await _factory.CreateDbContextAsync();

        string code = request.RoomCode.Trim();

        if (await db.Rooms.AnyAsync(x => x.RoomCode == code))
            throw new InvalidOperationException(
                "Room code already exists.");

        var room = new Room
        {
            RoomCode = code,
            RoomName = Clean(request.RoomName),
            Building = Clean(request.Building),
            FloorNumber = request.FloorNumber,
            Capacity = request.Capacity,
            IsActive = true
        };

        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return room.RoomId;
    }

    public async Task UpdateAsync(int id, SaveRoomRequest request)
    {
        Validate(request);
        await using var db = await _factory.CreateDbContextAsync();

        var room = await db.Rooms.FindAsync(id)
            ?? throw new InvalidOperationException("Room not found.");

        string code = request.RoomCode.Trim();

        if (await db.Rooms.AnyAsync(
            x => x.RoomCode == code && x.RoomId != id))
            throw new InvalidOperationException(
                "Room code already exists.");

        // Don't reduce room capacity below existing class capacity.
        bool capacityConflict = await db.Classes.AnyAsync(x =>
            x.RoomId == id && x.IsActive &&
            x.Capacity > request.Capacity);

        if (capacityConflict)
            throw new InvalidOperationException(
                "An active class requires more seats than this capacity.");

        room.RoomCode = code;
        room.RoomName = Clean(request.RoomName);
        room.Building = Clean(request.Building);
        room.FloorNumber = request.FloorNumber;
        room.Capacity = request.Capacity;

        await db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(int id, bool active)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var room = await db.Rooms.FindAsync(id)
            ?? throw new InvalidOperationException("Room not found.");

        if (!active && await db.Classes.AnyAsync(x =>
            x.RoomId == id && x.IsActive))
            throw new InvalidOperationException(
                "Cannot deactivate a room assigned to an active class.");

        if (!active && await db.Timetables.AnyAsync(x =>
            x.RoomId == id))
            throw new InvalidOperationException(
                "This room is used in timetables. Review assignments first.");

        room.IsActive = active;
        await db.SaveChangesAsync();
    }

    private static void Validate(SaveRoomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RoomCode))
            throw new ArgumentException("Room code is required.");

        if (request.RoomCode.Trim().Length > 50)
            throw new ArgumentException("Room code exceeds 50 characters.");

        if (request.RoomName?.Trim().Length > 100)
            throw new ArgumentException("Room name exceeds 100 characters.");

        if (request.Building?.Trim().Length > 100)
            throw new ArgumentException("Building exceeds 100 characters.");

        if (request.Capacity <= 0)
            throw new ArgumentException("Capacity must be greater than zero.");
    }

    private static string? Clean(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
