using System;
using System.Net;
using System.Collections.Generic;
using System.Text;

using MonoTouch.Foundation;
using MonoTouch.ObjCRuntime;
using MonoTouch.UIKit;

namespace Joost.UrbanAirship.Push
{
	public class UAPush
	{
		#region Fields & properties
		// conts
		internal const string UA_DEVICETOKEN = "JOOST_UA_DEVICE_TOKEN";
		internal const string UA_SERVER = "https://go.urbanairship.com/api";
		internal const string SHOULD_UNREG = "JOOST_SHOULDUNREGISTER";

		// fields
		private bool _isRegistering = false;
		private bool _hasEnteredBackground = false;
		private UAPushConfig _config = null;
		private string _cachedRegistrationPayload = "NO_CACHE_YET";
		private NSObject appBecomesActiveObs, appSentToBackgroundObs;

		// properties, settings and getters
		public string DeviceToken { get; set; }
		public bool IsRegistered { get; set; }
		public string NotificationTarget { 
			get {
				if (!string.IsNullOrEmpty (DeviceToken) && IsRegistered)
					return DeviceToken;
				else
					return string.Empty;
			}
		}

		public void UpdateConfiguration (UAPushConfig config) {
			Util.Assert (_config != null);
			Util.SyncProperties<UAPushConfig> (_config, config);
			UIDevice.CurrentDevice.InvokeOnMainThread (() => {
				UpdateRegistration ();
			});
		}
		public void SetPushEnabled (bool val) {
			Util.Assert (_config != null);
			
			if (_config.Enabled != val) {
				_config.Enabled = val;
				
				if (val) {
					Util.Debug("Enabling Push...");
					UIDevice.CurrentDevice.InvokeOnMainThread(() => 
						UIApplication.SharedApplication.RegisterForRemoteNotificationTypes(_config.NotificationTypes)
                    );
				} else {
					Util.Debug("Disabling Push...");
					ShouldUnregister (true);
					UIDevice.CurrentDevice.InvokeOnMainThread(() => {
						UIApplication.SharedApplication.RegisterForRemoteNotificationTypes (UIRemoteNotificationType.None);
						UpdateRegistration ();
					});
				}
			}
		}
		#endregion 


		#region ctor & dtor
		private UAPush () { 
			appBecomesActiveObs = NSNotificationCenter.DefaultCenter.AddObserver (UIApplication.DidBecomeActiveNotification, ApplicationDidBecomeActive);
			appSentToBackgroundObs = NSNotificationCenter.DefaultCenter.AddObserver (UIApplication.DidEnterBackgroundNotification, ApplicationSentToBackground);
		}
		~ UAPush () {
			if (appBecomesActiveObs != null) NSNotificationCenter.DefaultCenter.RemoveObserver (appBecomesActiveObs);
			if (appSentToBackgroundObs != null) NSNotificationCenter.DefaultCenter.RemoveObserver (appSentToBackgroundObs);
		}
		#endregion

		#region UA API Util
		public string GetPayload () {
			string payload = "";
			if (!string.IsNullOrEmpty (_config.Alias))
				payload += string.Format ("\"alias\": \"{0}\"", _config.Alias);
			
			if (_config.Tags != null && _config.Tags.Count > 0) {
				payload += ",\"tags\": [";
				foreach (var tag in _config.Tags) {
					payload += string.Format("\"{0}\"", tag);
				}
				payload += "]";
			}
			
			if (_config.Badge != -1)
				payload += string.Format(",\"badge\": {0}", _config.Badge);
			
			if (_config.QuiettimeStart != DateTime.MinValue && _config.QuiettimeEnd != DateTime.MinValue) {
				payload += string.Format (",\"quiettime\": { \"start\": \"{0}\", \"end\": \"{1}\" }", 
				                         _config.QuiettimeStart.ToString ("HH:mm"), 
				                         _config.QuiettimeEnd.ToString ("HH:mm"));

				if (!string.IsNullOrEmpty(_config.TimeZone))
					payload += string.Format("\"tz\": \"{0}\"", _config.TimeZone);
			}
						
			return payload;
		}
		private bool ShouldUnregister () {
			return NSUserDefaults.StandardUserDefaults.BoolForKey(SHOULD_UNREG);
		}
		private void ShouldUnregister (bool val) {
			if (val != ShouldUnregister ()) {
				NSUserDefaults.StandardUserDefaults.SetBool(val, SHOULD_UNREG);
			}
		}
		#endregion

