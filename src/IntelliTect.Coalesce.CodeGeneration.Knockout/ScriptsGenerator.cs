﻿using IntelliTect.Coalesce.DataAnnotations;
using IntelliTect.Coalesce.CodeGeneration.Common;
using IntelliTect.Coalesce.Models;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Web.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IntelliTect.Coalesce.TypeDefinition;
using IntelliTect.Coalesce.Validation;
using System.Globalization;
using System.Reflection;
using System.Threading;
using IntelliTect.Coalesce.CodeGeneration.Templating;
using IntelliTect.Coalesce.CodeGeneration.Utilities;
using IntelliTect.Coalesce.CodeGeneration.Analysis.Base;
using IntelliTect.Coalesce.CodeGeneration.Scripts;
using Microsoft.Extensions.DependencyInjection;
using IntelliTect.Coalesce.CodeGeneration.Templating.Resolution;
using IntelliTect.Coalesce.CodeGeneration.Knockout.Generators;
using IntelliTect.Coalesce.CodeGeneration.Generation;

namespace IntelliTect.Coalesce.CodeGeneration.Knockout
{
    public class ScriptsGenerator
    {
        public const string ScriptsFolderName = "Scripts";

        protected RazorTemplateCompiler TemplateProvider { get; }
        public ProjectContext WebProject { get; }
        public ProjectContext DataProject { get; }

        public ScriptsGenerator(ProjectContext webProject, ProjectContext dataProject)
        {
            TemplateProvider = new RazorTemplateCompiler(webProject);

            WebProject = webProject;
            DataProject = dataProject;
        }

        public async Task Generate(CommandLineGeneratorModel model)
        {
            Console.WriteLine($"Starting Generator");
            string targetNamespace = WebProject.RootNamespace;
            Console.WriteLine($"Target Namespace: {targetNamespace}");

            TypeViewModel dataContextType;
            if (string.IsNullOrWhiteSpace(model.DataContextClass))
            {
                var candidates = DataProject.TypeLocator
                    .FindDerivedTypes(typeof(Microsoft.EntityFrameworkCore.DbContext).FullName)
                    .ToList();
                if (candidates.Count() != 1)
                {
                    throw new InvalidOperationException($"Couldn't find a single DbContext to generate from. " +
                        $"Specify the name of your DbContext with the '-dc MyDbContext' command line param.");
                }
                dataContextType = candidates.Single();
            }
            else
            {
                dataContextType = DataProject.TypeLocator.FindType(model.DataContextClass, throwWhenNotFound: false);
            }
                

            if (model.ValidateOnly)
            {
                Console.WriteLine($"Validating model for: {dataContextType.FullName}");
            }
            else
            {
                Console.WriteLine($"Building scripts for: {dataContextType.FullName}");
            }

            List<ClassViewModel> models = ReflectionRepository
                                .AddContext(dataContextType)
                                .ToList();

            ValidationHelper validationResult = ValidateContext.Validate(models);

            bool foundIssues = false;
            foreach (var validation in validationResult.Where(f => !f.WasSuccessful))
            {
                foundIssues = true;
                Console.WriteLine("--- " + validation.ToString());
            }
            if (!foundIssues)
            {
                Console.WriteLine("Model validated successfully");
            }

            if (foundIssues)
            {
                //throw new Exception("Model did not validate. " + validationResult.First(f => !f.WasSuccessful).ToString());
                if (Debugger.IsAttached)
                {
                    Console.WriteLine("Press enter to quit");
                    Console.Read();
                }
                Environment.Exit(1);
            }

            if (model.ValidateOnly)
            {
                return;
            }
            else
            {
                var generationContext = new GenerationContext(model.CoalesceConfiguration)
                {
                    DataProject = DataProject,
                    WebProject = WebProject,
                    DbContextType = dataContextType,
                };


                var services = new ServiceCollection();
                services.AddSingleton(generationContext);
                services.AddSingleton(model.CoalesceConfiguration);
                services.AddSingleton(new RazorTemplateCompiler(WebProject));
                services.AddSingleton<ITemplateResolver, TemplateResolver>();
                services.AddTransient<RazorServices>();
                var provider = services.BuildServiceProvider();

                var generator = new KnockoutSuite(provider)
                    .WithModel(models)
                    .WithOutputPath(WebProject.ProjectPath);

                IEnumerable<IGenerator> Flatten(ICompositeGenerator composite) =>
                    composite.GetGenerators().SelectMany(g => (g is ICompositeGenerator c) ? Flatten(c) : new[] { g });

                var allGenerators = Flatten(generator).ToList();

                await Task.WhenAll(allGenerators.Select(g => g.GenerateAsync()));

                //return GenerateScripts(model, models, contextInfo, targetNamespace);
            }
        }

