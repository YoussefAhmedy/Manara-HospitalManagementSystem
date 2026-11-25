using Hospital.DAL.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hospital.BLL.ModelVM
{
    public class CreateDoctorViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(20, ErrorMessage = "First name must be 20 characters or fewer.")]
        [MinLength(3, ErrorMessage = "First name must be at least 3 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(20, ErrorMessage = "Last name must be 20 characters or fewer.")]
        [MinLength(3, ErrorMessage = "Last name must be at least 3 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^(?:\+20|0)?(1[0125]\d{8})$", ErrorMessage = "Please enter a valid Egyptian mobile number.")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [Range(10000, 80000)]
        [Display(Name = "Monthly Salary (EGP)")]
        public decimal Salary { get; set; }

        [Required]
        [Range(200, 2000)]
        [Display(Name = "Session Price (EGP)")]
        public decimal SessionPrice { get; set; }

        [Range(80, 1000)]
        [Display(Name = "Follow-Up Fee (EGP)")]
        public decimal FollowUpFee { get; set; } = 150m;

        [Display(Name = "Credentials")]
        public string? Credentials { get; set; }

        [Display(Name = "Title / Designation")]
        public string? Title { get; set; }

        [Display(Name = "Clinic Room")]
        public string? ClinicRoom { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        public string ConfirmedPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessage = "Specialization is required.")]
        [Display(Name = "Specialization")]
        public int SpecializationId { get; set; }

        public IEnumerable<Specialization>? Specializations { get; set; }
    }
}
