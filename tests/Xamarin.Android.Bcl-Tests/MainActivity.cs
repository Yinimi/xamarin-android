using System;
using System.Collections.Generic;

using Android.App;
using Android.Widget;
using Android.OS;
using Java.Interop;

using Xamarin.Android.NUnitLite;

namespace Xamarin.Android.BclTests
{
	[Activity (
			Label = "Xamarin.Android BCL Tests",
			MainLauncher = true,
			Name = "xamarin.android.bcltests.MainActivity")]
	public class MainActivity : TestSuiteActivity
	{
		public MainActivity ()
		{
			GCAfterEachFixture = true;
		}

		protected override void OnCreate (Bundle bundle)
		{
			App.ExtractBclTestFiles ();

			foreach (var tests in App.GetTestAssemblies ()) {
				AddTest (tests);
			}

			// Once you called base.OnCreate(), you cannot add more assemblies.
			base.OnCreate (bundle);
		}

		protected override IEnumerable<string> GetExcludedCategories ()
		{
			return App.GetExcludedCategories ();
		}

		protected override void UpdateFilter ()
		{
			Filter = App.UpdateFilter (Filter);
		}

		public override bool IsRunning {
			[Export ("IsRunning")]
			get => base.IsRunning;
		}

		public override string TestRunFailure {
			[Export ("TestRunFailure")]
			get => base.TestRunFailure;
		}

		public override string EncodedTestResults {
			[Export ("EncodedTestResults")]
			get => base.EncodedTestResults;
		}

		[Export ("StartTests")]
		public override void StartTests () => base.StartTests ();
	}
}

