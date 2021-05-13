﻿using System.Collections.Generic;
using BaseLibS.Api.Image;
using BaseLibS.Param;

namespace PerseusApi.Image{
	public static class ImageUtils{
		/// <summary>
		/// Translate from run selection (Output of MakeSelectionOptionsFunc() ) to list of names. 
		/// This can then be used as key for the Dictionary returned by GetFuncDict(IImageData mdata). 
		/// </summary>
		/// <param name="mdata">IImage object</param>
		/// <param name="selected">parameter generated by MakeSelectionOptionsFunc()</param>
		/// <returns>Names in List</returns>
		public static List<string> GetSelectedRunsFunc(IImageData mdata, SingleChoiceWithSubParams selected){
			// output
			List<string> names = new List<string>();

			// check which way of selection was chosen
			switch (selected.Value){
				// case all runs
				case 0:
					return ImageUtils.GetFuncNames(mdata);

				// case select by BIDS
				case 1:

					// all possible BIDS entities
					(List<string> allSub, List<string> allSes, List<string> allTask, List<string> allRun) =
						ImageUtils.GetFuncsBidsOptions(mdata);

					// BIDS entities selected by user
					List<string> selectedSub = new List<string>();
					List<string> selectedSes = new List<string>();
					List<string> selectedTask = new List<string>();
					List<string> selectedRun = new List<string>();

					// all indices of func runs
					(List<int> subIndsAll, List<int> sesIndsAll, List<int> funcIndsAll) =
						ImageUtils.GetAllFuncRunIndices(mdata);

					// parameters of user selection
					BoolWithSubParams sub = (BoolWithSubParams) selected.SubParams[1].FindParameter("All Subjects");
					BoolWithSubParams ses = (BoolWithSubParams) selected.SubParams[1].FindParameter("All Sessions");
					BoolWithSubParams task = (BoolWithSubParams) selected.SubParams[1].FindParameter("All Tasks");
					BoolWithSubParams run = (BoolWithSubParams) selected.SubParams[1].FindParameter("All Runs");

					// fill BIDS entities selected by user 
					FillBidsEntity(sub, selectedSub, allSub, "All Subjects");
					FillBidsEntity(ses, selectedSes, allSes, "All Sessions");
					FillBidsEntity(task, selectedTask, allTask, "All Tasks");
					FillBidsEntity(run, selectedRun, allRun, "All Runs");

					// fill output based on user selection
					for (int i = 0; i < subIndsAll.Count; i++){
						IImageSeries curFunc =
							mdata[subIndsAll[i]].GetSessionAt(sesIndsAll[i]).GetFuncAt(funcIndsAll[i]);
						if (sub != null){
							if (!selectedSub.Contains(curFunc.Metadata.GetBIDSEntity("sub"))){
								continue;
							}
						}
						if (ses != null){
							if (!selectedSes.Contains(curFunc.Metadata.GetBIDSEntity("ses"))){
								continue;
							}
						}
						if (task != null){
							if (!selectedTask.Contains(curFunc.Metadata.GetBIDSEntity("task"))){
								continue;
							}
						}
						if (run != null){
							if (!selectedRun.Contains(curFunc.Metadata.GetBIDSEntity("run"))){
								continue;
							}
						}
						names.Add(curFunc.Name);
					}
					break;

				// case select manually
				case 2:
					MultiChoiceParam sel = (MultiChoiceParam) selected.SubParams[2].FindParameter("Manual Selection");
					foreach (int i in sel.Value){
						names.Add(sel.Values[i]);
					}
					break;
			}
			return names;

			void FillBidsEntity(BoolWithSubParams param, List<string> list, List<string> all, string paramName){
				if (param != null){
					if (param.Value){
						list = all;
					} else{
						int[] inds = ((MultiChoiceParam) param.SubParamsFalse.GetParam(paramName)).Value;
						IList<string> values = ((MultiChoiceParam) param.SubParamsFalse.GetParam(paramName)).Values;
						foreach (int t in inds){
							list.Add(values[t]);
						}
					}
				}
			}

			/* DO not remove without checking first

			// all possible BIDS entities
			(List<string> allSub, List<string> allSes, List<string> allTask, List<string> allRun) = GetFuncsBIDSOptions(mdata);

			// BIDS entities selected by user
			List<string> selectedSub = new List<string>();
			List<string> selectedSes = new List<string>();
			List<string> selectedTask = new List<string>();
			List<string> selectedRun = new List<string>();

			// all indices of func runs
			(List<int> subIndsAll, List<int> sesIndsAll, List<int> funcIndsAll) = GetAllFuncRunIndices(mdata);

			// parameters of user selection
			BoolWithSubParams sub = (BoolWithSubParams)selected.SubParamsTrue.FindParameter("All Subjects");
			BoolWithSubParams ses = (BoolWithSubParams)selected.SubParamsTrue.FindParameter("All Sessions");
			BoolWithSubParams task = (BoolWithSubParams)selected.SubParamsTrue.FindParameter("All Tasks");
			BoolWithSubParams run = (BoolWithSubParams)selected.SubParamsTrue.FindParameter("All Runs");


			

			// fill BIDS entities selected by user 
			if (sub != null) {
				if (sub.Value == true) {
					selectedSub = allSub;
				}
				else {
					int[] inds = ((MultiChoiceParam)sub.SubParamsFalse.GetParam("Selected subjects")).Value;
					IList<string> values = ((MultiChoiceParam)sub.SubParamsFalse.GetParam("Selected subjects")).Values;
					for (int i = 0; i < inds.Length; i++) {
						selectedSub.Add(values[inds[i]]);
                    }
				}
            }
			if (ses != null) {
				if (ses.Value == true) {
					selectedSes = allSes;
				}
				else {
					int[] inds = ((MultiChoiceParam)ses.SubParamsFalse.GetParam("Selected session")).Value;
					IList<string> values = ((MultiChoiceParam)ses.SubParamsFalse.GetParam("Selected sessions")).Values;
					for (int i = 0; i < inds.Length; i++) {
						selectedSes.Add(values[inds[i]]);
					}
				}
			}
			if (task != null) {
				if (task.Value == true) {
					selectedTask = allTask;
				}
				else {
					int[] inds = ((MultiChoiceParam)task.SubParamsFalse.GetParam("Selected Tasks")).Value;
					IList<string> values = ((MultiChoiceParam)task.SubParamsFalse.GetParam("Selected Tasks")).Values;
					for (int i = 0; i < inds.Length; i++) {
						selectedTask.Add(values[inds[i]]);
					}
				}
			}
			if (run != null) {
				if (run.Value == true) {
					selectedRun = allRun;
				}
				else {
					int[] inds = ((MultiChoiceParam)run.SubParamsFalse.GetParam("Selected Runs")).Value;
					IList<string> values = ((MultiChoiceParam)run.SubParamsFalse.GetParam("Selected Runs")).Values;
					for (int i = 0; i < inds.Length; i++) {
						selectedRun.Add(values[inds[i]]);
					}
				}
			}

			

			// fill output based on user selection
			for (int i = 0; i < subIndsAll.Count; i++) {
				IImageSeries curFunc = mdata[subIndsAll[i]].GetSessionAt(sesIndsAll[i]).GetFuncAt(funcIndsAll[i]);
				if (sub != null) {
					if (!selectedSub.Contains(curFunc.Metadata.GetBIDSEntity("sub"))) {
						continue;
                    }
                }
				if (ses != null) {
					if (!selectedSes.Contains(curFunc.Metadata.GetBIDSEntity("ses"))) {
						continue;
					}
				}
				if (task != null) {
					if (!selectedTask.Contains(curFunc.Metadata.GetBIDSEntity("task"))) {
						continue;
					}
				}
				if (run != null) {
					if (!selectedRun.Contains(curFunc.Metadata.GetBIDSEntity("run"))) {
						continue;
					}
				}
				names.Add(i);
			}

			return names.ToArray();
			*/
		}

