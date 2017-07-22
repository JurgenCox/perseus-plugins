using System.Collections.Generic;
using BaseLibS.Num;

namespace PerseusPluginLib.AnnotCols{
	public class CountingResult{
		private IList<string> allCategoryType1 = new List<string>();
		private IList<string> allCategory1 = new List<string>();
		private IList<int> allTotalCount = new List<int>();
		private IList<int> selectCount = new List<int>();
		public int Count => allCategoryType1.Count;

		public void Sort(){
			int[] o = ArrayUtils.Order(allTotalCount);
			ArrayUtils.Revert(o);
			allCategoryType1 = ArrayUtils.SubArray(allCategoryType1, o);
			allCategory1 = ArrayUtils.SubArray(allCategory1, o);
			allTotalCount = ArrayUtils.SubArray(allTotalCount, o);
			selectCount = ArrayUtils.SubArray(selectCount, o);
		}

		public string GetType1At(int i){
			return allCategoryType1[i];
		}

		public string GetName1At(int i){
			return allCategory1[i];
		}

		public int GetTotalCountAt(int i){
			return allTotalCount[i];
		}

		public int GetSelectCountAt(int i){
			return selectCount[i];
		}

		public void Add(string categoryName1, string[] allTerms, int[] allTotal, int[] selectTotal){
			for (int i = 0; i < allTerms.Length; i++){
				string[] q = allTerms[i].Split(';');
				allCategoryType1.Add(categoryName1);
				allCategory1.Add(q[0]);
				allTotalCount.Add(allTotal[i]);
				selectCount.Add(selectTotal[i]);
			}
		}
	}
}