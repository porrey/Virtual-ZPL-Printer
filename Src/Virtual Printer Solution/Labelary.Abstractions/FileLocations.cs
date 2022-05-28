using System;
using System.IO;

namespace Labelary.Abstractions
{
	public static class FileLocations
	{
		public static DirectoryInfo RootPath = new($@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\Virtual ZPL Printer");
		public static FileInfo ImageCache = new($@"{RootPath}\Image Cache");
		public static FileInfo Database = new($@"{RootPath}\Database\db.sqlite");
		public static DirectoryInfo LabelTemplates = new($@"{RootPath}\Templates");
	}
}
