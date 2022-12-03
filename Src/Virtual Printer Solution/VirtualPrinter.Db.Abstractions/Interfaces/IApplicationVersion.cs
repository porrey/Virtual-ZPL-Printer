using Diamond.Core.Repository;

namespace VirtualPrinter.Db.Abstractions
{
	public interface IApplicationVersion : IEntity<int>
	{
		string Name { get; set; }
	}
}