namespace PerseusPluginLib.Basic{
	public class PerformanceColumnTypeTpFraction : PerformanceColumnType{
		public override string Name => "TP/All";

		public override double Calculate(double tp, double tn, double fp, double fn, double np, double nn){
			return tp / (np + nn);
		}
	}
}