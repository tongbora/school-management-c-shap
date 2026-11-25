using System;
using System.Collections.Generic;
using System.Text;


namespace SchoolManagement.Application.Teachers.DTOs;

public sealed record TeacherDto(
    int TeacherId,
    string TeacherCode,
    string FullName,
    string? Gender,
    DateOnly? DateOfBirth,
    string? Phone,
    string? Email,
    string? SubjectSpecialty,
    int? DepartmentId,
    DateOnly HireDate,
    string Status
);
