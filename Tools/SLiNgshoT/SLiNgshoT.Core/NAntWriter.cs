// NAntWriter.cs - a NAnt .build file writer
// Copyright (C) 2001, 2002  Jason Diamond
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.Collections;
using System.IO;
using System.Xml;

namespace SLiNgshoT.Core
{
	[OutputFormat("nant")]
	[OutputParameter("build.basedir", true, "Specifies the parent directory for the individual configuration directories.")]
	public class NAntWriter : SolutionWriter
	{
		public NAntWriter()
		{
		}

		private XmlTextWriter writer;
		private Solution solution;
		private Project project;
		private string depends;
		private Hashtable parameters;

		public void SetOutput(TextWriter output)
		{
			writer = new XmlTextWriter(output);
			writer.Formatting = Formatting.Indented;
		}

		public void SetParameters(Hashtable parameters)
		{
			this.parameters = parameters;
		}

		private void WriteProperty(string name, string value)
		{
			writer.WriteStartElement("property");
			writer.WriteAttributeString("name", name);
			writer.WriteAttributeString("value", value);
			writer.WriteEndElement(); // property
		}

		public void WriteStartSolution(Solution solution)
		{
			this.solution = solution;

			//writer.WriteStartDocument();

			writer.WriteComment(" Generated by SLiNgshoT <http://injektilo.org/> ");

			writer.WriteStartElement("project");
			writer.WriteAttributeString("name", solution.SolutionName);
			writer.WriteAttributeString("default", "Debug");

			WriteProperty("build.basedir", (string)parameters["build.basedir"]);

			foreach (string configurationName in solution.GetConfigurationNames())
			{
				writer.WriteStartElement("target");
				writer.WriteAttributeString("name", configurationName);

				string depends = null;

				foreach (Project project in solution.GetProjects())
				{
					Configuration configuration = project.GetConfiguration(configurationName);

					if (configuration != null)
					{
						if (depends != null)
						{
							depends += ",";
						}

						depends += project.Name + "." + configurationName;
					}
				}

				writer.WriteAttributeString("depends", depends);

				writer.WriteEndElement(); // target
			}

			WriteSetupTarget();
		}

		private void WriteSetupTarget()
		{
			writer.WriteStartElement("target");
			writer.WriteAttributeString("name", "setup");

			writer.WriteStartElement("mkdir");
			writer.WriteAttributeString("dir", "${build.dir}");
			writer.WriteEndElement(); // mkdir

			writer.WriteEndElement(); // target
		}

		public void WriteStartProjectSourceFiles(Project project)
		{
		}

		public void WriteProjectSourceFile(File file)
		{
		}

		public void WriteEndProjectSourceFiles()
		{
		}

		public void WriteStartProjectResXResourceFiles(Project project)
		{
		}

		public void WriteProjectResXResourceFile(File file)
		{
		}

		public void WriteEndProjectResXResourceFiles()
		{
		}

		public void WriteStartProjectNonResXResourceFiles(Project project)
		{
		}

		public void WriteProjectNonResXResourceFile(File file)
		{
		}

		public void WriteEndProjectNonResXResourceFiles()
		{
		}

