using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hospital.DAL.Entities
{
    public class Doctor : ApplicationUser
    {
        /// <summary>Monthly salary is restricted business data, visible only to administration.</summary>
        public decimal Salary { get; set; }

        public decimal SessionPrice { get; set; }

        /// <summary>Discounted consultation rate offered to returning patients for follow-up visits.</summary>
        public decimal FollowUpFee { get; set; } = 150m;

        public int ExperienceYears { get; set; } = 10;

        public decimal Rating { get; set; } = 4.8m;

        public int ReviewCount { get; set; } = 42;

        public bool IsTopRated { get; set; }

        [MaxLength(120)]
        public string? Credentials { get; set; }

        [MaxLength(120)]
        public string? Title { get; set; }

        [MaxLength(400)]
        public string? Bio { get; set; }

        [MaxLength(80)]
        public string? SealEmblem { get; set; }

        /// <summary>Appointment slot length in minutes; drives availability generation.</summary>
        [Range(5, 120)]
        public int SlotMinutes { get; set; } = 20;

        [MaxLength(20)]
        public string? EmployeeNumber { get; set; }

        [MaxLength(60)]
        public string? ClinicRoom { get; set; }

        [ForeignKey("Specialization")]
        public int? SpecializationId { get; set; }
        public Specialization? Specialization { get; set; }

        public int? DepartmentId { get; set; }
        public Organization.Department? Department { get; set; }

        public bool IsActive { get; set; } = true;

        public List<Schedule>? Schedules { get; set; }
        public List<Appointment>? Appointments { get; set; }
        public List<MedicalRecord>? MedicalRecords { get; set; }
    }
}
