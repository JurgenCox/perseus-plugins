namespace PerseusPluginLib.Basic{
	public class PerformanceColumnTypeTnFraction : PerformanceColumnType{
		public override string Name => "TN/All";

		public override double Calculate(double tp, double tn, double fp, double fn, double np, double nn){
			return tp / (np + nn);
		}
	}
}