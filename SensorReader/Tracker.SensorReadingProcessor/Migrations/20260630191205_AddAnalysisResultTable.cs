using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tracker.SensorReadingProcessor.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisResultTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "timeseriesanalysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    TrendSlope = table.Column<double>(type: "float", nullable: false),
                    PeakValue = table.Column<double>(type: "float", nullable: false),
                    ValleyValue = table.Column<double>(type: "float", nullable: false),
                    Average = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timeseriesanalysis", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "timeseriesanalysis");
        }
    }
}
