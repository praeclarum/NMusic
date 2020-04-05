using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;
using AVFoundation;

namespace Player
{
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		UIWindow window;

        public override UIWindow Window
        {
            get => window;
			set { }
        }

        public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			AVAudioSession.SharedInstance ().SetCategory (AVAudioSession.CategoryPlayback, out var error);

            window = new UIWindow(UIScreen.MainScreen.Bounds)
            {
                RootViewController = new PlayerViewController()
            };

            window.MakeKeyAndVisible ();
			
			return true;
		}
	}
}

