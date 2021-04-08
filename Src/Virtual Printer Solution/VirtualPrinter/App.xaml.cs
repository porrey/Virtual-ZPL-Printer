using Diamond.Core.Extensions.DependencyInjection;
using Diamond.Core.Wpf;
using Microsoft.Extensions.Hosting;

namespace VirtualPrinter
{
	public partial class App : HostedApplication
	{
		protected override IHostBuilder OnConfigureHost(IHostBuilder hostBuilder)
		{
			return hostBuilder.ConfigureServicesFolder("Services");
		}
	}
}
