namespace SchoolAdmission.Core.Entities;

public class SchoolClass
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ClassShift> ClassShifts { get; set; } = new List<ClassShift>();
}
