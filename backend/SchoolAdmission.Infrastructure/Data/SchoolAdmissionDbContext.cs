using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Core.Entities;

namespace SchoolAdmission.Infrastructure.Data;

public class SchoolAdmissionDbContext : DbContext
{
    public SchoolAdmissionDbContext(DbContextOptions<SchoolAdmissionDbContext> options) : base(options)
    {
    }

    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<ClassShift> ClassShifts => Set<ClassShift>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<AdmissionApplication> Applications => Set<AdmissionApplication>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SchoolClass>(entity =>
        {
            entity.ToTable("SchoolClasses", table =>
                table.HasCheckConstraint("CK_SchoolClasses_TotalSeats", "\"TotalSeats\" >= 0"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(120);
            entity.Property(x => x.TotalSeats).IsRequired();
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.ToTable("Shifts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(60);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<ClassShift>(entity =>
        {
            entity.ToTable("ClassShifts", table =>
            {
                table.HasCheckConstraint("CK_ClassShifts_TotalSeats", "\"TotalSeats\" >= 0");
                table.HasCheckConstraint("CK_ClassShifts_HeldSeats", "\"HeldSeats\" >= 0");
                table.HasCheckConstraint("CK_ClassShifts_ConfirmedSeats", "\"ConfirmedSeats\" >= 0");
                table.HasCheckConstraint(
                    "CK_ClassShifts_OccupiedSeats",
                    "\"HeldSeats\" + \"ConfirmedSeats\" <= \"TotalSeats\"");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TotalSeats).IsRequired();
            entity.HasIndex(x => new { x.SchoolClassId, x.ShiftId }).IsUnique();
            entity.HasOne(x => x.SchoolClass)
                .WithMany(x => x.ClassShifts)
                .HasForeignKey(x => x.SchoolClassId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Shift)
                .WithMany(x => x.ClassShifts)
                .HasForeignKey(x => x.ShiftId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students", table =>
                table.HasCheckConstraint(
                    "CK_Students_BirthRegistrationNumber",
                    "\"BirthRegistrationNumber\" ~ '^[0-9]{17}$'"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.BirthRegistrationNumber).IsRequired().HasMaxLength(17);
            entity.HasIndex(x => x.BirthRegistrationNumber).IsUnique();
        });

        modelBuilder.Entity<AdmissionApplication>(entity =>
        {
            entity.ToTable("Applications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>();
            entity.HasIndex(x => new { x.StudentId, x.Status }).HasDatabaseName("IX_Applications_Student_Status");
            entity.HasIndex(x => x.ClassShiftId).HasDatabaseName("IX_Applications_ClassShift");
            entity.HasIndex(x => x.HoldExpiresAtUtc).HasDatabaseName("IX_Applications_HoldExpiresAt");
            entity.HasOne(x => x.Student)
                .WithMany(x => x.Applications)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ClassShift)
                .WithMany(x => x.Applications)
                .HasForeignKey(x => x.ClassShiftId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Status).HasConversion<string>();
            entity.HasIndex(x => x.ApplicationId).IsUnique();
            entity.HasOne(x => x.Application)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
