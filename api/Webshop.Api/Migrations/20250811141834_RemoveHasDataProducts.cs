using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Webshop.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHasDataProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "ImageUrl", "Name", "Price" },
                values: new object[] { "Durable and stylish, made from recycled materials.", "https://picsum.photos/300?random=1", "Eco-Friendly Backpack", 49.99m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "ImageUrl", "Name", "Price" },
                values: new object[] { "High-quality sound and comfort for work or play.", "https://picsum.photos/300?random=2", "Noise Cancelling Headphones", 129.99m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "ImageUrl", "Name", "Price" },
                values: new object[] { "Touch control and adjustable lighting modes.", "https://picsum.photos/300?random=3", "Smart LED Lamp", 24.99m });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Description", "ImageUrl", "Name", "Price", "Quantity" },
                values: new object[,]
                {
                    { 4, "Sleek and simple design with a leather strap.", "https://picsum.photos/300?random=4", "Minimalist Wristwatch", 89.99m, 0 },
                    { 5, "Portable speaker with crystal clear sound and 12-hour battery life.", "https://picsum.photos/300?random=5", "Wireless Bluetooth Speaker", 79.99m, 0 },
                    { 6, "Soft, breathable, and sustainably made.", "https://picsum.photos/300?random=6", "Organic Cotton T-Shirt", 19.99m, 0 },
                    { 7, "Keep drinks cold for 24 hours or hot for 12 hours.", "https://picsum.photos/300?random=7", "Stainless Steel Water Bottle", 34.99m, 0 },
                    { 8, "Lumbar support and adjustable height for all-day comfort.", "https://picsum.photos/300?random=8", "Ergonomic Office Chair", 199.99m, 0 },
                    { 9, "Adjustable aluminum stand compatible with all phone sizes.", "https://picsum.photos/300?random=9", "Smartphone Stand", 14.99m, 0 },
                    { 10, "Ultra-soft fleece blanket perfect for movie nights.", "https://picsum.photos/300?random=10", "Cozy Throw Blanket", 39.99m, 0 },
                    { 11, "Eco-friendly kitchen essential with 3 different sizes.", "https://picsum.photos/300?random=11", "Bamboo Cutting Board Set", 29.99m, 0 },
                    { 12, "Complete set of 5 bands for full-body workouts at home.", "https://picsum.photos/300?random=12", "Fitness Resistance Bands", 24.99m, 0 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "ImageUrl", "Name", "Price" },
                values: new object[] { "Ceramic mug for hot drinks", "https://fastly.picsum.photos/id/866/200/200.jpg?hmac=i0ngmQOk9dRZEzhEosP31m_vQnKBQ9C19TBP1CGoIUA", "Coffee Mug", 49.95m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "ImageUrl", "Name", "Price" },
                values: new object[] { "100% cotton, unisex", "https://fastly.picsum.photos/id/866/200/200.jpg?hmac=i0ngmQOk9dRZEzhEosP31m_vQnKBQ9C19TBP1CGoIUA", "T-Shirt", 99.00m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "ImageUrl", "Name", "Price" },
                values: new object[] { "A5 notebook with grid paper", "https://fastly.picsum.photos/id/866/200/200.jpg?hmac=i0ngmQOk9dRZEzhEosP31m_vQnKBQ9C19TBP1CGoIUA", "Notebook", 39.50m });
        }
    }
}
