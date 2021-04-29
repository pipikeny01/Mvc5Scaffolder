using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Happy.Scaffolding.MVC.Utils
{
    internal class VisualStudioUtils
    {
        private DTE2 _dte;

        internal VisualStudioUtils()
        {
            // initialize DTE object -- the top level object for working with Visual Studio
            this._dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
        }

        internal void BuildProject(Project project)
        {
            var solutionConfiguration = _dte.Solution.SolutionBuild.ActiveConfiguration.Name;
            if (project == null)
            {
                throw new NullReferenceException("project");
            }
            _dte.Solution.SolutionBuild.BuildProject(solutionConfiguration, project.FullName, true);
        }

        internal Project FindProjectByName(string name)
        {
            foreach (Project project in _dte.Solution.Projects)
            {
                if (project.Name == name)
                    return project;
            }

            MessageBox.Show($"找不到專案:{name}");

            return null;
        }

        public void Build()
        {
            _dte.Solution.SolutionBuild.Build(true);
        }

        public  ProjectItem FindSolutionItemByName(DTE dte, string name, bool recursive)
        {
            ProjectItem projectItem = null;
            foreach (Project project in dte.Solution.Projects)
            {
                projectItem = FindProjectItemInProject(project, name, recursive);

                if (projectItem != null)
                {
                    break;
                }
            }
            return projectItem;
        }

        public static ProjectItem FindProjectItemInProject(Project project, string name, bool recursive)
        {
            ProjectItem projectItem = null;

            if (project.Kind != "")
            {
                if (project.ProjectItems != null && project.ProjectItems.Count > 0)
                {
                    projectItem = DteHelper.FindItemByName(project.ProjectItems, name, recursive);
                }
            }
            else
            {
                // if solution folder, one of its ProjectItems might be a real project
                foreach (ProjectItem item in project.ProjectItems)
                {
                    Project realProject = item.Object as Project;

                    if (realProject != null)
                    {
                        projectItem = FindProjectItemInProject(realProject, name, recursive);

                        if (projectItem != null)
                        {
                            break;
                        }
                    }
                }
            }

            return projectItem;
        }

        public void MoveFile(Project project, string outputFolderPath)
        {
            var dalProject = FindProjectByName("DAL");

            foreach (ProjectItem dalProjectProjectItem in dalProject.ProjectItems)
            {
                Trace.WriteLine(dalProjectProjectItem.Name);
            }

            dalProject.ProjectItems.AddFromTemplate(
                "D:\\Users\\pigi0\\Source\\Repos\\SPATemplate\\Solustion\\Web\\Class1.cs","ccc");
        }
    }
}