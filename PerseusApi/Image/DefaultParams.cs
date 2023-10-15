using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Accord.Math;
using BaseLibS.Api.Image;
using BaseLibS.Param;
using BaseLibS.Util;
using Newtonsoft.Json;
namespace PerseusApi.Image {

	/* From Béla to Carlo:
	 * 
	 * This is a namespace with static functions that I use multiple times. Thus, it is a library, 
	 * which allows a simple use of the same function in different .cs files. 
	 * 
	 * It is split into three classes, "Basics", "Analysis" and PreProcessing". Unfortunately, 
	 * I made these three classes after defining most of the functions in PreProcessing, so I still  
	 * have to move some of the functions from PreProcessing to Basics. However, this does not affect 
	 * their functionality, of course. Just don't be surprised if there will be some changes here in 
	 * the forseeable future. 
	 * 
	 */

	public static class Basics {

		/// <summary>
		/// Make a randomly named directory at the provided parent directory- 
		/// </summary>
		/// <param name="path">Path of containing directory</param>
		/// <returns>path of generated directory</returns>
		public static string MakeRandomDirectory(string path) {
			string dir = Path.GetRandomFileName();
			dir = dir.Substring(0, dir.Length - 4);
			dir = Path.Combine(path, dir);
			DirectoryInfo di = Directory.CreateDirectory(dir);
			return dir;
		}

		/// <summary>
		/// Get the complete data of an image. 
		/// </summary>
		/// <param name="image"></param>
		/// <returns></returns>
		public static float[][,,] GetData(IImageSeries image) {

			int xdim = image.LengthX;
			int ydim = image.LengthY;
			int zdim = image.LengthZ;
			int tdim = image.LengthT;

			float[][,,] data = new float[tdim][,,];

			for (int t = 0; t < tdim; t++) {
				data[t] = new float[xdim, ydim, zdim];
				for (int x = 0; x < xdim; x++) {
					for (int y = 0; y < ydim; y++) {
						for (int z = 0; z < zdim; z++) {
							data[t][x, y, z] = image.GetValueAt(t, x, y, z);
						}
					}
				}
			}

			return data;
		}

		/// <summary>
		/// Parse a JSON file containing MRI Metadata according to https://bids-specification.readthedocs.io/en/stable/04-modality-specific-files/01-magnetic-resonance-imaging-data.html to a Dictionary. 
		/// </summary>
		/// <param name="path">Path to JSON file</param>
		/// <returns></returns>
		public static Dictionary<string, object> ReadMetadataFromJSON(string path) {
			using (StreamReader r = new StreamReader(path)) {
				var text = File.ReadAllText(path);
				return JsonConvert.DeserializeObject<Dictionary<string, object>>(text);
			}

		}

	}

	public static class Analysis {

		/// <summary>
		/// Making a parameter that allows the user to double-check the event location and duration read in from the csv files .
		/// </summary>
		/// <param name="mdata"></param>
		/// <param name="name"></param>
		/// <returns>A Multi selection Parameter (=dropdown menu) where a textbox with the read in events is displayed for each selection. </returns>
		public static SingleChoiceWithSubParams MakeEventSelectionParam(IImageData mdata, string name = "Events") {
			(List<int> subInds, List<int> sesInds, List<int> runInds) = ImageUtils.GetAllFuncRunIndices(mdata);
			List<string> runs = ImageUtils.GetFuncNames(mdata);
			List<Parameters> p = new List<Parameters>();
			for (int i = 0; i < subInds.Count; i++) {
				List<string> x = new List<string>();
				for (int j = 0; j < mdata[subInds[i]].GetSessionAt(sesInds[i]).GetFuncAt(runInds[i]).Metadata.Events.GetLength(0); j++) {
					x.Add(mdata[subInds[i]].GetSessionAt(sesInds[i]).GetFuncAt(runInds[i]).Metadata.Events[j, 0] + "," + mdata[subInds[i]].GetSessionAt(sesInds[i]).GetFuncAt(runInds[i]).Metadata.Events[j, 1] + Environment.NewLine);
				}
				Parameters a = new Parameters(new MultiStringParam("Event Selection") {
					Value = x.ToArray()
				});
				p.Add(a);
			}
			SingleChoiceWithSubParams ev = new SingleChoiceWithSubParams(name) {
				Values = runs,
				SubParams = p,
			};
			return ev;
		}

