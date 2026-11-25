using Hospital.BLL.ModelVM;
using Hospital.BLL.Services.Abstraction;
using Hospital.DAL.Entities;
using Hospital.DAL.Repository.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Hospital.BLL.Services.Implementation
{
    public class DoctorService : IDoctorService
    {
        private readonly IDoctorRepository doctorRepository;

        public DoctorService(IDoctorRepository doctorRepository) 
        {
            this.doctorRepository = doctorRepository;
        }
        public List<Doctor> GetAllDoctors()
        {
           return doctorRepository.GetAllDoctors();
        }

        public List<PublicDoctorVm> GetPublicDoctorProfiles(int maximumResults)
        {
            var take = Math.Clamp(maximumResults, 1, 32);
            return doctorRepository.GetPublicDoctors()
                .OrderByDescending(doctor => doctor.IsTopRated)
                .ThenByDescending(doctor => doctor.Rating)
                .ThenBy(doctor => doctor.FirstName)
                .Take(take)
                .Select(doctor => new PublicDoctorVm
                {
                    Id = doctor.Id,
                    FirstName = doctor.FirstName,
                    LastName = doctor.LastName,
                    SpecializationName = doctor.Specialization == null ? null : doctor.Specialization.Name,
                    DepartmentName = doctor.Department == null ? (doctor.Specialization == null ? null : doctor.Specialization.Name) : doctor.Department.Name,
                    Title = doctor.Title ?? "Consultant Specialist",
                    Credentials = doctor.Credentials ?? "MD, Board Certified",
                    Bio = doctor.Bio ?? "Board-certified specialist committed to serene, evidence-based clinical excellence at MANARA Medical Complex.",
                    Image = doctor.Image,
                    ClinicRoom = doctor.ClinicRoom ?? "Main Pavilion",
                    SessionPrice = doctor.SessionPrice > 0 ? doctor.SessionPrice : 450m,
                    FollowUpFee = doctor.FollowUpFee > 0 ? doctor.FollowUpFee : 150m,
                    ExperienceYears = doctor.ExperienceYears > 0 ? doctor.ExperienceYears : 10,
                    Rating = doctor.Rating > 0 ? doctor.Rating : 4.8m,
                    ReviewCount = doctor.ReviewCount > 0 ? doctor.ReviewCount : 48,
                    IsTopRated = doctor.IsTopRated,
                    SealEmblem = doctor.SealEmblem ?? "MANARA • GUILD SEAL"
                })
                .ToList();
        }

        public List<DoctorVm> GetDoctorVms()
        {
            return doctorRepository.GetAllDoctors().Select(d =>
            
                new DoctorVm { Id = d.Id,FirstName = d.FirstName, LastName = d.LastName, Salary = d.Salary, Image = d.Image ,Specialization = d.Specialization?.Name ?? "no sepcialization" }
            ).ToList();
        }

        public async Task<Doctor> DoctorByIdAsync(string id)
        {
            return await doctorRepository.GetDoctorById(id);
        }

        public async Task<Doctor> GetDoctorAndSchedulesById(string id)
        {
           return await  doctorRepository.GetDoctorAndSchedulesById(id);
        }
    }
}
