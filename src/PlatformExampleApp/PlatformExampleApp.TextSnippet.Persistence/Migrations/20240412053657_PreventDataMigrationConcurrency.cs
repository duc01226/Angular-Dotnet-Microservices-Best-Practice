using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformExampleApp.TextSnippet.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreventDataMigrationConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlatformOutboxEventBusMessage_LastSendDate_SendStatus",
                table: "PlatformOutboxEventBusMessage");

            migrationBuilder.DropIndex(
                name: "IX_PlatformOutboxEventBusMessage_NextRetryProcessAfter",
                table: "PlatformOutboxEventBusMessage");

            migrationBuilder.DropIndex(
                name: "IX_PlatformOutboxEventBusMessage_RoutingKey",
                table: "PlatformOutboxEventBusMessage");

            migrationBuilder.DropIndex(
                name: "IX_PlatformOutboxEventBusMessage_SendStatus_NextRetryProcessAfter_LastSendDate",
                table: "PlatformOutboxEventBusMessage");

            migrationBuilder.DropIndex(
                name: "IX_PlatformInboxEventBusMessage_ConsumeStatus_NextRetryProcessAfter_LastConsumeDate",
                table: "PlatformInboxEventBusMessage");

            migrationBuilder.DropIndex(
                name: "IX_PlatformInboxEventBusMessage_LastConsumeDate_ConsumeStatus",
                table: "PlatformInboxEventBusMessage");

            migrationBuilder.DropIndex(
                name: "IX_PlatformInboxEventBusMessage_RoutingKey",
                table: "PlatformInboxEventBusMessage");

            migrationBuilder.AddColumn<Guid>(
                name: "ConcurrencyUpdateToken",
                table: "ApplicationDataMigrationHistoryDbSet",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastProcessError",
                table: "ApplicationDataMigrationHistoryDbSet",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastProcessingPingTime",
                table: "ApplicationDataMigrationHistoryDbSet",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ApplicationDataMigrationHistoryDbSet",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDataMigrationHistoryDbSet_ConcurrencyUpdateToken",
                table: "ApplicationDataMigrationHistoryDbSet",
                column: "ConcurrencyUpdateToken");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDataMigrationHistoryDbSet_Status",
                table: "ApplicationDataMigrationHistoryDbSet",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApplicationDataMigrationHistoryDbSet_ConcurrencyUpdateToken",
                table: "ApplicationDataMigrationHistoryDbSet");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationDataMigrationHistoryDbSet_Status",
                table: "ApplicationDataMigrationHistoryDbSet");

            migrationBuilder.DropColumn(
                name: "ConcurrencyUpdateToken",
                table: "ApplicationDataMigrationHistoryDbSet");

            migrationBuilder.DropColumn(
                name: "LastProcessError",
                table: "ApplicationDataMigrationHistoryDbSet");

            migrationBuilder.DropColumn(
                name: "LastProcessingPingTime",
                table: "ApplicationDataMigrationHistoryDbSet");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ApplicationDataMigrationHistoryDbSet");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformOutboxEventBusMessage_LastSendDate_SendStatus",
                table: "PlatformOutboxEventBusMessage",
                columns: ["LastSendDate", "SendStatus"]);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformOutboxEventBusMessage_NextRetryProcessAfter",
                table: "PlatformOutboxEventBusMessage",
                column: "NextRetryProcessAfter");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformOutboxEventBusMessage_RoutingKey",
                table: "PlatformOutboxEventBusMessage",
                column: "RoutingKey");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformOutboxEventBusMessage_SendStatus_NextRetryProcessAfter_LastSendDate",
                table: "PlatformOutboxEventBusMessage",
                columns: ["SendStatus", "NextRetryProcessAfter", "LastSendDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformInboxEventBusMessage_ConsumeStatus_NextRetryProcessAfter_LastConsumeDate",
                table: "PlatformInboxEventBusMessage",
                columns: ["ConsumeStatus", "NextRetryProcessAfter", "LastConsumeDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformInboxEventBusMessage_LastConsumeDate_ConsumeStatus",
                table: "PlatformInboxEventBusMessage",
                columns: ["LastConsumeDate", "ConsumeStatus"]);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformInboxEventBusMessage_RoutingKey",
                table: "PlatformInboxEventBusMessage",
                column: "RoutingKey");
        }
    }
}