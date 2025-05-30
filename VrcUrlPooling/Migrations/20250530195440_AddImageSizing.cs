using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VrcUrlPooling.Migrations
{
    /// <inheritdoc />
    public partial class AddImageSizing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "TextUrls",
                comment: "用于文字和图片")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "AllowCache",
                table: "TextUrls",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                comment: "对于图片，允许缓存结果");

            migrationBuilder.AddColumn<int>(
                name: "MaxSize",
                table: "TextUrls",
                type: "int",
                nullable: false,
                defaultValue: 0,
                comment: "如果有，就当成图片解析并且在返回时保证图片长宽不超过这个大小，max 2048，0 则不作为图片出来");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowCache",
                table: "TextUrls");

            migrationBuilder.DropColumn(
                name: "MaxSize",
                table: "TextUrls");

            migrationBuilder.AlterTable(
                name: "TextUrls",
                oldComment: "用于文字和图片")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
