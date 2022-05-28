using Diamond.Core.Repository.EntityFrameworkCore;
using Labelary.Abstractions;
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

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<PrinterConfiguration>().HasData(new PrinterConfiguration()
			{
				Id = 1,
				Name = "4x6 w/0˚ Rotation",
				HostAddress = "0.0.0.0",
				Port = 9100,
				LabelHeight = 6,
				LabelWidth = 4,
				ResolutionInDpmm = 8,
				RotationAngle = 0,
				LabelUnit = (int)LengthUnit.Inch,
				ImagePath = FileLocations.ImageCache.FullName
			});
		}
	}
}