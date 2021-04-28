using System;
using System.ComponentModel.Composition;
using AietScaffolderVsix.Generator;
using Happy.Scaffolding.MVC.Scaffolders;
using Microsoft.AspNet.Scaffolding;

namespace AietMvcScaffolding.Factory
{
    [Export(typeof(LegacyCodeGeneratorFactory))]
    public class MvcScaffolderFactory : LegacyCodeGeneratorFactory
    {
        public MvcScaffolderFactory() :
            base(new CodeGeneratorInformation(
                "DoNothing Code Generator",
                "Generator that doesn't do anything",
                "Some authors",
                new Version(1, 0, 0),
                nameof(DoNothingCodeGenerator)))
        {
        }

        public override ICodeGenerator CreateInstance(CodeGenerationContext context)
        {
            //return new DoNothingCodeGenerator(context, Information);
            return new MvcScaffolder(context, Information);
        }
    }
}