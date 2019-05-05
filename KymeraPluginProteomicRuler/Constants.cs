using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KymeraPluginProteomicRuler { 
    internal static class Constants{
		public static Dictionary<string, double> monoisotopicMasses = new Dictionary<string, double>(){
			{"A", 71.03711},
			{"R", 156.10111},
			{"N", 114.04293},
			{"D", 115.02694},
			{"C", 103.00919},
			{"E", 129.04259},
			{"Q", 128.05858},
			{"G", 57.02146},
			{"H", 137.05891},
			{"I", 113.08406},
			{"L", 113.08406},
			{"K", 128.09496},
			{"M", 131.04049},
			{"F", 147.06841},
			{"P", 97.05276},
			{"S", 87.03203},
			{"T", 101.04768},
			{"W", 186.07931},
			{"Y", 163.06333},
			{"V", 99.06841},
			{"term", 18.01056} // Proton on the N-terminus and OH on the C-terminus
		};

		public static Dictionary<string, double> averageMasses = new Dictionary<string, double>(){
			{"A", 71.0788},
			{"R", 156.1875},
			{"N", 114.1038},
			{"D", 115.0886},
			{"C", 103.1388},
			{"E", 129.1155},
			{"Q", 128.1307},
			{"G", 57.0519},
			{"H", 137.1411},
			{"I", 113.1594},
			{"L", 113.1594},
			{"K", 128.1741},
			{"M", 131.1926},
			{"F", 147.1766},
			{"P", 97.1167},
			{"S", 87.0782},
			{"T", 101.1051},
			{"W", 186.2132},
			{"Y", 163.1760},
			{"V", 99.1326},
			{"term", 18.01528} // Proton on the N-terminus and OH on the C-terminus
		};

		public static Protease trypsin = new Protease("trypsin/P", new Regex(@"(.*?(?:K|R|$))"));
		public static Protease lysC = new Protease("lysC/P", new Regex(@"(.*?(?:K|$))"));
		public static Protease argC = new Protease("argC", new Regex(@"(.*?(?:R|$))"));
		public static Protease aspC = new Protease("aspC", new Regex(@"(.*?(?:D|$))"));
		public static Protease gluC = new Protease("gluC", new Regex(@"(.*?(?:E|$))"));
		public static Protease gluN = new Protease("gluN", new Regex(@"([E|^][^E]*)"));
		public static Protease aspN = new Protease("aspN", new Regex(@"([D|^][^D]*)"));
		public static Protease[] defaultProteases = new[]{trypsin, lysC, gluC, aspN, gluN, argC};

		public static List<string> DefaultProteasesNames(){
			List<string> names = new List<string>();
			foreach (Protease protease in defaultProteases){
				names.Add(protease.name);
			}
			return names;
		}
	}
}