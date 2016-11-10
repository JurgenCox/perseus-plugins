using System;
using System.Windows.Forms;
using BaseLib.Graphic;
using BaseLibS.Num;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusPluginLib.Utils;

namespace PerseusPluginLib.Manual{
	public partial class SelectRowsManuallyControl : UserControl{
		private readonly IMatrixData mdata;
		private readonly Action<IData> createNewMatrix;

		public SelectRowsManuallyControl(IMatrixData mdata, Action<IData> createNewMatrix){
			InitializeComponent();
			this.mdata = mdata;
			this.createNewMatrix = createNewMatrix;
			tableView1.TableModel = new MatrixDataTable(mdata);
			removeSelectedRowsButton.Click += RemoveSelectedRowsButton_OnClick;
			keepSelectedRowsButton.Click += KeepSelectedRowsButton_OnClick;
			removeSelectedRowsButton.Image = GraphUtils.ToBitmap(PerseusPluginUtils.GetImage("hand.png"));
			keepSelectedRowsButton.Image = GraphUtils.ToBitmap(PerseusPluginUtils.GetImage("hand.png"));
		}

		private void RemoveSelectedRowsButton_OnClick(object sender, EventArgs e){
			int[] sel = tableView1.GetSelectedRows();
			if (sel.Length == 0){
				MessageBox.Show("Please select some rows.");
			}
			IMatrixData mx = (IMatrixData) mdata.Clone();
			mx.ExtractRows(ArrayUtils.Complement(sel, tableView1.RowCount));
			createNewMatrix(mx);
		}

		private void KeepSelectedRowsButton_OnClick(object sender, EventArgs e){
			int[] sel = tableView1.GetSelectedRows();
			if (sel.Length == 0){
				MessageBox.Show("Please select some rows.");
			}
			IMatrixData mx = (IMatrixData) mdata.Clone();
			mx.ExtractRows(sel);
			createNewMatrix(mx);
		}
	}
}