        private string ActiveTemplatesPath => Path.Combine(
                WebProject.ProjectPath,
                "Coalesce",
                "Templates");

        private bool _hasExplainedCopying = false;
        private void CopyToOriginalsAndDestinationIfNeeded(string fileName, string sourcePath, string originalsPath, string destinationPath, bool alertIfNoCopy = true)
        {
            string originalsFile = Path.Combine(originalsPath, fileName);
            string destinationFile = Path.Combine(destinationPath, fileName.Replace(".template", ""));

            if ((File.Exists(originalsFile) || !File.Exists(destinationFile)) && !FileUtilities.HasDifferences(originalsFile, destinationFile))
            {
                // The original file and the active file are the same,
                // and either the original does exist, or the destination doesn't. (this prevents overwriting of an existing destination file when the original doesn't exist).
                // Overwrite the active file with the new template.
                CopyToDestination(fileName, sourcePath, destinationPath, destinationIsTemplate: false);
            }
            else if (alertIfNoCopy)
            {
                Console.WriteLine($"Skipping copy to {destinationFile.Replace(WebProject.ProjectPath, "")} because it has been modified from the original.");
                if (!_hasExplainedCopying)
                {
                    _hasExplainedCopying = true;
                    Console.WriteLine("    If you would like for your templates to be updated by the CLI, restore the copies of ");
                    Console.WriteLine("    your templates in Coalesce/Templates with those from Coalesce/Originals/Templates.");
                    Console.WriteLine("    If you experience issues with your templates, compare your template with the original to see what might need changing.");
                }
            }

            string originalFile = Path.Combine(originalsPath, fileName);


            FileAttributes attr;
            if (File.Exists(originalFile))
            {
                // unset read-only
                attr = File.GetAttributes(originalFile);
                attr = attr & ~FileAttributes.ReadOnly;
                File.SetAttributes(originalFile, attr);
            }

            CopyToDestination(fileName, sourcePath, originalsPath);

            // set read-only
            if (File.Exists(originalFile))
            {
                attr = File.GetAttributes(originalFile);
                attr = attr | FileAttributes.ReadOnly;
                File.SetAttributes(originalFile, attr);
            }
        }

        private void CopyToDestination(string fileName, string sourcePath, string destinationPath, bool destinationIsTemplate = true)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string sourceFile = assembly.GetName().Name + "." + Path.Combine(sourcePath, fileName).Replace('/', '.').Replace('\\', '.');

            Directory.CreateDirectory(destinationPath);

            string destinationFile = destinationIsTemplate
                ? Path.Combine(destinationPath, fileName)
                : Path.Combine(destinationPath, fileName.Replace(".template", ""));

            var inputStream = assembly.GetManifestResourceStream(sourceFile);
            if (inputStream == null)
            {
                throw new FileNotFoundException("Embedded resource not found: " + sourceFile);
            }

            if (!File.Exists(destinationFile) || FileUtilities.HasDifferencesAsync(inputStream, destinationFile).Result)
            {
                const int tries = 3;
                for (int i = 1; i <= tries; i++)
                {
                    FileStream fileStream = null;
                    try
                    {
                        fileStream = File.Create(destinationFile);
                        inputStream.Seek(0, SeekOrigin.Begin);
                        inputStream.CopyTo(fileStream);
                        if (i > 1) Console.WriteLine($"Attempt {i} succeeded.");
                        break;
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine($"Attempt {i} of {tries} failed: {ex.Message}");
                        if (i == tries)
                            throw;

                        // Errors here are almost always because a file is in use. Just wait a second and it probably won't be in use anymore.
                        Thread.Sleep(1000);
                    }
                    finally
                    {
                        fileStream?.Dispose();
                    }
                }
            }
        }

