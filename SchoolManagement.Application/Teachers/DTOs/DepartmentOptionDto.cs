using System;
using System.Collections.Generic;
using System.Text;


namespace SchoolManagement.Application.Teachers.DTOs;

public sealed record DepartmentOptionDto(
    int DepartmentId,
    string DepartmentName
);