		/// <summary>
		/// Benjamini-Hochberg procedure to control the FDR in the calculated 3-D p-value map.
		/// </summary>
		/// <param name="pValueList">List of p-values</param>
		/// /// <param name="pValueMap">3D space of p-values ("statistical parameter map")</param>
		/// <param name="alpha">wanted false discovery rate</param>
		/// <returns>A 3D float array indicating positive voxels ("significant") with 1 and negatives ("non-significant") with 0.</returns>
		public static float[,,] BenjaminiHochberg(List<float> pValueList, float[,,] pValueMap, float alpha) {

			int xdim = pValueMap.GetLength(0);
			int ydim = pValueMap.GetLength(1);
			int zdim = pValueMap.GetLength(2);

			float[,,] result = new float[xdim, ydim, zdim];

			List<float> allResults = new List<float>(pValueList.OrderBy(x => x).ToArray());
			float threshold = 0;
			int N = allResults.Count;
			for (int j = 0; j < N; j++) {
				if (alpha * (j + 1) / N <= allResults[j]) {
					//Console.WriteLine("alpha: {0}, j: {1}, N: {2}, p-value: {3}", alpha, j + 1, N, allResults[j]);
					threshold = allResults[j];
					break;
				}
			}
			for (int i = 0; i < xdim; i++) {
				for (int j = 0; j < ydim; j++) {
					for (int k = 0; k < zdim; k++) {
						if (pValueMap[i, j, k] <= threshold) {
							result[i, j, k] = 1; // "positives"
						}
						else {
							result[i, j, k] = 0; // "negatives"
						}
					}
				}
			}
			//Console.WriteLine("FDR Threshold: {0}", threshold);
			return result;
		}

		/// <summary>
		/// Estimates minimum variance unbiased estimators (MVUE) of the parameters (beta) of a GLM by least squares. Based on Gauß-Markov theorem, that is, assuming the error variance is constant. 
		/// See Ashby, 2019, p. 129ff for more information. 
		/// </summary>
		/// <param name="X">design matrix</param>
		/// <param name="y">observed data</param>
		/// <returns></returns>
		public static float[] EstimateParametersInGLM(float[,] X, float[] y) {
			float[] b = Accord.Math.Matrix.Dot(Accord.Math.Matrix.Dot(Accord.Math.Matrix.Inverse(Accord.Math.Matrix.Dot(X.Transpose(), X)), X.Transpose()), y); // beta = (X' * X) ** -1 * X' * y, equation (6.18) in Ashby, 2019, p. 130
			return b;
		}

		/// <summary>
		/// Calculates a t-value for the GLM of a FBR analysis. 
		/// </summary>
		/// <param name="designMatrix"></param>
		/// <param name="hypothesisMatrix"></param>
		/// <param name="observedData"></param>
		/// <param name="parameters"></param>
		/// <returns>t-value</returns>
		public static float ApplyGLM(float[,] designMatrix, float[] hypothesisMatrix, float[] observedData, float[] parameters) {
			int df = observedData.Length - parameters.Length; // degrees of freedom, equation (6.20) in Ashby, 2019, p. 131
			float var = Accord.Math.Matrix.Dot(observedData.Subtract(Accord.Math.Matrix.Dot(designMatrix, parameters)), observedData.Subtract(Accord.Math.Matrix.Dot(designMatrix, parameters))) / df; // estimated variance, equation (6.19) in Ashby, 2019, p. 131
			double t = (Accord.Math.Matrix.Dot(hypothesisMatrix, parameters) - 0) / (System.Math.Sqrt(var * Accord.Math.Matrix.Dot(Accord.Math.Matrix.Dot(hypothesisMatrix, Accord.Math.Matrix.Dot(designMatrix.Transpose(), designMatrix).Inverse()), hypothesisMatrix)));// calculate t-score, equation (6.24) in Ashby, 2019, p. 135
			return (float)t;
		}

