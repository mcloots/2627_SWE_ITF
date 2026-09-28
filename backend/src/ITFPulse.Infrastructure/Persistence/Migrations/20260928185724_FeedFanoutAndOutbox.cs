using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ITFPulse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FeedFanoutAndOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "feed_entries",
                schema: "itfpulse",
                columns: table => new
                {
                    follower_id = table.Column<Guid>(type: "uuid", nullable: false),
                    post_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feed_entries", x => new { x.follower_id, x.post_id });
                    table.ForeignKey(
                        name: "FK_feed_entries_posts_post_id",
                        column: x => x.post_id,
                        principalSchema: "itfpulse",
                        principalTable: "posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "follows",
                schema: "itfpulse",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    follower_id = table.Column<Guid>(type: "uuid", nullable: false),
                    followed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_follows", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "InboxState",
                schema: "itfpulse",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Received = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceiveCount = table.Column<int>(type: "integer", nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Consumed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxState", x => x.Id);
                    table.UniqueConstraint("AK_InboxState_MessageId_ConsumerId", x => new { x.MessageId, x.ConsumerId });
                });

            migrationBuilder.CreateTable(
                name: "OutboxState",
                schema: "itfpulse",
                columns: table => new
                {
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxState", x => x.OutboxId);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                schema: "itfpulse",
                columns: table => new
                {
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnqueueTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Headers = table.Column<string>(type: "text", nullable: true),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    InboxMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    InboxConsumerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MessageType = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitiatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DestinationAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ResponseAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FaultAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessage", x => x.SequenceNumber);
                    table.ForeignKey(
                        name: "FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId",
                        columns: x => new { x.InboxMessageId, x.InboxConsumerId },
                        principalSchema: "itfpulse",
                        principalTable: "InboxState",
                        principalColumns: new[] { "MessageId", "ConsumerId" });
                    table.ForeignKey(
                        name: "FK_OutboxMessage_OutboxState_OutboxId",
                        column: x => x.OutboxId,
                        principalSchema: "itfpulse",
                        principalTable: "OutboxState",
                        principalColumn: "OutboxId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_posts_author_id",
                schema: "itfpulse",
                table: "posts",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_feed_entries_follower_id_sequence",
                schema: "itfpulse",
                table: "feed_entries",
                columns: new[] { "follower_id", "sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_feed_entries_post_id",
                schema: "itfpulse",
                table: "feed_entries",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "IX_follows_author_id_follower_id",
                schema: "itfpulse",
                table: "follows",
                columns: new[] { "author_id", "follower_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_follows_author_id_id",
                schema: "itfpulse",
                table: "follows",
                columns: new[] { "author_id", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_InboxState_Delivered",
                schema: "itfpulse",
                table: "InboxState",
                column: "Delivered");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_EnqueueTime",
                schema: "itfpulse",
                table: "OutboxMessage",
                column: "EnqueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ExpirationTime",
                schema: "itfpulse",
                table: "OutboxMessage",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber",
                schema: "itfpulse",
                table: "OutboxMessage",
                columns: new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_OutboxId_SequenceNumber",
                schema: "itfpulse",
                table: "OutboxMessage",
                columns: new[] { "OutboxId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_Created",
                schema: "itfpulse",
                table: "OutboxState",
                column: "Created");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "feed_entries",
                schema: "itfpulse");

            migrationBuilder.DropTable(
                name: "follows",
                schema: "itfpulse");

            migrationBuilder.DropTable(
                name: "OutboxMessage",
                schema: "itfpulse");

            migrationBuilder.DropTable(
                name: "InboxState",
                schema: "itfpulse");

            migrationBuilder.DropTable(
                name: "OutboxState",
                schema: "itfpulse");

            migrationBuilder.DropIndex(
                name: "IX_posts_author_id",
                schema: "itfpulse",
                table: "posts");
        }
    }
}