		public void WriteStartProject(Project project)
		{
			this.project = project;

			foreach (Configuration configuration in project.GetConfigurations())
			{
				writer.WriteStartElement("target");
				writer.WriteAttributeString("name", project.Name + "." + configuration.Name);

				depends = null;

				foreach (Project dependency in solution.GetDependencies(project))
				{
					if (depends != null)
					{
						depends += ",";
					}

					depends += dependency.Name + "." + configuration.Name;
				}

				foreach (Project projectReference in project.GetReferencedProjects())
				{
					if (depends != null)
					{
						depends += ",";
					}

					depends += projectReference.Name + "." + configuration.Name;
				}

				if (depends != null)
				{
					writer.WriteAttributeString("depends", depends);
				}

				WriteProperty("project.name", project.Name);
				WriteProperty("build.dir", "${build.basedir}\\" + configuration.Name);

				//WriteProperty("directory", Path.GetDirectoryName(project.GetRelativeOutputPathForConfiguration(configuration.Name)));
				WriteProperty("output", "${build.dir}\\" + Path.GetFileName(project.GetRelativeOutputPathForConfiguration(configuration.Name)));
				WriteProperty("debug", configuration.DebugSymbols ? "true" : "false");

				#warning fix this once NAnt supports unsafe & checked
				WriteProperty("unsafe", "/unsafe" + (configuration.AllowUnsafeBlocks ? "+" : "-"));
				//WriteProperty("unsafe", configuration.AllowUnsafeBlocks ? "true" : "false");
				WriteProperty("checked", "/checked" + (configuration.CheckForOverflowUnderflow ? "+" : "-"));

				WriteProperty("define", configuration.DefineConstants);

				string documentationFile = Path.GetFileName(project.GetRelativePathToDocumentationFile(configuration.Name));

				if (documentationFile == null || documentationFile.Length == 0)
				{
					WriteProperty("doc", "");
				}
				else
				{
					WriteProperty("doc", "${build.dir}\\" + Path.GetFileName(project.GetRelativePathToDocumentationFile(configuration.Name)));
				}

				writer.WriteStartElement("call");
				writer.WriteAttributeString("target", project.Name);
				writer.WriteEndElement(); // call

				writer.WriteEndElement(); // target
			}

			writer.WriteStartElement("target");
			writer.WriteAttributeString("name", project.Name);
		}

		public void WriteStartProjectDependencies()
		{
			depends = "setup";
		}

		public void WriteProjectDependency(Project project)
		{
			//depends += "," + project.Name;
		}

		public void WriteProjectDependency(File file)
		{
		}

		public void WriteEndProjectDependencies()
		{
			writer.WriteAttributeString("depends", depends);
		}

		public void WriteStartResXFiles()
		{
		}

		public void WriteResXFile(File file)
		{
			writer.WriteStartElement("resgen");

			writer.WriteAttributeString("input", file.RelativePathFromSolutionDirectory);

			writer.WriteAttributeString(
				"output",
				"${build.dir}\\" +
					project.RootNamespace +
					"." +
					Path.GetFileNameWithoutExtension(file.RelativePath) +
					".resources");

			writer.WriteEndElement(); // resgen
		}

		public void WriteEndResXFiles()
		{
		}

		public void WriteStartAssembly()
		{
			if (project.ProjectType.StartsWith("VB"))
			{
				writer.WriteStartElement("vbc");
			}
			else
			{
				// default to 'csc'
				writer.WriteStartElement("csc");
				#warning uncomment this once NAnt supports unsafe and checked
				//writer.WriteAttributeString("unsafe", "${unsafe}");
				//writer.WriteAttributeString("checked", "${checked}");
			}

			writer.WriteAttributeString("target", project.OutputType.ToLower());
			writer.WriteAttributeString("output", "${output}");
			writer.WriteAttributeString("debug", "${debug}");

			writer.WriteAttributeString("define", "${define}");
			writer.WriteAttributeString("doc", "${doc}");

			if (project.ProjectType.StartsWith("C#")) 
			{
				#warning remove these once NAnt supports unsafe and checked
				writer.WriteStartElement("arg");
				writer.WriteAttributeString("value", "${unsafe}");
				writer.WriteEndElement();

				writer.WriteStartElement("arg");
				writer.WriteAttributeString("value", "${checked}");
				writer.WriteEndElement();
			}

			if (project.ProjectType.StartsWith("VB"))
			{
				writer.WriteAttributeString("rootnamespace", project.RootNamespace);
				writer.WriteAttributeString("imports", project.GetImports());
			}
		}

		public void WriteStartSourceFiles()
		{
			writer.WriteStartElement("sources");
		}

		public void WriteSourceFile(File file)
		{
			writer.WriteStartElement("includes");
			writer.WriteAttributeString(
				"name",
				file.RelativePathFromSolutionDirectory);
			writer.WriteEndElement(); // includes
		}

		public void WriteEndSourceFiles()
		{
			writer.WriteEndElement(); // sources
		}

		public void WriteStartReferences()
		{
			writer.WriteStartElement("references");
		}

