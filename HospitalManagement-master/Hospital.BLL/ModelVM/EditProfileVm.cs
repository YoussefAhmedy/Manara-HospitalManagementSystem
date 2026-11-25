using System.ComponentModel.DataAnnotations;

namespace Hospital.BLL.ModelVM
{
    public class EditProfileVm
    {
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(40, ErrorMessage = "First name must be 40 characters or fewer.")]
        [MinLength(2, ErrorMessage = "First name must be at least 2 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(40, ErrorMessage = "Last name must be 40 characters or fewer.")]
        [MinLength(2, ErrorMessage = "Last name must be at least 2 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [RegularExpression(@"^$|^(?:\+20|0)?(1[0125]\d{8})$", ErrorMessage = "Invalid Egyptian mobile number (leave empty if unchanged).")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? Image { get; set; }
    }
}
