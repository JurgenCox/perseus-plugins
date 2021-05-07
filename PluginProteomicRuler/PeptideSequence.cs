namespace PluginProteomicRuler
{
	public class PeptideSequence
	{
		private string sequence = "";
		private int length;
		private double molecularMassMonoisotopic;
		private double molecularMassAverage;
		private bool hasMolecularMassMonoisotopic;
		private bool hasMolecularMassAverage;

		public void SetSequence(string sequence1)
		{
			sequence1 = sequence1.ToUpper();
			sequence = sequence1;
			length = sequence.Length;
		}

		private void CalculateAverageMass()
		{
			foreach (char aa in sequence)
			{
				if (Constants.averageMasses.ContainsKey(aa.ToString()))
				{
					molecularMassAverage += Constants.averageMasses[aa.ToString()];
				}
			}
			if (molecularMassAverage != 0)
			{
				molecularMassAverage += Constants.averageMasses["term"];
			}
			hasMolecularMassAverage = true;
		}

		private void CalculateMonoisotopicMass()
		{
			foreach (char aa in sequence)
			{
				if (Constants.monoisotopicMasses.ContainsKey(aa.ToString()))
				{
					molecularMassMonoisotopic += Constants.monoisotopicMasses[aa.ToString()];
				}
			}
			if (molecularMassMonoisotopic != 0)
			{
				molecularMassMonoisotopic += Constants.monoisotopicMasses["term"];
			}
			hasMolecularMassMonoisotopic = true;
		}

		public string GetSequence()
		{
			return sequence;
		}

		public int GetLength()
		{
			return length;
		}

		public double GetAverageMolecularMass()
		{
			if (!hasMolecularMassAverage)
			{
				CalculateAverageMass();
			}
			return molecularMassAverage;
		}

		public double GetMonoisotopicMolecularMass()
		{
			if (!hasMolecularMassMonoisotopic)
			{
				CalculateMonoisotopicMass();
			}
			return molecularMassMonoisotopic;
		}
	}
}
