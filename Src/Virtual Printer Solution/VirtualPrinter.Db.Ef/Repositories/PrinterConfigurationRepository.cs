using Diamond.Core.Repository;
using Diamond.Core.Repository.EntityFrameworkCore;
using VirtualPrinter.Db.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace VirtualPrinter.Db.Ef
{
	internal class PrinterConfigurationRepository : EntityFrameworkRepository<IPrinterConfiguration, PrinterConfiguration, VirtualPrinterContext>
	{
		public PrinterConfigurationRepository(VirtualPrinterContext context, IEntityFactory<IPrinterConfiguration> entityFactory)
			: base(context, entityFactory)
		{
		}

		protected override DbSet<PrinterConfiguration> MyDbSet(VirtualPrinterContext context) => context.PrinterConfigurations;
	}
}