		// TODO
		public static float[,,] PermutationBasedFWERVoxel(IImageSeries run, float[,] designMatrix, float[] hypothesisMatrix, float[,,] tValueMap, float alpha, int repetitions) {

			Console.WriteLine("Starting Permutations");
			// stop total time
			var watchTotal = System.Diagnostics.Stopwatch.StartNew();

			float threshold = 1;

			int tdim = run.LengthT;

			// get indices of time steps
			int[] indexes = new int[tdim];
			for (int i = 0; i < indexes.Length; i++) {
				indexes[i] = i;
			}

			// make random number generator
			Random rng = new Random();

			// list of maximum t-values
			float[] maxTs = new float[repetitions];

			// reshuffle, apply GLM and get maximum t-value
			for (int k = 0; k < repetitions; k++) {

				Console.WriteLine("Reshuffling #{0}", k + 1);

				// stop time
				var watch = System.Diagnostics.Stopwatch.StartNew();

				float maxT = 0; // maximum t-value of current reshuffled run
				FisherYatesShuffle(rng, indexes);
				for (int xpos = 0; xpos < run.LengthX; xpos++) {
					for (int ypos = 0; ypos < run.LengthY; ypos++) {
						for (int zpos = 0; zpos < run.LengthZ; zpos++) {
							float[] y = new float[tdim];
							for (int i = 0; i < tdim; i++) {
								y[i] = run.GetValueAt(indexes[i], xpos, ypos, zpos);
							}
							float[] b = EstimateParametersInGLM(designMatrix, y);
							float t = ApplyGLM(designMatrix, hypothesisMatrix, y, b);
							if (t > maxT) {
								maxT = t;
							}
						}
					}
				}
				maxTs[k] = maxT;

				// print consumed time
				watch.Stop();
				var elapsed = watch.Elapsed;
				Console.WriteLine("Time consumed for this reshuffling: {0}", elapsed);
			}

			// order maxTs from largest to smallest
			maxTs = maxTs.OrderBy(x => x).ToArray().Reversed();

			// calculate threshold
			threshold = maxTs[(int)(repetitions * alpha)]; // typecast makes it round to the lower int, therefore the higher t-value is selected (being conservative). 

			// create boolean map
			float[,,] result = new float[run.LengthX, run.LengthY, run.LengthZ];
			for (int i = 0; i < run.LengthX; i++) {
				for (int j = 0; j < run.LengthY; j++) {
					for (int k = 0; k < run.LengthZ; k++) {
						if (tValueMap[i, j, k] >= threshold) {
							result[i, j, k] = 1; // "positives"
						}
						else {
							result[i, j, k] = 0; // "negatives"
						}
					}
				}
			}


			// print total consumed time
			watchTotal.Stop();
			var elapsedTotal = watchTotal.Elapsed;
			Console.WriteLine("Total time consumed for this multiple comparison control: {0}", elapsedTotal);

			return result;
		}