		#region UA Registration API Calls
		private void UpdateRegistration () {
			if (_isRegistering) {
				Util.Debug ("We're already running a registration. Not running registration.");
				return;
			}

			string registrationPayload = GetPayload ();
			if (_cachedRegistrationPayload != null && _cachedRegistrationPayload == registrationPayload) {
				Util.Debug ("Registration is up to date. Not running registration.");
				return;
			}

			_isRegistering = true;

			if (_config.Enabled) {
				// Register if needed
				if (string.IsNullOrEmpty (DeviceToken)) {
					Util.Debug ("Device token is empty. (Should) retry later!");
					_isRegistering = false;
					return;
				}

				Util.Debug ("Starting registration");
				StartUARegistration ();
			} else {
				// unregister if needed
				if (string.IsNullOrEmpty(DeviceToken)) {
					Util.Debug ("We're either not registered or already unregistered. No need to call UA unregistration!");
					_isRegistering = false;
					return;
				}

				if (ShouldUnregister ()) {
					Util.Debug ("Starting the unregistration call to UA");
					StartUAUnregistraion ();
				} else {
					Util.Debug ("We're already unregistered.");
					_isRegistering = false;
				}
			}
		}
		private void StartUARegistration () {
			Util.Assert (!string.IsNullOrEmpty (_config.UAAppKey));
			Util.Assert (!string.IsNullOrEmpty (_config.UAAppSecret));

			try {
				Uri service_url = new Uri (string.Format ("{0}/{1}/{2}/", UA_SERVER, "device_tokens", DeviceToken));
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create (service_url);
				request.Method = "PUT";
				request.Credentials = new NetworkCredential (_config.UAAppKey, _config.UAAppSecret);
				request.PreAuthenticate = true;
				
				// If we don't have anything extra to tell UA, we shoudn't set the
				//  content-type. The UA Service call will fail otherwise.
				string payload = GetPayload ();
				if (!string.IsNullOrEmpty (payload))
					request.ContentType = "application/json";
				
				using (var request_stream = request.GetRequestStream ()) {
					if (!string.IsNullOrEmpty (payload)) {
						var bytes = Encoding.UTF8.GetBytes (payload);
						request_stream.Write (bytes, 0, bytes.Length);
					}
				}
				
				using (var response = (HttpWebResponse) request.GetResponse ()) {
					if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created) {
						Util.Debug ("Registration succeeded!");
						IsRegistered = true;
					} else {
						IsRegistered = false;
						Util.Debug ("Registration failed with statuscode:'{0}-{1}'", 
						                      response.StatusCode.ToString (), 
						                      response.StatusDescription);
					}
				}
			} catch (Exception ex) {
				Util.Debug ("exception while registering:type:'{0}-{1}'", 
				           ex.GetType ().ToString (), 
				           ex.Message.ToString ());
			} finally {
				_isRegistering = false;
			}
		}
		private void StartUAUnregistraion () {
			try {
				Uri service_url = new Uri (string.Format ("{0}/{1}/{2}", UA_SERVER, "device_tokens", DeviceToken));
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create (service_url);
				request.Method = "DELETE";
				request.Credentials = new NetworkCredential (_config.UAAppKey, _config.UAAppSecret);
				request.PreAuthenticate = true;

				using (var request_stream = request.GetRequestStream ()) { }
				
				using (var response = (HttpWebResponse) request.GetResponse ()) {
					if (response.StatusCode == HttpStatusCode.NoContent) {
						Util.Debug ("Unregistration succeeded!");
						IsRegistered = false;
						ShouldUnregister (false);
					} else {
						Util.Debug ("Unregistration failed with statuscode:'{0}-{1}'", 
						                      response.StatusCode.ToString (), 
						                      response.StatusDescription);
					}
				}
			} catch (Exception ex) {
				Util.Debug ("Exception while unregistering:type:'{0}-{1}'", 
				            ex.GetType ().ToString (), 
				            ex.Message.ToString ());
			} finally {
				_isRegistering = false;
			}
		}
		#endregion 

