using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Test1APBD.Model;

namespace Test1APBD.Services;


public interface IAppointmentsService
{
    Task<IActionResult> GetAppointmentById(int id);
    Task<IActionResult> CreateAppointment(CreateAppointmentDTO dto);
}