using System;
using BaseLibS.Graph;
using BaseLibS.Param;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using PerseusApi.Utils;

namespace PerseusPluginLib.Export
{
    public class TabSeparatedExport : IMatrixExport
    {
        public bool HasButton => true;
		public Bitmap2 DisplayImage => Bitmap2.GetImage("Save-icon.png");

        public string Description
            => "Save the matrix to a tab-separated text file. Information on column types will be retained.";

        public string Name => "Generic matrix export";
        public bool IsActive => true;
        public float DisplayRank => 0;

        public int GetMaxThreads(Parameters parameters)
        {
            return 1;
        }

        public string Url => "http://coxdocs.org/doku.php?id=perseus:user:activities:MatrixExport:TabSeparatedExport";

        public void Export(Parameters parameters, IMatrixData data, ProcessInfo processInfo)
        {
            string filename = parameters.GetParam<string>("File name").Value;
            bool addtlMatrices = parameters.GetParam<bool>("Write quality and imputed matrices").Value;
            addtlMatrices = addtlMatrices && data.IsImputed != null && data.Quality != null && data.IsImputed.IsInitialized() &&
                            data.Quality.IsInitialized();
            try
            {
                PerseusUtils.WriteMatrixToFile(data, filename, addtlMatrices);
            }
            catch (Exception e)
            {
                processInfo.ErrString = e.Message;
            }
        }

        public Parameters GetParameters(IMatrixData matrixData, ref string errorString)
        {
            return
                new Parameters(
                    new FileParam("File name"){Filter = "Tab separated file (*.txt)|*.txt", Save = true},
                    new BoolParam("Write quality and imputed matrices", false)
                );
        }
    }
}