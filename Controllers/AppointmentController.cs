using Test1APBD.Services;
using Microsoft.AspNetCore.Mvc;
using Test1APBD.Model;

namespace Test1APBD.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentsService _appointmentsService;

    public AppointmentsController(IAppointmentsService appointmentsService)
    {
        _appointmentsService = appointmentsService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAppointmentById(int id)
    {
        var result = await _appointmentsService.GetAppointmentById(id);
        return result;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDTO DTOcreateAppointment)
    {
        var result = await _appointmentsService.CreateAppointment(DTOcreateAppointment);
        return result;
    }
}