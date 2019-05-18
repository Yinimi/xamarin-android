﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Xamarin.ProjectTools;

namespace MSBuild.Fuzzer
{
	class Program
	{
		static Random random;
		static ProjectBuilder builder;
		static XamarinFormsAndroidApplicationProject application;
		static bool needsInstall = true;

		static void Main ()
		{
			var temp = Path.Combine (Path.GetDirectoryName (typeof (Program).Assembly.Location));
			using (builder = new ProjectBuilder (Path.Combine ("temp", "Fuzzer")) {
				AutomaticNuGetRestore = false,
				CleanupAfterSuccessfulBuild = false,
				CleanupOnDispose = true,
				Root = Xamarin.Android.Build.Paths.TestOutputDirectory,
			}) {
				if (Directory.Exists (builder.ProjectDirectory))
					Directory.Delete (builder.ProjectDirectory, recursive: true);

				application = new XamarinFormsAndroidApplicationProject ();
				application.AndroidManifest = application.AndroidManifest.Replace ("<uses-sdk />", "<uses-sdk android:targetSdkVersion=\"28\" />");
				application.MainActivity = application.DefaultMainActivity.Replace ("//${AFTER_ONCREATE}", "Android.Util.Log.Debug (\"FUZZER\", \"App started!\");");
				var abis = new string [] { "armeabi-v7a", "arm64-v8a", "x86" };
				application.SetProperty (KnownProperties.AndroidSupportedAbis, string.Join (";", abis));

				var success = NuGetRestore ();
				if (!success) {
					Console.WriteLine ("Initial NuGet restore failed!");
					return;
				}

				Func<bool> [] operations = {
					NuGetRestore,
					Build,
					DesignTimeBuild,
					DesignerBuild,
					Install,
					Run,
					ChangePackageName,
					AddClass,
					RemoveClass,
					RenameClass,
					AddResource,
					RemoveResource,
					RenameResource,
				};

				random = new Random ();
				while (true) {
					var operation = operations [random.Next (operations.Length)];
					success = operation ();
					if (!success) {
						Console.WriteLine ("Error!");
						break;
					}
				}
			}

			Console.WriteLine ("Press enter to exit...");
			Console.ReadLine ();
		}

		static bool NuGetRestore ()
		{
			Console.WriteLine (nameof (NuGetRestore));
			return builder.RunTarget (application, "Restore", doNotCleanupOnUpdate: true);
		}

		static bool Build ()
		{
			Console.WriteLine (nameof (Build));
			return builder.Build (application, doNotCleanupOnUpdate: true);
		}

		static bool DesignTimeBuild ()
		{
			Console.WriteLine (nameof (DesignTimeBuild));
			return builder.DesignTimeBuild (application, doNotCleanupOnUpdate: true);
		}

		static readonly string [] DesignerParameters = new [] { "DesignTimeBuild=True", "AndroidUseManagedDesignTimeResourceGenerator=False" };

		static bool DesignerBuild ()
		{
			Console.WriteLine (nameof (DesignerBuild));
			return builder.RunTarget (application, "SetupDependenciesForDesigner", doNotCleanupOnUpdate: true, parameters: DesignerParameters);
		}

		static bool Install ()
		{
			Console.WriteLine (nameof (Install));
			if (!builder.RunTarget (application, "Install")) {
				return false;
			}
			needsInstall = false;
			return true;
		}

		static bool Run ()
		{
			Console.WriteLine (nameof (Run));
			if (needsInstall && !Install ())
				return false;
			Adb ($"shell killall -9 {application.PackageName}", ignoreExitCode: true);
			Adb ("logcat -c");
			string activity = $"{application.PackageName}/md52d9cf6333b8e95e8683a477bc589eda5.MainActivity";
			string start = Adb ($"shell am start -n {activity}");
			Console.WriteLine (start.Trim ());
			//Wait for the app to start
			Thread.Sleep (3000);
			string logcat = Adb ("logcat -d");
			if (!logcat.Contains ("App started!")) {
				Console.WriteLine (logcat);
				throw new Exception ($"Activity {activity} did not start!");
			}
			return true;
		}

		static string Adb (string arguments, bool ignoreExitCode = false)
		{
			var info = new ProcessStartInfo {
				FileName = "adb",
				Arguments = arguments,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
			};
			var builder = new StringBuilder ();
			using (var process = new Process ()) {
				process.StartInfo = info;
				process.OutputDataReceived += (sender, e) => builder.AppendLine (e.Data);
				process.ErrorDataReceived += (sender, e) => builder.AppendLine (e.Data);
				process.Start ();
				process.BeginErrorReadLine ();
				process.BeginOutputReadLine ();
				process.WaitForExit ();

				if (!ignoreExitCode && process.ExitCode != 0) {
					Console.WriteLine (builder);
					throw new Exception ($"Adb exited with code: {process.ExitCode}");
				}
			}
			return builder.ToString ();
		}

		static bool ChangePackageName ()
		{
			Console.WriteLine (nameof (ChangePackageName));
			application.PackageName = "com.foo.a" + RandomName ();
			application.Touch ("Properties\\AndroidManifest.xml");
			needsInstall = true;
			return true;
		}

		static bool AddClass ()
		{
			Console.WriteLine (nameof (AddClass));
			application.Sources.Add (new Class ());
			return true;
		}

		static bool RemoveClass ()
		{
			Console.WriteLine (nameof (RemoveClass));
			for (int i = application.Sources.Count - 1; i >= 0; i--) {
				if (application.Sources [i] is Class) {
					application.Sources.RemoveAt (i);
					break;
				}
			}
			return true;
		}

		static bool RenameClass ()
		{
			Console.WriteLine (nameof (RemoveClass));
			var clazz = application.Sources.OfType<Class> ().FirstOrDefault ();
			if (clazz != null) {
				clazz.Rename ();
			}
			return true;
		}

		static bool AddResource ()
		{
			Console.WriteLine (nameof (AddResource));
			application.Sources.Add (new AndroidResource ());
			return true;
		}

		static bool RemoveResource ()
		{
			Console.WriteLine (nameof (RemoveResource));
			for (int i = application.Sources.Count - 1; i >= 0; i--) {
				if (application.Sources [i] is AndroidResource) {
					application.Sources.RemoveAt (i);
					break;
				}
			}
			return true;
		}

		static bool RenameResource ()
		{
			Console.WriteLine (nameof (RenameResource));
			var resource = application.Sources.OfType<AndroidResource> ().FirstOrDefault ();
			if (resource != null) {
				resource.Rename ();
			}
			return true;
		}

		static string RandomName () => Guid.NewGuid ().ToString ("N");

		class Class : BuildItem.Source
		{
			public string TypeName { get; set; }

			public Class () : base (RandomName () + ".cs")
			{
				Rename ();
				TextContent = () => $"public class Foo{TypeName} : Java.Lang.Object {{ }}";
			}

			public void Rename ()
			{
				TypeName = RandomName ();
				Timestamp = null;
			}
		}

		class AndroidResource : BuildItem
		{
			public string ResourceId { get; set; }

			public string StringValue { get; set; }

			public AndroidResource () : base ("AndroidResource", RandomName () + ".xml")
			{
				Rename ();
				TextContent = () => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<resources>
	<string name=""foo_{ResourceId}"">{StringValue}</string>
</resources>";
			}

			public void Rename ()
			{
				ResourceId = RandomName ();
				StringValue = RandomName ();
				Timestamp = null;
			}
		}
	}
}