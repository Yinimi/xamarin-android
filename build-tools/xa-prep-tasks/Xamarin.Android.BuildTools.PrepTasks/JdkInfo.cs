using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xamarin.Android.Build.Utilities;

namespace Xamarin.Android.BuildTools.PrepTasks
{
	public class JdkInfo : Task
	{
		[Required]
		public ITaskItem Output { get; set; }

		public override bool Execute ()
		{
			Log.LogMessage (MessageImportance.Low, $"Task {nameof (JdkInfo)}");
			Log.LogMessage (MessageImportance.Low, $"  {nameof (Output)}: {Output}");

			AndroidLogger.Error += (task, message) => Log.LogError ($"{task}: {message}");
			AndroidLogger.Warning += (task, message) => Log.LogWarning ($"{task}: {message}");
			AndroidLogger.Info += (task, message) => Log.LogMessage ($"{task}: {message}");
			AndroidSdk.Refresh ();

			var javaSdkPath = AndroidSdk.JavaSdkPath;
			Log.LogMessage (MessageImportance.Low, $"  {nameof (AndroidSdk.JavaSdkPath)}: {javaSdkPath}");

			var jvmPath = Path.Combine (javaSdkPath, "jre", "bin", "server", "jvm.dll");
			if (!File.Exists(jvmPath)) {
				Log.LogError ($"JdkJvmPath not found at {0}", jvmPath);
				return false;
			}

			var javaIncludePath = Path.Combine (javaSdkPath, "include");
			var includes = new List<string> { javaIncludePath };
			includes.AddRange (Directory.GetDirectories (javaIncludePath)); //Include dirs such as "win32"

			var includeXmlTags = new StringBuilder ();
			foreach (var include in includes) {
				includeXmlTags.AppendLine ($"<JdkIncludePath Include=\"&quot;{include}&quot;\" />");
			}

			var directory = Path.GetDirectoryName (Output.ItemSpec);
			if (!Directory.Exists (directory))
				Directory.CreateDirectory (directory);

			File.WriteAllText (Output.ItemSpec, $@"<Project xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Choose>
    <When Condition="" '$(JdkJvmPath)' == '' "">
      <PropertyGroup>
        <JdkJvmPath>&quot;{jvmPath}&quot;</JdkJvmPath>
      </PropertyGroup>
      <ItemGroup>
        {includeXmlTags}
      </ItemGroup>
    </When>
  </Choose>
</Project>");

			return !Log.HasLoggedErrors;
		}
	}
}
