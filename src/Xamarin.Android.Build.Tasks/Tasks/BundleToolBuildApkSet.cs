﻿using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;
using Xamarin.Android.Tools;

namespace Xamarin.Android.Tasks
{
	/// <summary>
	/// Invokes `bundletool` to create an APK set (.apks file)
	/// 
	/// Usage: bundletool build-apks --bundle=foo.aab --output=foo.apks
	/// </summary>
	public class BundleToolBuildApkSet : BundleTool
	{
		[Required]
		public string AppBundle { get; set; }

		[Required]
		public string Output { get; set; }

		/// <summary>
		/// This is used to detect the attached device and generate an APK set specifically for it
		/// </summary>
		[Required]
		public string AdbToolPath { get; set; }

		[Required]
		public string Aapt2ToolPath { get; set; }

		[Required]
		public string KeyStore { get; set; }

		[Required]
		public string KeyAlias { get; set; }

		[Required]
		public string KeyPass { get; set; }

		[Required]
		public string StorePass { get; set; }

		public override bool Execute ()
		{
			//NOTE: bundletool will not overwrite
			if (File.Exists (Output))
				File.Delete (Output);

			base.Execute ();

			return !Log.HasLoggedErrors;
		}

		protected override CommandLineBuilder GetCommandLineBuilder ()
		{
			var adb = OS.IsWindows ? "adb.exe" : "adb";
			var aapt2 = OS.IsWindows ? "aapt2.exe" : "aapt2";
			var cmd = base.GetCommandLineBuilder ();
			cmd.AppendSwitch ("build-apks");
			cmd.AppendSwitch ("--connected-device");
			cmd.AppendSwitchIfNotNull ("--bundle ", AppBundle);
			cmd.AppendSwitchIfNotNull ("--output ", Output);
			cmd.AppendSwitchIfNotNull ("--mode ", "default");
			cmd.AppendSwitchIfNotNull ("--adb ", Path.Combine (AdbToolPath, adb));
			cmd.AppendSwitchIfNotNull ("--aapt2 ", Path.Combine (Aapt2ToolPath, aapt2));
			cmd.AppendSwitchIfNotNull ("--ks ", KeyStore);
			cmd.AppendSwitchIfNotNull ("--ks-key-alias ", KeyAlias);
			cmd.AppendSwitchIfNotNull ("--key-pass ", $"pass:{KeyPass}");
			cmd.AppendSwitchIfNotNull ("--ks-pass ", $"pass:{StorePass}");
			return cmd;
		}
	}
}
