using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BaseLibS.Util;

namespace PerseusApi.Generic{
	public class ProcessInfo{
		private readonly int numThreads;
		public Settings Settings { get; }
		public Action<string> Status { get; }
		public Action<int> Progress { get; }
		public string ErrString { get; set; }
		public List<ThreadDistributor> threadDistributors = new List<ThreadDistributor>();
		private readonly List<Thread> registeredThreads = new List<Thread>();

		public ProcessInfo(Settings settings, Action<string> status, Action<int> progress, int numThreads){
			Settings = settings;
			Status = status;
			Progress = progress;
			this.numThreads = numThreads;
		}

		public int NumThreads => Math.Min(numThreads, Settings.Nthreads);

		public void Abort(){
			foreach (
				ThreadDistributor threadDistributor in threadDistributors.Where(threadDistributor => threadDistributor != null)){
				threadDistributor.Abort();
			}
			if (registeredThreads != null){
				foreach (Thread t in registeredThreads.Where(t => t != null)){
					t.Abort();
				}
			}
		}

		public void RegisterThread(Thread t){
			registeredThreads.Add(t);
		}

		public void ClearRegisteredThreads(){
			registeredThreads.Clear();
		}
	}
}