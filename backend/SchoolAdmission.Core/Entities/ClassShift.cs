namespace SchoolAdmission.Core.Entities;

public class ClassShift
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SchoolClassId { get; set; }
    public SchoolClass SchoolClass { get; set; } = null!;
    public Guid ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;
    public int TotalSeats { get; set; }
    public int HeldSeats { get; set; }
    public int ConfirmedSeats { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<AdmissionApplication> Applications { get; set; } = new List<AdmissionApplication>();

    public int AvailableSeats => TotalSeats - HeldSeats - ConfirmedSeats;
}
