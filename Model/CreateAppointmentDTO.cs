namespace Test1APBD.Model;

public class CreateAppointmentDTO
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public string Pwz { get; set; }
    public List<ServiceDTO> Services { get; set; }
}