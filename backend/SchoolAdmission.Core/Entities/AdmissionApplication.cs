using SchoolAdmission.Core.Enums;

namespace SchoolAdmission.Core.Entities;

public class AdmissionApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public Guid ClassShiftId { get; set; }
    public ClassShift ClassShift { get; set; } = null!;
    public ApplicationStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime HoldExpiresAtUtc { get; set; }
    public DateTime? ConfirmedAtUtc { get; set; }
    public DateTime? ExpiredAtUtc { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