		/// <summary>
		/// Calculate significance by FDR approach, FDR calculated by permutation. 
		/// </summary>
		/// <param name="run"></param>
		/// <param name="designMatrix"></param>
		/// <param name="hypothesisMatrix"></param>
		/// <param name="tValueMap"></param>
		/// <param name="alpha"></param>
		/// <param name="repetitions"></param>
		/// <returns></returns>
		public static float[,,] PermutationBasedFDRVoxel(IImageSeries run, float[,] designMatrix, float[] hypothesisMatrix, float[,,] tValueMap, float alpha, int repetitions) {

			// threshold calculation:
			// FDR = FP / TP
			// we estimate FP by permutations and TP by the data provided. 
			// make list of all t-values generated by the permutation: t_perm. It has N values: t_perm_1 , ... , t_perm_n
			// make list of all t_values given tValueMap (from data): t_data. It has M values: t_data_1, ..., t_data:m
			// FP = #{i where t_perm_i > thres, i in [1,N]}/#permutations, TP = #{j where t_data_j > thres, j in [1,M]}
			// select threshold where FDR is closest to alpha (but lower)

			// TODO: see https://academic.oup.com/bioinformatics/article/21/23/4280/194680 

			Console.WriteLine("Starting Permutations");
			// stop total time
			var watchTotal = System.Diagnostics.Stopwatch.StartNew();

			int tdim = run.LengthT;
			int xdim = run.LengthX;
			int ydim = run.LengthY;
			int zdim = run.LengthZ;
			int voxelNumber = xdim * ydim * zdim;

			// get indices of time steps
			int[] indexes = new int[tdim];
			for (int i = 0; i < indexes.Length; i++) {
				indexes[i] = i;
			}

			// make random number generator
			Random rng = new Random();

			// list of all t-values generated by the permutation test
			List<float> allTs = new List<float>();

			// reshuffle, apply GLM and get maximum t-value
			for (int k = 0; k < repetitions; k++) {

				Console.WriteLine("Reshuffling #{0}", k + 1);

				// stop time
				var watch = System.Diagnostics.Stopwatch.StartNew();

				float maxT = 0; // maximum t-value of current reshuffled run
				FisherYatesShuffle(rng, indexes);
				for (int xpos = 0; xpos < run.LengthX; xpos++) {
					for (int ypos = 0; ypos < run.LengthY; ypos++) {
						for (int zpos = 0; zpos < run.LengthZ; zpos++) {
							float[] y = new float[tdim];
							for (int i = 0; i < tdim; i++) {
								y[i] = run.GetValueAt(indexes[i], xpos, ypos, zpos);
							}
							float[] b = EstimateParametersInGLM(designMatrix, y);
							float t = ApplyGLM(designMatrix, hypothesisMatrix, y, b);
							allTs.Add(t);
						}
					}
				}

				// print consumed time
				watch.Stop();
				var elapsed = watch.Elapsed;
				Console.WriteLine("Time consumed for this reshuffling: {0}", elapsed);
			}

			// order largest to smallest
			float[] t_perm = allTs.ToArray().OrderBy(x => x).ToArray().Reversed();

			// make flattened ordered data t-values (largest to smallest)
			List<float> t_data_list = new List<float>();
			for (int x = 0; x < xdim; x++) {
				for (int y = 0; y < ydim; y++) {
					for (int z = 0; z < zdim; z++) {
						t_data_list.Add(tValueMap[x, y, z]);
					}
				}
			}
			float[] t_data = t_data_list.ToArray().OrderBy(x => x).ToArray().Reversed();

			float threshold = 1;

			// find best threshold. TODO: Speed optimisation. This is super slow. 
			float fdr = 0;
			int threshold_ind = 0;
			while (fdr < alpha && threshold_ind < t_data.Length) {
				int tp = threshold_ind + 1;
				int fp = 0;

				float t_perm_cur = 1000;

				for (int j = 0; j < t_perm.Length; j++) {
					if (t_perm[j] < t_data[threshold_ind]) { // first occurrence of t-value smaller than threshold. All before were significant. 
						fp = j;
						t_perm_cur = t_perm[j];
						break;
					}
				}

				fdr = ((float)fp / (float)repetitions) / (float)tp;
				Console.WriteLine("Index: {0} t-value_data: {1} FDR: {2} t-value_perm: {3} FP: {4}", threshold_ind, t_data[threshold_ind], fdr, t_perm_cur, fp);

				threshold_ind++;
			}
			if (threshold_ind <= 1) {
				threshold = 1;
			}
			else {
				threshold = t_data[threshold_ind - 2]; // threshold is last t-value of t_data where fdr was <alpha. 
			}

			Console.WriteLine("Selected threshold: {0}", threshold);

			// create boolean map
			float[,,] result = new float[run.LengthX, run.LengthY, run.LengthZ];
			for (int i = 0; i < run.LengthX; i++) {
				for (int j = 0; j < run.LengthY; j++) {
					for (int k = 0; k < run.LengthZ; k++) {
						if (tValueMap[i, j, k] >= threshold) {
							result[i, j, k] = 1; // "positives"
						}
						else {
							result[i, j, k] = 0; // "negatives"
						}
					}
				}
			}


			// print total consumed time
			watchTotal.Stop();
			var elapsedTotal = watchTotal.Elapsed;
			Console.WriteLine("FDR threshold for t-values: {0}", threshold);
			Console.WriteLine("Total time consumed for this multiple comparison control: {0}", elapsedTotal);

			return result;
		}



		/// <summary>
		/// Randomly shuffles an int[] array
		/// </summary>
		/// <param name="rng">random number generator</param>
		/// <param name="array">array to shuffle</param>
		private static void FisherYatesShuffle(Random rng, int[] array) {
			int n = array.Length;
			for (int i = n - 1; i > 0; i--) {
				int j = rng.Next(i + 1);
				int tmp = array[i];
				array[i] = array[j];
				array[j] = tmp;
			}
			return;
		}

	}

	public static class PreProcessing {

		public static string SPMpath = @"C:\Users\frohn\Desktop\spm12\spm12";

		/// <summary>
		/// Returns a StringParameter to get the path to SPM-containing folder (default: last choice)
		/// </summary>
		public static FolderParam AddSPMPath(string name = "Path to SPM") {
			return new FolderParam(name, SPMpath) {
				Help = "To run SPM, Perseus needs the path to the SPM folder on your machine."
			};
		}

