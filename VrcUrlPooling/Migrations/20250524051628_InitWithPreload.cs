using System;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VrcUrlPooling.Migrations
{
    /// <inheritdoc />
    public partial class InitWithPreload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TextUrls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Url = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TextUrls", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "VideoUrls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Url = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoUrls", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TextUrls_Id",
                table: "TextUrls",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TextUrls_Url",
                table: "TextUrls",
                column: "Url",
                unique: false);

            migrationBuilder.CreateIndex(
                name: "IX_VideoUrls_Id",
                table: "VideoUrls",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VideoUrls_Url",
                table: "VideoUrls",
                column: "Url",
                unique: false);

            // 初始化一万条数据
            foreach (var table in ((string[])["TextUrls", "VideoUrls"]))
            {
                var insertSql = new StringBuilder();
                insertSql.AppendLine($"INSERT INTO {table} () VALUES");
                for (int i = 0; i < 10000; i++)
                {
                    insertSql.Append("()");
                    insertSql.AppendLine(i == 9999 ? ";" : ",");
                }

                migrationBuilder.Sql(insertSql.ToString());
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TextUrls");

            migrationBuilder.DropTable(
                name: "VideoUrls");
        }
    }
}
