#Implementation of UrbanAirship's push API for Xamarin IOS#

For a new project I needed to use the UrbanAirship Push API to send notifications. After downloading the lib I was horrified at the size of the whole thing, doubts aside I wanted to give it a fair chance and started the process of binding the UA lib in a Xamarin.IOS.Binding project.

This resulted in a lot of frustration & anger, at the time there was little proper documentation (or useful troubleshooting) to be found on the 'net, and it just wouldn't work.

I decided to look for a descent C# implementation of the API, which was non existent. So here is my go at it, the implementation as found in the UrbanAirship Lib.

###How to use these files###
- Clone this repo to your local drive
- Copy or link the files in your project
- Initialize the class in the AppDelegate by calling `UAPush.Initialize`. For more configuration options you should see the `UAPushConfig.cs` file.
```
UAPush.Initialize (new PushConfig {
    UAAppKey = /* insert your Urbanairship app key */,
    UAAppSecret = /* insert your Urbanairship app secret */
});
```
- Tell the class to start registering for notifications, it will handle everything with Apple & Urban Airship. This should be done in the `FinishedLaunching` override in the app delegate.
```
UAPush.Instance.RegisterForNotifications();
```
- Handling notifications is no different than any other app. Just override the `ReceivedRemoteNotification()` in your app delegate and bob's your uncle. If you don't feel like writing the code to update the badge number, you can have a look at `UAPush.Instance.HandleNotification(payload, state)`, which can do that for you.


###Notes###
- This library is not finished, and should be used with caution.
- This library uses parts of my personal Util library, which I can't opensource. So I created `Placeholder.cs` that holds the functions relevant to this library.