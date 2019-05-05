using System.Collections.Generic;

namespace KymeraPluginProteomicRuler{
	class ProteomicRulerUtils{
		/// <summary>
		/// Finds strings in an array of strings.
		/// </summary>
		/// <param name="haystack">An array of strings that are to be searched.</param>
		/// <param name="needles">An array of string that are to be searched for individually.</param>
		/// <param name="caseSensitive"></param>
		/// <param name="matchSubstring"></param>
		/// <param name="matchFirstIfNothingFound">If nothing matched, the first element (index 0) will be returned to avoid returning null.</param>
		/// <returns>An array of the indices of the matched elements of haystack.</returns>
		public static int[] Match(string[] haystack, string[] needles, bool caseSensitive, bool matchSubstring,
			bool matchFirstIfNothingFound){
			List<int> matches = new List<int>();
			for (int i = 0; i < haystack.Length; i++){
				string hay = caseSensitive ? haystack[i] : haystack[i].ToLower();
				hay = hay.Trim();
				foreach (string hit in needles){
					string needle = caseSensitive ? hit : hit.ToLower();
					needle = needle.Trim();
					if (hay.Equals(needle) || matchSubstring && hay.Contains(needle)){
						matches.Add(i);
					}
				}
			}
			if (matches.Count == 0 && matchFirstIfNothingFound){
				matches.Add(0);
			}
			return matches.ToArray();
		}
	}
}