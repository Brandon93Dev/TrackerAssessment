using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tracker.SensorReadingProcessor.Migrations
{
    /// <inheritdoc />
    public partial class CreatePersistAnalysisProc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_InsertAnalysisResults
                    @Timestamp DATETIMEOFFSET,
                    @Value FLOAT,
                    @TrendSlope FLOAT,
                    @PeakValue FLOAT,
                    @ValleyValue FLOAT,
                    @Average FLOAT
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    INSERT INTO timeseriesanalysis (Id, Timestamp, Value, TrendSlope, PeakValue, ValleyValue, Average)
                    VALUES (NEWID(), @Timestamp, @Value, @TrendSlope, @PeakValue, @ValleyValue, @Average);           
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_InsertAnalysisResults;");
        }
    }
}
