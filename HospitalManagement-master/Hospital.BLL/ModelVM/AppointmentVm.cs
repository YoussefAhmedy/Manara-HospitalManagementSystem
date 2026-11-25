using Hospital.DAL.Entities;
using Hospital.DAL.Entities.Organization;
using System.ComponentModel.DataAnnotations;

public class AppointmentVm
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Appointment date is required.")]
    [Display(Name = "Date")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime AppointmentDate { get; set; }

    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    public string? Notes { get; set; }

    [Display(Name = "Medical Department")]
    public int? DepartmentId { get; set; }

    [Required(ErrorMessage = "Doctor is required.")]
    [Display(Name = "Doctor")]
    public string DoctorId { get; set; } = string.Empty;

    public List<Doctor>? Doctors { get; set; }

    public List<Department>? Departments { get; set; }
}
