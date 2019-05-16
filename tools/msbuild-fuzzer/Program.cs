using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xamarin.ProjectTools;

namespace MSBuild.Fuzzer
{
	class Program
	{
		static Random random;

		static void Main ()
		{
			var temp = Path.Combine (Path.GetDirectoryName (typeof (Program).Assembly.Location));
			using (var builder = new ProjectBuilder (Path.Combine ("temp", "Fuzzer")) {
				AutomaticNuGetRestore = false,
				CleanupAfterSuccessfulBuild = false,
				CleanupOnDispose = true,
				Root = Xamarin.Android.Build.Paths.TestOutputDirectory,
			}) {
				var project = new XamarinFormsAndroidApplicationProject ();
				var success = NuGetRestore (builder, project);
				if (!success) {
					Console.WriteLine ("Initial NuGet restore failed!");
					return;
				}

				Func<bool> [] operations = {
					() => NuGetRestore (builder, project),
					() => Build (builder, project),
					() => DesignTimeBuild (builder, project),
					() => DesignerBuild (builder, project),
					() => ChangePackageName (builder, project),
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

		static bool NuGetRestore (ProjectBuilder builder, XamarinAndroidApplicationProject project)
		{
			Console.WriteLine (nameof (NuGetRestore));
			return builder.RunTarget (project, "Restore", doNotCleanupOnUpdate: true);
		}

		static bool Build (ProjectBuilder builder, XamarinAndroidApplicationProject project)
		{
			Console.WriteLine (nameof (Build));
			return builder.Build (project, doNotCleanupOnUpdate: true);
		}

		static bool DesignTimeBuild (ProjectBuilder builder, XamarinAndroidApplicationProject project)
		{
			Console.WriteLine (nameof (DesignTimeBuild));
			return builder.DesignTimeBuild (project, doNotCleanupOnUpdate: true);
		}

		static readonly string [] DesignerParameters = new [] { "DesignTimeBuild=True", "AndroidUseManagedDesignTimeResourceGenerator=False" };

		static bool DesignerBuild (ProjectBuilder builder, XamarinAndroidApplicationProject project)
		{
			Console.WriteLine (nameof (DesignerBuild));
			return builder.RunTarget (project, "SetupDependenciesForDesigner", doNotCleanupOnUpdate: true, parameters: DesignerParameters);
		}

		static bool ChangePackageName (ProjectBuilder builder, XamarinAndroidApplicationProject project)
		{
			Console.WriteLine (nameof (ChangePackageName));
			project.PackageName = "com.foo.a" + Guid.NewGuid ().ToString ("N");
			return true;
		}
	}
}