		/// <summary>
		/// Get the Sub-, Ses- and Run-Indices of the runs in a list (indicated by name). 
		/// </summary>
		/// <param name="mdata"></param>
		/// <param name="selectedNames"></param>
		/// <returns></returns>
		public static (List<int>, List<int>, List<int>) GetIndicesSelectedFunc(IImageData mdata,
			List<string> selectedNames){
			List<int> subInds = new List<int>();
			List<int> sesInds = new List<int>();
			List<int> runInds = new List<int>();
			Dictionary<string, int[]> dict = GetFuncDict(mdata);
			foreach (string name in selectedNames){
				if (dict.ContainsKey(name)){
					subInds.Add(dict[name][0]);
					sesInds.Add(dict[name][1]);
					runInds.Add(dict[name][2]);
				}
			}
			return (subInds, sesInds, runInds);
		}

		/// <summary>
		/// Get a list of all anat image names.
		/// </summary>
		/// <param name="mdata"></param>
		/// <returns></returns>
		public static List<string> GetAnatNames(IImageData mdata){
			List<string> allAnats = new List<string>();
			for (int subjInd = 0; subjInd < mdata.Count; subjInd++){
				for (int sesInd = 0; sesInd < mdata[subjInd].SessionCount; sesInd++){
					for (int anatInd = 0; anatInd < mdata[subjInd].GetSessionAt(sesInd).AnatCount; anatInd++){
						allAnats.Add(mdata[subjInd].GetSessionAt(sesInd).GetAnatAt(anatInd).Name);
					}
				}
			}
			return allAnats;
		}

