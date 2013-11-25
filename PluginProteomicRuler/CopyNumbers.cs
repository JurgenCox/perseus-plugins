﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using BasicLib.ParamWf;
using BasicLib.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PluginProteomicRuler{
	public class CopyNumbers : IMatrixProcessing{
		public bool HasButton { get { return false; } }
		public Image ButtonImage { get { return null; } }
		public string HelpDescription{
			get{
				return "Estimate cellular copy numbers from protein intensities using the proteomic ruler approach.\n\n" +
					"Requirements:\nA deep proteomic dataset of either " + String.Join(", ", SupportedOrganismNames()) +
					", generated from a nucleated cell type or tissue, processed with a recent uniprot database.\n\n" +
					"Parameters:\n\n" +
					"Intensities: A series of columns containing the protein intensities in the different samples.\nIf you want to use " +
					"LFQ intensities, it is recommended to process the dataset with 1 min. LFQ ratio count.\n\n" +
					"Averaging mode: Select how to handle multiple columns. By default, all columns will be treated independently. " +
					"Alternatively, enforce the same normalization for all columns or for groups of replicates. " +
					"This will ensure that equal intensity values get translated into equal copy numbers. This mode is mainly useful " +
					"for LFQ intensities, which are already normalized to ensure comparability across samples." +
					"However, the samples will then not strictly have histone amounts or total protein amounts corresponding to the " +
					"expected or user-defined values. " +
					"As a last option, all selected columns can be averaged prior to estimating copy numbers. This is useful e.g. for " +
					"a set of technical replicates." +
					"Detectability normalization: By default, the algorithm assumes direct proportionality of the MS intensity and the " +
					"protein mass. In other words, the molecular mass serves as normalization factor to calculate copy numbers from intensities. " +
					"Optionally, one can introduce another level of normalization using e.g. the number of theoretical peptides as used " +
					"in the iBAQ method. Just select a column containing normalization factors of your choice.\n\n" +
					"Scaling mode: Select whether you want to scale the total protein amount using the histone proteomic ruler or if you " +
					"want to define a total protein mass per cell.\n\n" +
					"Total cellular protein concentration: To translate protein mass per cell into cell volume, an estimate of the total " +
					"cellular protein concentration is needed. This value varies only to a small extent between different cell types as " +
					"diverse as E. coli, yeast or human cells.";
			}
		}
		public string HelpOutput { get { return "In the parameters, you can select which output you want to add to the matrix."; } }
		public DocumentType HelpDescriptionType { get { return DocumentType.PlainText; } }
		public DocumentType HelpOutputType { get { return DocumentType.PlainText; } }
		public DocumentType[] HelpSupplTablesType { get { return new DocumentType[0]; } }
		public string[] HelpSupplTables { get { return new string[0]; } }
		public int NumSupplTables { get { return 0; } }
		public string Name { get { return "Estimate copy numbers"; } }
		public string Heading { get { return "Proteomic ruler"; } }
		public bool IsActive { get { return true; } }
		public float DisplayOrder { get { return 2; } }
		public string[] HelpDocuments { get { return new string[0]; } }
		public DocumentType[] HelpDocumentTypes { get { return new DocumentType[0]; } }
		public int NumDocuments { get { return 0; } }

		public int GetMaxThreads(ParametersWf parameters) {
			return 1;
		}

		private const double avogadro = 6.02214129e23;
		private const double basePairWeight = 615.8771;

		public void ProcessData(IMatrixData mdata, ParametersWf param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int[] outputColumns = param.GetMultiChoiceParam("Output").Value;
			int proteinIdColumnInd = param.GetSingleChoiceParam("Protein IDs").Value;
			string[] proteinIds = mdata.StringColumns[proteinIdColumnInd];
			int[] intensityCols = param.GetMultiChoiceParam("Intensities").Value;
			if (intensityCols.Length == 0){
				processInfo.ErrString = "Please select at least one column containing protein intensities.";
				return;
			}
			// variable to hold all intensity values
			List<double[]> columns = new List<double[]>();
			string[] sampleNames = new string[intensityCols.Length];
			for (int col = 0; col < intensityCols.Length; col++){
				double[] values;
				if (intensityCols[col] < mdata.ExpressionColumnCount){
					values = ArrayUtils.ToDoubles(mdata.GetExpressionColumn(intensityCols[col]));
					sampleNames[col] = mdata.ExpressionColumnNames[intensityCols[col]];
				} else{
					values = mdata.NumericColumns[intensityCols[col] - mdata.ExpressionColumnCount];
					sampleNames[col] = mdata.NumericColumnNames[intensityCols[col] - mdata.ExpressionColumnCount];
				}
				sampleNames[col] = new Regex(@"^(?:(?:LFQ )?[Ii]ntensity )?(.*)$").Match(sampleNames[col]).Groups[1].Value;
				columns.Add(values);
			}
			// average over columns if this option is selected
			if (param.GetSingleChoiceWithSubParams("Averaging mode").Value == 3){
				double[] column = new double[mdata.RowCount];
				for (int row = 0; row < mdata.RowCount; row++){
					double[] values = new double[intensityCols.Length];
					for (int col = 0; col < intensityCols.Length; col++){
						values[col] = columns[col][row];
					}
					column[row] = ArrayUtils.Median(ExtractValidValues(values, false));
				}
				// delete the original list of columns
				columns = new List<double[]>{column};
				sampleNames = new[]{""};
			}
			// revert logarithm if necessary
			if (param.GetBoolWithSubParams("Logarithmized").Value){
				double[] logBases = new[]{2, Math.E, 10};
				double logBase =
					logBases[param.GetBoolWithSubParams("Logarithmized").GetSubParameters().GetSingleChoiceParam("log base").Value];
				foreach (double[] t in columns){
					for (int row = 0; row < mdata.RowCount; row++){
						if (t[row] == 0){
							processInfo.ErrString = "Are the columns really logarithmized?\nThey contain zeroes!";
						}
						t[row] = Math.Pow(logBase, t[row]);
					}
				}
			}
			double[] mw = mdata.NumericColumns[param.GetSingleChoiceParam("Molecular masses").Value];
			// detect whether the molecular masses are given in Da or kDa
			if (ArrayUtils.Median(mw) < 250) // likely kDa
			{
				for (int i = 0; i < mw.Length; i++){
					mw[i] *= 1000;
				}
			}
			double[] detectabilityNormFactor = mw;
			if (param.GetBoolWithSubParams("Detectability correction").Value){
				detectabilityNormFactor =
					mdata.NumericColumns[
						param.GetBoolWithSubParams("Detectability correction")
						     .GetSubParameters()
						     .GetSingleChoiceParam("Correction factor")
						     .Value];
			}
			// the normalization factor needs to be nonzero for all proteins
			// check and replace with 1 for all relevant cases
			for (int row = 0; row < mdata.RowCount; row++){
				if (detectabilityNormFactor[row] == 0 || detectabilityNormFactor[row] == double.NaN){
					detectabilityNormFactor[row] = 1;
				}
			}
			// detect the organism
			Organism organism = DetectOrganism(proteinIds);
			// c value the amount of DNA per cell, see: http://en.wikipedia.org/wiki/C-value
			double cValue = (organism.genomeSize*basePairWeight)/avogadro;
			// find the histones
			int[] histoneRows = FindHistones(proteinIds, organism);
			// write a categorical column indicating the histones
			string[][] histoneCol = new string[mdata.RowCount][];
			for (int row = 0; row < mdata.RowCount; row++){
				histoneCol[row] = (ArrayUtils.Contains(histoneRows, row)) ? new[]{"+"} : new[]{""};
			}
			mdata.AddCategoryColumn("Histones", "", histoneCol);
			// initialize the variables for the annotation rows
			double[] totalProteinRow = new double[mdata.ExpressionColumnCount];
			double[] totalMoleculesRow = new double[mdata.ExpressionColumnCount];
			string[][] organismRow = new string[mdata.ExpressionColumnCount][];
			double[] histoneMassRow = new double[mdata.ExpressionColumnCount];
			double[] ploidyRow = new double[mdata.ExpressionColumnCount];
			double[] cellVolumeRow = new double[mdata.ExpressionColumnCount];
			double[] normalizationFactors = new double[columns.Count];
			// calculate normalization factors for each column
			for (int col = 0; col < columns.Count; col++){
				string sampleName = sampleNames[col];
				double[] column = columns[col];
				// normalization factor to go from intensities to copies,
				// needs to be determined either using the total protein or the histone scaling approach
				double factor;
				switch (param.GetSingleChoiceWithSubParams("Scaling mode").Value){
					case 0: // total protein amount
						double mwWeightedNormalizedSummedIntensities = 0;
						for (int row = 0; row < mdata.RowCount; row++){
							if (!double.IsNaN(column[row]) && !double.IsNaN(mw[row])){
								mwWeightedNormalizedSummedIntensities += (column[row]/detectabilityNormFactor[row])*mw[row];
							}
						}
						factor =
							(param.GetSingleChoiceWithSubParams("Scaling mode")
							      .GetSubParameters()
							      .GetDoubleParam("Protein amount per cell [pg]")
							      .Value*1e-12*avogadro)/mwWeightedNormalizedSummedIntensities;
						break;
					case 1: // histone mode
						double mwWeightedNormalizedSummedHistoneIntensities = 0;
						foreach (int row in histoneRows){
							if (!double.IsNaN(column[row]) && !double.IsNaN(mw[row])){
								mwWeightedNormalizedSummedHistoneIntensities += (column[row]/detectabilityNormFactor[row])*mw[row];
							}
						}
						double ploidy =
							param.GetSingleChoiceWithSubParams("Scaling mode").GetSubParameters().GetDoubleParam("Ploidy").Value;
						factor = (cValue*ploidy*avogadro)/mwWeightedNormalizedSummedHistoneIntensities;
						break;
					default:
						factor = 1;
						break;
				}
				normalizationFactors[col] = factor;
			}
			// check averaging mode
			if (param.GetSingleChoiceWithSubParams("Averaging mode").Value == 1) // same factor for all
			{
				double factor = ArrayUtils.Mean(normalizationFactors);
				for (int i = 0; i < normalizationFactors.Length; i++){
					normalizationFactors[i] = factor;
				}
			}
			if (param.GetSingleChoiceWithSubParams("Averaging mode").Value == 2) // same factor in each group
			{
				if (
					param.GetSingleChoiceWithSubParams("Averaging mode").GetSubParameters().GetSingleChoiceParam("Grouping").Value ==
						-1){
					processInfo.ErrString = "No grouping selected.";
					return;
				}
				string[][] groupNames =
					mdata.GetCategoryRowAt(
						param.GetSingleChoiceWithSubParams("Averaging mode").GetSubParameters().GetSingleChoiceParam("Grouping").Value);
				string[] uniqueGroupNames = Unique(groupNames);
				int[] grouping = new int[columns.Count];
				for (int i = 0; i < columns.Count; i++){
					if (intensityCols[i] >= mdata.ExpressionColumnCount){ // Numeric annotation columns cannot be grouped
						grouping[i] = i;
						continue;
					}
					if (ArrayUtils.Contains(uniqueGroupNames, groupNames[i][0])){
						grouping[i] = ArrayUtils.IndexOf(uniqueGroupNames, groupNames[i][0]);
						continue;
					}
					grouping[i] = i;
				}
				Dictionary<int, List<double>> factors = new Dictionary<int, List<double>>();
				for (int i = 0; i < columns.Count; i++){
					if (factors.ContainsKey(grouping[i])){
						factors[grouping[i]].Add(normalizationFactors[i]);
					} else{
						factors.Add(grouping[i], new List<double>{normalizationFactors[i]});
					}
				}
				double[] averagedNormalizationFactors = new double[columns.Count];
				for (int i = 0; i < columns.Count; i++){
					List<double> factor;
					factors.TryGetValue(grouping[i], out factor);
					averagedNormalizationFactors[i] = ArrayUtils.Mean(factor);
				}
				normalizationFactors = averagedNormalizationFactors;
			}
			// loop over all selected columns and calculate copy numbers
			for (int col = 0; col < columns.Count; col++){
				string sampleName = sampleNames[col];
				double[] column = columns[col];
				double factor = normalizationFactors[col];
				double[] copyNumbers = new double[mdata.RowCount];
				double[] concentrations = new double[mdata.RowCount]; // femtoliters
				double[] massFraction = new double[mdata.RowCount];
				double[] moleFraction = new double[mdata.RowCount];
				double totalProtein = 0; // picograms
				double histoneMass = 0; // picograms
				double totalMolecules = 0;
				for (int row = 0; row < mdata.RowCount; row++){
					if (!double.IsNaN(column[row]) && !double.IsNaN(mw[row])){
						copyNumbers[row] = (column[row]/detectabilityNormFactor[row])*factor;
						totalMolecules += copyNumbers[row];
						totalProtein += (copyNumbers[row]*mw[row]*1e12)/avogadro; // picograms
						if (ArrayUtils.Contains(histoneRows, row)){
							histoneMass += (copyNumbers[row]*mw[row]*1e12)/avogadro; // picograms
						}
					}
				}
				double totalVolume = (totalProtein/(param.GetDoubleParam("Total cellular protein concentration [g/l]").Value))*1000;
				// femtoliters
				for (int row = 0; row < mdata.RowCount; row++){
					if (!double.IsNaN(column[row]) && !double.IsNaN(mw[row])){
						concentrations[row] = ((copyNumbers[row]/(totalVolume*1e-15))/avogadro)*1e9; // nanomolar
						massFraction[row] = (((copyNumbers[row]*mw[row]*1e12)/avogadro)/totalProtein)*1e6; // ppm
						moleFraction[row] = (copyNumbers[row]/totalMolecules)*1e6; // ppm
					}
				}
				string suffix = (sampleName == "") ? "" : " " + sampleName;
				if (ArrayUtils.Contains(outputColumns, 0)){
					mdata.AddNumericColumn("Copy number" + suffix, "", copyNumbers);
				}
				if (ArrayUtils.Contains(outputColumns, 1)){
					mdata.AddNumericColumn("Concentration [nM]" + suffix, "", concentrations);
				}
				if (ArrayUtils.Contains(outputColumns, 2)){
					mdata.AddNumericColumn("Abundance (mass/total mass) [*10^-6]" + suffix, "", massFraction);
				}
				if (ArrayUtils.Contains(outputColumns, 3)){
					mdata.AddNumericColumn("Abundance (molecules/total molecules) [*10^-6]" + suffix, "", moleFraction);
				}
				double[] rank = ArrayUtils.Rank(copyNumbers);
				double[] relativeRank = new double[mdata.RowCount];
				double validRanks = mdata.RowCount;
				for (int row = 0; row < mdata.RowCount; row++){
					// remove rank for protein with no copy number information
					if (double.IsNaN((copyNumbers[row])) || copyNumbers[row] == 0){
						rank[row] = double.NaN;
						validRanks--; // do not consider as valid
					}
					// invert ranking, so that rank 0 is the most abundant protein
					rank[row] = mdata.RowCount - rank[row];
				}
				for (int row = 0; row < mdata.RowCount; row++){
					relativeRank[row] = rank[row]/validRanks;
				}
				if (ArrayUtils.Contains(outputColumns, 4)){
					mdata.AddNumericColumn("Copy number rank" + suffix, "", rank);
				}
				if (ArrayUtils.Contains(outputColumns, 5)){
					mdata.AddNumericColumn("Relative copy number rank" + suffix, "", relativeRank);
				}
				if (intensityCols[col] < mdata.ExpressionColumnCount &&
					param.GetSingleChoiceWithSubParams("Averaging mode").Value != 3){
					totalProteinRow[intensityCols[col]] = Math.Round(totalProtein, 2);
					totalMoleculesRow[intensityCols[col]] = Math.Round(totalMolecules, 0);
					organismRow[intensityCols[col]] = new string[]{organism.name};
					histoneMassRow[intensityCols[col]] = Math.Round(histoneMass, 4);
					ploidyRow[intensityCols[col]] = Math.Round((histoneMass*1e-12)/cValue, 2);
					cellVolumeRow[intensityCols[col]] = Math.Round(totalVolume, 2); // femtoliters
				}
			}
			if (param.GetSingleChoiceWithSubParams("Averaging mode").Value != 3 && ArrayUtils.Contains(outputColumns, 6)){
				mdata.AddNumericRow("Total protein [pg/cell]", "", totalProteinRow);
				mdata.AddNumericRow("Total molecules per cell", "", totalMoleculesRow);
				mdata.AddCategoryRow("Organism", "", organismRow);
				mdata.AddNumericRow("Histone mass [pg/cell]", "", histoneMassRow);
				mdata.AddNumericRow("Ploidy", "", ploidyRow);
				mdata.AddNumericRow("Cell volume [fl]", "", cellVolumeRow);
			}
		}

		private static string[] Unique(string[][] x){
			List<string> result = new List<string>();
			foreach (string[] s1 in x){
				foreach (string s2 in s1){
					if (!result.Contains(s2)){
						result.Add(s2);
					}
				}
			}
			return result.ToArray();
		}

		private static IList<double> ExtractValidValues(double[] values, bool zerosAreValid){
			List<double> validValues = new List<double>();
			foreach (double value in values){
				if (!Double.IsNaN(value) && (!zerosAreValid || value != 0)){
					validValues.Add(value);
				}
			}
			return validValues.ToArray();
		}

		public ParametersWf GetParameters(IMatrixData mdata, ref string errorString) {
			return
				new ParametersWf(new ParameterWf[]{
					new SingleChoiceParamWf("Protein IDs"){
						Help = "Specify the column containing the protein IDs", Values = mdata.StringColumnNames,
						Value = Match(mdata.StringColumnNames.ToArray(), new[]{"Protein ID"}, false, true, true)[0]
					},
					new MultiChoiceParamWf("Intensities"){
						Help =
							"Specify the columns that contain the intensities to be used for copy number estimation. If several columns " +
								"are selected, the method will calculate the median.",
						Values = ArrayUtils.Concat(mdata.ExpressionColumnNames, mdata.NumericColumnNames),
						Value =
							Match(ArrayUtils.Concat(mdata.ExpressionColumnNames, mdata.NumericColumnNames), new[]{"intensit"}, false, true,
								false)
					},
					new SingleChoiceWithSubParamsWf("Averaging mode", 0){
						Values =
							new string[]{
								"All columns separately", "Same normalization for all columns", "Same normalization within groups",
								"Average all columns"
							},
						Help = "Select how multiple columns will be treated",
						SubParams =
							new List<ParametersWf>(){
								new ParametersWf(new ParameterWf[]{}), new ParametersWf(new ParameterWf[]{}),
								new ParametersWf(new ParameterWf[]{
									new SingleChoiceParamWf("Grouping"){
										Values = mdata.CategoryRowNames,
										Value = Match(mdata.CategoryRowNames.ToArray(), new[]{"group"}, false, true, true)[0]
									}
								}),
								new ParametersWf(new ParameterWf[]{})
							}
					},
					new BoolWithSubParamsWf("Logarithmized", false){
						Help = "Specify whether the intensities are logarithmized in the selected columns.",
						SubParamsFalse = new ParametersWf(new ParameterWf[]{}),
						SubParamsTrue =
							new ParametersWf(new ParameterWf[]
							{new SingleChoiceParamWf("log base"){Values = new[]{"2", "natural", "10"}, Value = 0}})
					},
					new SingleChoiceParamWf("Molecular masses"){
						Values = mdata.NumericColumnNames,
						Value = Match(mdata.NumericColumnNames.ToArray(), new[]{"weight"}, false, true, true)[0]
					},
					new BoolWithSubParamsWf("Detectability correction", false){
						Help =
							"Without correction, the algorithm assumes linearity between the signal and the mass of the proteins.\n" +
								"Optionally select protein-specific correction factors such as the number of theoretical peptides.",
						SubParamsFalse = new ParametersWf(new ParameterWf[]{}),
						SubParamsTrue =
							new ParametersWf(new ParameterWf[]{
								new SingleChoiceParamWf("Correction factor"){
									Values = mdata.NumericColumnNames,
									Value = Match(mdata.NumericColumnNames.ToArray(), new[]{"theoretical"}, false, true, true)[0]
								}
							})
					},
					new SingleChoiceWithSubParamsWf("Scaling mode", 1){
						Help = "Select how the absolute values should be scaled.",
						Values = new[]{"Total protein amount", "Histone proteomic ruler"},
						SubParams =
							new List<ParametersWf>(){
								new ParametersWf(new ParameterWf[]{
									new DoubleParamWf("Protein amount per cell [pg]", 200)
									{Help = "Specify the amount of protein per cell in picograms."}
								}),
								new ParametersWf(new ParameterWf[]{new DoubleParamWf("Ploidy", 2){}})
							}
					},
					new DoubleParamWf("Total cellular protein concentration [g/l]", 200)
					{Help = "Specify the expected total protein concentration (typically 200-300 g/l)."},
					new MultiChoiceParamWf("Output"){
						Help = "Select the desired output",
						Values =
							new[]{
								"Copy number per cell", "Concentration [nM]", "Relative abundance (mass/total mass)",
								"Relative abundance (molecules/total molecules)", "Copy number rank", "Relative copy number rank",
								"Sample summary (total protein, total molecules, cell volume, ...)"
							},
						Value = new[]{0, 6}
					}
				});
		}

		/// <summary>
		/// Finds strings in an array of strings.
		/// </summary>
		/// <param name="haystack">An array of strings that are to be searched.</param>
		/// <param name="needles">An array of string that are to be searched for individually.</param>
		/// <param name="caseSensitive"></param>
		/// <param name="matchSubstring"></param>
		/// <param name="matchFirstIfNothingFound">If nothing matched, the first element (index 0) will be returned to avoid returning null.</param>
		/// <returns>An array of the indices of the matched elements of haystack.</returns>
		private int[] Match(string[] haystack, string[] needles, bool caseSensitive, bool matchSubstring,
			bool matchFirstIfNothingFound){
			List<int> matches = new List<int>();
			for (int i = 0; i < haystack.Length; i++){
				string hay = (caseSensitive) ? haystack[i] : haystack[i].ToLower();
				hay.Trim();
				foreach (string hit in needles){
					string needle = (caseSensitive) ? hit : hit.ToLower();
					needle.Trim();
					if (hay.Equals(needle) || (matchSubstring && hay.Contains(needle))){
						matches.Add(i);
					}
				}
			}
			if (matches.Count == 0 && matchFirstIfNothingFound){
				matches.Add(0);
			}
			return matches.ToArray();
		}

		/// <summary>
		/// An object representing a model organism
		/// </summary>
		private class Organism{
			public string name = "n.d.";
			public double genomeSize = 0;
			public string[] histoneIds = new string[0];

			public override int GetHashCode(){
				return name.GetHashCode();
			}

			public override bool Equals(object obj){
				return Equals(obj as Organism);
			}

			private bool Equals(Organism o){
				return o != null && name.Equals(o.name);
			}
		}

		/// <summary>
		/// The list of the organisms that are supported.
		/// These organisms and their histones can be auto-detected, provided that uniprot IDs are used.
		/// </summary>
		/// <returns>A list of Organism objects</returns>
		private static Organism[] SupportedOrganisms(){
			List<Organism> organisms = new List<Organism>();
			Organism hSapiens = new Organism{
				name = "H. sapiens", genomeSize = 3200000000,
				histoneIds =
					new[]{
						"P07305", "Q8IZA3", "Q92522", "P0C5Y9", "P0C5Z0", "H0YFX9", "Q9BTM1", "A8MQC5", "C9J0D1", "C9J386", "E5RJU1",
						"Q71UI9", "P16104", "B4DJC3", "D6RCF2", "O75367", "Q5SQT3", "Q9P0M6", "P0C0S5", "P0C1H6", "A9UJN3", "P57053",
						"Q7Z2G1", "B4DEB1", "P84243", "B2R4P9", "K7EMV3", "K7ES00", "K7EK07", "K7EP01", "Q6NXT2", "Q02539", "P16401",
						"P16403", "P16402", "Q4VB24", "P10412", "A3R0T8", "A1L407", "P22492", "Q96QV6", "P04908", "Q08AJ9", "Q93077",
						"P20671", "P0C0S8", "A3KPC7", "Q96KK5", "Q99878", "A4FTV9", "Q92646", "Q96A08", "P33778", "P62807", "P58876",
						"B2R4S9", "Q93079", "P06899", "O60814", "Q99880", "I6L9F7", "Q99879", "Q99877", "P23527", "P68431", "P62805",
						"Q99525", "Q0VAS5", "B2R4R0", "Q6FI13", "Q8IUE6", "Q16777", "Q16778", "B4DR52", "Q5QNW6", "Q71DI3", "Q5TEC6",
						"Q7L7L0", "Q8N257", "Q16695", "Q6TXQ4", "Q14463", "B4E0B3", "B2R5B6", "A2RUA4", "B2R5B3", "Q9HA11", "A8K9J7",
						"B2R6Y1", "B4E380", "A8K4Y7", "Q6B823", "Q6LBZ2", "A3R0T7"
					}
			};
			organisms.Add(hSapiens);
			Organism mMusculus = new Organism{
				name = "M. musculus", genomeSize = 2700000000,
				histoneIds =
					new[]{
						"Q9DAD9", "B2RTM0", "Q8CBB6", "Q921L4", "Q5M8Q2", "Q810S6", "B1AV31", "Q497L1", "A9Z055", "Q8CGP9", "P10922",
						"Q8CJI4", "E0CZ52", "E0CYL2", "Q8VIK3", "Q80ZM5", "Q9CQ70", "Q8R1M2", "Q3THW5", "Q8R029", "B2RVP5", "P27661",
						"Q9QZQ8", "Q8CA90", "Q8BP16", "Q9CTR1", "Q8CCK0", "Q9D3V6", "Q9D3U7", "Q3UA95", "Q3TFU6", "G3UWL7", "G3UX40",
						"P0C0S6", "F8WI35", "E0CZ27", "E0CYN1", "E0CYR7", "P84244", "P02301", "Q9QYL0", "P43275", "P43276", "P15864",
						"Q5SZA3", "P43277", "Q149Z9", "P43274", "Q07133", "I7HFT9", "Q8CGP4", "P22752", "B2RVF0", "Q61668", "Q8CGP5",
						"A0AUV1", "Q8CGP6", "A3KPD0", "Q8CGP7", "F8WIX8", "A0JNS9", "P70696", "Q64475", "Q6ZWY9", "P10853", "Q64478",
						"A0JLV3", "Q8CGP1", "B2RVD5", "P10854", "B2RTK3", "Q8CGP2", "P68433", "P84228", "A1L0U3", "A1L0V4", "P62806",
						"B2RWH3", "Q6GSS7", "Q64522", "Q64523", "Q149V4", "Q64525", "G3X9D5", "Q64524", "B9EI85", "Q61667", "Q8BFU2",
						"A2AB79", "Q9D2U9", "Q8CGP0", "Q6B822", "P07978", "Q9D9Z7"
					}
			};
			organisms.Add(mMusculus);
			Organism dMelanogaster = new Organism{
				name = "D. melanogaster", genomeSize = 130000000,
				histoneIds =
					new[]{
						"Q6TXQ1", "P02255", "Q4AB54", "Q4ABE3", "Q4ABD8", "Q4AB94", "P84051", "Q4AB57", "P08985", "P02283", "P02299",
						"E2QCP0", "P84249", "P84040"
					}
			};
			organisms.Add(dMelanogaster);
			Organism cElegans = new Organism{
				name = "C. elegans", genomeSize = 100300000,
				histoneIds =
					new[]{
						"P10771", "P15796", "Q19743", "O17536", "O01833", "Q9U3W3", "Q18336", "P09588", "J7S164", "J7SA65", "Q27485",
						"Q23429", "Q27511", "P04255", "Q27894", "P08898", "K7ZUH9", "Q10453", "Q9U281", "Q27490", "Q27532", "P62784",
						"Q27484", "Q27876", "O16277", "Q27489"
					}
			};
			organisms.Add(cElegans);
			Organism sCerevisiae = new Organism{
				name = "S. cerevisiae", genomeSize = 12100000,
				histoneIds = new[]{"P53551", "P04911", "P04912", "Q12692", "P02293", "P02294", "P61830", "P02309"}
			};
			organisms.Add(sCerevisiae);
			Organism sPombe = new Organism{
				name = "S. pombe", genomeSize = 14100000,
				histoneIds = new[]{"P48003", "P04909", "P04910", "P04913", "P09988", "P10651", "P09322"}
			};
			return organisms.ToArray();
		}

		/// <summary>
		/// The list of the names of organisms that are supported.
		/// </summary>
		/// <returns>The names of the supported organisms.</returns>
		private string[] SupportedOrganismNames(){
			Organism[] organisms = SupportedOrganisms();
			List<string> names = new List<string>();
			foreach (Organism organism in organisms){
				names.Add(organism.name);
			}
			return names.ToArray();
		}

		/// <summary>
		/// Finds the organism given a set of ProteinIDs
		/// The function will look at all proteinIDs and match them to the list of known histone IDs for each supported organism
		/// The organism with most hits will be returned
		/// </summary>
		/// <param name="proteinGroupIds">protein group IDs (semicolon-separated)</param>
		/// <returns>the organism</returns>
		private static Organism DetectOrganism(string[] proteinGroupIds){
			Dictionary<Organism, int> histoneHits = new Dictionary<Organism, int>();
			foreach (Organism organism in SupportedOrganisms()){
				histoneHits.Add(organism, 0);
			}
			foreach (string proteinGroupId in proteinGroupIds){
				string[] ids = proteinGroupId.Split(new char[]{';'});
				foreach (string id in ids){
					foreach (Organism organism in SupportedOrganisms()){
						if (ArrayUtils.Contains(organism.histoneIds, id)){
							histoneHits[organism] += 1;
						}
					}
				}
			}
			Organism[] organisms = ArrayUtils.GetKeys(histoneHits);
			int[] counts = ArrayUtils.GetValues(histoneHits);
			if (histoneHits.Count > 0){
				return organisms[ArrayUtils.Order(counts)[counts.Length - 1]];
			}
			return new Organism();
		}

		/// <summary>
		/// Finds those protein groups that represent histones in the given organism
		/// </summary>
		/// <param name="proteinIds">protein group IDs (semicolon-separated)</param>
		/// <param name="organism">the organism</param>
		/// <returns>the row indices that represent histones</returns>
		private static int[] FindHistones(string[] proteinIds, Organism organism){
			List<int> histoneRows = new List<int>();
			for (int row = 0; row < proteinIds.Length; row++){
				bool isHistone = false;
				string[] ids = proteinIds[row].Split(new char[]{';'});
				foreach (string id in ids){
					if (ArrayUtils.Contains(organism.histoneIds, id)){
						isHistone = true;
					}
				}
				if (isHistone){
					histoneRows.Add(row);
				}
			}
			return histoneRows.ToArray();
		}
	}
}