using System.Windows.Forms;
using Microsoft.AspNet.Scaffolding;

namespace AietMvcScaffolding.Generator
{
    public class DoNothingCodeGenerator : CodeGenerator
    {
        public DoNothingCodeGenerator(
            CodeGenerationContext context,
            CodeGeneratorInformation information)
            : base(context, information)
        {
        }

        public override void GenerateCode()
        {
            MessageBox.Show("Generate!!!");
        }

        public override bool ShowUIAndValidate()
        {
            return true;
        }
    }
}