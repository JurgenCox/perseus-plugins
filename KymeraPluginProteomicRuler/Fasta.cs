using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using PerseusApi.Generic;

namespace KymeraPluginProteomicRuler{
	internal class Fasta{
		private static readonly Regex regexUniprotAccession = new Regex(@"^>.*\|(.*)\|");
		public Dictionary<string, ProteinSequence> entries = new Dictionary<string, ProteinSequence>(100000);

		public void ParseFile(string path, ProcessInfo processInfo){
			processInfo.Status("Parsing " + path);
			string accession = "";
			int sequenceCounter = 0;
			StringBuilder sequence = new StringBuilder();
			ProteinSequence protein = new ProteinSequence();
			try{
				StreamReader file = new StreamReader(path);
				string line;
				while ((line = file.ReadLine()) != null){ // valid line
					if (sequenceCounter%500 == 0){
						processInfo.Status("Parsing " + path + ", " + (int) ((double) file.BaseStream.Position/file.BaseStream.Length*100) +
											"%");
					}
					bool lineIsHeader = line.StartsWith(">");

					// skip all lines until the first header is found
					if (sequenceCounter == 0 && !lineIsHeader){
						continue;
					}

					// line is a piece of a sequence
					if (sequenceCounter > 0 && !lineIsHeader){
						sequence.Append(line.Trim());
						continue;
					}

					// line is a fasta header
					if (lineIsHeader){
						if (sequenceCounter > 0)
							// this is not the first header, i.e. the previous sequence is now completely read in
						{
							// add the previous protein  
							protein.SetSequence(sequence.ToString());
							entries.Add(accession, protein);
						}
						// initialize a new protein
						protein = new ProteinSequence();
						sequenceCounter++;
						// then parse the new header
						string header = line;
						Match m = regexUniprotAccession.Match(header);
						if (m.Success){ // uniprot header
							accession = m.Groups[1].Value;
							protein.Accession = accession;
							protein.Header = header;
						} else{ // fallback position: take entire header after the > as accession
							accession = header.Substring(1).Trim();
							protein.Accession = accession;
							protein.Header = header;
						}
						sequence = new StringBuilder();
					}
				} //end while
				file.Close();

				//add the last protein
				if (sequenceCounter > 0){ // make sure there is at least one sequence in the file
					protein.SetSequence(sequence.ToString());
					entries.Add(accession, protein);
				}
			} catch (Exception){
				processInfo.ErrString =
					"Something went wrong while parsing the fasta file.\nMake sure the path is correct and the " +
					"file is not opened in another application.\nMake sure the fasta file is valid.";
			}
		}

		public ProteinSequence GetEntry(string accession){
			return entries.ContainsKey(accession) ? entries[accession] : null;
		}
	}
}