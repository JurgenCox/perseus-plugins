using System;
using System.Reflection;
using BaseLibS.Param;
using NUnit.Framework;
using PerseusApi.Document;
using PerseusApi.Generic;
using PerseusApi.Matrix;

namespace PerseusPluginLib.Test
{
    public static class BaseTest
    {
        public static ProcessInfo ProcessData(this IMatrixProcessing processing, IMatrixData mdata, Parameters parameters)
        {
            var pInfo = CreateProcessInfo();
            IMatrixData[] suppl = null;
            IDocumentData[] supplDocs = null;
            processing.ProcessData(mdata, parameters, ref suppl, ref supplDocs, pInfo);
            return pInfo;
        }
        
        public static ProcessInfo CreateProcessInfo()
        {
            return new ProcessInfo(new Settings(), s => { }, i => { }, 1, i => { });
        }
    }
}