		/// <summary>
		/// Make temporary Nifti files from selected runs and return sorted pathes. This version is for ordering runs by subject ignoring sessions. 
		/// </summary>
		/// <param name="subInds">Subject Indices</param>
		/// <param name="sesInds">Session Indices</param>
		/// <param name="runInds">Run Indices</param>
		/// <param name="pathToTmpFiles">Path to directory where temporary files should be generated</param>
		/// <param name="mdata">current data</param>
		/// <returns>List of List of List of strings. Structure: First List indicates subject, sublists contain runs. </returns>
		public static List<List<string>> MakeSPMInputFuncFilesAllSubjects(List<int> subInds, List<int> sesInds, List<int> runInds, string pathToTmpFiles, IImageData mdata) {
			List<List<string>> pathes = new List<List<string>>();
			for (int i = 0; i < subInds.Count; i++) {

				// make new entry for every subject.
				while (subInds[i] >= pathes.Count) {
					pathes.Add(new List<string>());
				}

				// make filename
				string randomPath = pathToTmpFiles + "\\tmp" + i.ToString() + ".nii";

				// Make temporary Nifti file and add path to List
				IImageSeries image = mdata[subInds[i]].GetSessionAt(sesInds[i]).GetFuncAt(runInds[i]);
				MriMetadata tmpMetadata = (MriMetadata)image.Metadata;
				float[][,,] data = Basics.GetData(image);
				tmpMetadata.WriteNiftiFile(randomPath, data);
				pathes[subInds[i]].Add(randomPath);
			}
			return pathes;
		}

		/// <summary>
		/// Make temporary Nifti files from selected runs and return sorted pathes. This version is for ordering runs by subject and sessions. 
		/// </summary>
		/// <param name="subInds">Subject Indices</param>
		/// <param name="sesInds">Session Indices</param>
		/// <param name="runInds">Run Indices</param>
		/// <param name="pathToTmpFiles">Path to directory where temporary files should be generated</param>
		/// <param name="mdata">current data</param>
		/// <returns>List of List of List of strings. Structure: First List indicates subject, sublist session, subsublists runs. </returns>
		public static List<List<List<string>>> MakeSPMInputFuncFilesPerSubject(List<int> subInds, List<int> sesInds, List<int> runInds, string pathToTmpFiles, IImageData mdata) {
			List<List<List<string>>> pathes = new List<List<List<string>>>();
			for (int i = 0; i < subInds.Count; i++) {

				// make new entry for every subject
				while (subInds[i] >= pathes.Count) {
					pathes.Add(new List<List<string>>());
				}

				// make new entry for every session
				while (sesInds[i] >= pathes[subInds[i]].Count) {
					pathes[subInds[i]].Add(new List<string>());
				}

				// make filename
				string randomPath = pathToTmpFiles + "\\tmp" + i.ToString() + ".nii";

				// Make temporary Nifti file and add path to List
				IImageSeries image = mdata[subInds[i]].GetSessionAt(sesInds[i]).GetFuncAt(runInds[i]);
				MriMetadata tmpMetadata = (MriMetadata)image.Metadata;
				float[][,,] data = Basics.GetData(image);
				tmpMetadata.WriteNiftiFile(randomPath, data);
				pathes[subInds[i]][sesInds[i]].Add(randomPath);
			}
			return pathes;
		}

		/// <summary>
		/// Make temporary Nifti files from selected runs and return unsorted pathes. A list of all runs is returned. 
		/// </summary>
		/// <param name="subInds">Subject Indices</param>
		/// <param name="sesInds">Session Indices</param>
		/// <param name="runInds">Run Indices</param>
		/// <param name="pathToTmpFiles">Path to directory where temporary files should be generated</param>
		/// <param name="mdata">current data</param>
		/// <returns>List of strings. Each entry is the path to a run's nifti file. </returns>
		public static List<string> MakeSPMInputFuncFilesUnsorted(List<int> subInds, List<int> sesInds, List<int> runInds, string pathToTmpFiles, IImageData mdata) {
			List<string> pathes = new List<string>();
			for (int i = 0; i < subInds.Count; i++) {

				// make filename
				string randomPath = pathToTmpFiles + "\\tmp" + i.ToString() + ".nii";

				// Make temporary Nifti file and add path to List
				IImageSeries image = mdata[subInds[i]].GetSessionAt(sesInds[i]).GetFuncAt(runInds[i]);
				MriMetadata tmpMetadata = (MriMetadata)image.Metadata;
				float[][,,] data = Basics.GetData(image);
				tmpMetadata.WriteNiftiFile(randomPath, data);
				pathes.Add(randomPath);
			}
			return pathes;
		}

