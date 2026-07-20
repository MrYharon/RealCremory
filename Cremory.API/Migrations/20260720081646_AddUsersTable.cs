using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cremory.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RECIPE_INGREDIENTS");

            migrationBuilder.DropTable(
                name: "INGREDIENTS");

            migrationBuilder.DropTable(
                name: "RECIPES");

            migrationBuilder.DropColumn(
                name: "ACCOUNT_TYPE",
                table: "USERS");

            migrationBuilder.DropColumn(
                name: "CONTACT_NUMBER",
                table: "USERS");

            migrationBuilder.DropColumn(
                name: "EMAIL",
                table: "USERS");

            migrationBuilder.DropColumn(
                name: "NAME",
                table: "USERS");

            migrationBuilder.RenameColumn(
                name: "USER_ID",
                table: "USERS",
                newName: "UserId");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "USERS",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "USERS",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "USERS",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "USERS",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "USERS",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "AUTO_DEDUCT",
                table: "PRODUCTS",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UNIT",
                table: "PRODUCTS",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ADDRESS",
                table: "ORDERS",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DELIVERY_TYPE",
                table: "ORDERS",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IS_ARCHIVED",
                table: "ORDERS",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PAYMENT_STATUS",
                table: "ORDERS",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "APP_SETTINGS",
                columns: table => new
                {
                    KEY = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    VALUE = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APP_SETTINGS", x => x.KEY);
                });

            migrationBuilder.CreateTable(
                name: "DEVICE_TOKENS",
                columns: table => new
                {
                    TOKEN_ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TOKEN = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PLATFORM = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LAST_USED_AT = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEVICE_TOKENS", x => x.TOKEN_ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "APP_SETTINGS");

            migrationBuilder.DropTable(
                name: "DEVICE_TOKENS");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "USERS");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "USERS");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "USERS");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "USERS");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "USERS");

            migrationBuilder.DropColumn(
                name: "AUTO_DEDUCT",
                table: "PRODUCTS");

            migrationBuilder.DropColumn(
                name: "UNIT",
                table: "PRODUCTS");

            migrationBuilder.DropColumn(
                name: "ADDRESS",
                table: "ORDERS");

            migrationBuilder.DropColumn(
                name: "DELIVERY_TYPE",
                table: "ORDERS");

            migrationBuilder.DropColumn(
                name: "IS_ARCHIVED",
                table: "ORDERS");

            migrationBuilder.DropColumn(
                name: "PAYMENT_STATUS",
                table: "ORDERS");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "USERS",
                newName: "USER_ID");

            migrationBuilder.AddColumn<string>(
                name: "ACCOUNT_TYPE",
                table: "USERS",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CONTACT_NUMBER",
                table: "USERS",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EMAIL",
                table: "USERS",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NAME",
                table: "USERS",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "INGREDIENTS",
                columns: table => new
                {
                    INGREDIENT_ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NAME = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    REORDER_LEVEL = table.Column<decimal>(type: "TEXT", nullable: false),
                    STOCK_QUANTITY = table.Column<decimal>(type: "TEXT", nullable: false),
                    UNIT = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INGREDIENTS", x => x.INGREDIENT_ID);
                });

            migrationBuilder.CreateTable(
                name: "RECIPES",
                columns: table => new
                {
                    RECIPE_ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DESCRIPTION = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IS_ACTIVE = table.Column<bool>(type: "INTEGER", nullable: false),
                    NAME = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SELLING_PRICE = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RECIPES", x => x.RECIPE_ID);
                });

            migrationBuilder.CreateTable(
                name: "RECIPE_INGREDIENTS",
                columns: table => new
                {
                    RECIPE_INGREDIENT_ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    INGREDIENT_ID = table.Column<int>(type: "INTEGER", nullable: false),
                    RECIPE_ID = table.Column<int>(type: "INTEGER", nullable: false),
                    QUANTITY = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RECIPE_INGREDIENTS", x => x.RECIPE_INGREDIENT_ID);
                    table.ForeignKey(
                        name: "FK_RECIPE_INGREDIENTS_INGREDIENTS_INGREDIENT_ID",
                        column: x => x.INGREDIENT_ID,
                        principalTable: "INGREDIENTS",
                        principalColumn: "INGREDIENT_ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RECIPE_INGREDIENTS_RECIPES_RECIPE_ID",
                        column: x => x.RECIPE_ID,
                        principalTable: "RECIPES",
                        principalColumn: "RECIPE_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RECIPE_INGREDIENTS_INGREDIENT_ID",
                table: "RECIPE_INGREDIENTS",
                column: "INGREDIENT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_RECIPE_INGREDIENTS_RECIPE_ID",
                table: "RECIPE_INGREDIENTS",
                column: "RECIPE_ID");
        }
    }
}
