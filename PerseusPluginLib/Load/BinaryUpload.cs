using System.Collections.Generic;
using System.IO;
using System.Text;
using BaseLibS.Graph;
using BaseLibS.Num;
using BaseLibS.Param;
using BaseLibS.Util;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Load{
	public class BinaryUpload : IMatrixUpload{
		private const string hexAlphabet = "0123456789ABCDEF";
		public bool HasButton => true;
		public Bitmap2 DisplayImage => PerseusPluginUtils.GetImage("binary.png");
		public string Name => "Binary upload";
		public bool IsActive => true;
		public float DisplayRank => 12;
		public string Description => "Load all bytes from a binary file and display them as hexadecimal numbers.";
		public string[] HelpSupplTables => new string[0];
		public int NumSupplTables => 0;
		public string[] HelpDocuments => new string[0];
		public int NumDocuments => 0;
		public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixUpload:BinaryUpload";
		public int GetMaxThreads(Parameters parameters) { return 1; }

		public Parameters GetParameters(ref string errString){
			return
				new Parameters(new Parameter[]{
					new FileParam("File"){
						Filter = "All files (*.*)|*.*",
						Help = "Please specify here the name of the file to be uploaded including its full path."
					}
				});
		}

		public void LoadData(IMatrixData mdata, Parameters parameters, ref IMatrixData[] supplTables,
			ref IDocumentData[] documents, ProcessInfo processInfo){
				string filename = parameters.GetParam<string>("File").Value;
			BinaryReader reader = FileUtils.GetBinaryReader(filename);
			byte[] x = reader.ReadBytes((int) reader.BaseStream.Length);
			reader.Close();
			const int nb = 16;
			List<string> hexLines = new List<string>();
			List<string> charLines = new List<string>();
			for (int i = 0; i < x.Length/nb; i++){
				byte[] y = ArrayUtils.SubArray(x, i*nb, (i + 1)*(nb));
				hexLines.Add(ToHex(y));
				charLines.Add(ToChar(y));
			}
			if (x.Length/nb > 0){
				byte[] y = ArrayUtils.SubArray(x, (x.Length/nb)*nb, x.Length);
				hexLines.Add(ToHex(y));
				charLines.Add(ToChar(y));
			}
			mdata.Values.Init(hexLines.Count,0);
			mdata.SetAnnotationColumns(new List<string>(new[]{"Hex", "Char"}), new List<string>(new[]{"Hex", "Char"}),
				new List<string[]>(new[]{hexLines.ToArray(), charLines.ToArray()}), new List<string>(), new List<string>(),
				new List<string[][]>(), new List<string>(), new List<string>(), new List<double[]>(), new List<string>(),
				new List<string>(), new List<double[][]>());
		}

		private static string ToHex(byte b) { return "" + hexAlphabet[b >> 4] + hexAlphabet[b & 0xF]; }

		private static readonly HashSet<byte> replace =
			new HashSet<byte>(new byte[]{
				0x7F, 0x80, 0x81, 0x84, 0x85, 0x88, 0x8C, 0x8D, 0x8E, 0x8F, 0x90, 0x91, 0x92, 0x95, 0x99, 0x9A, 0x9B, 0x9D, 0x9E,
				0xAD
			});

		private static string ToChar(byte b) { return b <= 0x1F || replace.Contains(b) ? "." : "" + (char) b; }

		private static string ToChar(IList<byte> b){
			if (b.Count == 0){
				return "";
			}
			StringBuilder sb = new StringBuilder(ToChar(b[0]));
			for (int i = 1; i < b.Count; i++){
				sb.Append(ToChar(b[i]));
			}
			return sb.ToString();
		}

		private static string ToHex(IList<byte> b){
			if (b.Count == 0){
				return "";
			}
			StringBuilder sb = new StringBuilder(ToHex(b[0]));
			for (int i = 1; i < b.Count; i++){
				sb.Append(" ");
				sb.Append(ToHex(b[i]));
			}
			return sb.ToString();
		}
	}
}