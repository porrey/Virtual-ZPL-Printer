﻿/*
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
using System.Windows;
using Diamond.Core.Extensions.DependencyInjection;
using Diamond.Core.Extensions.DependencyInjection.EntityFrameworkCore;
using Diamond.Core.Wpf;
using Microsoft.Extensions.Hosting;
using VirtualPrinter.Views;

namespace VirtualPrinter
{
	public partial class App : HostedApplication
	{
		protected override IHostBuilder OnConfigureHost(IHostBuilder hostBuilder)
		{
			return hostBuilder.ConfigureServicesFolder("Services")
							  .UseConfiguredDatabaseServices();
		}

		protected SplashView Splash { get; set; }

		protected override void OnBeginStartup(StartupEventArgs e)
		{
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
	}
}
