using System;
using System.IO;
using System.Linq;
using Xamarin.ProjectTools;

namespace MSBuild.Fuzzer
{
	class Program
	{
		static Random random;
		static ProjectBuilder builder;
		static XamarinFormsAndroidApplicationProject application;

		static void Main ()
		{
			var temp = Path.Combine (Path.GetDirectoryName (typeof (Program).Assembly.Location));
			using (builder = new ProjectBuilder (Path.Combine ("temp", "Fuzzer")) {
				AutomaticNuGetRestore = false,
				CleanupAfterSuccessfulBuild = false,
				CleanupOnDispose = true,
				Root = Xamarin.Android.Build.Paths.TestOutputDirectory,
			}) {
				application = new XamarinFormsAndroidApplicationProject ();

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

		static bool ChangePackageName ()
		{
			Console.WriteLine (nameof (ChangePackageName));
			application.PackageName = "com.foo.a" + RandomName ();
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
