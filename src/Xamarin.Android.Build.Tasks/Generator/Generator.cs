﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Microsoft.Build.Utilities;

using Java.Interop.Tools.Diagnostics;
using Java.Interop.Tools.JavaCallableWrappers;
using System.IO;

namespace Xamarin.Android.Tasks
{
	class Generator
	{
		public static bool CreateJavaSources (TaskLoggingHelper log, IEnumerable<TypeDefinition> javaTypes, string outputPath, string applicationJavaClass, bool useSharedRuntime, bool generateOnCreateOverrides, bool hasExportReference)
		{
			bool ok = true;
			foreach (var t in javaTypes) {
				try {
					GenerateJavaSource (log, t, outputPath, applicationJavaClass, useSharedRuntime, generateOnCreateOverrides, hasExportReference);
				} catch (XamarinAndroidException xae) {
					ok = false;
					log.LogError (
							subcategory:      "",
							errorCode:        "XA" + xae.Code,
							helpKeyword:      string.Empty,
							file:             xae.SourceFile,
							lineNumber:       xae.SourceLine,
							columnNumber:     0,
							endLineNumber:    0,
							endColumnNumber:  0,
							message:          xae.MessageWithoutCode,
							messageArgs:      new object [0]
					);
				}
			}
			return ok;
		}

		static void GenerateJavaSource (TaskLoggingHelper log, TypeDefinition t, string outputPath, string applicationJavaClass, bool useSharedRuntime, bool generateOnCreateOverrides, bool hasExportReference)
		{
			try {
				var jti = new JavaCallableWrapperGenerator (t, log.LogWarning) {
					UseSharedRuntime = useSharedRuntime,
					GenerateOnCreateOverrides = generateOnCreateOverrides,
					ApplicationJavaClass        = applicationJavaClass,
				};

				using (var memoryStream = new MemoryStream ())
				using (var writer = new StreamWriter (memoryStream)) {
					jti.Generate (writer);

					writer.Flush ();
					var path = jti.GetDestinationPath (outputPath);
					MonoAndroidHelper.CopyIfStreamChanged (memoryStream, path);
				}
				if (jti.HasExport && !hasExportReference)
					Diagnostic.Error (4210, "You need to add a reference to Mono.Android.Export.dll when you use ExportAttribute or ExportFieldAttribute.");
			} catch (Exception ex) {
				if (ex is XamarinAndroidException)
					throw;
				Diagnostic.Error (4209, "Failed to create JavaTypeInfo for class: {0} due to {1}", t.FullName, ex);
			}
		}
	}
}
