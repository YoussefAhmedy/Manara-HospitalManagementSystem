using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hospital.DAL.Entities.config;

internal sealed class MedicalRecordConfig : IEntityTypeConfiguration<MedicalRecord>
{
    public void Configure(EntityTypeBuilder<MedicalRecord> builder)
    {
        builder.HasOne(record => record.Patient)
            .WithMany(patient => patient.MedicalRecords)
            .HasForeignKey(record => record.PatientID)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(record => record.Doctor)
            .WithMany(doctor => doctor.MedicalRecords)
            .HasForeignKey(record => record.DoctorID)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(record => new { record.PatientID, record.RecordDate });
        builder.HasIndex(record => new { record.DoctorID, record.RecordDate });
    }
}
