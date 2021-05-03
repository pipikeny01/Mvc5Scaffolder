using EnvDTE;
using Happy.Scaffolding.MVC.Models;
using Happy.Scaffolding.MVC.UI;
using Happy.Scaffolding.MVC.Utils;
using Microsoft.AspNet.Scaffolding;
using Microsoft.AspNet.Scaffolding.Core.Metadata;
using Microsoft.AspNet.Scaffolding.EntityFramework;
using Microsoft.AspNet.Scaffolding.NuGet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace Happy.Scaffolding.MVC.Scaffolders
{
    // This class performs all of the work of scaffolding. The methods are executed in the
    // following order:
    // 1) ShowUIAndValidate() - displays the Visual Studio dialog for setting scaffolding options
    // 2) Validate() - validates the model collected from the dialog
    // 3) GenerateCode() - if all goes well, generates the scaffolding output from the templates

    public class MvcScaffolder : CodeGenerator
    {
        private MvcCodeGeneratorViewModel _codeGeneratorViewModel;
        private ModelMetadataViewModel _ModelMetadataVM;
        private VisualStudioUtils _visualStudioUtils;

        public MvcScaffolder(CodeGenerationContext context, CodeGeneratorInformation information)
            : base(context: context, information: information)
        {
            _visualStudioUtils = new VisualStudioUtils();
        }

        //public override IEnumerable<string> TemplateFolders
        //{
        //    get
        //    {
        //        var baseTemplatePath = Path.Combine(Path.GetDirectoryName(path: GetType().Assembly.Location),
        //            "baseTemplatePath");
        //        var projectTemplatePath =
        //            Path.Combine(path1: Context.ActiveProject.GetFullPath(), "projectTemplatePath");
        //        return new[] { projectTemplatePath, baseTemplatePath };
        //    }
        //}

        // Shows the Visual Studio dialog that collects scaffolding options
        // from the user.
        // Passing the dialog to this method so that all scaffolder UIs
        // are modal is still an open question and tracked by bug 578173.
        public override bool ShowUIAndValidate()
        {
            ////TODO: 不知道怎麼Move到其他專案
            //var outputFullPath = Path.Combine(Context.ActiveProjectItem.GetFullPath(), "");
            //_visualStudioUtils.MoveFile(Context.ActiveProject, outputFullPath + ".cs");

            _codeGeneratorViewModel = new MvcCodeGeneratorViewModel(context: Context);

            MvcScaffolderDialog window = new MvcScaffolderDialog(viewModel: _codeGeneratorViewModel);
            bool? isOk = window.ShowModal();

            if (isOk == true)
            {
                Validate();

                if (_codeGeneratorViewModel.GenerateViews)
                {
                    isOk = ShowColumnSetting();
                }
            }
            return (isOk == true);
        }

        // Setting Columns : display name, allow null
        private bool? ShowColumnSetting()
        {
            var modelType = _codeGeneratorViewModel.ModelType.CodeType;
            string savefolderPath = Path.Combine(path1: Context.ActiveProject.GetFullPath(), path2: "CodeGen");
            StorageMan<MetaTableInfo> sm = new StorageMan<MetaTableInfo>(modelTypeName: modelType.Name, savefolderPath: savefolderPath);
            MetaTableInfo data = sm.Read();
            if (data.Columns.Any())
            {
                _ModelMetadataVM = new ModelMetadataViewModel(dataModel: data);
            }
            else
            {
                string dbContextTypeName = _codeGeneratorViewModel.DbContextModelType.TypeName;
                ICodeTypeService codeTypeService = GetService<ICodeTypeService>();
                CodeType dbContext = codeTypeService.GetCodeType(project: Context.ActiveProject, fullName: dbContextTypeName);
                IEntityFrameworkService efService = Context.ServiceProvider.GetService<IEntityFrameworkService>();
                ModelMetadata efMetadata = efService.AddRequiredEntity(context: Context, contextTypeFullName: dbContextTypeName, entityTypeFullName: modelType.FullName);
                _ModelMetadataVM = new ModelMetadataViewModel(efMetadata: efMetadata);
            }

            ModelMetadataDialog dialog = new ModelMetadataDialog(viewModel: _ModelMetadataVM);
            bool? isOk = dialog.ShowModal();
            if (isOk == true)
            {
                sm.Save(data: _ModelMetadataVM.DataModel);
            }

            return isOk;
        }

        // Validates the model returned by the Visual Studio dialog.
        // We always force a Visual Studio build so we have a model
        // that we can use with the Entity Framework.
        private void Validate()
        {
            CodeType modelType = _codeGeneratorViewModel.ModelType.CodeType;
            ModelType dbContextType = _codeGeneratorViewModel.DbContextModelType;
            string dbContextTypeName = (dbContextType != null)
                ? dbContextType.TypeName
                : null;

            if (modelType == null)
            {
                throw new InvalidOperationException(message: Resources.WebFormsScaffolder_SelectModelType);
            }

            if (dbContextType == null || String.IsNullOrEmpty(value: dbContextTypeName))
            {
                throw new InvalidOperationException(message: Resources.WebFormsScaffolder_SelectDbContextType);
            }

            // always force the project to build so we have a compiled
            // model that we can use with the Entity Framework
            //visualStudioUtils.BuildProject(Context.ActiveProject);
            _visualStudioUtils.Build();

            Type reflectedModelType = GetReflectionType(typeName: modelType.FullName);
            if (reflectedModelType == null)
            {
                throw new InvalidOperationException(message: Resources.WebFormsScaffolder_ProjectNotBuilt);
            }
        }

        // Top-level method that generates all of the scaffolding output from the templates.
        // Shows a busy wait mouse cursor while working.
        public override void GenerateCode()
        {
            var project = Context.ActiveProject;
            var selectionRelativePath = GetSelectionRelativePath();
            var modelFolderPath = GetModelFolderPath(selectionRelativePath: selectionRelativePath);

            if (_codeGeneratorViewModel == null)
            {
                throw new InvalidOperationException(message: Resources.WebFormsScaffolder_ShowUIAndValidateNotCalled);
            }

            Cursor currentCursor = Mouse.OverrideCursor;
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                GenerateCode(project: project, selectionRelativePath: selectionRelativePath, codeGeneratorViewModel: this._codeGeneratorViewModel);
            }
            finally
            {
                Mouse.OverrideCursor = currentCursor;
            }
        }

        // Collects the common data needed by all of the scaffolded output and generates:
        // 1) Add Controller
        // 2) Add View
        private void GenerateCode(Project project, string selectionRelativePath,
            MvcCodeGeneratorViewModel codeGeneratorViewModel)
        {
            // Get Model Type
            var modelType = codeGeneratorViewModel.ModelType.CodeType;

            // Get the dbContext
            string dbContextTypeName = codeGeneratorViewModel.DbContextModelType.TypeName;
            ICodeTypeService codeTypeService = GetService<ICodeTypeService>();
            CodeType dbContext = codeTypeService.GetCodeType(project: project, fullName: dbContextTypeName);

            // Get the Entity Framework Meta Data
            IEntityFrameworkService efService = Context.ServiceProvider.GetService<IEntityFrameworkService>();
            ModelMetadata efMetadata = efService.AddRequiredEntity(context: Context,
                contextTypeFullName: dbContextTypeName, entityTypeFullName: modelType.FullName);

            // Create Controller
            string controllerName = codeGeneratorViewModel.ControllerName;
            string controllerRootName = controllerName.Replace(oldValue: "Controller", newValue: "");
            string outputFolderPath = Path.Combine(path1: selectionRelativePath, path2: controllerName);
            string viewPrefix = codeGeneratorViewModel.ViewPrefix;
            string programTitle = codeGeneratorViewModel.ProgramTitle;


            if (codeGeneratorViewModel.GenerateApiController)
            {
                AddMvcController(project: project
                    , controllerName: controllerName
                    , controllerRootName: controllerRootName
                    , outputPath: outputFolderPath
                    , ContextTypeName: dbContext.Name
                    , modelType: modelType
                    , efMetadata: efMetadata
                    , viewPrefix: viewPrefix
                    , overwrite: codeGeneratorViewModel.OverwriteViews);
            }

            if (codeGeneratorViewModel.GenerateService)
            {
                AddService(project: project
                    , controllerName: controllerName
                    , controllerRootName: controllerRootName
                    , outputPath: PathHelper.ServiceOutPath(codeGeneratorViewModel: codeGeneratorViewModel)
                    , ContextTypeName: dbContext.Name
                    , modelType: modelType
                    , efMetadata: efMetadata
                    , viewPrefix: viewPrefix
                    , overwrite: codeGeneratorViewModel.OverwriteViews);
            }

            if (codeGeneratorViewModel.GenerateRepository)
            {
                project = _visualStudioUtils.FindProjectByName("DAL").Object;
                AddRepository(project: project
                    , controllerName: controllerName
                    , controllerRootName: controllerRootName
                    , outputPath: PathHelper.RepositoryOutPath(codeGeneratorViewModel: codeGeneratorViewModel)
                    , ContextTypeName: dbContext.Name
                    , modelType: modelType
                    , efMetadata: efMetadata
                    , viewPrefix: viewPrefix
                    , overwrite: codeGeneratorViewModel.OverwriteViews);
            }

            // add Metadata for Model
            if (codeGeneratorViewModel.GenerateViews)
            {
                AddListViewModel(project: Context.ActiveProject
                    , controllerName: controllerName
                    , controllerRootName: controllerRootName
                    , outputPath: PathHelper.ListViewModelOutPath(codeGeneratorViewModel: codeGeneratorViewModel)
                    , ContextTypeName: dbContext.Name
                    , modelType: modelType
                    , efMetadata: efMetadata
                    , overwrite: codeGeneratorViewModel.OverwriteViews);

                AddEditViewModel(project: Context.ActiveProject
                    , controllerName: controllerName
                    , controllerRootName: controllerRootName
                    , outputPath: PathHelper.EditViewModelOutPath(codeGeneratorViewModel: codeGeneratorViewModel)
                    , ContextTypeName: dbContext.Name
                    , modelType: modelType
                    , efMetadata: efMetadata
                    , overwrite: codeGeneratorViewModel.OverwriteViews);
            }

            ////_ViewStart & Create _Layout
            //string viewRootPath = GetViewsFolderPath(selectionRelativePath);
            //if (codeGeneratorViewModel.LayoutPageSelected)
            //{
            //    string areaName = GetAreaName(selectionRelativePath);
            //    AddDependencyFile(project, viewRootPath, areaName);
            //}
            //// EditorTemplates, DisplayTemplates
            //AddDataFieldTemplates(project, viewRootPath);

            //// Views for  C.R.U.D
            //string viewFolderPath = Path.Combine(viewRootPath, controllerRootName);
            //foreach (string viewName in new string[4] { "Index", "Create", "Edit", "EditForm" })
            //{
            //    //string viewName = string.Format(view, viewPrefix);
            //    //未完成
            //    /*
            //     Index        CustIndex
            //     Create       CustCreate
            //     Edit           CustEdit
            //     EditForm    CustEditForm
            //     *
            //     _Edit      _CustEdit
            //     */

            //    AddView(project
            //        , viewFolderPath, viewPrefix, viewName, programTitle
            //        , controllerRootName, modelType, efMetadata
            //        , referenceScriptLibraries: codeGeneratorViewModel.ReferenceScriptLibraries
            //        , isLayoutPageSelected: codeGeneratorViewModel.LayoutPageSelected
            //        , layoutPageFile: codeGeneratorViewModel.LayoutPageFile
            //        , overwrite: codeGeneratorViewModel.OverwriteViews
            //        );
            //}
        }

        //add MVC Controller
        private void AddMvcController(Project project
            , string controllerName
            , string controllerRootName
            , string outputPath
            , string ContextTypeName /*"Entities"*/
            , CodeType modelType
            , ModelMetadata efMetadata
            , string viewPrefix
            , bool overwrite = false
            )
        {
            AddControllerHandler(project: project,
                controllerName: controllerName,
                controllerRootName: controllerRootName,
                outputPath: outputPath,
                ContextTypeName: ContextTypeName,
                modelType: modelType,
                efMetadata: efMetadata,
                viewPrefix: viewPrefix,
                overwrite: overwrite, t4Name: "Controller");
        }

        private void AddService(Project project,
            string controllerName,
            string controllerRootName,
            string outputPath,
            string ContextTypeName /*"Entities"*/,
            CodeType modelType,
            ModelMetadata efMetadata,
            string viewPrefix,
            bool overwrite = false
    )
        {
            AddControllerHandler(project: project,
                controllerName: controllerName,
                controllerRootName: controllerRootName,
                outputPath: outputPath,
                ContextTypeName: ContextTypeName,
                modelType: modelType,
                efMetadata: efMetadata,
                viewPrefix: viewPrefix,
                overwrite: overwrite, t4Name: "Service");
        }

        private void AddRepository(Project project,
            string controllerName,
            string controllerRootName,
            string outputPath,
            string ContextTypeName /*"Entities"*/,
            CodeType modelType,
            ModelMetadata efMetadata,
            string viewPrefix,
            bool overwrite = false
    )
        {
            AddControllerHandler(project: project,
                controllerName: controllerName,
                controllerRootName: controllerRootName,
                outputPath: outputPath,
                ContextTypeName: ContextTypeName,
                modelType: modelType,
                efMetadata: efMetadata,
                viewPrefix: viewPrefix,
                overwrite: overwrite, t4Name: "Repository");
        }

        private void AddControllerHandler(Project project, string controllerName, string controllerRootName, string outputPath,
            string ContextTypeName, CodeType modelType, ModelMetadata efMetadata, string viewPrefix, bool overwrite, string t4Name)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(paramName: "modelType");
            }

            if (String.IsNullOrEmpty(value: controllerName))
            {
                //TODO
                throw new ArgumentException(message: Resources.WebFormsViewScaffolder_EmptyActionName, paramName: "webFormsName");
            }

            PropertyMetadata primaryKey = efMetadata.PrimaryKeys.FirstOrDefault();
            string pluralizedName = efMetadata.EntitySetName;
            string modelNameSpace = modelType.Namespace != null ? modelType.Namespace.FullName : String.Empty;
            string relativePath = outputPath.Replace(oldValue: @"\", newValue: @"/");

            //Project project = Context.ActiveProject;
            var templatePath = Path.Combine(path1: "ApiControllerWithContext", path2: t4Name);
            var defaultNamespace = GetDefaultNamespace();
            string modelTypeVariable = GetTypeVariable(typeName: modelType.Name);
            string bindAttributeIncludeText = GetBindAttributeIncludeText(efMetadata: efMetadata);

            Dictionary<string, object> templateParams = new Dictionary<string, object>()
            {
                {"ControllerName", controllerName}, {"ControllerRootName", controllerRootName}, {"Namespace", defaultNamespace},
                {"AreaName", string.Empty}, {"ContextTypeName", ContextTypeName}, {"ModelTypeName", modelType.Name},
                {"ModelVariable", modelTypeVariable}, {"ModelMetadata", efMetadata}, {"EntitySetVariable", modelTypeVariable},
                {"UseAsync", false}, {"IsOverpostingProtectionRequired", true},
                {"BindAttributeIncludeText", bindAttributeIncludeText},
                {
                    "OverpostingWarningMessage",
                    "To protect from overposting attacks, please enable the specific properties you want to bind to, for more details see http://go.microsoft.com/fwlink/?LinkId=317598."
                },
                {"RequiredNamespaces", new HashSet<string>() {modelType.Namespace.FullName}}, {"ViewPrefix", viewPrefix}
            };

            AddFileFromTemplate(project: project
                , outputPath: outputPath
                , templateName: templatePath
                , templateParameters: templateParams
                , skipIfExists: !overwrite);
        }

        private void AddListViewModel(Project project,
            string controllerName,
            string controllerRootName,
            string outputPath,
            string ContextTypeName /*"Entities"*/,
            CodeType modelType,
            ModelMetadata efMetadata,
            bool overwrite = false)
        {
            AddViewModelHendler(project: project,
                controllerName: controllerName,
                outputPath: outputPath,
                modelType: modelType,
                efMetadata: efMetadata,
                overwrite: overwrite, t4Name: "ListViewModel");
        }

        private void AddEditViewModel(Project project,
            string controllerName,
            string controllerRootName,
            string outputPath,
            string ContextTypeName /*"Entities"*/,
            CodeType modelType,
            ModelMetadata efMetadata,
            bool overwrite = false)
        {
            AddViewModelHendler(project: project,
                controllerName: controllerName,
                outputPath: outputPath,
                modelType: modelType,
                efMetadata: efMetadata,
                overwrite: overwrite, t4Name: "EditViewModel");
        }

        private void AddViewModelHendler(Project project, string controllerName, string outputPath, CodeType modelType,
            ModelMetadata efMetadata, bool overwrite, string t4Name)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(paramName: "modelType");
            }

            if (String.IsNullOrEmpty(value: controllerName))
            {
                //TODO
                throw new ArgumentException(message: Resources.WebFormsViewScaffolder_EmptyActionName,
                    paramName: "webFormsName");
            }

            PropertyMetadata primaryKey = efMetadata.PrimaryKeys.FirstOrDefault();
            string pluralizedName = efMetadata.EntitySetName;
            string modelNameSpace = modelType.Namespace != null ? modelType.Namespace.FullName : String.Empty;
            string relativePath = outputPath.Replace(oldValue: @"\", newValue: @"/");

            //Project project = Context.ActiveProject;
            var templatePath = Path.Combine(path1: "ApiControllerWithContext", path2: t4Name);
            string defaultNamespace = modelType.Namespace.FullName;
            string modelTypeVariable = GetTypeVariable(typeName: modelType.Name);
            string bindAttributeIncludeText = GetBindAttributeIncludeText(efMetadata: efMetadata);

            Dictionary<string, object> templateParams = new Dictionary<string, object>()
            {
                {"Namespace", defaultNamespace}, {"ModelTypeName", modelType.Name}, {"ModelMetadata", efMetadata},
                {"MetaTable", _ModelMetadataVM.DataModel}
            };

            AddFileFromTemplate(project: project
                , outputPath: outputPath
                , templateName: templatePath
                , templateParameters: templateParams
                , skipIfExists: !overwrite);
        }

        private string GetTypeVariable(string typeName)
        {
            return typeName.Substring(startIndex: 0, length: 1).ToLower() + typeName.Substring(startIndex: 1, length: typeName.Length - 1);
        }

        private string GetBindAttributeIncludeText(ModelMetadata efMetadata)
        {
            string result = "";
            foreach (PropertyMetadata m in efMetadata.Properties)
                result += "," + m.PropertyName;
            return result.Substring(startIndex: 1);
        }

        private void AddModelMetadata(Project project
            , string controllerName
            , string controllerRootName
            , string outputPath
            , string ContextTypeName /*"Entities"*/
            , CodeType modelType
            , ModelMetadata efMetadata
            , bool overwrite = false)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(paramName: "modelType");
            }
            if (String.IsNullOrEmpty(value: controllerName))
            {
                //TODO
                throw new ArgumentException(message: Resources.WebFormsViewScaffolder_EmptyActionName, paramName: "webFormsName");
            }

            PropertyMetadata primaryKey = efMetadata.PrimaryKeys.FirstOrDefault();
            string pluralizedName = efMetadata.EntitySetName;
            string modelNameSpace = modelType.Namespace != null ? modelType.Namespace.FullName : String.Empty;
            string relativePath = outputPath.Replace(oldValue: @"\", newValue: @"/");

            //Project project = Context.ActiveProject;
            var templatePath = Path.Combine(path1: "Model", path2: "Metadata");
            string defaultNamespace = modelType.Namespace.FullName;
            string modelTypeVariable = GetTypeVariable(typeName: modelType.Name);
            string bindAttributeIncludeText = GetBindAttributeIncludeText(efMetadata: efMetadata);

            Dictionary<string, object> templateParams = new Dictionary<string, object>(){
                {"Namespace", defaultNamespace}
                , {"ModelTypeName", modelType.Name}
                , {"ModelMetadata", efMetadata}
                , {"MetaTable", _ModelMetadataVM.DataModel}
            };

            AddFileFromTemplate(project: project
                , outputPath: outputPath
                , templateName: templatePath
                , templateParameters: templateParams
                , skipIfExists: !overwrite);
        }

        private void AddView(Project project
            , string viewsFolderPath
            , string viewPrefix
            , string viewName
            , string programTitle
            , string controllerRootName
            , CodeType modelType
            , ModelMetadata efMetadata
            , bool referenceScriptLibraries = true
            , bool isLayoutPageSelected = true
            , string layoutPageFile = null
            , bool isBundleConfigPresent = true
            , bool overwrite = false)
        {
            //Project project = Context.ActiveProject;
            string outputPath = Path.Combine(path1: viewsFolderPath, path2: viewPrefix + viewName);
            string templatePath = Path.Combine(path1: "MvcView", path2: viewName);
            string viewDataTypeName = modelType.Namespace.FullName + "." + modelType.Name;

            if (layoutPageFile == null)
                layoutPageFile = string.Empty;

            Dictionary<string, object> templateParams = new Dictionary<string, object>(){
                {"ControllerRootName" , controllerRootName}
                , {"ModelMetadata", efMetadata}
                , {"ViewPrefix", viewPrefix}
                , {"ViewName", viewName}
                , {"ProgramTitle", programTitle}
                , {"ViewDataTypeName", viewDataTypeName}
                , {"IsPartialView" , false}
                , {"LayoutPageFile", layoutPageFile}
                , {"IsLayoutPageSelected", isLayoutPageSelected}
                , {"ReferenceScriptLibraries", referenceScriptLibraries}
                , {"IsBundleConfigPresent", isBundleConfigPresent}
                //, {"ViewDataTypeShortName", modelType.Name} // 可刪除
                , {"MetaTable", _ModelMetadataVM.DataModel}
                , {"JQueryVersion","2.1.0"} // 如何讀取專案的 jQuery 版本
                , {"MvcVersion", new Version(version: "5.1.2.0")}
            };

            AddFileFromTemplate(project: project
                , outputPath: outputPath
                , templateName: templatePath
                , templateParameters: templateParams
                , skipIfExists: !overwrite);
        }

        //add _Layout & _ViewStart
        private void AddDependencyFile(Project project, string viewRootPath, string areaName
            )
        {
            // add _Layout
            string viewName = "_ViewStart";
            string outputPath = Path.Combine(path1: viewRootPath, path2: viewName);
            string templatePath = Path.Combine(path1: "MvcFullDependencyCodeGenerator", path2: viewName);
            Dictionary<string, object> templateParams = new Dictionary<string, object>(){
                {"AreaName", areaName}
            };
            AddFileFromTemplate(project: project
                , outputPath: outputPath
                , templateName: templatePath
                , templateParameters: templateParams
                , skipIfExists: true);

            // add _ViewStart
            viewName = "_Layout";
            outputPath = Path.Combine(path1: viewRootPath, path2: "Shared", path3: viewName);
            templatePath = Path.Combine(path1: "MvcFullDependencyCodeGenerator", path2: viewName);
            templateParams = new Dictionary<string, object>(){
                {"IsBundleConfigPresent", true}
                , {"JQueryVersion",""}
                , {"ModernizrVersion", ""}
            };
            AddFileFromTemplate(project: project
                , outputPath: outputPath
                , templateName: templatePath
                , templateParameters: templateParams
                , skipIfExists: true);
        }

        private void AddDataFieldTemplates(Project project, string viewRootPath)
        {
            Dictionary<string, object> templateParams = new Dictionary<string, object>();

            var fieldTemplates = new[] {
                "EditorTemplates\\Date"
                , "DisplayTemplates\\Date"
                , "DisplayTemplates\\DateTime"
            };

            foreach (var fieldTemplate in fieldTemplates)
            {
                string outputPath = Path.Combine(path1: viewRootPath, path2: "Shared", path3: fieldTemplate);
                string templatePath = Path.Combine(path1: "DataFieldTemplates", path2: fieldTemplate);

                AddFileFromTemplate(project: project
                    , outputPath: outputPath
                    , templateName: templatePath
                    , templateParameters: templateParams
                    , skipIfExists: true);
            }

            //var fieldTemplatesPath = "DynamicData\\FieldTemplates";

            //// Add the folder
            //AddFolder(project, fieldTemplatesPath);

            //foreach (var fieldTemplate in fieldTemplates)
            //{
            //    var templatePath = Path.Combine(fieldTemplatesPath, fieldTemplate);
            //    var outputPath = Path.Combine(fieldTemplatesPath, fieldTemplate);

            //    AddFileFromTemplate(
            //        project: project,
            //        outputPath: outputPath,
            //        templateName: templatePath,
            //        templateParameters: new Dictionary<string, object>()
            //        {
            //            {"DefaultNamespace", project.GetDefaultNamespace()},
            //            {"GenericRepositoryNamespace", genericRepositoryNamespace}
            //        },
            //        skipIfExists: true);
            //}
        }

        #region function library

        public string GetJqueryVersion(Project project)
        {
            //NuGetPackage p=new NuGetPackage("jquery",
            //                                 "1.6.4",
            //                                 new NuGetSourceRepository());
            // context.Packages.Add(p);

            for (int x = 0; x < project.Properties.Count; x++)
            {
                object xx = project.Properties.Item(index: x);
            }

            NuGetPackage currPage = Context.Packages.FirstOrDefault(predicate: p => p.PackageId == "jquery");
            return (currPage != null ? currPage.Version : string.Empty);
        }

        // Called to ensure that the project was compiled successfully
        private Type GetReflectionType(string typeName)
        {
            return GetService<IReflectedTypesService>().GetType(project: Context.ActiveProject, fullName: typeName);
        }

        private TService GetService<TService>() where TService : class
        {
            return (TService)ServiceProvider.GetService(serviceType: typeof(TService));
        }

        // Returns the relative path of the folder selected in Visual Studio or an empty
        // string if no folder is selected.
        protected string GetSelectionRelativePath()
        {
            return Context.ActiveProjectItem == null ? String.Empty : ProjectItemUtils.GetProjectRelativePath(projectItem: Context.ActiveProjectItem);
        }

        private string GetAreaName(string selectionRelativePath)
        {
            string[] dirs = selectionRelativePath.Split(separator: new char[1] { '\\' });

            if (dirs[0].Equals(value: "Areas"))
                return dirs[1];
            else
                return string.Empty;
        }

        /// <summary>
        /// Get Views folder
        /// </summary>
        /// <param name="folderName"></param>
        /// <returns></returns>
        private string GetViewsFolderPath(string selectionRelativePath)
        {
            //string keyControllers = "Controllers";
            //string keyViews = "Views";

            //return (
            //    (
            //    controllerPath.IndexOf(keyControllers) >= 0)
            //    ? controllerPath.Replace(keyControllers, keyViews)
            //    : Path.Combine(controllerPath, keyViews)
            //    );
            return GetRelativeFolderPath(selectionRelativePath: selectionRelativePath, folderName: "Views");
        }

        private string GetModelFolderPath(string selectionRelativePath)
        {
            return GetRelativeFolderPath(selectionRelativePath: selectionRelativePath, folderName: "Models");
        }

        private string GetRelativeFolderPath(string selectionRelativePath, string folderName)
        {
            string keyControllers = "Controllers";
            string keyViews = folderName;

            return (
                (
                selectionRelativePath.IndexOf(value: keyControllers) >= 0)
                ? selectionRelativePath.Replace(oldValue: keyControllers, newValue: keyViews)
                : Path.Combine(path1: selectionRelativePath, path2: keyViews)
                );
        }

        // If a Visual Studio folder is selected then returns the folder's namespace, otherwise
        // returns the project namespace.
        protected string GetDefaultNamespace()
        {
            return Context.ActiveProjectItem == null
                ? Context.ActiveProject.GetDefaultNamespace()
                : Context.ActiveProjectItem.GetDefaultNamespace();
        }

        #endregion function library

        #region no used

        //// A single generic repository is created no matter how many models are scaffolded
        //// with the Web Forms scaffolder. This generic repository is added to the Models folder.
        //private void EnsureGenericRepository(Project project, CodeType dbContext, string genericRepositoryNamespace)
        //{
        //    string dbContextNameSpace = dbContext.Namespace != null ? dbContext.Namespace.FullName : String.Empty;

        //    // Add the folder
        //    AddFolder(project, "Models");

        //    AddFileFromTemplate(
        //        project: project,
        //        outputPath: "Models\\GenericRepository",
        //        templateName: "Models\\GenericRepository",
        //        templateParameters: new Dictionary<string, object>()
        //            {
        //                {"Namespace", genericRepositoryNamespace},
        //                {"DBContextType", dbContext.Name},
        //                {"DBContextNamespace", dbContextNameSpace}
        //            },
        //        skipIfExists: true);
        //}

        //// A set of Dynamic Data field templates is created that support Bootstrap
        //private void EnsureDynamicDataFieldTemplates(Project project, string genericRepositoryNamespace)
        //{
        //    var fieldTemplates = new[] {
        //        "Boolean", "Boolean.ascx.designer", "Boolean.ascx",
        //        "Boolean_Edit", "Boolean_Edit.ascx.designer", "Boolean_Edit.ascx",
        //        "Children", "Children.ascx.designer", "Children.ascx",
        //        "Children_Insert", "Children_Insert.ascx.designer", "Children_Insert.ascx",
        //        "DateTime", "DateTime.ascx.designer", "DateTime.ascx",
        //        "DateTime_Edit", "DateTime_Edit.ascx.designer", "DateTime_Edit.ascx",
        //        "Decimal_Edit", "Decimal_Edit.ascx.designer", "Decimal_Edit.ascx",
        //        "EmailAddress", "EmailAddress.ascx.designer", "EmailAddress.ascx",
        //        "Enumeration", "Enumeration.ascx.designer", "Enumeration.ascx",
        //        "Enumeration_Edit", "Enumeration_Edit.ascx.designer", "Enumeration_Edit.ascx",
        //        "ForeignKey", "ForeignKey.ascx.designer", "ForeignKey.ascx",
        //        "ForeignKey_Edit", "ForeignKey_Edit.ascx.designer", "ForeignKey_Edit.ascx",
        //        "Integer_Edit", "Integer_Edit.ascx.designer", "Integer_Edit.ascx",
        //        "FieldLabel", "FieldLabel.ascx.designer", "FieldLabel.ascx",
        //        "MultilineText_Edit", "MultilineText_Edit.ascx.designer", "MultilineText_Edit.ascx",
        //        "Text", "Text.ascx.designer", "Text.ascx",
        //        "Text_Edit", "Text_Edit.ascx.designer", "Text_Edit.ascx",
        //        "Url", "Url.ascx.designer", "Url.ascx",
        //        "Url_Edit", "Url_Edit.ascx.designer", "Url_Edit.ascx"
        //    };
        //    var fieldTemplatesPath = "DynamicData\\FieldTemplates";

        //    // Add the folder
        //    AddFolder(project, fieldTemplatesPath);

        //    foreach (var fieldTemplate in fieldTemplates)
        //    {
        //        var templatePath = Path.Combine(fieldTemplatesPath, fieldTemplate);
        //        var outputPath = Path.Combine(fieldTemplatesPath, fieldTemplate);

        //        AddFileFromTemplate(
        //            project: project,
        //            outputPath: outputPath,
        //            templateName: templatePath,
        //            templateParameters: new Dictionary<string, object>()
        //            {
        //                {"DefaultNamespace", project.GetDefaultNamespace()},
        //                {"GenericRepositoryNamespace", genericRepositoryNamespace}
        //            },
        //            skipIfExists: true);
        //    }
        //}

        //// Generates all of the Web Forms Pages (Default Insert, Edit, Delete),
        //private void AddWebFormsPages(
        //    Project project,
        //    string selectionRelativePath,
        //    string genericRepositoryNamespace,
        //    CodeType modelType,
        //    ModelMetadata efMetadata,
        //    bool useMasterPage,
        //    string masterPage = null,
        //    string desktopPlaceholderId = null,
        //    bool overwriteViews = true
        //)
        //{
        //    if (modelType == null)
        //    {
        //        throw new ArgumentNullException("modelType");
        //    }

        //    // Generate dictionary for related entities
        //    var relatedModels = GetRelatedModelDictionary(efMetadata);

        //    var webForms = new[] { "Default", "Insert", "Edit", "Delete" };

        //    // Extract these from the selected master page : Tracked by 721707
        //    var sectionNames = new[] { "HeadContent", "MainContent" };

        //    // Add folder for views. This is necessary to display an error when the folder already exists but
        //    // the folder is excluded in Visual Studio: see https://github.com/Superexpert/WebFormsScaffolding/issues/18
        //    string outputFolderPath = Path.Combine(selectionRelativePath, modelType.Name);
        //    AddFolder(Context.ActiveProject, outputFolderPath);

        //    // Now add each view
        //    foreach (string webForm in webForms)
        //    {
        //        AddWebFormsViewTemplates(
        //            outputFolderPath: outputFolderPath,
        //            modelType: modelType,
        //            efMetadata: efMetadata,
        //            relatedModels: relatedModels,
        //            genericRepositoryNamespace: genericRepositoryNamespace,
        //            webFormsName: webForm,
        //            useMasterPage: useMasterPage,
        //            masterPage: masterPage,
        //            sectionNames: sectionNames,
        //            primarySectionName: desktopPlaceholderId,
        //            overwrite: overwriteViews);
        //    }
        //}

        //private void AddWebFormsViewTemplates(
        //                        string outputFolderPath,
        //                        CodeType modelType,
        //                        ModelMetadata efMetadata,
        //                        IDictionary<string, RelatedModelMetadata> relatedModels,
        //                        string genericRepositoryNamespace,
        //                        string webFormsName,
        //                        bool useMasterPage,
        //                        string masterPage = "",
        //                        string[] sectionNames = null,
        //                        string primarySectionName = "",
        //                        bool overwrite = false
        //)
        //{
        //    if (modelType == null)
        //    {
        //        throw new ArgumentNullException("modelType");
        //    }
        //    if (String.IsNullOrEmpty(webFormsName))
        //    {
        //        throw new ArgumentException(Resources.WebFormsViewScaffolder_EmptyActionName, "webFormsName");
        //    }

        //    PropertyMetadata primaryKey = efMetadata.PrimaryKeys.FirstOrDefault();
        //    string pluralizedName = efMetadata.EntitySetName;

        //    string modelNameSpace = modelType.Namespace != null ? modelType.Namespace.FullName : String.Empty;
        //    string relativePath = outputFolderPath.Replace(@"\", @"/");

        //    List<string> webFormsTemplates = new List<string>();
        //    webFormsTemplates.AddRange(new string[] { webFormsName, webFormsName + ".aspx", webFormsName + ".aspx.designer" });

        //    // Scaffold aspx page and code behind
        //    foreach (string webForm in webFormsTemplates)
        //    {
        //        Project project = Context.ActiveProject;
        //        var templatePath = Path.Combine("WebForms", webForm);
        //        string outputPath = Path.Combine(outputFolderPath, webForm);

        //        var defaultNamespace = GetDefaultNamespace() + "." + modelType.Name;
        //        AddFileFromTemplate(project,
        //            outputPath,
        //            templateName: templatePath,
        //            templateParameters: new Dictionary<string, object>()
        //            {
        //                {"RelativePath", relativePath},
        //                {"DefaultNamespace", defaultNamespace},
        //                {"Namespace", modelNameSpace},
        //                {"IsContentPage", useMasterPage},
        //                {"MasterPageFile", masterPage},
        //                {"SectionNames", sectionNames},
        //                {"PrimarySectionName", primarySectionName},
        //                {"PrimaryKeyMetadata", primaryKey},
        //                {"PrimaryKeyName", primaryKey.PropertyName},
        //                {"PrimaryKeyType", primaryKey.ShortTypeName},
        //                {"ViewDataType", modelType},
        //                {"ViewDataTypeName", modelType.Name},
        //                {"GenericRepositoryNamespace", genericRepositoryNamespace},
        //                {"PluralizedName", pluralizedName},
        //                {"ModelMetadata", efMetadata},
        //                {"RelatedModels", relatedModels}
        //            },
        //            skipIfExists: !overwrite);
        //    }
        //}

        // We are just pulling in some dependent nuget packages
        // to meet "Web Application Project" experience in this change.
        // There are some open questions regarding the experience for
        // webforms scaffolder in the case of an empty project.
        // Those details need to be worked out and
        // depending on that, we would modify the list of packages below
        // or conditions which determine when they are installed etc.
        //public override IEnumerable<NuGetPackage> Dependencies
        //{
        //    get
        //    {
        //        return GetService<IEntityFrameworkService>().Dependencies;
        //    }
        //}

        // Create a dictionary that maps foreign keys to related models. We only care about associations
        // with a single key (so we can display in a DropDownList)
        //protected IDictionary<string, RelatedModelMetadata> GetRelatedModelDictionary(ModelMetadata efMetadata)
        //{
        //    var dict = new Dictionary<string, RelatedModelMetadata>();

        //    foreach (var relatedEntity in efMetadata.RelatedEntities)
        //    {
        //        if (relatedEntity.ForeignKeyPropertyNames.Count() == 1)
        //        {
        //            dict[relatedEntity.ForeignKeyPropertyNames[0]] = relatedEntity;
        //        }
        //    }
        //    return dict;
        //}

        //private void WriteLog(string message)
        //{
        //    System.IO.StreamWriter sw = new StreamWriter("R:\\LOG.Scaffold.txt", true);
        //    sw.WriteLine(message);
        //    sw.Close();
        //}

        #endregion no used
    }
}