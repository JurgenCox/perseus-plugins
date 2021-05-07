using System.Text.RegularExpressions;

namespace PluginProteomicRuler
{
	public class Protease
	{
		public string name;
		public Regex cleavageSpecificity;

		public Protease(string name, Regex regex)
		{
			this.name = name;
			cleavageSpecificity = regex;
		}
	}
}
