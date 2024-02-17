namespace VirtualPrinter.TemplateManager
{
	public interface ITemplateFactory
	{
		public Task<string> CreateZplAsync(ILabelTemplate template);
	}
}
