using System;
using System.Threading;
using Android.Graphics;
using Android.Util;
using NUnit.Framework;

namespace Java.LangTests
{
	[TestFixture]
	public class ObjectLifetimeTests
	{
		void RunAndGC (Action action)
		{
			var resetEvent = new ManualResetEvent (false);
			var thread = new Thread (() => {
				action ();

				//Full GC + Finalizers
				GC.Collect ();
				GC.WaitForPendingFinalizers ();

				//Sleep for good measure
				Thread.Sleep (10);

				//Signal
				resetEvent.Set ();
			});
			thread.Start ();

			//Wait for completion
			resetEvent.WaitOne ();

			//Full GC + Finalizers
			GC.Collect ();
			GC.WaitForPendingFinalizers ();
		}

		[Test, Category ("Lifetime")]
		public void BitmapShouldNotLeak ()
		{
			WeakReference weakRef = null;

			RunAndGC (() => {
				var obj = Bitmap.CreateBitmap(1, 1, Bitmap.Config.Argb8888);
				weakRef = new WeakReference(obj, true);
			});

			Assert.IsFalse(weakRef.IsAlive, "Object should not be alive!");
		}

		[Test, Category ("Lifetime")]
		public void StringShouldNotLeak ()
		{
			WeakReference weakRef = null;

			RunAndGC (() => {
				var obj = new Java.Lang.String ();
				weakRef = new WeakReference (obj, true);
			});

			Assert.IsFalse(weakRef.IsAlive, "Object should not be alive!");
		}

		[Test, Category ("Lifetime")]
		public void SubclassShouldNotLeak ()
		{
			Subclass.FinalizerCalled = false;
			Subclass.DisposeCalled = false;
			WeakReference weakRef = null;

			RunAndGC (() => {
				var obj = new Subclass ();
				weakRef = new WeakReference (obj, true);
			});

			Assert.IsFalse (weakRef.IsAlive, "Object should not be alive!");
			Assert.IsTrue (Subclass.DisposeCalled, "Dispose should have been called!");
			Assert.IsTrue (Subclass.FinalizerCalled, "Finalizer should have been called!");
		}

		class Subclass : Java.Lang.Object
		{
			public static bool FinalizerCalled { get; set; }
			public static bool DisposeCalled { get; set; }

			~Subclass ()
			{
				Log.Debug ("Mono.Android", $"~ {nameof(Subclass)}(), Handle={Handle}");
				FinalizerCalled = true;
			}

			public Subclass ()
			{
				Log.Debug ("Mono.Android", $"new {nameof(Subclass)}(), Handle={Handle}");
			}

			protected override void Dispose (bool disposing)
			{
				Log.Debug ("Mono.Android", $"{nameof(Subclass)}.Dispose({disposing})");
				DisposeCalled = true;

				base.Dispose (disposing);
			}
		}
	}
}
