using System.IO;

namespace VirtualZplPrinter.Models
{
	public class LabelTemplate
	{
		public FileInfo TemplateFile { get; set; }
		public string Name => Path.GetFileNameWithoutExtension(this.TemplateFile.Name);
		public string Zpl => File.ReadAllText(this.TemplateFile.FullName);
	}
}
