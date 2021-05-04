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

        public void Build()
        {
            _dte.Solution.SolutionBuild.Build(true);
        }

        /// <summary>
        /// https://stackoverflow.com/questions/19427333/how-to-find-a-projectitem-by-the-file-name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ProjectItem FindProjectByName(string name)
        {
            ProjectItem projectItem = null;

            foreach (Project project in _dte.Solution.Projects)
            {
                if (project.Name == name)
                {
                    projectItem = FindProjectItemInProject(project, name);

                    if (projectItem != null)
                    {
                        break;
                    }
                }
            }
            return projectItem;
        }

        public  ProjectItem FindProjectItemInProject(Project project, string name)
        {
            foreach (ProjectItem item in project.ProjectItems)
            {
                Project realProject = item.Object as Project;

                if (realProject != null && realProject.Name == name)
                {
                    return item;

                }
            }

            return null;
        }

        public void MoveFile(Project project, string outputFolderPath)
        {
            //var findSolutionItemByName = FindSolutionItemByName(_dte, "DAL",true);

            //var dalProject = FindProjectByName("DAL");

            //foreach (ProjectItem dalProjectProjectItem in dalProject.ProjectItems)
            //{
            //    Trace.WriteLine(dalProjectProjectItem.Name);
            //}

            //dalProject.ProjectItems.AddFromTemplate(
            //    "D:\\Users\\pigi0\\Source\\Repos\\SPATemplate\\Solustion\\Web\\Class1.cs","ccc");
        }

        public void Open(string fullPath)
        {
            _dte.ItemOperations.OpenFile(fullPath, Constants.vsViewKindTextView);

        }
        
    }
}