		// Apple Notifications
		public void HandleNotification (NSDictionary payload, UIApplicationState state) {
			if (state != UIApplicationState.Active) {
				// Standard APNS contents
				if (payload.ContainsKey ((NSString) "aps")) {
					// HOUSTON We have a push notification!
					Util.Debug ("HOUSTON We have a apple push notification!");
					var nsPush = (NSDictionary) payload[(NSString)"aps"];
					var push = Util.ConvertToDictionary (nsPush);

					if (push.ContainsKey ("badge")) {
						int b;
						if (Int32.TryParse(push["badge"].ToString (), out b)) {
							UIDevice.ChangeNewKey.InvokeOnMainThread(() => {
								UIApplication.SharedApplication.ApplicationIconBadgeNumber = b;
							});
						}
					}
				}
			}
		}

		#region Apple Registration API Calls
		public void RegisterForNotifications () {
			Util.Debug ("Starting registration process ...");
			if (!NSThread.Current.IsMainThread) {
				Util.Debug ("We can only register for notifications from the main thread!");
				throw new ArgumentException("We can only register for notifications from the main thread!");
			}

			if (Util.IsRunningInSimulator ()) {
				Util.Debug ("We cannot register for notifications from a simulator!");
				return;
			}

			if (_config == null) {
				Util.Debug("RegisterForNotifications called before initialize!");
				return;
			}

			if (_isRegistering) {
				Util.Debug ("Already in the process registering with UA.");
				return;
			}

			if (_config.Enabled)
				UIApplication.SharedApplication.RegisterForRemoteNotificationTypes (_config.NotificationTypes);
		}
		public void FailedToRegister (NSError error) {
			Util.Debug ("Registration failed w/ exception:'{0}'", error.Description);
		}
		public void RegisterDeviceToken (NSData token) {
			if (_config == null) {
				Util.Debug ("Configuration not found! Use the UAPush.RegisterForNotifications instead of UIApplication equivalant!");
				return;
			}

			// We can't already set the NotificationTarget for UrbanAirship here, because we haven't registered
			//  it with UA yet.
			var devicetoken = token.DebugDescription.ToString ().Replace ('<', ' ').Replace ('>', ' ').Replace (" ", "");
			DeviceToken = devicetoken;

			// Start the registration process if needed
			UpdateRegistration ();
		}
		#endregion

		public static void Initialize (UAPushConfig config) {
			if (_instance != null) {
				Util.Debug ("UA Lib already initialized.");
				return;
			}

			Util.Debug ("Initializing UA Lib ...");
			_instance = new UAPush ();
			_instance._config = config;

			Util.Debug ("UA Lib Initialized!");
		}
				
		private void ApplicationSentToBackground(NSNotification notification) {
			Util.Debug ();
			if (_hasEnteredBackground) {
				_hasEnteredBackground = false;
				
				UpdateRegistration();
			}
		}
		private void ApplicationDidBecomeActive(NSNotification notification) {
			Util.Debug ();
			_hasEnteredBackground = true;
		}

		#region Singleton
		// Singleton
		private static UAPush _instance;
		public static UAPush Instance { 
			get { return _instance; }
		}
		#endregion
	}
}