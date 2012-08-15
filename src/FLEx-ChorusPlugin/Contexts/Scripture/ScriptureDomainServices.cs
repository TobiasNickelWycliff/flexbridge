﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FLEx_ChorusPlugin.Infrastructure;
using Palaso.Progress.LogBox;

namespace FLEx_ChorusPlugin.Contexts.Scripture
{
	/// <summary>
	/// This domain services class interacts with the Scripture bounded contexts.
	/// </summary>
	internal static class ScriptureDomainServices
	{
		internal static void WriteNestedDomainData(IProgress progress, bool writeVerbose, string rootDir,
			IDictionary<string, SortedDictionary<string, string>> classData,
			Dictionary<string, string> guidToClassMapping)
		{
/*
		BC 1. ScrRefSystem (no owner)
			Books prop owns seq of ScrBookRef (which has all basic props).
			No other props.
			[Put all in one file in a subfolder of Scripture?]
			[[Nesting]]

		BC 2. CheckLists prop on LP that holds col of CmPossibilityList items.
			Each CmPossibilityList will hold ChkTerm (called ChkItem in model file) objects (derived from CmPossibility)
			[Store each list in a file. Put all lists in subfolder.]
			[[Nesting]]

		BC 3. Scripture (owned by LP)
			Leave in:
				ScriptureBooks prop owns seq of ScrBook, so leave as nested.
				BookAnnotations prop owns seq of ScrBookAnnotations. [Leave here.]
				NoteCategories prop owns one CmPossibilityList [Leave.]
				Resources prop owns col of CmResource. [Leave.]
			Extract:
		BC 4.		ArchivedDrafts prop owns col of ScrDraft. [Each ScrDraft goes in its own file.
						Archived stuff goes into subfolder of Scripture.]
		BC 5.		Styles props owns col of StStyle. [Put styles in subfolder and one for each style.]
		BC 6.		ImportSettings prop owns col of ScrImportSet.  [Put sets in subfolder and one for each set.]
*/
			var scriptureBaseDir = Path.Combine(rootDir, SharedConstants.Other);
			if (!Directory.Exists(scriptureBaseDir))
				Directory.CreateDirectory(scriptureBaseDir);

			if (writeVerbose)
				progress.WriteVerbose("Writing the other data....");
			else
				progress.WriteMessage("Writing the other data....");
			ScriptureReferenceSystemBoundedContextService.NestContext(scriptureBaseDir, classData, guidToClassMapping);
			var langProj = XElement.Parse(classData["LangProject"].Values.First());
			ScriptureCheckListsBoundedContextService.NestContext(langProj, scriptureBaseDir, classData, guidToClassMapping);

			// These are intentionally out of order from the above numbering scheme.
			var scrAsString = classData[SharedConstants.Scripture].Values.FirstOrDefault();
			// // Lela Teli-3 has null.
			if (!string.IsNullOrEmpty(scrAsString))
			{
				var scripture = XElement.Parse(scrAsString);
				ArchivedDraftsBoundedContextService.NestContext(scripture.Element(SharedConstants.ArchivedDrafts), scriptureBaseDir, classData, guidToClassMapping);
				ScriptureStylesBoundedContextService.NestContext(scripture.Element(SharedConstants.Styles), scriptureBaseDir, classData, guidToClassMapping);
				ImportSettingsBoundedContextService.NestContext(scripture.Element(SharedConstants.ImportSettings), scriptureBaseDir, classData, guidToClassMapping);
				ScriptureBoundedContextService.NestContext(langProj, scripture, scriptureBaseDir, classData, guidToClassMapping);
			}

			RemoveFolderIfEmpty(scriptureBaseDir);
		}

		internal static void FlattenDomain(
			IProgress progress, bool writeVerbose,
			SortedDictionary<string, XElement> highLevelData,
			SortedDictionary<string, XElement> sortedData,
			string rootDir)
		{
			var scriptureBaseDir = Path.Combine(rootDir, SharedConstants.Other);
			if (!Directory.Exists(scriptureBaseDir))
				return;

			if (writeVerbose)
				progress.WriteVerbose("Collecting the other data....");
			else
				progress.WriteMessage("Collecting the other data....");
			ScriptureReferenceSystemBoundedContextService.FlattenContext(highLevelData, sortedData, scriptureBaseDir);
			ScriptureCheckListsBoundedContextService.FlattenContext(highLevelData, sortedData, scriptureBaseDir);

			// Have to flatten the main Scripture context before the rest, since the main context owns the other four.
			// The main obj gets stuffed into highLevelData, so the owned stuff can have owner guid restored.
			ScriptureBoundedContextService.FlattenContext(highLevelData, sortedData, scriptureBaseDir);
			if (highLevelData.ContainsKey(SharedConstants.Scripture))
			{
				ArchivedDraftsBoundedContextService.FlattenContext(highLevelData, sortedData, scriptureBaseDir);
				ScriptureStylesBoundedContextService.FlattenContext(highLevelData, sortedData, scriptureBaseDir);
				ImportSettingsBoundedContextService.FlattenContext(highLevelData, sortedData, scriptureBaseDir);
			}
		}

		internal static void RemoveBoundedContextData(string pathRoot)
		{
			BaseDomainServices.RemoveBoundedContextDataCore(Path.Combine(pathRoot, SharedConstants.Other));
		}

		private static void RemoveFolderIfEmpty(string scriptureDir)
		{
			if (!Directory.Exists(scriptureDir))
				return;

			if (Directory.GetDirectories(scriptureDir).Length == 0 && Directory.GetFiles(scriptureDir).Length == 0)
				Directory.Delete(scriptureDir);
		}
	}
}