using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Core.DTOs;
using SchoolAdmission.Core.Interfaces;

namespace SchoolAdmission.API.Controllers;

[ApiController]
[Route("api")]
public class ClassesController : ControllerBase
{
    private readonly IAdmissionService _admissionService;

    public ClassesController(IAdmissionService admissionService)
    {
        _admissionService = admissionService;
    }

    [HttpGet("shifts")]
    public async Task<ActionResult<IReadOnlyList<ShiftDto>>> GetShifts()
    {
        return Ok(await _admissionService.GetShiftsAsync());
    }

    [HttpPost("shifts")]
    public async Task<ActionResult<ShiftDto>> CreateShift([FromBody] CreateShiftRequest request)
    {
        var result = await _admissionService.CreateShiftAsync(request);
        return Created("/api/shifts", result);
    }

    [HttpGet("classes")]
    public async Task<ActionResult<IReadOnlyList<SchoolClassDto>>> GetClasses()
    {
        return Ok(await _admissionService.GetClassesAsync());
    }

    [HttpGet("classes/{classId:guid}")]
    public async Task<ActionResult<SchoolClassDto>> GetClass(Guid classId)
    {
        var result = await _admissionService.GetClassAsync(classId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("classes/availability")]
    public async Task<ActionResult<IReadOnlyList<ClassAvailabilityDto>>> GetAvailability([FromQuery] Guid? classId = null)
    {
        return Ok(await _admissionService.GetClassAvailabilityAsync(classId));
    }

    [HttpPost("classes")]
    public async Task<ActionResult<SchoolClassDto>> CreateClass([FromBody] CreateClassRequest request)
    {
        var result = await _admissionService.CreateClassAsync(request);
        return CreatedAtAction(nameof(GetClass), new { classId = result.Id }, result);
    }

    [HttpPost("classes/with-shift")]
    public async Task<ActionResult<SchoolClassDto>> CreateClassWithShift([FromBody] CreateClassWithShiftRequest request)
    {
        var result = await _admissionService.CreateClassWithShiftAsync(request);
        return CreatedAtAction(nameof(GetClass), new { classId = result.Id }, result);
    }

    [HttpPost("class-shifts")]
    public async Task<ActionResult<SchoolClassDto>> CreateClassShift([FromBody] CreateClassShiftRequest request)
    {
        var result = await _admissionService.CreateClassShiftAsync(request);
        return CreatedAtAction(nameof(GetClass), new { classId = request.ClassId }, result);
    }

    [HttpPut("class-shifts/{classShiftId:guid}/capacity")]
    public async Task<ActionResult<ClassAvailabilityDto>> UpdateClassShiftCapacity(
        Guid classShiftId,
        [FromBody] UpdateClassShiftCapacityRequest request)
    {
        return Ok(await _admissionService.UpdateClassShiftCapacityAsync(classShiftId, request.TotalSeats));
    }
}
