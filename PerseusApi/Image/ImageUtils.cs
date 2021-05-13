using System.Collections.Generic;
using BaseLibS.Api.Image;
using BaseLibS.Param;

namespace PerseusApi.Image{
	public static class ImageUtils{
		/// <summary>
		/// Make a parameter that allows the user to select which func runs should be processed. 
		/// Three options are possible: (1) all runs (2) select by BIDS filter (3) select manually
		/// </summary>
		/// <param name="mdata"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static SingleChoiceWithSubParams MakeSelectionOptionsFunc(IImageData mdata, string name = "Select Runs"){
			(List<string> subBids, List<string> sesBids, List<string> taskBids, List<string> runBids) =
				GetFuncsBidsOptions(mdata);
			List<Parameter> options = new List<Parameter>();
			if (subBids.Count != 0){
				options.Add(new BoolWithSubParams("All Subjects", true){
					SubParamsFalse = new Parameters(new MultiChoiceParam("Selected subjects"){Values = subBids})
				});
			}
			if (sesBids.Count != 0){
				options.Add(new BoolWithSubParams("All Sessions", true){
					SubParamsFalse = new Parameters(new MultiChoiceParam("Selected subjects"){Values = sesBids})
				});
			}
			if (taskBids.Count != 0){
				options.Add(new BoolWithSubParams("All Tasks", true){
					SubParamsFalse = new Parameters(new MultiChoiceParam("Selected Tasks"){Values = taskBids})
				});
			}
			if (runBids.Count != 0){
				options.Add(new BoolWithSubParams("All Runs", true){
					SubParamsFalse = new Parameters(new MultiChoiceParam("Selected Runs"){Values = runBids})
				});
			}
			MultiChoiceParam manually = new MultiChoiceParam("Manual Selection"){
				Help = "Select the runs you want to process/analyze.", Values = new List<string>(GetFuncNames(mdata)),
			};
			SingleChoiceWithSubParams selection = new SingleChoiceWithSubParams(name){
				Values = new List<string>(){"All runs", "Select by BIDS entities", "Select manually"},
				SubParams = new List<Parameters>(){new Parameters(), new Parameters(options), new Parameters(manually)},
				Help = "Select the runs you want to analyze based on their BIDS classification scheme"
			};
			return selection;
		}

		/// <summary>
		/// Searches the BIDS classification of functional runs for subjects, sessions, tasks and runs. Returns a list of possible values for each of these four entities. 
		/// </summary>
		/// <param name="mdata">The IImageData to search</param>
		/// <returns>A list for each entity filled with the possible values observed in mdata</returns>
		public static (List<string>, List<string>, List<string>, List<string>) GetFuncsBidsOptions(IImageData mdata){
			(List<int> subjInds, List<int> sesInds, List<int> funcInds) = GetAllFuncRunIndices(mdata);
			List<string> subBids = new List<string>();
			List<string> sesBids = new List<string>();
			List<string> taskBids = new List<string>();
			List<string> runBids = new List<string>();
			for (int i = 0; i < subjInds.Count; i++){
				IImageSeries curFunc = mdata[subjInds[i]].GetSessionAt(sesInds[i]).GetFuncAt(funcInds[i]);
				string curSubBids = curFunc.Metadata.GetBIDSEntity("sub");
				string curSesbBids = curFunc.Metadata.GetBIDSEntity("ses");
				string curTaskBids = curFunc.Metadata.GetBIDSEntity("task");
				string curRunBids = curFunc.Metadata.GetBIDSEntity("run");
				if (curSubBids != "unknown" && curSubBids != null && !subBids.Contains(curSubBids)){
					subBids.Add(curSubBids);
				}
				if (curSesbBids != "unknown" && curSesbBids != null && !sesBids.Contains(curSesbBids)){
					sesBids.Add(curSesbBids);
				}
				if (curTaskBids != "unknown" && curTaskBids != null && !taskBids.Contains(curTaskBids)){
					taskBids.Add(curTaskBids);
				}
				if (curRunBids != "unknown" && curRunBids != null && !runBids.Contains(curRunBids)){
					runBids.Add(curRunBids);
				}
			}
			return (subBids, sesBids, taskBids, runBids);
		}

		/// <summary>
		/// Get the Subject, Session and Run indices of all functional runs
		/// </summary>
		/// <param name="mdata"></param>
		/// <returns></returns>
		public static (List<int>, List<int>, List<int>) GetAllFuncRunIndices(IImageData mdata){
			List<int> subInds = new List<int>();
			List<int> sesInds = new List<int>();
			List<int> runInds = new List<int>();
			for (int subInd = 0; subInd < mdata.Count; subInd++){
				IImageSubject sub = mdata[subInd];
				for (int sesInd = 0; sesInd < sub.SessionCount; sesInd++){
					IImageSession ses = sub.GetSessionAt(sesInd);
					for (int funcInd = 0; funcInd < ses.FuncCount; funcInd++){
						subInds.Add(subInd);
						sesInds.Add(sesInd);
						runInds.Add(funcInd);
					}
				}
			}
			return (subInds, sesInds, runInds);
		}

		/// <summary>
		/// Get a list of all func image names.
		/// </summary>
		/// <param name="mdata"></param>
		/// <returns></returns>
		public static List<string> GetFuncNames(IImageData mdata){
			List<string> allFuncs = new List<string>();
			foreach (IImageSubject t in mdata){
				for (int sesInd = 0; sesInd < t.SessionCount; sesInd++){
					for (int funcInd = 0; funcInd < t.GetSessionAt(sesInd).FuncCount; funcInd++){
						allFuncs.Add(t.GetSessionAt(sesInd).GetFuncAt(funcInd).Name);
					}
				}
			}
			return allFuncs;
		}
	}
}