		/// <summary>
		/// Make temporary Nifti files from selected runs and return unsorted pathes. A list of all runs is returned. 
		/// </summary>
		/// <param name="subInds">Subject Indices</param>
		/// <param name="sesInds">Session Indices</param>
		/// <param name="runInds">Run Indices</param>
		/// <param name="pathToTmpFiles">Path to directory where temporary files should be generated</param>
		/// <param name="mdata">current data</param>
		/// <returns>List of strings. Each entry is the path to a run's nifti file. </returns>
		public static List<string> MakeSPMInputAnatFilesUnsorted(List<int> subInds, List<int> sesInds, List<int> runInds, string pathToTmpFiles, IImageData mdata, string appendix = "") {
			List<string> pathes = new List<string>();
			for (int i = 0; i < subInds.Count; i++) {

				// make filename
				string randomPath = pathToTmpFiles + "\\tmp" + appendix + Parser.ToString(i) + ".nii";

				// Make temporary Nifti file and add path to List
				IImageSeries image = mdata[subInds[i]].GetSessionAt(sesInds[i]).GetAnatAt(runInds[i]);
				MriMetadata tmpMetadata = (MriMetadata)image.Metadata;
				float[][,,] data = Basics.GetData(image);
				tmpMetadata.WriteNiftiFile(randomPath, data);
				pathes.Add(randomPath);
			}
			return pathes;
		}

		/// <summary>
		/// Returns a SingleChoiceParam that allows selection of the SPM wrapping parameter
		/// </summary>
		public static SingleChoiceParam AddWrapping(string name = "Wrapping") {
			return new SingleChoiceParam(name) {
				Values = new List<string>{
					"No wrap",
					"Wrap X",
					"Wrap Y",
					"Wrap X & Y",
					"Wrap Z",
					"Wrap X & Z",
					"Wrap Y & Z",
					"Wrap X, Y & Z",
				},
				Help = "This indicates which direction in the volumes the values should wrap around in.",
			};
		}

		/// <summary>
		/// Returns a SingleChoiceParam that allows selection of the SPM interpolation parameter
		/// </summary>
		public static SingleChoiceParam AddInterpolation(string name = "Interpolation", int defaultValue = 4) {
			SingleChoiceParam interpolation = new SingleChoiceParam(name, defaultValue) {
				Values = new List<string>{
					"Nearest neighbour", // indexes are identical to SPM input values except when Fourier transformation is added. This has value Inf
					"Trilinear",
					"2nd Degree B-Spline",
					"3rd Degree B-Spline",
					"4th Degree B-Spline",
					"5th Degree B-Spline",
					"6th Degree B-Spline",
					"7th Degree B-Spline"
				},
				Help = "The method by which the images are sampled when estimating the optimum transformation. " +
				"\nHigher degree interpolation methods provide the better interpolation, but they are slower because " +
				"they use more neighbouring voxels. "
			};
			return interpolation;
		}

		/// <summary>
		/// Deletes the input nifti files (indicated by param pathes), updates the data and deletes the nifti files generated by SPM.
		/// </summary>
		/// <param name="pathes">A list of pathes to the input files to be deleted</param>
		/// <param name="prefix">Appendix of the SPM output files (not .nii but appendix added to previous file name before identifier)</param>
		/// <param name="mdata">current IImageData</param>
		/// <param name="SubInds">List of Subject Indices</param>
		/// <param name="RunInds">List of Func Run Indices</param>
		public static (bool, string) UpdateData(List<string> pathes, string prefix, IImageData mdata, List<int> SubInds, List<int> SesInds, List<int> RunInds, string nameAddition) {
			// deleting input files
			foreach (string path in pathes) {
				File.Delete(path);
			}

			// get output pathes
			List<string> newpathes = new List<string>();
			foreach (string path in pathes) {
				string[] splitted = path.Split('\\');
				splitted[splitted.Length - 1] = prefix + splitted[splitted.Length - 1];
				newpathes.Add(string.Join("\\", splitted));
			}

			// change data
			for (int i = 0; i < SubInds.Count; i++) {
				MriMetadata tmpMetadata = new MriMetadata();
				(bool valid, string errorMessage) = tmpMetadata.ReadNiftiHeader(newpathes[i]);
				if (!valid) {
					return (valid, errorMessage);
				}
				float[][,,] data = tmpMetadata.GetDataFromNifti(newpathes[i]);
				IImageSeries curRun = mdata[SubInds[i]].GetSessionAt(SesInds[i]).GetFuncAt(RunInds[i]);
				curRun.SetData(data);
				curRun.Name += nameAddition;
			}

			// deleting output files
			foreach (string path in newpathes) {
				File.Delete(path);
			}

			return (true, String.Empty);
		}

