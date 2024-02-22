/*
 *  This file is part of Virtual ZPL Printer.
 *  
 *  Virtual ZPL Printer is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Virtual ZPL Printer is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Virtual ZPL Printer.  If not, see <https://www.gnu.org/licenses/>.
 */
using Diamond.Core.Repository.EntityFrameworkCore;
using Labelary.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UnitsNet.Units;

namespace VirtualPrinter.Db.Ef
{
	public class VirtualPrinterContext : RepositoryContext<VirtualPrinterContext>
	{
		public VirtualPrinterContext(ILogger<VirtualPrinterContext> logger)
					: base(logger)
		{
		}

		public VirtualPrinterContext(ILogger<VirtualPrinterContext> logger, DbContextOptions<VirtualPrinterContext> options)
			: base(logger, options)
		{
			string connectionString = this.Database.GetDbConnection().ConnectionString;
			this.Logger.LogDebug("{context} is using connection string '{connectionString}'.", nameof(VirtualPrinterContext), connectionString);
		}

		public string File { set => this.Database.SetConnectionString($"Filename={value};"); }
		public virtual DbSet<PrinterConfiguration> PrinterConfigurations { get; set; }
		public virtual DbSet<ApplicationVersion> ApplicationVersions { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			string defaultFilters = "[]";

			modelBuilder.Entity<PrinterConfiguration>().HasIndex(t => t.Name);
			modelBuilder.Entity<ApplicationVersion>().HasIndex(t => t.Name);

			modelBuilder.Entity<PrinterConfiguration>().HasData(
			[
				new()
				{
					Id = 1,
					Name = $"4x6 w/0˚",
					HostAddress = "0.0.0.0",
					Port = 9100,
					LabelHeight = 6,
					LabelWidth = 4,
					ResolutionInDpmm = 8,
					RotationAngle = 0,
					LabelUnit = (int)LengthUnit.Inch,
					ImagePath = FileLocations.ImageCache.FullName,
					Filters = defaultFilters
				},
				new()
				{
					Id = 2,
					Name = $"4x6 w/90˚",
					HostAddress = "0.0.0.0",
					Port = 9100,
					LabelHeight = 6,
					LabelWidth = 4,
					ResolutionInDpmm = 8,
					RotationAngle = 90,
					LabelUnit = (int)LengthUnit.Inch,
					ImagePath = FileLocations.ImageCache.FullName,
					Filters = defaultFilters
				},
				new()
				{
					Id = 3,
					Name = $"4x6 w/180˚",
					HostAddress = "0.0.0.0",
					Port = 9100,
					LabelHeight = 6,
					LabelWidth = 4,
					ResolutionInDpmm = 8,
					RotationAngle = 180,
					LabelUnit = (int)LengthUnit.Inch,
					ImagePath = FileLocations.ImageCache.FullName,
					Filters = defaultFilters
				},
				new()
				{
					Id = 4,
					Name = $"4x6 w/270˚",
					HostAddress = "0.0.0.0",
					Port = 9100,
					LabelHeight = 6,
					LabelWidth = 4,
					ResolutionInDpmm = 8,
					RotationAngle = 270,
					LabelUnit = (int)LengthUnit.Inch,
					ImagePath = FileLocations.ImageCache.FullName,
					Filters = defaultFilters
				},
				new()
				{
					Id = 5,
					Name = "2x2",
					HostAddress = "0.0.0.0",
					Port = 9100,
					LabelHeight = 2,
					LabelWidth = 2,
					ResolutionInDpmm = 8,
					RotationAngle = 0,
					LabelUnit = (int)LengthUnit.Inch,
					ImagePath = FileLocations.ImageCache.FullName,
					Filters = defaultFilters
				}
			]);

			modelBuilder.Entity<ApplicationVersion>().HasData(
			[
				new()
				{
					Id = 1,
					Name = "3.0.0"
				}
			]);
		}

		public async Task CheckUpgradeAsync()
		{
			try
			{
				if (!this.PrinterConfigurations.Any())
				{
					//
					// Rebuild the database.
					//
					await this.Database.EnsureDeletedAsync();
					await this.Database.EnsureCreatedAsync();
				}
				else
				{
					//
					// This will throw an exception if the field does not exist.
					//
					_ = this.PrinterConfigurations.First();
				}
			}
			catch (SqliteException ex)
			{
				if (ex.Message == "SQLite Error 1: 'no such column: p.PhysicalPrinter'.")
				{
					//
					// Update the table.
					//
					this.Database.ExecuteSqlRaw("ALTER TABLE PrinterConfiguration ADD PhysicalPrinter TEXT;");

					//
					// Set the default field value.
					//
					foreach (PrinterConfiguration item in this.PrinterConfigurations)
					{
						item.PhysicalPrinter = "{}";
					}

					//
					// Update the version.
					//
					this.ApplicationVersions.First().Name = "3.0.0";

					await this.SaveChangesAsync();
				}
			}
		}
	}
}