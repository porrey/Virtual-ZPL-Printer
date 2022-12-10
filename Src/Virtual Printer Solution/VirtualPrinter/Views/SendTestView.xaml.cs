using System;
using System.ComponentModel;
using System.Windows;
using Prism.Events;
using VirtualPrinter.Events;
using VirtualPrinter.ViewModels;

namespace VirtualPrinter.Views
{
	public partial class SendTestView : Window
	{
		public SendTestView(IEventAggregator eventAggregator, SendTestViewModel viewModel)
		{
			this.DataContext = viewModel;
			this.EventAggregator = eventAggregator;
			this.RestoreWindow();
			this.InitializeComponent();
		}

		protected IEventAggregator EventAggregator { get; set; }
		public SendTestViewModel ViewModel => (SendTestViewModel)this.DataContext;

		private void RestoreWindow()
		{
			if (Properties.Settings.Default.SendTestLabelLeft != 0)
			{
				this.Left = Properties.Settings.Default.SendTestLabelLeft;
				this.Top = Properties.Settings.Default.SendTestLabelTop;
			}
		}

		private void SaveWindow()
		{
			Properties.Settings.Default.SendTestLabelLeft = this.Left;
			Properties.Settings.Default.SendTestLabelTop = this.Top;
			Properties.Settings.Default.Save();
		}

		protected override async void OnInitialized(EventArgs e)
		{
			await this.ViewModel.InitializeAsync();
			base.OnInitialized(e);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			this.SaveWindow();
			this.EventAggregator.GetEvent<WindowHiddenEvent>().Publish(new WindowHiddenEventArgs() { Window = this });
		}
	}
}
