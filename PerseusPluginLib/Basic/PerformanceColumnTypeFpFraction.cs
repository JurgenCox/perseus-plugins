namespace PerseusPluginLib.Basic{
	public class PerformanceColumnTypeFpFraction : PerformanceColumnType{
		public override string Name => "FP/All";

		public override double Calculate(double tp, double tn, double fp, double fn, double np, double nn){
			return tp / (np + nn);
		}
	}
}