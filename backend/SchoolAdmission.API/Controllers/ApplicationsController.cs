using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Core.DTOs;
using SchoolAdmission.Core.Interfaces;

namespace SchoolAdmission.API.Controllers;

[ApiController]
[Route("api")]
public class ApplicationsController : ControllerBase
{
    private readonly IAdmissionService _admissionService;

    public ApplicationsController(IAdmissionService admissionService)
    {
        _admissionService = admissionService;
    }

    [HttpPost("applications")]
    public async Task<ActionResult<ApplicationSubmissionResponse>> CreateApplication([FromBody] CreateApplicationRequest request)
    {
        var result = await _admissionService.CreateApplicationAsync(request);
        return CreatedAtAction(nameof(GetApplication), new { applicationId = result.ApplicationId }, result);
    }

    [HttpGet("applications/{applicationId:guid}")]
    public async Task<ActionResult<ApplicationDetailDto>> GetApplication(Guid applicationId)
    {
        var result = await _admissionService.GetApplicationAsync(applicationId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("students/{studentId:guid}/applications")]
    public async Task<ActionResult<IReadOnlyList<ApplicationDetailDto>>> GetStudentApplications(Guid studentId)
    {
        return Ok(await _admissionService.GetStudentApplicationsAsync(studentId));
    }

    [HttpGet("applications/{applicationId:guid}/status")]
    public async Task<ActionResult<ApplicationStatusDto>> GetApplicationStatus(Guid applicationId)
    {
        return Ok(await _admissionService.GetApplicationStatusAsync(applicationId));
    }
}
