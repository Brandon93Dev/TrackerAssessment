using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tracker.Publisher.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredProcs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create stored proc for persising sensor readings to db
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_SavePendingReading 
                    @Timestamp DATETIMEOFFSET,
                    @Value FLOAT,
                    @SensorType NVARCHAR(50)
                AS 
                BEGIN
                    SET NOCOUNT ON;

                    INSERT INTO pendingreadings (ID, Timestamp, Value, SensorType)
                    VALUES (NEWID(), @Timestamp, @Value, @SensorType);
                END
            ");

            // Create a stored proc to get Oldest Pending data
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_GetOldestPending
                AS
                BEGIN
                    SELECT TOP (1) * FROM pendingreadings
                    ORDER BY Timestamp ASC
                END
            ");          
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_SavePendingReading;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetOldestPending;");
        }
    }
}