        private async Task CopyStaticFiles(CommandLineGeneratorModel commandLineGeneratorModel)
        {
            Console.WriteLine("Copying Static Files");
            string areaLocation = "";

            if (!string.IsNullOrWhiteSpace(commandLineGeneratorModel.AreaLocation))
            {
                areaLocation = Path.Combine("Areas", commandLineGeneratorModel.AreaLocation);
            }

            // Our gulp tasks don't like it if the areas folder doesn't exist, so just create it by default.
            Directory.CreateDirectory(Path.Combine(WebProject.ProjectPath, "Areas"));

            // Directory location for all "original" files from intellitect
            var baseCoalescePath = Path.Combine(
                WebProject.ProjectPath,
                areaLocation,
                "Coalesce");

            var coalescePathExisted = Directory.Exists(baseCoalescePath);

            var originalsPath = Path.Combine(baseCoalescePath, "Originals");
            var originalTemplatesPath = Path.Combine(originalsPath, "Templates");
            var activeTemplatesPath = ActiveTemplatesPath;


            Directory.CreateDirectory(baseCoalescePath);

            // We need to preserve the old intelliTect folder so that we don't overwrite any custom files,
            // since the contents of this folder are what is used to determine if changes have been made.
            // If the Coalesce folder isn't found, we will assume this is effectively a new installation of Coalesce.
            var oldOriginalsPath = Path.Combine(
                 WebProject.ProjectPath,
                 areaLocation,
                 "intelliTect");
            if (Directory.Exists(oldOriginalsPath))
                Directory.Move(oldOriginalsPath, originalsPath);// TODO: remove this at some point after all projects are upgraded.

            Directory.CreateDirectory(originalsPath);
            Directory.CreateDirectory(originalTemplatesPath);
            Directory.CreateDirectory(activeTemplatesPath);

            // Copy over Api Folder and Files
            var apiViewOutputPath = Path.Combine(
                WebProject.ProjectPath,
                areaLocation,
                "Views", "Api");

            CopyToOriginalsAndDestinationIfNeeded(
                    fileName: "Docs.cshtml",
                    sourcePath: "Templates/Views/Api",
                    originalsPath: originalsPath,
                    destinationPath: apiViewOutputPath);

            CopyToOriginalsAndDestinationIfNeeded(
                    fileName: "EditorHtml.cshtml",
                    sourcePath: "Templates/Views/Api",
                    originalsPath: originalsPath,
                    destinationPath: apiViewOutputPath);


            if (string.IsNullOrWhiteSpace(commandLineGeneratorModel.AreaLocation))
            {
                // only copy the intellitect scripts when generating the root site, this isn't needed for areas since it will already exist at the root
                // Copy files for the scripts folder
                var scriptsOutputPath = Path.Combine(
                    WebProject.ProjectPath,
                    "Scripts", "Coalesce");

                var oldScriptsOutputPath = Path.Combine(
                  WebProject.ProjectPath,
                    "Scripts", "IntelliTect");
                if (Directory.Exists(oldScriptsOutputPath)) Directory.Delete(oldScriptsOutputPath, true); // TODO: remove this at some point after all projects are upgraded.

                string[] intellitectScripts =
                {
                    "coalesce.ko.base.ts",
                    "coalesce.ko.bindings.ts",
                    "coalesce.ko.utilities.ts",
                    "coalesce.utilities.ts",
                };
                // These were renamed from intellitect.* to coalesce.*. Delete the old ones.
                if (System.IO.Directory.Exists(scriptsOutputPath))
                {
                    foreach (var oldName in Directory.EnumerateFiles(scriptsOutputPath, "intellitect.*"))
                    {
                        File.Delete(oldName);
                    }
                }
                foreach (var fileName in intellitectScripts)
                {
                    CopyToDestination(
                            fileName: fileName,
                            sourcePath: "Templates/Scripts/Coalesce",
                            destinationPath: scriptsOutputPath);
                }

                string[] generationTemplates =
                {
                    "ApiController.cshtml",
                    "CardView.cshtml",
                    "ClassDto.cshtml",
                    "CreateEditView.cshtml",
                    "KoExternalType.cshtml",
                    "KoListViewModel.cshtml",
                    "KoViewModel.cshtml",
                    "KoViewModelPartial.cshtml",
                    "LocalBaseApiController.cshtml",
                    "TableView.cshtml",
                    "ViewController.cshtml",
                };
                foreach (var fileName in generationTemplates)
                {
                    CopyToOriginalsAndDestinationIfNeeded(
                            fileName: fileName,
                            sourcePath: "Templates",
                            originalsPath: originalTemplatesPath,
                            destinationPath: activeTemplatesPath);
                }
            }
        }


