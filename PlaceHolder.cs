using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

using MonoTouch.UIKit;
using MonoTouch.Foundation;

namespace Xamarin.iOS.UrbanAirship.Push
{
	public static class Util 
	{
		public static Dictionary<string, object> ConvertToDictionary (NSDictionary toConvert) {
			Dictionary<string, object> convertedDictionary = new Dictionary<string, object> ();
			foreach (NSString oKey in toConvert.Keys) {
				string cKey = (string) ((NSString) oKey);
				string cValue = toConvert.ValueForKey (oKey).ToString();
				convertedDictionary.Add (cKey, cValue);
			}

			return convertedDictionary;
		}

		#region Device helpers
		public static void RunOnUIThread (Action action) {
			UIDevice.CurrentDevice.InvokeOnMainThread (delegate { action (); });
		}
		public static bool IsRunningInSimulator() {
			return MonoTouch.ObjCRuntime.Runtime.Arch == MonoTouch.ObjCRuntime.Arch.SIMULATOR;
		}
		private static StackFrame GetCurrentStackFrame() {
			StackFrame frame;
			try {
				if (Util.IsRunningInSimulator ()) {
					frame = new StackFrame (2);
				} else {
					frame = new StackFrame (1);
				}
			} catch {
				Util.Debug("StackFrame not high enough!");
				frame = new StackFrame (0);
			}

			return frame;
		}
		#endregion

		#region Debug helpers
		/// <summary>
		/// Assert the specified condition and if it fails writes a message to the console.<br />
		/// Aslo appends the current classname and methodname to the message.
		/// </summary>
		/// <param name='condition'>The condition to test</param>
		/// <param name='message'>The message to display when the condition failes. Defaults to "An Assertion failed!"</param>
		/// <exception cref="AssertionFailedException">Spawned when the <see cref="condition" /> is not met.</exception>
		public static void Assert (bool condition, string message = "An assertion failed!") {
			string classname = GetCurrentStackFrame ().GetMethod ().DeclaringType.Name;
			string methodname = GetCurrentStackFrame ().GetMethod ().Name;
			message = string.Format ("{0}.{1}: {2}", classname, methodname, message, System.DateTime.Now);
			if (!condition) {
				Debug (message);
				throw new AssertionFailedException(message);
			}
		}

		/// <summary>
		/// Writes a log line with just the classname + method name.
		/// </summary>
		public static void Debug() {
			MethodBase method = new StackFrame (1).GetMethod ();
			string classname = method.DeclaringType.Name;
			string methodname = method.Name;    
			string l = classname + "." + methodname;

			System.Diagnostics.Debug.WriteLine(l);
		}
		/// <summary>
		/// Logs a message to the console.
		/// </summary>
		/// <param name='msgformat'>The format including the message to use</param>
		/// <param name='obj'>The string objects to put in to the <see cref="msgformat" /></param>
		public static void Debug(string msgformat, params string[] obj) {
			MethodBase method = new StackFrame (1).GetMethod ();
			string classname = method.DeclaringType.Name;
			string methodname = method.Name;    
			string l = classname + "." + methodname + " - " + string.Format(msgformat, obj);

			System.Diagnostics.Debug.WriteLine (l);
		}
		#endregion

		#region Reflection helpers
		private static Dictionary<string, List<PropertyInfo>> CachedProperties = new Dictionary<string, List<PropertyInfo>> ();

		/// <summary>
		/// Synchronizes the properties between two objects (of the same kind).
		/// To avoid a major systems strain caching is enabled (the first time the object passes through the propertyinfo's are stored in a dictionary).
		/// </summary>
		/// <typeparam name="T">Type of the to be synchronized objects.</typeparam>
		/// <param name="target">Object to place the data in.</param>
		/// <param name="source">Object to get the data from.</param>
		public static void SyncProperties<T> (T target, T source) {
			SyncProperties (target, source, null);
		}
		/// <summary>
		/// Synchronizes the properties between two objects (of the same kind).
		/// To avoid a major systems strain caching is enabled (the first time the object passes through the propertyinfo's are stored in a dictionary).
		/// </summary>
		/// <typeparam name="T">Type of the to be synchronized objects.</typeparam>
		/// <param name="target">Object to place the data in.</param>
		/// <param name="source">Object to get the data from.</param>
		/// <param name="excluded">String array of the names(!) of properties to excluded from synchronisation.</param>
		public static void SyncProperties<T> (T target, T source, string[] excluded) {
			var key = typeof (T).ToString ();
			if (!CachedProperties.ContainsKey (key))
				CacheProperties<T> (key);

			var sourceProperties = CachedProperties[key];
			foreach (var sourceProperty in sourceProperties) {
				if (excluded != null && excluded.Contains (sourceProperty.Name))
					continue;

				if (sourceProperty.CanRead) {
					if (sourceProperty.CanWrite) {
						var sourceValue = sourceProperty.GetValue (source, BindingFlags.Public, null, null, null);
						sourceProperty.SetValue (target, sourceValue, null);
					}
				}
			}
		}

		private static void CacheProperties<T> (string key) {
			var properties = typeof (T).GetProperties ();
			CachedProperties.Add (key, properties.ToList ());
		}
		#endregion
	}

	public class AssertionFailedException : ArgumentException
	{
		public string Title { get; private set; }

		public AssertionFailedException (string message) : base (message) { }
		public AssertionFailedException (string title, string message) : base (message) {
			this.Title = title;
		}
	}
}

