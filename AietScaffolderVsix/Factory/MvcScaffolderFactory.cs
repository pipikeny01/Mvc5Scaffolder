using System;
using System.ComponentModel.Composition;
using Happy.Scaffolding.MVC.Scaffolders;
using Microsoft.AspNet.Scaffolding;

namespace AietMvcScaffolding.Factory
{
    [Export(contractType: typeof(LegacyCodeGeneratorFactory))]
    public class MvcScaffolderFactory : LegacyCodeGeneratorFactory
    {
        public MvcScaffolderFactory() :
            base(codeGeneratorInformation: new CodeGeneratorInformation(
                displayName: "Aiet SapTemplate 程式碼產生器",
                description: "產生 ApiController Service Repository ViewModel 程式碼範本",
                author: "Kenny",
                version: new Version(major: 1, minor: 0, build: 0),
                id: nameof(MvcScaffolder)))
        {
        }

        public override ICodeGenerator CreateInstance(CodeGenerationContext context)
        {
            //return new DoNothingCodeGenerator(context, Information);
            return new MvcScaffolder(context: context, information: Information);
        }
    }
}