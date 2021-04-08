using System;

namespace VirtualPrinter.Models
{
	public class Label
	{
		public string ImagePath { get; set; }
		public string Zpl { get; set; }
		public DateTime Timestamp { get; } = DateTime.Now;
	}
}
