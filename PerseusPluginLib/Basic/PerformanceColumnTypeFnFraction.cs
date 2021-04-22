namespace PerseusPluginLib.Basic{
	public class PerformanceColumnTypeFnFraction : PerformanceColumnType{
		public override string Name => "FN/All";

		public override double Calculate(double tp, double tn, double fp, double fn, double np, double nn){
			return tp / (np + nn);
		}
	}
}