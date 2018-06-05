using BaseLibS.Graph;
using BaseLibS.Num.Matrix;
using BaseLibS.Param;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;
using System;
using System.IO;

namespace PerseusPluginLib.DESeq2
{
    public class AdjustCounts : IMatrixProcessing
    {
        private MatrixIndexer a;

        public bool HasButton => false;
        public Bitmap2 DisplayImage => null;
        public string Description => "The negative and float values are not allow for using DESeq2. " +
            "This function can adjust all the values become positive integers.";
        public string HelpOutput => "Adjuct all count number to be positive integers without negative and float values";
        public string[] HelpSupplTables => new string[0];
        public int NumSupplTables => 0;
        public string Name => "Adjust values";
        public string Heading => "DE analysis";
        public bool IsActive => true;
        public float DisplayRank => 100;
        public string[] HelpDocuments => new string[0];
        public int NumDocuments => 0;

        public int GetMaxThreads(Parameters parameters)
        {
            return 1;
        }

        public string Url
            => "https://bioconductor.org/packages/release/bioc/html/DESeq2.html";

        public void ProcessData(IMatrixData mdata, Parameters param, ref IMatrixData[] supplTables,
            ref IDocumentData[] documents, ProcessInfo processInfo)
        {
            bool decimalValues = param.GetParam<bool>("Rounding decimal values").Value;
            bool negativeValue = param.GetParam<bool>("Subtract Min. negative value").Value;
            string constantValue = param.GetParam<string>("Add a constant value").Value;
            double minNegativeNum = 0;
            for (int i = 0; i < mdata.Values.RowCount; i++)
            {
                for (int j = 0; j < mdata.Values.ColumnCount; j++)
                {
                    if (minNegativeNum > mdata.Values.Get(i, j))
                    {
                        minNegativeNum = mdata.Values.Get(i, j);
                    }
                }
            }
            for (int i = 0; i < mdata.Values.ColumnCount; i++)
            {
                for (int j = 0; j < mdata.Values.RowCount; j++)
                {
                    double newCount = mdata.Values.Get(j, i);
                    if (negativeValue)
                        newCount = mdata.Values.Get(j, i) - minNegativeNum;
                    //                    double newCount = mdata.Values.Get(j, i) - minNegativeNum + raiseConstant;
                    if (decimalValues)
                        newCount = (int)Math.Round(newCount, 0, MidpointRounding.AwayFromZero);
                    if (Convert.ToDouble(constantValue) != 0)
                        newCount = newCount + Convert.ToDouble(constantValue);
                    mdata.Values.Set(j, i, newCount);
                }
            }
        }

        public Parameters GetParameters(IMatrixData mdata, ref string errorString)
        {
            return new Parameters(new Parameter[] {
                new BoolParam("Rounding decimal values"){Help="Round all decimal values to integers."},
                new BoolParam("Subtract Min. negative value"){Help="Make all values in the table positive."},
                new StringParam("Add a constant value", "0"){Help="Add a constant values to all counts/hits in the table. " +
                "It can deal with the issue that every gene contains at least one zero count/hit."} });
            //return new Parameters(new IntParam("Number of top rows", 15));
        }
    }
}