        private async Task WriteFileAsync(Stream contentsStream, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            if (File.Exists(outputPath))
            {
                if (!await FileUtilities.HasDifferencesAsync(contentsStream, outputPath))
                {
                    return;
                }

                // Remove read only flag, if it exists.
                // Commented out because I don't know why we do this. If something is read only, its probably that way on purpose.
                // File.SetAttributes(outputPath, File.GetAttributes(outputPath) & ~FileAttributes.ReadOnly);
            }

            using (FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                contentsStream.Seek(0, SeekOrigin.Begin);
                await contentsStream.CopyToAsync(fileStream);

                // Manually flush to get async goodness for finishing the saving of the file.
                await fileStream.FlushAsync();
            };
        }

        private async Task Generate(string templateName, string outputPath, object model)
        {
            //var template = TemplateProvider.GetCachedCompiledTemplate(Path.Combine(ActiveTemplatesPath, templateName));

            //using (var sourceStream = await TemplateProvider.RunTemplateAsync(template, model, outputPath))
            //{
            //    await WriteFileAsync(sourceStream, outputPath);
            //}
        }

        class GenerationOutputContext : IDisposable
        {
            private string OutputDir { get; }

            private ScriptsGenerator Generator { get; }

            private List<string> GeneratedFiles { get; } = new List<string>();

            public GenerationOutputContext(ScriptsGenerator generator, string outputDir)
            {
                Generator = generator;
                OutputDir = outputDir;
            }

            public Task Generate(string templateName, string outputName, object model)
            {
                string outputPath = Path.Combine(OutputDir, outputName);
                GeneratedFiles.Add(outputPath);

                return Generator.Generate(templateName, outputPath, model);
            }

            public void Cleanup()
            {
                //foreach ( var generatedFile in GeneratedFiles )
                //{
                //    Console.WriteLine($"Generated {generatedFile}");
                //}
                foreach (var file in Directory.EnumerateFiles(OutputDir, "*", SearchOption.AllDirectories))
                {
                    if (!GeneratedFiles.Contains(file))
                    {
                        Console.WriteLine($"   Deleting {file} because it seems to be unused.");
                        File.Delete(file);
                    }
                }
            }

            public void Dispose()
            {
                Cleanup();
            }
        }

