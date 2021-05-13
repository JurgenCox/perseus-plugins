using System.IO;
using System.Text;
using BaseLib.Graphic;
using BaseLibS.Graph;
using BaseLibS.Param;
using Path = System.IO.Path;

namespace PluginDependentPeptides
{
    public class Upload : PluginInterop.Python.MatrixUpload
    {
        public override string Name => "Dependent peptides";
        public override string Description => "Create the dependent peptides table from the allPeptides.txt and rawFilesTable.txt";

        public override Bitmap2 DisplayImage => Bitmap2.GetImage("dependent_peptides_icon.png"); 

        protected override bool TryGetCodeFile(Parameters param, out string codeFile)
        {
            codeFile = Path.GetTempFileName();
            File.WriteAllText(codeFile, Encoding.UTF8.GetString(Properties.Resources.dependent_peptides));
            return true;
        }

        protected override Parameter[] SpecificParameters(ref string errString)
        {
            return new Parameter[]
            {
                new FileParam("allPeptides.txt"),
                new FileParam("Raw files table"),
            };
        }
    }
}
