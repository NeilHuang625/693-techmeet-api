﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace techmeet_api.Migrations
{
    /// <inheritdoc />
    public partial class vipExpirationDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "VIPExpirationDate",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VIPExpirationDate",
                table: "AspNetUsers");
        }
    }
}