		public static (bool, string) UpdateDataAnat(List<string> pathes, string prefix, IImageData mdata, List<int> SubInds, List<int> SesInds, List<int> RunInds, string nameAddition) {
			// deleting input files
			foreach (string path in pathes) {
				File.Delete(path);
			}

			// get output pathes
			List<string> newpathes = new List<string>();
			foreach (string path in pathes) {
				string[] splitted = path.Split('\\');
				splitted[splitted.Length - 1] = prefix + splitted[splitted.Length - 1];
				newpathes.Add(string.Join("\\", splitted));
			}

			// change data
			for (int i = 0; i < SubInds.Count; i++) {
				MriMetadata tmpMetadata = new MriMetadata();
				(bool valid, string errorMessage) = tmpMetadata.ReadNiftiHeader(newpathes[i]);
				if (!valid) {
					return (valid, errorMessage);
				}
				float[][,,] data = tmpMetadata.GetDataFromNifti(newpathes[i]);
				IImageSeries curRun = mdata[SubInds[i]].GetSessionAt(SesInds[i]).GetAnatAt(RunInds[i]);
				curRun.SetData(data);
				curRun.Name += nameAddition;
			}

			// deleting output files
			foreach (string path in newpathes) {
				File.Delete(path);
			}

			return (true, String.Empty);
		}

		// command to update source image in coregistration
		public static (bool, string) UpdateDataSource(List<string> pathes, string prefix, IImageData mdata, List<int> SubInds, List<int> SesInds, List<int> RunInds, string nameAddition) {
			// deleting input files
			foreach (string path in pathes) {
				File.Delete(path);
			}

			// get output pathes
			List<string> newpathes = new List<string>();
			foreach (string path in pathes) {
				string[] splitted = path.Split('\\');
				splitted[splitted.Length - 1] = prefix + splitted[splitted.Length - 1];
				newpathes.Add(string.Join("\\", splitted));
			}

			// change data
			for (int i = 0; i < SubInds.Count; i++) {
				MriMetadata tmpMetadata = new MriMetadata();
				(bool valid, string errorMessage) = tmpMetadata.ReadNiftiHeader(newpathes[i]);
				if (!valid) {
					return (valid, errorMessage);
				}
				float[][,,] data = tmpMetadata.GetDataFromNifti(newpathes[i]);
				IImageSeries curRun = mdata[SubInds[i]].GetSessionAt(SesInds[i]).GetAnatAt(RunInds[i]);
				curRun.SetData(data);
				curRun.Name += nameAddition;
				((MriMetadata)curRun.Metadata).NiftiHeader = tmpMetadata.NiftiHeader;
			}

			// deleting output files
			foreach (string path in newpathes) {
				File.Delete(path);
			}

			return (true, String.Empty);
		}

		public static (bool, string) UpdateDataSource2(List<string> pathes, IImageData mdata, List<int> SubInds, List<int> SesInds, List<int> RunInds, string nameAddition) {

			// get input pathes
			List<string> newpathes = new List<string>();
			foreach (string path in pathes) {
				newpathes.Add(path);
			}

			// change data
			for (int i = 0; i < SubInds.Count; i++) {
				MriMetadata tmpMetadata = new MriMetadata();
				(bool valid, string errorMessage) = tmpMetadata.ReadNiftiHeader(newpathes[i]);
				if (!valid) {
					return (valid, errorMessage);
				}
				float[][,,] data = tmpMetadata.GetDataFromNifti(newpathes[i]);
				IImageSeries curRun = mdata[SubInds[i]].GetSessionAt(SesInds[i]).GetAnatAt(RunInds[i]);
				curRun.SetData(data);
				curRun.Name += nameAddition;
				((MriMetadata)curRun.Metadata).NiftiHeader = tmpMetadata.NiftiHeader;

			}

			// deleting output files
			foreach (string path in newpathes) {
				File.Delete(path);
			}

			return (true, String.Empty);
		}

