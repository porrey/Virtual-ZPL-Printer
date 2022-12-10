using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Diamond.Core.Repository;
using VirtualPrinter.Db.Abstractions;

namespace VirtualPrinter.Db.Ef
{
	[Table("ApplicationVersion")]
	public class ApplicationVersion : Entity, IApplicationVersion
	{
		[Column("ApplicationVersionnId")]
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		public string Name { get; set; }
	}
}