        private async Task GenerateScripts(
            CommandLineGeneratorModel controllerGeneratorModel,
            List<ClassViewModel> models,
            ContextInfo dataContext,
            string targetNamespace)
        {
            string areaLocation = "";

            if (!string.IsNullOrWhiteSpace(controllerGeneratorModel.AreaLocation))
            {
                areaLocation = Path.Combine("Areas", controllerGeneratorModel.AreaLocation);
            }

            // TODO: do we need this anymore?
            //var layoutDependencyInstaller = ActivatorUtilities.CreateInstance<MvcLayoutDependencyInstaller>(ServiceProvider);
            //await layoutDependencyInstaller.Execute();

            ViewModelForTemplates apiModels = new ViewModelForTemplates
            {
                Models = models,
                ContextInfo = dataContext,
                Namespace = targetNamespace,
                AreaName = controllerGeneratorModel.AreaLocation,
                ModulePrefix = controllerGeneratorModel.TypescriptModulePrefix
            };
            //services.AddSingleton<CoalesceConfig>


            // Copy over the static files
            // await CopyStaticFiles(controllerGeneratorModel);

            var apiViewOutputPath = Path.Combine(
                WebProject.ProjectPath,
                areaLocation,
                "Views", "Api");

            if (!Directory.Exists(apiViewOutputPath))
            {
                Directory.CreateDirectory(apiViewOutputPath);
            }

            Console.WriteLine("Generating Code");
            Console.WriteLine("-- Generating DTOs");
            Console.Write("   ");
            string modelOutputPath = Path.Combine(
                    WebProject.ProjectPath,
                    areaLocation,
                    "Models", "Generated");
            using (var output = new GenerationOutputContext(this, modelOutputPath))
            {
                foreach (var model in apiModels.ViewModelsForTemplates.Where(f => !f.Model.IsDto))
                {
                    Console.Write($"{model.Model.Name}  ");

                    await output.Generate("ClassDto.cshtml", Path.Combine(modelOutputPath, model.Model.Name + "DtoGen.cs"), model);
                }
                Console.WriteLine();
            }


            string scriptOutputPath = Path.Combine(
                    WebProject.ProjectPath,
                    areaLocation,
                    ScriptsFolderName, "Generated");
            using (var output = new GenerationOutputContext(this, scriptOutputPath))
            {
                Console.WriteLine("-- Generating TypeScript Models");
                Console.Write("   ");
                foreach (var model in apiModels.ViewModelsForTemplates.Where(f => f.Model.OnContext || f.Model.IsDto))
                {
                    Console.Write($"{model.Model.Name}  ");

                    var fileName = "Ko";
                    if (!string.IsNullOrWhiteSpace(model.ModulePrefix)) fileName += "." + model.ModulePrefix;
                    fileName += "." + model.Model.Name;
                    if (model.Model.HasTypeScriptPartial) fileName += ".Partial";
                    fileName += ".ts";
                    await output.Generate("KoViewModel.cshtml", fileName, model);

                    //Console.WriteLine("   Added Script: " + viewOutputPath);

                    fileName = (string.IsNullOrWhiteSpace(model.ModulePrefix)) ? $"Ko.{model.Model.ListViewModelClassName}.ts" : $"Ko.{model.ModulePrefix}.{model.Model.ListViewModelClassName}.ts";
                    await output.Generate("KoListViewModel.cshtml", fileName, model);

                    //Console.WriteLine("   Added Script: " + viewOutputPath);
                }
                Console.WriteLine();



                if (apiModels.ViewModelsForTemplates.Any(f => !f.Model.OnContext))
                {
                    Console.WriteLine("-- Generating TypeScript External Types");
                    Console.Write("   ");
                    foreach (var externalType in apiModels.ViewModelsForTemplates.Where(f => !f.Model.OnContext))
                    {
                        var fileName = "Ko";
                        if (!string.IsNullOrWhiteSpace(externalType.ModulePrefix)) fileName += "." + externalType.ModulePrefix;
                        fileName += "." + externalType.Model.Name;
                        if (externalType.Model.HasTypeScriptPartial) fileName += ".Partial";
                        fileName += ".ts";
                        await output.Generate("KoExternalType.cshtml", fileName, externalType);

                        Console.Write(externalType.Model.Name + "  ");
                    }
                    Console.WriteLine();
                }

            }


            if (apiModels.ViewModelsForTemplates.Any(f => f.Model.HasTypeScriptPartial))
            {
                string partialOutputPath = Path.Combine(
                        WebProject.ProjectPath,
                        areaLocation,
                        ScriptsFolderName, "Partials");
                foreach (var model in apiModels.ViewModelsForTemplates.Where(f => f.Model.HasTypeScriptPartial))
                {
                    var fileName = (string.IsNullOrWhiteSpace(model.ModulePrefix)) ? $"Ko.{model.Model.Name}.partial.ts" : $"Ko.{model.ModulePrefix}.{model.Model.Name}.partial.ts";
                    var fullName = Path.Combine(partialOutputPath, fileName);

                    if (!File.Exists(fullName))
                    {
                        await Generate("KoViewModelPartial.cshtml", fullName, model);

                        Console.Write($"    Generated Partial stub for {model.Model.Name}  ");
                    }
                }
                Console.WriteLine();
            }





            Console.WriteLine("-- Generating API Controllers");
            string apiOutputPath = Path.Combine(
                    WebProject.ProjectPath,
                    areaLocation,
                    "Api", "Generated");
            using (var output = new GenerationOutputContext(this, apiOutputPath))
            {
                // Generate base api controller if it doesn't already exist
                {
                    var model = apiModels.ViewModelsForTemplates.First(f => f.Model.OnContext);

                    await output.Generate("LocalBaseApiController.cshtml", "LocalBaseApiController.cs", model);
                }

                // Generate model api controllers
                foreach (var model in apiModels.ViewModelsForTemplates.Where(f => f.Model.OnContext))
                {
                    await output.Generate("ApiController.cshtml", model.Model.Name + "ControllerGen.cs", model);
                }
            }


            Console.WriteLine("-- Generating View Controllers");
            string controllerOutputPath = Path.Combine(
                    WebProject.ProjectPath,
                    areaLocation,
                    "Controllers", "Generated");
            using (var output = new GenerationOutputContext(this, controllerOutputPath))
            {
                foreach (var model in apiModels.ViewModelsForTemplates.Where(f => f.Model.OnContext))
                {
                    await output.Generate("ViewController.cshtml", model.Model.Name + "ControllerGen.cs", model);
                }
            }

            Console.WriteLine("-- Generating Views");
            string viewOutputPath = Path.Combine(
                    WebProject.ProjectPath,
                    areaLocation,
                    "Views", "Generated");
            using (var output = new GenerationOutputContext(this, viewOutputPath))
            {
                foreach (var model in apiModels.ViewModelsForTemplates.Where(f => f.Model.OnContext))
                {
                    var filename = Path.Combine(model.Model.Name, "Table.cshtml");
                    await output.Generate("TableView.cshtml", filename, model);

                    filename = Path.Combine(model.Model.Name, "Cards.cshtml");
                    await output.Generate("CardView.cshtml", filename, model);

                    filename = Path.Combine(model.Model.Name, "CreateEdit.cshtml");
                    await output.Generate("CreateEditView.cshtml", filename, model);
                }
            }

            //await layoutDependencyInstaller.InstallDependencies();

            var tsReferenceOutputPath = Path.Combine(WebProject.ProjectPath, ScriptsFolderName);
            GenerateTSReferenceFiles(tsReferenceOutputPath);

            //await GenerateTypeScriptDocs(scriptOutputPath);

            Console.WriteLine("-- Generation Complete --");
        }

