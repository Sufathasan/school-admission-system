using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Core.DTOs;
using SchoolAdmission.Core.Enums;
using SchoolAdmission.Core.Interfaces;

namespace SchoolAdmission.API.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdmissionService _admissionService;

    public AdminController(IAdmissionService admissionService)
    {
        _admissionService = admissionService;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboard()
    {
        return Ok(await _admissionService.GetDashboardStatsAsync());
    }

    [HttpGet("applications")]
    public async Task<ActionResult<IReadOnlyList<AdmissionApplicationListItem>>> GetApplications(
        [FromQuery] Guid? classId = null,
        [FromQuery] Guid? shiftId = null,
        [FromQuery] ApplicationStatus? status = null,
        [FromQuery] string? search = null)
    {
        return Ok(await _admissionService.GetAdminApplicationsAsync(classId, shiftId, status, search));
    }
}
