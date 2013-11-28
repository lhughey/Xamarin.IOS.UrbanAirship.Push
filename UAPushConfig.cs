using System;
using System.Collections.Generic;

using MonoTouch.UIKit;
using MonoTouch.Foundation;

namespace Xamarin.iOS.UrbanAirship.Push
{
	public class UAPushConfig
	{
		public UAPushConfig() {
			// Set defaults
			Badge = -1;
			Alias = null;
			Tags = new List<string>();
			TimeZone = NSTimeZone.LocalTimeZone.ToString ();
			QuiettimeStart = DateTime.MinValue;
			QuiettimeEnd = DateTime.MinValue;
			NotificationTypes = UIRemoteNotificationType.Alert | UIRemoteNotificationType.Sound | UIRemoteNotificationType.Badge;
		}

		// General
		public bool Enabled { get; set; }

		// UA API Settings
		public string PackageName { get; set; }
		public string UAAppKey { get; set; }
		public string UAAppSecret { get; set; }

		// Push messaging settings
		public UIRemoteNotificationType NotificationTypes { get; set; }
		public string Alias { get; set; }
		public List<string> Tags { get; set; }
		public int Badge { get; set; }
		public DateTime QuiettimeStart { get; set; }
		public DateTime QuiettimeEnd { get; set; }
		public string TimeZone { get; set; }
	}
}

