using System;
using System.ComponentModel.Composition;
using AietMvcScaffolding.Generator;
using Microsoft.AspNet.Scaffolding;

namespace AietMvcScaffolding.Factory
{
    [Export(typeof(LegacyCodeGeneratorFactory))]
    public class DoNothingCodeGeneratorFactory : LegacyCodeGeneratorFactory
    {
        public DoNothingCodeGeneratorFactory() :
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
            return new DoNothingCodeGenerator(context, Information);
        }
    }
}