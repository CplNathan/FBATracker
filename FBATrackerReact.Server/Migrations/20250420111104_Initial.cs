using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FBATrackerReact.Server.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "amazon_products",
                columns: table => new
                {
                    AmazonStandardIdentificationNumber = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amazon_products", x => x.AmazonStandardIdentificationNumber);
                });

            migrationBuilder.CreateTable(
                name: "scrapes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: true),
                    Started = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedPostProcessing = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Ended = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Abandoned = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scrapes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "amazon_amp",
                columns: table => new
                {
                    OwnerAmazonStandardIdentificationNumber = table.Column<string>(type: "text", nullable: false),
                    SalesRank = table.Column<int>(type: "integer", nullable: true),
                    ProductsInCategory = table.Column<int>(type: "integer", nullable: true),
                    EstimatedSales = table.Column<string>(type: "text", nullable: true),
                    PrivateLabel = table.Column<int>(type: "integer", nullable: true),
                    PrivateLabelMessage = table.Column<string>(type: "text", nullable: true),
                    IntellectualProperty = table.Column<int>(type: "integer", nullable: true),
                    IntellectualPropertyMessage = table.Column<string>(type: "text", nullable: true),
                    Oversized = table.Column<bool>(type: "boolean", nullable: true),
                    BuyBox = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amazon_amp", x => x.OwnerAmazonStandardIdentificationNumber);
                    table.ForeignKey(
                        name: "FK_amazon_amp_amazon_products_OwnerAmazonStandardIdentificatio~",
                        column: x => x.OwnerAmazonStandardIdentificationNumber,
                        principalTable: "amazon_products",
                        principalColumn: "AmazonStandardIdentificationNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "amazon_eligibility",
                columns: table => new
                {
                    OwnerAmazonStandardIdentificationNumber = table.Column<string>(type: "text", nullable: false),
                    IsGateLocked = table.Column<bool>(type: "boolean", nullable: false),
                    IsRestricted = table.Column<bool>(type: "boolean", nullable: false),
                    RestrictedReason = table.Column<List<string>>(type: "text[]", nullable: true),
                    GatedOnwardUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amazon_eligibility", x => x.OwnerAmazonStandardIdentificationNumber);
                    table.ForeignKey(
                        name: "FK_amazon_eligibility_amazon_products_OwnerAmazonStandardIdent~",
                        column: x => x.OwnerAmazonStandardIdentificationNumber,
                        principalTable: "amazon_products",
                        principalColumn: "AmazonStandardIdentificationNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "amazon_pricing",
                columns: table => new
                {
                    OwnerAmazonStandardIdentificationNumber = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LowestPreferredPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    HighestPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    Fees = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amazon_pricing", x => x.OwnerAmazonStandardIdentificationNumber);
                    table.ForeignKey(
                        name: "FK_amazon_pricing_amazon_products_OwnerAmazonStandardIdentific~",
                        column: x => x.OwnerAmazonStandardIdentificationNumber,
                        principalTable: "amazon_products",
                        principalColumn: "AmazonStandardIdentificationNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "amazon_visibility",
                columns: table => new
                {
                    OwnerAmazonStandardIdentificationNumber = table.Column<string>(type: "text", nullable: false),
                    ProductList = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_amazon_visibility", x => x.OwnerAmazonStandardIdentificationNumber);
                    table.ForeignKey(
                        name: "FK_amazon_visibility_amazon_products_OwnerAmazonStandardIdenti~",
                        column: x => x.OwnerAmazonStandardIdentificationNumber,
                        principalTable: "amazon_products",
                        principalColumn: "AmazonStandardIdentificationNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scraped_products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    AmazonProductAmazonStandardIdentificationNumber = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    ProductReference_Reference = table.Column<string>(type: "text", nullable: true),
                    ProductReference_Type = table.Column<int>(type: "integer", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: true),
                    Profit = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scraped_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_scraped_products_amazon_products_AmazonProductAmazonStandar~",
                        column: x => x.AmazonProductAmazonStandardIdentificationNumber,
                        principalTable: "amazon_products",
                        principalColumn: "AmazonStandardIdentificationNumber");
                    table.ForeignKey(
                        name: "FK_scraped_products_scrapes_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "scrapes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_scraped_products_AmazonProductAmazonStandardIdentificationN~",
                table: "scraped_products",
                column: "AmazonProductAmazonStandardIdentificationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_scraped_products_OwnerId",
                table: "scraped_products",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_scraped_products_ProductReference_Reference",
                table: "scraped_products",
                column: "ProductReference_Reference");

            migrationBuilder.CreateIndex(
                name: "IX_scraped_products_Profit",
                table: "scraped_products",
                column: "Profit");

            migrationBuilder.CreateIndex(
                name: "IX_scrapes_Abandoned",
                table: "scrapes",
                column: "Abandoned");

            migrationBuilder.CreateIndex(
                name: "IX_scrapes_Ended",
                table: "scrapes",
                column: "Ended");

            migrationBuilder.CreateIndex(
                name: "IX_scrapes_Source",
                table: "scrapes",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_scrapes_Started",
                table: "scrapes",
                column: "Started");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "amazon_amp");

            migrationBuilder.DropTable(
                name: "amazon_eligibility");

            migrationBuilder.DropTable(
                name: "amazon_pricing");

            migrationBuilder.DropTable(
                name: "amazon_visibility");

            migrationBuilder.DropTable(
                name: "scraped_products");

            migrationBuilder.DropTable(
                name: "amazon_products");

            migrationBuilder.DropTable(
                name: "scrapes");
        }
    }
}