		public static (bool, string) UpdateData(List<List<string>> pathes, string appendix, IImageData mdata, List<int> SubInds, List<int> SesInds, List<int> RunInds, string nameAddition) {
			// adding new pathes to list
			List<string> newpathes = new List<string>();
			foreach (List<string> subject in pathes) {
				foreach (string path in subject) {
					string[] splitted = path.Split('\\');
					splitted[splitted.Length - 1] = appendix + splitted[splitted.Length - 1];
					newpathes.Add(string.Join("\\", splitted));
				}
			}

			// change data
			for (int i = 0; i < SubInds.Count; i++) {
				MriMetadata tmpMetadata = new MriMetadata();
				(bool valid, string errorMessage) = tmpMetadata.ReadNiftiHeader(newpathes[i]);
				if (!valid) {
					return (valid, errorMessage);
				}
				float[][,,] data = tmpMetadata.GetDataFromNifti(newpathes[i]);
				IImageSeries curRun = mdata[SubInds[i]].GetSessionAt(SesInds[i]).GetFuncAt(RunInds[i]);
				curRun.SetData(data);
				curRun.Name += nameAddition;
			}

			return (true, String.Empty);
		}

		public static (bool, string) UpdateData(List<List<List<string>>> pathes, string appendix, IImageData mdata, List<int> SubInds, List<int> SesInds, List<int> RunInds, string nameAddition) {
			// adding new pathes to list
			List<string> newpathes = new List<string>();
			foreach (List<List<string>> subject in pathes) {
				foreach (List<string> session in subject) {
					foreach (string path in session) {
						string[] splitted = path.Split('\\');
						splitted[splitted.Length - 1] = appendix + splitted[splitted.Length - 1];
						newpathes.Add(string.Join("\\", splitted));
					}
				}
			}
			// change data
			for (int i = 0; i < SubInds.Count; i++) {
				MriMetadata tmpMetadata = new MriMetadata();
				(bool valid, string errorMessage) = tmpMetadata.ReadNiftiHeader(newpathes[i]);
				if (!valid) {
					return (valid, errorMessage);
				}
				//Console.WriteLine(newpathes[i]); // TODO DEBUG
				float[][,,] data = tmpMetadata.GetDataFromNifti(newpathes[i]);
				IImageSeries curRun = mdata[SubInds[i]].GetSessionAt(SesInds[i]).GetFuncAt(RunInds[i]);
				curRun.SetData(data);
				curRun.Name += nameAddition;
				((MriMetadata)curRun.Metadata).NiftiHeader = tmpMetadata.NiftiHeader;
			}

			return (true, String.Empty);
		}

		public static string DecodeWrapping(int value) {
			List<string> translation = new List<string> {
				"[0 0 0]",
				"[1 0 0]",
				"[0 1 0]",
				"[1 1 0]",
				"[0 0 1]",
				"[1 0 1]",
				"[0 1 1]",
				"[1 1 1]"
			};
			return translation[value];
		}

		public static double[,,] NestedTo3D(double[][][] input) {
			double[,,] output = new double[input.Length, input[0].Length, input[0][0].Length];
			for (int i = 0; i < input.Length; i++) {
				if (input[i].Length != output.GetLength(1)) {
					throw new Exception("Input contains arrays of different lengths");
				}
				for (int j = 0; j < input[0].Length; j++) {
					if (input[i][j].Length != output.GetLength(2)) {
						throw new Exception("Input contains arrays of different lengths");
					}
					for (int k = 0; k < input[0][0].Length; k++) {
						output[i, j, k] = input[i][j][k];
					}
				}
			}
			return output;
		}

		// change 7-D matrix into 5-D matrix (big nifti file to deformation field)
		public static float[,][,,] Nifti7Dto5D(float[,,,][,,] input) {
			float[,][,,] output = new float[input.GetLength(3), input.GetLength(2)][,,];
			for (int m = 0; m < input.GetLength(3); m++) {
				for (int l = 0; l < input.GetLength(2); l++) {
					output[m, l] = new float[input[0, 0, 0, 0].GetLength(0), input[0, 0, 0, 0].GetLength(1), input[0, 0, 0, 0].GetLength(2)];
					for (int k = 0; k < input[0, 0, 0, 0].GetLength(0); k++) {
						for (int j = 0; j < input[0, 0, 0, 0].GetLength(1); j++) {
							for (int i = 0; i < input[0, 0, 0, 0].GetLength(2); i++) {
								output[m, l][k, j, i] = input[0, 0, l, m][k, j, i];
							}
						}
					}
				}
			}
			return output;
		}
	}
}
