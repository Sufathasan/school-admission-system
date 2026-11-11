namespace SchoolAdmission.Core.Entities;

public class Student
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string BirthRegistrationNumber { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<AdmissionApplication> Applications { get; set; } = new List<AdmissionApplication>();
}
