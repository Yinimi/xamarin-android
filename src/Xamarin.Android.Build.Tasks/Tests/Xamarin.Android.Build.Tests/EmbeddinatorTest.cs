using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Xamarin.ProjectTools;

namespace Xamarin.Android.Build.Tests
{
	public class EmbeddinatorTest : BaseTest
    {
		[Test]
		public void GenerateJavaStubs ()
		{
			var proj = new XamarinAndroidApplicationProject ();
			proj.Imports.Add (new Import (() => @"$(MSBuildExtensionsPath)\Xamarin\Android\Xamarin.Android.Embedding.targets"));
			using (var b = CreateApkBuilder ($"temp/{TestContext.CurrentContext.Test.Name}")) {
				b.Target = "GenerateEmbeddedJavaStubs";
				Assert.IsTrue (b.Build (proj), "Build should have succeeded.");
			}
		}
	}
}
