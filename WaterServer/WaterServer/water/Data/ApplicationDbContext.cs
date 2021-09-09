using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using water.Models;

namespace water.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }

        public DbSet<Meter> Meters { get; set; }
        public DbSet<MeterReading> MeterReadings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<MeterReading>()
                .HasIndex(mr => mr.MeterId)
                .IncludeProperties(mr => mr.Date);

            base.OnModelCreating(builder);
        }
    }
}
