using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.Versioning;
using System.Windows.Forms;
using AietScaffolderVsix;
using Happy.Scaffolding.MVC.Scaffolders;
using Happy.Scaffolding.MVC.Utils;
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
                version: new Version(major: 1, minor: 0, build: 1),
                id: nameof(MvcScaffolder)))
        {
        }

        public override ICodeGenerator CreateInstance(CodeGenerationContext context)
        {
            //Alert.Trace(GetExtensionInstallationDirectoryOrNull());
            //return new DoNothingCodeGenerator(context, Information);
            return new MvcScaffolder(context: context, information: Information);
        }

        // We support CSharp WAPs targetting at least .Net Framework 4.5 or above.
        // We DON'T currently support VB
        public override bool IsSupported(CodeGenerationContext codeGenerationContext)
        {
            if (ProjectLanguage.CSharp.Equals(codeGenerationContext.ActiveProject.GetCodeLanguage()))
            {
                FrameworkName targetFramework = codeGenerationContext.ActiveProject.GetTargetFramework();
                return (targetFramework != null) &&
                       String.Equals(".NetFramework", targetFramework.Identifier, StringComparison.OrdinalIgnoreCase) &&
                       targetFramework.Version >= new Version(4, 5);
            }

            return false;
        }

        public static string GetExtensionInstallationDirectoryOrNull()
        {
            try
            {
                var uri = new Uri(typeof(AietScaffolderVsixPackage).Assembly.CodeBase, UriKind.Absolute);
                return Path.GetDirectoryName(uri.LocalPath);
            }
            catch
            {
                return null;
            }
        }
    }
}