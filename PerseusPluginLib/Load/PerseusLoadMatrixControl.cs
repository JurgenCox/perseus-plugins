using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using BaseLib.Forms;
using BaseLibS.Param;
using BaseLibS.Parse;
using BaseLibS.Util;
using PerseusApi.Utils;

namespace PerseusPluginLib.Load{
	public partial class PerseusLoadMatrixControl : UserControl{
		public string Filter { get; set; }
		public PerseusLoadMatrixControl() : this(new string[0]){}
		public PerseusLoadMatrixControl(IList<string> items) : this(items, null){}

		public PerseusLoadMatrixControl(IList<string> items, string filename){
			InitializeComponent();
			selectButton.Click += SelectButton_OnClick;
			multiListSelectorControl1.Init(items, new[]{"Main", "Numerical", "Categorical", "Text", "Multi-numerical"},
				new Func<string[], Parameters>[]{
					s => new Parameters(PerseusUtils.GetNumFilterParams(s)), s => new Parameters(PerseusUtils.GetNumFilterParams(s)),
					null, null, null
				});
			if (!string.IsNullOrEmpty(filename)){
				UpdateFile(filename);
			}
		}

		public string Filename => textBox1.Text;
		public int[] MainColumnIndices => multiListSelectorControl1.GetSelectedIndices(0);
		public int[] NumericalColumnIndices => multiListSelectorControl1.GetSelectedIndices(1);
		public int[] CategoryColumnIndices => multiListSelectorControl1.GetSelectedIndices(2);
		public int[] TextColumnIndices => multiListSelectorControl1.GetSelectedIndices(3);
		public int[] MultiNumericalColumnIndices => multiListSelectorControl1.GetSelectedIndices(4);

		public string[] Value{
			get{
				string[] result = new string[8];
				result[0] = Filename;
				result[1] = StringUtils.Concat(";", multiListSelectorControl1.items);
				result[2] = StringUtils.Concat(";", MainColumnIndices);
				result[3] = StringUtils.Concat(";", NumericalColumnIndices);
				result[4] = StringUtils.Concat(";", CategoryColumnIndices);
				result[5] = StringUtils.Concat(";", TextColumnIndices);
				result[6] = StringUtils.Concat(";", MultiNumericalColumnIndices);
				result[7] = "" + checkBox1.Checked;
				return result;
			}
			set{
				textBox1.Text = value[0];
				multiListSelectorControl1.items = value[1].Length > 0 ? value[1].Split(';') : new string[0];
				for (int i = 0; i < 5; i++){
					foreach (int ind in GetIndices(value[i + 2])){
						multiListSelectorControl1.SetSelected(i, ind, true);
					}
				}
				if (!string.IsNullOrEmpty(value[7])){
					checkBox1.Checked = bool.Parse(value[7]);
				}
			}
		}

		public string Text1{
			get { return textBox1.Text; }
			set { textBox1.Text = value; }
		}

		private static IEnumerable<int> GetIndices(string s){
			string[] q = s.Length > 0 ? s.Split(';') : new string[0];
			int[] result = new int[q.Length];
			for (int i = 0; i < result.Length; i++){
				result[i] = int.Parse(q[i]);
			}
			return result;
		}

		internal void UpdateFile(string filename){
			textBox1.Text = filename;
			bool csv = filename.ToLower().EndsWith(".csv");
			char separator = csv ? ',' : '\t';
			string[] colNames;
			Dictionary<string, string[]> annotationRows = new Dictionary<string, string[]>();
			try{
				colNames = TabSep.GetColumnNames(filename, PerseusUtils.commentPrefix, PerseusUtils.commentPrefixExceptions,
					annotationRows, separator);
			} catch (Exception){
				MessageBox.Show("Could not open the file '" + filename +
													"'. It is probably opened by another program.");
				return;
			}
			string[] colTypes = null;
			if (annotationRows.ContainsKey("Type")){
				colTypes = annotationRows["Type"];
				annotationRows.Remove("Type");
			}
			string msg = TabSep.CanOpen(filename);
			if (msg != null){
				MessageBox.Show(msg);
				return;
			}
			multiListSelectorControl1.Init(colNames);
			if (colTypes != null){
				FormUtils.SelectExact(colNames, colTypes, multiListSelectorControl1);
			} else{
				FormUtils.SelectHeuristic(colNames, multiListSelectorControl1);
			}
		}

		private void SelectButton_OnClick(object sender, EventArgs e){
			OpenFileDialog ofd = new OpenFileDialog();
			if (Filter != null && !Filter.Equals("")){
				ofd.Filter = Filter;
			}
			DialogResult dr = ofd.ShowDialog();
			if (dr != DialogResult.OK){
				return;
			}
			string filename = ofd.FileName;
			if (string.IsNullOrEmpty(filename)){
				MessageBox.Show("Please specify a filename");
				return;
			}
			if (!File.Exists(filename)){
				MessageBox.Show("File '" + filename + "' does not exist.");
				return;
			}
			UpdateFile(filename);
			textBox1.Focus();
		}

		public IList<Parameters[]> GetSubParameterValues(){
			return multiListSelectorControl1.GetSubParameterValues();
		}
	}
}