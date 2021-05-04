using System;
using System.IO;
using EnvDTE;
using Happy.Scaffolding.MVC.UI;
using Microsoft.AspNet.Scaffolding;

namespace Happy.Scaffolding.MVC.Utils
{
    internal class PathHelper
    {
        internal static string ServiceOutPath(MvcCodeGeneratorViewModel codeGeneratorViewModel)
        {
            return Path.Combine(
                "AppCode",
                "Project",
                "Service",
                codeGeneratorViewModel.ModelType.ShortName + "Service");
        }

        internal static string RepositoryOutPath(MvcCodeGeneratorViewModel codeGeneratorViewModel)
        {
            return Path.Combine(
                "Project",
                "Repository",
                codeGeneratorViewModel.ModelType.ShortName + "Repos");
        }

        public static string ListViewModelOutPath(MvcCodeGeneratorViewModel codeGeneratorViewModel)
        {
            return Path.Combine(
                "AppCode",
                "Project",
                "ViewModel",
                codeGeneratorViewModel.ModelType.ShortName + "ListViewModel");
        }
        public static string EditViewModelOutPath(MvcCodeGeneratorViewModel codeGeneratorViewModel)
        {
            return Path.Combine(
                "AppCode",
                "Project",
                "ViewModel",
                codeGeneratorViewModel.ModelType.ShortName + "EditViewModel");
        }

        public static string GetProjectItemFullPath(Project project, string outputPath, string extension)
        {
            return Path.Combine(project.GetFullPath(), outputPath) + extension;
        }

    }
}