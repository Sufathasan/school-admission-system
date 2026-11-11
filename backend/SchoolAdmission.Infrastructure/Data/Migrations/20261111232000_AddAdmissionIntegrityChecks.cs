using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SchoolAdmission.Infrastructure.Data;

#nullable disable

namespace SchoolAdmission.Infrastructure.Data.Migrations;

[DbContext(typeof(SchoolAdmissionDbContext))]
[Migration("20261111232000_AddAdmissionIntegrityChecks")]
public partial class AddAdmissionIntegrityChecks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint(
            name: "CK_ClassShifts_OccupiedSeats",
            table: "ClassShifts",
            sql: "\"HeldSeats\" + \"ConfirmedSeats\" <= \"TotalSeats\"");

        migrationBuilder.AddCheckConstraint(
            name: "CK_Students_BirthRegistrationNumber",
            table: "Students",
            sql: "\"BirthRegistrationNumber\" ~ '^[0-9]{17}$'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_ClassShifts_OccupiedSeats",
            table: "ClassShifts");

        migrationBuilder.DropCheckConstraint(
            name: "CK_Students_BirthRegistrationNumber",
            table: "Students");
    }
}
