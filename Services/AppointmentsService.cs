using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Test1APBD.Model;

namespace Test1APBD.Services;
    public class AppointmentsService : IAppointmentsService
    {
        private readonly string _connectionString;

        public AppointmentsService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> GetAppointmentById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT ap.date,
                       pa.first_name, pa.last_name, pa.date_of_birth,
                       do.doctor_id, do.pwz,
                       se.name AS service_name, aps.service_fee
                FROM Appointment ap
                JOIN Patient pa ON ap.patient_id = pa.patient_id
                JOIN Doctor do ON ap.doctor_id = do.doctor_id
                LEFT JOIN Appointment_Service aps ON aps.appoitment_id = ap.appoitment_id
                LEFT JOIN Service se ON se.service_id = aps.service_id
                WHERE ap.appoitment_id = @id", conn);

            cmd.Parameters.AddWithValue("@id", id);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows)
                return new NotFoundObjectResult(new { message = "This appointment is not found" });

            DateTime date = default;
            string firstName = "", lastName = "", pwz = "";
            DateTime dob = default;
            int doctorId = 0;
            var services = new List<object>();

            while (await reader.ReadAsync())
            {
                if (services.Count == 0)
                {
                    date = reader.GetDateTime(0);
                    firstName = reader.GetString(1);
                    lastName = reader.GetString(2);
                    dob = reader.GetDateTime(3);
                    doctorId = reader.GetInt32(4);
                    pwz = reader.GetString(5);
                }

                if (!reader.IsDBNull(6))
                {
                    services.Add(new
                    {
                        name = reader.GetString(6),
                        serviceFee = reader.GetDecimal(7)
                    });
                }
            }

            return new OkObjectResult(new
            {
                date,
                patient = new { firstName, lastName, dateOfBirth = dob },
                doctor = new { doctorId, pwz },
                appointmentServices = services
            });
        }

        public async Task<IActionResult> CreateAppointment(CreateAppointmentDTO dto)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var tran = await conn.BeginTransactionAsync();

            try
            {
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Appointment WHERE appoitment_id = @id", conn, (SqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@id", dto.AppointmentId);
                    if ((int)await cmd.ExecuteScalarAsync() > 0)
                        return new ConflictObjectResult(new { message = "You cannot create appoitnment with ID that is already taken" });
                }

                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Patient WHERE patient_id = @id", conn, (SqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@id", dto.PatientId);
                    if ((int)await cmd.ExecuteScalarAsync() == 0)
                        return new NotFoundObjectResult(new { message = "This patient is not found" });
                }

                int doctorId;
                using (var cmd = new SqlCommand("SELECT doctor_id FROM Doctor WHERE pwz = @pwz", conn, (SqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@pwz", dto.Pwz);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result == null)
                        return new NotFoundObjectResult(new { message = "This doctor is not found" });

                    doctorId = (int)result;
                }

                using (var cmd = new SqlCommand(@"
                    INSERT INTO Appointment (appoitment_id, patient_id, doctor_id, date)
                    VALUES (@appointmentId, @patientId, @doctorId, @date)", conn, (SqlTransaction)tran))
                {
                    cmd.Parameters.AddWithValue("@appointmentId", dto.AppointmentId);
                    cmd.Parameters.AddWithValue("@patientId", dto.PatientId);
                    cmd.Parameters.AddWithValue("@doctorId", doctorId);
                    cmd.Parameters.AddWithValue("@date", DateTime.UtcNow);
                    await cmd.ExecuteNonQueryAsync();
                }

                foreach (var service in dto.Services)
                {
                    int serviceId;
                    using (var cmd = new SqlCommand("SELECT service_id FROM Service WHERE name = @name", conn, (SqlTransaction)tran))
                    {
                        cmd.Parameters.AddWithValue("@name", service.ServiceName);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result == null)
                            return new NotFoundObjectResult(new { message = $"Service '{service.ServiceName}' not found" });

                        serviceId = (int)result;
                    }

                    using (var cmd = new SqlCommand(@"
                        INSERT INTO Appointment_Service (appoitment_id, service_id, service_fee)
                        VALUES (@appointmentId, @serviceId, @fee)", conn, (SqlTransaction)tran))
                    {
                        cmd.Parameters.AddWithValue("@appointmentId", dto.AppointmentId);
                        cmd.Parameters.AddWithValue("@serviceId", serviceId);
                        cmd.Parameters.AddWithValue("@fee", service.ServiceFee);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                await tran.CommitAsync();
                return new CreatedAtActionResult("GetAppointmentById", "Appointments", new { id = dto.AppointmentId }, new { message = "Appointment created successfully" });
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                return new ObjectResult(new { message = "Some error has occurred.", details = ex.Message }) { StatusCode = 500 };
            }
        }
    }