		public void WriteReference(string name, bool built)
		{
			string path = built ? "${build.dir}\\" + name : name;

			writer.WriteStartElement("includes");
			writer.WriteAttributeString("name", path);

			if (project.ProjectType.StartsWith("VB")) 
			{
				if (path.StartsWith("System")) 
				{
					writer.WriteAttributeString("frompath", "true");
				}
			}

			writer.WriteEndElement(); // includes
		}

		public void WriteReference(Project project)
		{
			writer.WriteStartElement("includes");
			writer.WriteAttributeString("name", "${build.dir}\\" + project.OutputFile);
			writer.WriteEndElement(); // includes
		}

		public void WriteEndReferences()
		{
			writer.WriteEndElement(); // references
		}

		public void WriteStartResources()
		{
		}

		public void WriteResource(string path, string name, bool built)
		{
			string arg = "/resource:";

			if (built)
			{
				arg += "${build.dir}\\";
			}

			arg += path;

			writer.WriteStartElement("arg");

			if (name != null)
			{
				arg += "," + name;
			}

			writer.WriteAttributeString("value", arg);

			writer.WriteEndElement(); // arg
		}

		public void WriteEndResources()
		{
		}

		public void WriteStartCopyProjectAssemblies()
		{
			//writer.WriteStartElement("copy");
			//writer.WriteAttributeString("todir", "${build.dir}");
			//writer.WriteStartElement("fileset");
		}

		public void WriteCopyProjectAssembly(Project project)
		{
			//writer.WriteStartElement("includes");
			//writer.WriteAttributeString("name", project.Name);
			//writer.WriteEndElement(); // includes
		}

		public void WriteEndCopyProjectAssemblies()
		{
			//writer.WriteEndElement(); // fileset
			//writer.WriteEndElement(); // copy
		}

		public void WriteEndAssembly()
		{
			writer.WriteEndElement(); // csc
		}

		public void WriteEndProject()
		{
			writer.WriteEndElement(); // target
		}

		public void WriteStartCleanTarget()
		{
			writer.WriteStartElement("target");
			writer.WriteAttributeString("name", "DebugClean");
				WriteProperty("build.dir","${build.basedir}\\Debug");
				writer.WriteStartElement("call");
				writer.WriteAttributeString("target", "Clean");
				writer.WriteEndElement(); // call
			writer.WriteEndElement(); // target DebugClean

			writer.WriteStartElement("target");
			writer.WriteAttributeString("name", "ReleaseClean");
				WriteProperty("build.dir","${build.basedir}\\Release");
				writer.WriteStartElement("call");
				writer.WriteAttributeString("target", "Clean");
				writer.WriteEndElement(); // call
			writer.WriteEndElement(); // target ReleaseClean
			
			writer.WriteStartElement("target");
			writer.WriteAttributeString("name", "Clean");
		}

		public void WriteCleanProject(Project project)
		{
			writer.WriteStartElement("delete");
			writer.WriteAttributeString("file", "${build.dir}\\" + project.OutputFile);
			writer.WriteAttributeString("failonerror", "false");
			writer.WriteEndElement(); // delete

			writer.WriteStartElement("delete");
			writer.WriteAttributeString("file", "${build.dir}\\" + project.AssemblyName + ".pdb");
			writer.WriteAttributeString("failonerror", "false");
			writer.WriteEndElement(); // delete

			foreach (File file in project.GetResXResourceFiles())
			{
				string path =
					project.RootNamespace +
					"." +
					Path.GetFileNameWithoutExtension(file.RelativePath) +
					".resources";

				writer.WriteStartElement("delete");
				writer.WriteAttributeString("file", "${build.dir}\\" + path);
				writer.WriteAttributeString("failonerror", "false");
				writer.WriteEndElement(); // delete
			}

			foreach (File file in project.GetNonResXResourceFiles())
			{
				writer.WriteStartElement("delete");
				writer.WriteAttributeString("file", file.RelativePathFromSolutionDirectory + file.ResourceName);
				writer.WriteAttributeString("failonerror", "false");
				writer.WriteEndElement(); // delete
			}
		}

		public void WriteEndCleanTarget()
		{
			writer.WriteEndElement(); // target Clean
		}

		public void WriteEndSolution()
		{
			writer.WriteEndElement(); // project
			//writer.WriteEndDocument();
		}
	}
}
