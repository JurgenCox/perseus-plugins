using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PluginProteomicRuler{
	public class AnnotateSequences : IMatrixProcessing{
		public bool HasButton => false;
		public Bitmap2 DisplayImage => null;

		public string Description
			=>
				"Annotate proteins using information extracted from a (uniprot) fasta file.\n" +
				"This plugin extracts gene names, protein names, entry names, species names from uniprot fasta headers.\n" +
				"It calculates the length of the amino acid sequences, monoisotopic and average molecular masses, " +
				"optionally for the leading IDs or as the median of all IDs.\n" +
				"Finally, the numbers of theoretical peptides can be calculated for a range of proteases.\n";

		public string HelpOutput
			=> "A series of categorical or numeric annotation columns are added depending on the user selection.";

		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string Name => "Annotate proteins (fasta headers, sequence features, ...)";
		public string Heading => "Proteomic ruler";
		public bool IsActive => true;
		public float DisplayRank => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;

		public int GetMaxThreads(Parameters parameters){
			return 1;
		}

		public string Url => "http://141.61.102.17/perseus_doku/doku.php?id=perseus:plugins:proteomicruler:annotateproteins";

		public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
			int proteinIdColumnInd = param.GetParam<int>("Protein IDs").Value;
			string[][] proteinIds = new string[mdata.RowCount][];
			string[][] leadingIds = new string[mdata.RowCount][];
			List<string> allIds = new List<string>();
			for (int row = 0; row < mdata.RowCount; row++){
				proteinIds[row] = mdata.StringColumns[proteinIdColumnInd][row].Split(';');
				leadingIds[row] = new[]{proteinIds[row][0]};
				allIds.AddRange(proteinIds[row]);
			}
			string fastaFilePath = param.GetParam<string>("Fasta file").Value;
			Fasta fasta = new Fasta();
			fasta.ParseFile(fastaFilePath, processInfo);
			// Text annotations
			processInfo.Status("Adding fasta header annotations.");
			int[] selection =
				param.GetParamWithSubParams<int>("Fasta header annotations").GetSubParameters().GetParam<int[]>("Annotations").Value;
			string[][] idsToBeAnnotated = (param.GetParamWithSubParams<int>("Fasta header annotations").Value == 0)
				? proteinIds
				: leadingIds;
			ProteinSequence[][] fastaEntries = new ProteinSequence[mdata.RowCount][];
			for (int row = 0; row < mdata.RowCount; row++){
				List<ProteinSequence> rowEntries = new List<ProteinSequence>();
				foreach (string id in idsToBeAnnotated[row]){
					ProteinSequence entry = fasta.GetEntry(id);
					if (entry == null){
						continue;
					}
					rowEntries.Add(entry);
				}
				fastaEntries[row] = rowEntries.ToArray();
			}
			if (ArrayUtils.Contains(selection, 0)){ // Entry name
				string[] annotationColumn = new string[mdata.RowCount];
				for (int row = 0; row < mdata.RowCount; row++){
					List<string> rowAnnotations = new List<string>();
					foreach (ProteinSequence entry in fastaEntries[row]){
						string entryName = entry.EntryName;
						if (entryName != null && !ArrayUtils.Contains(rowAnnotations, entryName)){
							rowAnnotations.Add(entryName);
						}
					}
					annotationColumn[row] = string.Join(";", rowAnnotations.ToArray());
				}
				mdata.AddStringColumn("Entry name", "", annotationColumn);
			}
			if (ArrayUtils.Contains(selection, 1)){ // Gene name
				string[] annotationColumn = new string[mdata.RowCount];
				for (int row = 0; row < mdata.RowCount; row++){
					List<string> rowAnnotations = new List<string>();
					foreach (ProteinSequence entry in fastaEntries[row]){
						string geneName = entry.GeneName;
						if (geneName != null && !ArrayUtils.Contains(rowAnnotations, geneName)){
							rowAnnotations.Add(geneName);
						}
					}
					annotationColumn[row] = string.Join(";", rowAnnotations.ToArray());
				}
				mdata.AddStringColumn("Gene name", "", annotationColumn);
			}
			if (ArrayUtils.Contains(selection, 2)){
				// Verbose protein name, i.e. all protein names annotated in all fasta headers, including the 
				//'Isoform x of...' prefixes and '(Fragment)' suffixes
				string[] annotationColumn = new string[mdata.RowCount];
				for (int row = 0; row < mdata.RowCount; row++){
					List<string> rowAnnotations = new List<string>();
					foreach (ProteinSequence entry in fastaEntries[row]){
						string proteinName = entry.ProteinName;
						if (proteinName != null && !ArrayUtils.Contains(rowAnnotations, proteinName)){
							rowAnnotations.Add(proteinName);
						}
					}
					annotationColumn[row] = string.Join(";", rowAnnotations.ToArray());
				}
				mdata.AddStringColumn("Protein name (verbose)", "", annotationColumn);
			}
			if (ArrayUtils.Contains(selection, 3)){ // Consensus protein name
				string[] annotationColumn = new string[mdata.RowCount];
				for (int row = 0; row < mdata.RowCount; row++){
					List<string> rowAnnotations = new List<string>();
					foreach (ProteinSequence entry in fastaEntries[row]){
						string proteinName = entry.ConsensusProteinName;
						if (proteinName != null && !ArrayUtils.Contains(rowAnnotations, proteinName)){
							rowAnnotations.Add(proteinName);
						}
					}
					annotationColumn[row] = String.Join(";", rowAnnotations.ToArray());
				}
				mdata.AddStringColumn("Protein name", "", annotationColumn);
			}
			if (ArrayUtils.Contains(selection, 4)){ // Species
				string[] annotationColumn = new string[mdata.RowCount];
				for (int row = 0; row < mdata.RowCount; row++){
					List<string> rowAnnotations = new List<string>();
					foreach (ProteinSequence entry in fastaEntries[row]){
						string speciesName = entry.Species;
						if (speciesName != null && !ArrayUtils.Contains(rowAnnotations, speciesName)){
							rowAnnotations.Add(speciesName);
						}
					}
					annotationColumn[row] = String.Join(";", rowAnnotations.ToArray());
				}
				mdata.AddStringColumn("Species", "", annotationColumn);
			}
			// Numeric annotations
			processInfo.Status("Adding numeric annotations.");
			selection =
				param.GetParamWithSubParams<int>("Numeric annotations").GetSubParameters().GetParam<int[]>("Annotations").Value;
			bool annotateLeadingId = (param.GetParamWithSubParams<int>("Numeric annotations").Value == 1);
			if (ArrayUtils.Contains(selection, 0)){ // Sequence length
				double[] annotationColumn = new double[mdata.RowCount];
				for (int row = 0; row < mdata.RowCount; row++){
					List<double> rowAnnotations = new List<double>();
					foreach (ProteinSequence entry in fastaEntries[row]){
						double sequenceLength = entry.GetSequence().Length;
						rowAnnotations.Add(sequenceLength);
						if (annotateLeadingId && rowAnnotations.Count > 0){
							break;
						}
					}
					annotationColumn[row] = ArrayUtils.Median(rowAnnotations.ToArray());
				}
				mdata.AddNumericColumn("Sequence length", "", annotationColumn);
			}
			if (ArrayUtils.Contains(selection, 1)){ // Monoisotopic molecular mass
				double[] annotationColumn = new double[mdata.RowCount];
				for (int row = 0; row < mdata.RowCount; row++){
					List<double> rowAnnotations = new List<double>();
					foreach (ProteinSequence entry in fastaEntries[row]){
						double monoisotopicMass = entry.GetMonoisotopicMolecularMass();
						rowAnnotations.Add(monoisotopicMass);
						if (annotateLeadingId && rowAnnotations.Count > 0){
							break;
						}
					}
					annotationColumn[row] = ArrayUtils.Median(rowAnnotations.ToArray());
				}
				mdata.AddNumericColumn("Monoisotopic molecular mass", "", annotationColumn);
			}
			if (ArrayUtils.Contains(selection, 2)){ // Average molecular mass
				double[] annotationColumn = new double[mdata.RowCount];
				for (int row = 0; row < mdata.RowCount; row++){
					List<double> rowAnnotations = new List<double>();
					foreach (ProteinSequence entry in fastaEntries[row]){
						double averageMass = entry.GetAverageMolecularMass();
						rowAnnotations.Add(averageMass);
						if (annotateLeadingId && rowAnnotations.Count > 0){
							break;
						}
					}
					annotationColumn[row] = ArrayUtils.Median(rowAnnotations.ToArray());
				}
				mdata.AddNumericColumn("Average molecular mass", "", annotationColumn);
			}
			// Theoretical peptides
			processInfo.Status("Calculating theoretical peptides.");
			annotateLeadingId = (param.GetParamWithSubParams<int>("Calculate theoretical peptides").Value == 1);
			Protease[] proteases = ArrayUtils.SubArray(Constants.defaultProteases,
				param.GetParamWithSubParams<int>("Calculate theoretical peptides").GetSubParameters().GetParam<int[]>("Proteases")
					.Value);
			double minLength =
				param.GetParamWithSubParams<int>("Calculate theoretical peptides").GetSubParameters().GetParam<double>(
					"Min. peptide length").Value;
			double maxLength =
				param.GetParamWithSubParams<int>("Calculate theoretical peptides").GetSubParameters().GetParam<double>(
					"Max. peptide length").Value;
			bool displayPeptideSequences = annotateLeadingId &&
											param.GetParamWithSubParams<int>("Calculate theoretical peptides").GetSubParameters().GetParam<bool>(
												"Show sequences").Value;
			foreach (Protease protease in proteases){
				double[] annotationColumn = new double[mdata.RowCount];
				string[] peptideColumn = new string[mdata.RowCount];
				for (int row = 0; row < mdata.RowCount; row++){
					List<double> rowAnnotations = new List<double>();
					List<string> rowPeptides = new List<string>();
					foreach (ProteinSequence entry in fastaEntries[row]){
						double nTheoreticalPeptides = entry.GetNumberOfTheoreticalPeptides(protease, (int) minLength, (int) maxLength);
						rowAnnotations.Add(nTheoreticalPeptides);
						if (displayPeptideSequences){
							rowPeptides.AddRange(entry.GetTheoreticalPeptideSequences(protease, (int) minLength, (int) maxLength));
						}
						if (annotateLeadingId && rowAnnotations.Count > 0){
							break;
						}
					}
					annotationColumn[row] = ArrayUtils.Median(rowAnnotations.ToArray());
					peptideColumn[row] = String.Join(";", rowPeptides);
				}
				mdata.AddNumericColumn(
					"Number of theoretical peptides (" + protease.name + ", " + minLength + "-" + maxLength + ")", "", annotationColumn);
				if (displayPeptideSequences){
					mdata.AddStringColumn(
						"Theoretical peptide sequences (" + protease.name + ", " + minLength + "-" + maxLength + ")", "", peptideColumn);
				}
			}
			// Sequence features
			processInfo.Status("Counting sequence features.");
			annotateLeadingId = (param.GetParamWithSubParams<int>("Count sequence features").Value == 1);
			bool normalizeBySequenceLength =
				param.GetParamWithSubParams<int>("Count sequence features").GetSubParameters().GetParam<bool>(
					"Normalize by sequence length").Value;
			if (param.GetParamWithSubParams<int>("Count sequence features").GetSubParameters().GetParam<string>("Regex").Value !=
				""){
				Regex regex;
				try{
					regex =
						new Regex(
							param.GetParamWithSubParams<int>("Count sequence features").GetSubParameters().GetParam<string>("Regex").Value);
				} catch (ArgumentException){
					processInfo.ErrString = "The regular expression you provided has invalid syntax.";
					return;
				}
				double[] sequenceFeatureColumn = new double[mdata.RowCount];
				for (int row = 0; row < mdata.RowCount; row++){
					List<double> featureCount = new List<double>();
					foreach (ProteinSequence entry in fastaEntries[row]){
						double nFeatures = regex.Matches(entry.GetSequence()).Count;
						featureCount.Add(normalizeBySequenceLength ? nFeatures/entry.GetLength() : nFeatures);
						if (annotateLeadingId){
							break;
						}
					}
					sequenceFeatureColumn[row] = ArrayUtils.Median(featureCount.ToArray());
				}
				mdata.AddNumericColumn(
					(normalizeBySequenceLength ? "Normalized feature count (" : "Feature count (") + regex + ")", "",
					sequenceFeatureColumn);
			}
			processInfo.Status("Done.");
		}

		public Parameters GetParameters(IMatrixData mdata, ref string errorString){
			return
				new Parameters(new SingleChoiceParam("Protein IDs"){
					Help = "Specify the column containing the protein IDs",
					Values = mdata.StringColumnNames,
					Value = ProteomicRulerUtils.Match(mdata.StringColumnNames.ToArray(), new[]{"majority"}, false, true, true)[0],
				}, new FileParam("Fasta file"){
					Filter = FileUtils.fastaFilter,
					Help =
						"Select the fasta file used for the database search of this dataset. The software will assume " +
						"uniprot-formatted headers for extracting accession IDs and metadata. As fallback position, everything " +
						"after the > will be taken as ID, but no header metadata can be extracted from non-uniprot headers."
				}, new SingleChoiceWithSubParams("Fasta header annotations"){
					ParamNameWidth = 120,
					TotalWidth = 500,
					Help = "Specify the annotations to be extracted from uniprot fasta headers",
					Values = new[]{"for all IDs", "for the leading ID"},
					Value = 0,
					SubParams =
						new List<Parameters>(){
							new Parameters(new Parameter[]{
								new MultiChoiceParam("Annotations"){
									Values = new[]{"Entry name", "Gene name", "Protein name (verbose)", "Protein name (consensus)", "Species"},
									Value = new[]{1, 3},
								}
							}),
							new Parameters(new Parameter[]{
								new MultiChoiceParam("Annotations"){
									Values = new[]{"Entry name", "Gene name", "Protein name (verbose)", "Protein name (consensus)", "Species"},
									Value = new[]{1, 3},
								}
							})
						}
				}, new SingleChoiceWithSubParams("Numeric annotations"){
					ParamNameWidth = 120,
					TotalWidth = 500,
					Help = "Specify the annotations to be mapped as numeric annotations",
					Values = new[]{"median of all IDs", "for the leading ID"},
					Value = 0,
					SubParams =
						new List<Parameters>(){
							new Parameters(new Parameter[]{
								new MultiChoiceParam("Annotations"){
									Values = new[]{"Sequence length", "Monoisotopic molecular mass", "Average molecular mass"},
									Value = new[]{0, 2}
								}
							}),
							new Parameters(new Parameter[]{
								new MultiChoiceParam("Annotations"){
									Values = new[]{"Sequence length", "Monoisotopic molecular mass", "Average molecular mass"},
									Value = new[]{0, 2}
								}
							})
						}
				}, new SingleChoiceWithSubParams("Calculate theoretical peptides"){
					ParamNameWidth = 120,
					TotalWidth = 500,
					Help = "Calculate the numbers of theoretical peptides (without miscleavages) by in silico digestion.",
					Values = new[]{"median of all IDs", "for the leading ID"},
					Value = 0,
					SubParams =
						new List<Parameters>(){
							new Parameters(new MultiChoiceParam("Proteases"){Values = Constants.DefaultProteasesNames(), Value = new[]{0}}, new DoubleParam("Min. peptide length", 7), new DoubleParam("Max. peptide length", 30)),
							new Parameters(new MultiChoiceParam("Proteases"){Values = Constants.DefaultProteasesNames(), Value = new[]{0}}, new DoubleParam("Min. peptide length", 7), new DoubleParam("Max. peptide length", 30), new BoolParam("Show sequences", false))
						}
				}, new SingleChoiceWithSubParams("Count sequence features"){
					ParamNameWidth = 180,
					TotalWidth = 500,
					Help =
						"Count the number of matches to a given regular expression in the amino acid sequence (optionally normalized " +
						"by sequence length).\n\nExamples:\n[KR] tryptic cleavage sites\nN[^P][ST][^P] N-glysosylation motifs",
					Values = new[]{"median of all IDs", "for the leading ID"},
					Value = 0,
					SubParams =
						new List<Parameters>{
							new Parameters(new StringParam("Regex"), new BoolParam("Normalize by sequence length", true)),
							new Parameters(new StringParam("Regex"), new BoolParam("Normalize by sequence length", false))
						}
				});
		}
	}
}