using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Core.DTOs;
using SchoolAdmission.Core.Interfaces;

namespace SchoolAdmission.API.Controllers;

[ApiController]
[Route("api")]
public class PaymentsController : ControllerBase
{
    private readonly IAdmissionService _admissionService;

    public PaymentsController(IAdmissionService admissionService)
    {
        _admissionService = admissionService;
    }

    [HttpPost("payments/{applicationId:guid}/confirm")]
    public async Task<ActionResult<PaymentConfirmationResponse>> ConfirmPayment(Guid applicationId)
    {
        var result = await _admissionService.ConfirmPaymentAsync(applicationId);
        return Ok(result);
    }
}
