using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hospital.DAL.Entities.config
{
    internal class DoctorConfig : IEntityTypeConfiguration<Doctor>
    {
        public void Configure(EntityTypeBuilder<Doctor> builder)
        {
            builder.ToTable("Doctors");
            builder.Property(d => d.Salary).HasPrecision(18, 2);
            builder.Property(d => d.SessionPrice).HasPrecision(18, 2);
            builder.Property(d => d.FollowUpFee).HasPrecision(18, 2);
            builder.Property(d => d.Rating).HasPrecision(4, 2);
        }
    }
}
