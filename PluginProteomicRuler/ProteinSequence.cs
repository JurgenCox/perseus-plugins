using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PluginProteomicRuler{
	public class ProteinSequence : PeptideSequence{
		private static readonly Regex regexEntryName = new Regex(@"^>.*\|.*\|(\w*)");

		// allow spaces in gene names
		private static readonly Regex regexGeneName = new Regex(@"^>.*\|.*\|.*\sGN=(.*?)(?:\sPE=|\sSV=|$)");
		private static readonly Regex regexProteinName = new Regex(@"^>.*\|.*\|\w*\s(.*?)\s(?:OS|GN|PE|SV)=");
		private static readonly Regex regexConsensusProteinName = new Regex(@"(?:Isoform .* of )?(.*(?=( \(Fragment\)))|.*)");
		private static readonly Regex regexSpecies = new Regex(@"^>.*\|.*\|.*\sOS=(.*?)\s(?:GN|PE|SV)=");
		public string Header { get; set; }
		public string Accession { get; set; }
		private string geneName;
		private string species;
		private string entryName;
		private string proteinName;
		private string consensusProteinName;

		public bool Equals(ProteinSequence otherSequence){
			return Accession == otherSequence.Accession && GetSequence() == otherSequence.GetSequence();
		}

		private readonly Dictionary<Protease, PeptideSequence[]> theoreticalPeptides =
			new Dictionary<Protease, PeptideSequence[]>();

		// generic method; protease and margins can be defined
		private void CalculateTheoreticalPeptides(Protease protease, int minLength, int maxLength, double minWeight,
			double maxWeight){
			MatchCollection peptideMatches = protease.cleavageSpecificity.Matches(GetSequence());
			List<PeptideSequence> theoreticalPeptides1 = new List<PeptideSequence>();
			foreach (Match match in peptideMatches){
				PeptideSequence theoreticalPeptide = new PeptideSequence();
				theoreticalPeptide.SetSequence(match.Groups[1].Value);
				if (theoreticalPeptide.GetLength() >= minLength && theoreticalPeptide.GetLength() <= maxLength &&
					(minWeight > 0 && maxWeight < double.PositiveInfinity // speed up calculations in case there are no weight limits
					||
					theoreticalPeptide.GetMonoisotopicMolecularMass() >= minWeight &&
					theoreticalPeptide.GetMonoisotopicMolecularMass() <= maxWeight)){
					theoreticalPeptides1.Add(theoreticalPeptide);
				}
			}
			theoreticalPeptides[protease] = theoreticalPeptides1.ToArray();
		}

		private void CalculateTheoreticalPeptides(Protease protease){
			int minLength = 7;
			int maxLength = 30;
			double minWeight = 0;
			double maxWeight = double.PositiveInfinity;
			CalculateTheoreticalPeptides(protease, minLength, maxLength, minWeight, maxWeight);
		}

		public int GetNumberOfTheoreticalPeptides(Protease protease, int minLength, int maxLength){
			if (!theoreticalPeptides.ContainsKey(protease)){
				CalculateTheoreticalPeptides(protease, minLength, maxLength, 0, double.PositiveInfinity);
			}
			return theoreticalPeptides[protease].Length;
		}

		public int GetNumberOfTheoreticalPeptides(Protease protease){
			if (!theoreticalPeptides.ContainsKey(protease)){
				CalculateTheoreticalPeptides(protease);
			}
			return theoreticalPeptides[protease].Length;
		}

		public int GetNumberOfTheoreticalPeptides(){
			Protease protease = Constants.trypsin;
			if (!theoreticalPeptides.ContainsKey(protease)){
				CalculateTheoreticalPeptides(protease);
			}
			return theoreticalPeptides[protease].Length;
		}

		public PeptideSequence[] GetTheoreticalPeptides(Protease protease){
			if (!theoreticalPeptides.ContainsKey(protease)){
				CalculateTheoreticalPeptides(protease);
			}
			return theoreticalPeptides[protease];
		}

		public PeptideSequence[] GetTheoreticalPeptides(Protease protease, int minLength, int maxLength){
			if (!theoreticalPeptides.ContainsKey(protease)){
				CalculateTheoreticalPeptides(protease, minLength, maxLength, 0, double.PositiveInfinity);
			}
			return theoreticalPeptides[protease];
		}

		public PeptideSequence[] GetTheoreticalPeptides(){
			Protease protease = Constants.trypsin;
			if (!theoreticalPeptides.ContainsKey(protease)){
				CalculateTheoreticalPeptides(protease);
			}
			return theoreticalPeptides[protease];
		}

		public string[] GetTheoreticalPeptideSequences(Protease protease){
			PeptideSequence[] peptides = GetTheoreticalPeptides(protease);
			List<string> peptideSequences = new List<string>();
			foreach (PeptideSequence peptide in peptides){
				peptideSequences.Add(peptide.GetSequence());
			}
			return peptideSequences.ToArray();
		}

		public string[] GetTheoreticalPeptideSequences(Protease protease, int minLength, int maxLength){
			PeptideSequence[] peptides = GetTheoreticalPeptides(protease, minLength, maxLength);
			List<string> peptideSequences = new List<string>();
			foreach (PeptideSequence peptide in peptides){
				peptideSequences.Add(peptide.GetSequence());
			}
			return peptideSequences.ToArray();
		}

		public string EntryName{
			get{
				if (entryName == null){
					Match m = regexEntryName.Match(Header);
					if (m.Success){
						entryName = m.Groups[1].Value;
					}
				}
				return entryName;
			}
		}

		public string ProteinName{
			get{
				if (proteinName == null){
					Match m = regexProteinName.Match(Header);
					if (m.Success){
						proteinName = m.Groups[1].Value;
						consensusProteinName = regexConsensusProteinName.Match(proteinName).Groups[1].Value;
					}
				}
				return proteinName;
			}
		}

		public string ConsensusProteinName{
			get{
				if (proteinName == null){
					string dummy = ProteinName; // this function will invoke the parsing of the consensus protein name
				}
				return consensusProteinName;
			}
		}

		public string GeneName{
			get{
				if (geneName == null){
					Match m = regexGeneName.Match(Header);
					if (m.Success){
						geneName = m.Groups[1].Value;
					}
				}
				return geneName;
			}
		}

		public string Species{
			get{
				if (species == null){
					Match m = regexSpecies.Match(Header);
					if (m.Success){
						species = m.Groups[1].Value;
					}
				}
				return species;
			}
		}
	}
}