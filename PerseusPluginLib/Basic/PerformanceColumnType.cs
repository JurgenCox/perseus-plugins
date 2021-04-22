namespace PerseusPluginLib.Basic{
	public abstract class PerformanceColumnType{
		public static PerformanceColumnType precision = new PerformanceColumnTypePrecision();
		public static PerformanceColumnType recall = new PerformanceColumnTypeRecall();

		public static PerformanceColumnType[] allTypes = {
			precision, recall, new PerformanceColumnTypeFpTp(), new PerformanceColumnTypeTpNp(),
			new RocColumnTypeSensitivity(), new PerformanceColumnTypeSpecificity(),
			new PerformanceColumnTypeTpFraction(), new PerformanceColumnTypeTnFraction(),
			new PerformanceColumnTypeFpFraction(), new PerformanceColumnTypeFnFraction()
		};

		public static string[] AllTypeNames{
			get{
				string[] names = new string[allTypes.Length];
				for (int i = 0; i < names.Length; i++){
					names[i] = allTypes[i].Name;
				}
				return names;
			}
		}

		public abstract string Name{ get; }
		public abstract double Calculate(double tp, double tn, double fp, double fn, double np, double nn);
	}
}