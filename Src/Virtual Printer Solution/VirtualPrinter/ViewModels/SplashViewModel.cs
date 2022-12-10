using System;
using System.Reflection;

namespace VirtualPrinter.ViewModels
{
	public class SplashViewModel
	{
        public string Version
        {
            get
            {
                Version version = Assembly.GetEntryAssembly().GetName().Version;
                return $"Version {version.Major}.{version.Minor}.{version.Build}";
            }
        }
    }
}