		/// <summary>
		/// Get a list of all dwi image names.
		/// </summary>
		/// <param name="mdata"></param>
		/// <returns></returns>
		public static List<string> GetDwiNames(IImageData mdata){
			List<string> allDwis = new List<string>();
			foreach (IImageSubject t in mdata){
				for (int sesInd = 0; sesInd < t.SessionCount; sesInd++){
					for (int dwiInd = 0; dwiInd < t.GetSessionAt(sesInd).DwiCount; dwiInd++){
						allDwis.Add(t.GetSessionAt(sesInd).GetDwiAt(dwiInd).Name);
					}
				}
			}
			return allDwis;
		}

		/// <summary>
		/// Get a dictionary that translates from image name to the set of three indices Sub, Ses and Run, plus total index (=> int[4]). 
		/// </summary>
		/// <param name="mdata"></param>
		/// <returns>Dictionary<string, int[]>, where string = name and int[4] = {SubInd, SesInd, RunInd, TotalInd}</string></returns>
		public static Dictionary<string, int[]> GetAnatDict(IImageData mdata){
			Dictionary<string, int[]> allAnats = new Dictionary<string, int[]>();
			int totalInd = 0;
			for (int subjInd = 0; subjInd < mdata.Count; subjInd++){
				for (int sesInd = 0; sesInd < mdata[subjInd].SessionCount; sesInd++){
					for (int anatInd = 0; anatInd < mdata[subjInd].GetSessionAt(sesInd).AnatCount; anatInd++){
						allAnats.Add(mdata[subjInd].GetSessionAt(sesInd).GetAnatAt(anatInd).Name,
							new int[4]{subjInd, sesInd, anatInd, totalInd});
						totalInd += 1;
					}
				}
			}
			return allAnats;
		}

		/// <summary>
		/// Get a dictionary that translates from image name to the set of three indices Sub, Ses and Run, plus total index (=> int[4]). 
		/// </summary>
		/// <param name="mdata"></param>
		/// <returns>Dictionary<string, int[]>, where string = name and int[4] = {SubInd, SesInd, RunInd, TotalInd}</string></returns>
		public static Dictionary<string, int[]> GetFuncDict(IImageData mdata){
			Dictionary<string, int[]> allFuncs = new Dictionary<string, int[]>();
			int totalInd = 0;
			for (int subjInd = 0; subjInd < mdata.Count; subjInd++){
				for (int sesInd = 0; sesInd < mdata[subjInd].SessionCount; sesInd++){
					for (int funcInd = 0; funcInd < mdata[subjInd].GetSessionAt(sesInd).FuncCount; funcInd++){
						allFuncs.Add(mdata[subjInd].GetSessionAt(sesInd).GetFuncAt(funcInd).Name,
							new int[4]{subjInd, sesInd, funcInd, totalInd});
						totalInd += 1;
					}
				}
			}
			return allFuncs;
		}

		/// <summary>
		/// Get a dictionary that translates from image name to the set of three indices Sub, Ses and Run, plus total index (=> int[4]). 
		/// </summary>
		/// <param name="mdata"></param>
		/// <returns>Dictionary<string, int[]>, where string = name and int[4] = {SubInd, SesInd, RunInd, TotalInd}</string></returns>
		public static Dictionary<string, int[]> GetDwiDict(IImageData mdata){
			Dictionary<string, int[]> allDwis = new Dictionary<string, int[]>();
			int totalInd = 0;
			for (int subjInd = 0; subjInd < mdata.Count; subjInd++){
				for (int sesInd = 0; sesInd < mdata[subjInd].SessionCount; sesInd++){
					for (int dwiInd = 0; dwiInd < mdata[subjInd].GetSessionAt(sesInd).DwiCount; dwiInd++){
						allDwis.Add(mdata[subjInd].GetSessionAt(sesInd).GetDwiAt(dwiInd).Name,
							new int[4]{subjInd, sesInd, dwiInd, totalInd});
						totalInd += 1;
					}
				}
			}
			return allDwis;
		}

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