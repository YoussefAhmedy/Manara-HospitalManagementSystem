using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hospital.DAL.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string LastName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        
        public string? Gender { get; set; } 

        
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string? Image { get; set; }

        // Notification & accessibility preferences
        public bool EmailNotifyLabs { get; set; } = true;
        public bool EmailNotifyPrescriptions { get; set; } = true;
        public bool EmailNotifyAppointments { get; set; } = true;
        public bool EmailNotifyReplies { get; set; } = true;

        [MaxLength(16)]
        public string UiFontSize { get; set; } = "default";

        [MaxLength(16)]
        public string UiDensity { get; set; } = "comfortable";

        public bool UiReducedMotion { get; set; }
    }
}
