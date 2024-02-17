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
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using Diamond.Core.Clonable.Newtonsoft;
using Diamond.Core.Extensions.DependencyInjection;
using Diamond.Core.Extensions.DependencyInjection.EntityFrameworkCore;
using Diamond.Core.Wpf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
#if !DEBUG
using VirtualPrinter.Views;
#endif

namespace VirtualPrinter
{
	public partial class App : HostedApplication
	{
#if !DEBUG
		protected SplashView Splash { get; set; }
#endif

		protected override IHostBuilder OnConfigureHost(IHostBuilder hostBuilder)
		{
			return hostBuilder.ConfigureServicesFolder("Services")
							  .UseSerilog()
							  .UseConfiguredDatabaseServices()
							  .UseObjectCloning();
		}

		protected override void OnConfigureAppConfiguration(HostBuilderContext hostContext, IConfigurationBuilder configurationBuilder)
		{
			//
			// Build the configuration so Serilog can read from it.
			//
			IConfiguration configuration = configurationBuilder.Build();

			//
			// Create a logger from the configuration.
			//
			Log.Logger = new LoggerConfiguration()
					  .ReadFrom.Configuration(configuration)
					  .CreateLogger();
		}

		protected override void OnBeginStartup(StartupEventArgs e)
		{
			//
			// Attempt to set culture for the current thread.
			//
			try
			{
				Thread.CurrentThread.CurrentUICulture = new CultureInfo(CultureInfo.CurrentCulture.LCID, true);
			}
			catch
			{
			}

			//
			// Attempt to set culture for all controls.
			//
			try
			{
				FrameworkPropertyMetadata metaData = new(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag));
				FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), metaData);
			}
			catch
			{
			}

#if !DEBUG
			this.Splash = new SplashView(new());
			this.Splash.Show();
#endif
		}

		protected override void OnCompletedStartup(StartupEventArgs e)
		{
#if !DEBUG
			this.Splash.Close();
			this.Splash = null;
#endif
		}

		protected override void OnConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
		{
			//
			// Add a memory cache to the services.
			//
			services.AddMemoryCache();
		}
	}
}