        private async Task GenerateTypeScriptDocs(string path)
        {
            var dir = new DirectoryInfo(path);
            Console.WriteLine($"-- Creating Doc Files");
            foreach (var file in dir.GetFiles("*.ts"))
            {
                // don't gen json documentation for definition files
                if (!file.FullName.EndsWith(".d.ts"))
                {
                    var reader = file.OpenText();
                    var doc = new TypeScriptDocumentation();
                    doc.TsFilename = file.Name;
                    doc.Generate(await reader.ReadToEndAsync());
                    var serializer = Newtonsoft.Json.JsonSerializer.Create();
                    // Create the doc file.
                    FileInfo docFile = new FileInfo(file.FullName.Replace(".ts", ".json"));
                    // Remove it if it exists.
                    try
                    {
                        if (docFile.Exists) docFile.Delete();
                    }
                    catch (Exception)
                    {
                        System.Threading.Thread.Sleep(3000);
                        try
                        {
                            if (docFile.Exists) docFile.Delete();
                        }
                        catch (Exception)
                        {
                            Console.WriteLine($"Could not delete file {docFile.FullName}");
                        }
                    }
                    using (var tw = docFile.CreateText())
                    {
                        serializer.Serialize(tw, doc);
                        tw.Close();
                    }
                }
            }
        }

        private void GenerateTSReferenceFiles(string path)
        {
            var fileName = "coalesce.dependencies.d.ts";
            var generateDeps = Path.Combine(path, fileName);
            if (!File.Exists(generateDeps))
            {
                CopyToDestination(
                    fileName: fileName,
                    sourcePath: "Templates/Scripts",
                    destinationPath: path);
            }


            var fileContents = new List<string>
            {
                "\n\n",
                "// This file is automatically generated.",
                "// It is not in the generated folder for ease-of-use (no relative paths).",
                "// This file must remain in place relative to the generated scripts (<WebProject>/Scripts/Generated).",
                "\n\n",
                $"/// <reference path=\"coalesce.dependencies.d.ts\" />"
            };

            // Do files in the Generated folder.
            var dir = new DirectoryInfo(path + "\\Generated");
            foreach (var file in dir.GetFiles("*.ts"))
            {
                if ((file.Name.StartsWith("intellitect", true, CultureInfo.InvariantCulture) || file.Name.StartsWith("ko.", true, CultureInfo.InvariantCulture)) &&
                    !file.Name.EndsWith(".d.ts"))
                {
                    fileContents.Add($"/// <reference path=\"Generated\\{file.Name}\" />");
                }
            }

            // Do files in the Partials folder.
            dir = new DirectoryInfo(path + "\\Partials");
            if (dir.Exists)
            { 
                foreach (var file in dir.GetFiles("*.ts"))
                {
                    fileContents.Add($"/// <reference path=\"Partials\\{file.Name}\" />");
                }
            }

            var old = Path.Combine(path, "intellitect.references.d.ts");
            if (File.Exists(old)) File.Delete(old);
            // Write the file with the array list of content.
            File.WriteAllLines(Path.Combine(path, "viewmodels.generated.d.ts"), fileContents);
        }
    }
}
