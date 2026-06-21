using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cremory.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CATEGORIES",
                columns: table => new
                {
                    CATEGORY_ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NAME = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DISPLAY_ORDER = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CATEGORIES", x => x.CATEGORY_ID);
                });

            migrationBuilder.CreateTable(
                name: "INGREDIENTS",
                columns: table => new
                {
                    INGREDIENT_ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NAME = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    STOCK_QUANTITY = table.Column<decimal>(type: "numeric", nullable: false),
                    UNIT = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    REORDER_LEVEL = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INGREDIENTS", x => x.INGREDIENT_ID);
                });

            migrationBuilder.CreateTable(
                name: "ORDERS",
                columns: table => new
                {
                    ORDER_ID = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CUSTOMER_NAME = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ITEMS = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TOTAL_PRICE = table.Column<decimal>(type: "numeric", nullable: false),
                    STATUS = table.Column<int>(type: "integer", nullable: false),
                    SOURCE = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CUSTOMER_CONTACT = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UPDATED_AT = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ORDERS", x => x.ORDER_ID);
                });

            migrationBuilder.CreateTable(
                name: "RECIPES",
                columns: table => new
                {
                    RECIPE_ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NAME = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DESCRIPTION = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SELLING_PRICE = table.Column<decimal>(type: "numeric", nullable: false),
                    IS_ACTIVE = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RECIPES", x => x.RECIPE_ID);
                });

            migrationBuilder.CreateTable(
                name: "USERS",
                columns: table => new
                {
                    USER_ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NAME = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EMAIL = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CONTACT_NUMBER = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ACCOUNT_TYPE = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USERS", x => x.USER_ID);
                });

            migrationBuilder.CreateTable(
                name: "PRODUCTS",
                columns: table => new
                {
                    PRODUCT_ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CATEGORY_ID = table.Column<int>(type: "integer", nullable: false),
                    NAME = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VARIANT = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FLAVOR = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BASE_PRICE = table.Column<decimal>(type: "numeric", nullable: false),
                    ADD_ON_DESCRIPTION = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ADD_ON_PRICE_PER_UNIT = table.Column<decimal>(type: "numeric", nullable: true),
                    IS_ACTIVE = table.Column<bool>(type: "boolean", nullable: false),
                    DISPLAY_ORDER = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRODUCTS", x => x.PRODUCT_ID);
                    table.ForeignKey(
                        name: "FK_PRODUCTS_CATEGORIES_CATEGORY_ID",
                        column: x => x.CATEGORY_ID,
                        principalTable: "CATEGORIES",
                        principalColumn: "CATEGORY_ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RECIPE_INGREDIENTS",
                columns: table => new
                {
                    RECIPE_INGREDIENT_ID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RECIPE_ID = table.Column<int>(type: "integer", nullable: false),
                    INGREDIENT_ID = table.Column<int>(type: "integer", nullable: false),
                    QUANTITY = table.Column<decimal>(type: "numeric", nullable: false)
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
                name: "IX_PRODUCTS_CATEGORY_ID",
                table: "PRODUCTS",
                column: "CATEGORY_ID");

            migrationBuilder.CreateIndex(
                name: "IX_RECIPE_INGREDIENTS_INGREDIENT_ID",
                table: "RECIPE_INGREDIENTS",
                column: "INGREDIENT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_RECIPE_INGREDIENTS_RECIPE_ID",
                table: "RECIPE_INGREDIENTS",
                column: "RECIPE_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ORDERS");

            migrationBuilder.DropTable(
                name: "PRODUCTS");

            migrationBuilder.DropTable(
                name: "RECIPE_INGREDIENTS");

            migrationBuilder.DropTable(
                name: "USERS");

            migrationBuilder.DropTable(
                name: "CATEGORIES");

            migrationBuilder.DropTable(
                name: "INGREDIENTS");

            migrationBuilder.DropTable(
                name: "RECIPES");
        }
    }
}
