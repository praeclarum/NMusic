﻿using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AudioToolbox;
using AudioUnit;
using CoreMidi;
using Foundation;
using UIKit;
using System.Threading;
using Praeclarum.UI;
using NMusic;

namespace Player
{
	/// <summary>
	/// A little class showing how to use iOS's AudioToolbox
	/// to synthesize audio dynamically and from MIDI files.
	/// </summary>
	public class PlayerViewController : UIViewController
	{
		int mixNode;

		UIButton nextButton;

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			nextButton = UIButton.FromType (UIButtonType.RoundedRect);
			nextButton.SetTitle ("Next", UIControlState.Normal);
			nextButton.SetTitleColor (UIColor.FromHSB(60.0f / 360.0f, 0.5f, 1.0f), UIControlState.Normal);
			nextButton.Font = UIFont.SystemFontOfSize (24);
			nextButton.TouchUpInside += delegate {
				Begin ();
			};
			View.AddSubview (nextButton);
			View.ConstrainLayout (() =>
				nextButton.Frame.GetCenterX () == View.Frame.GetCenterX () &&
				nextButton.Frame.GetCenterY () == View.Frame.GetCenterY () &&
				nextButton.Frame.Width == 200 &&
				nextButton.Frame.Height == 88 &&
				true);

			Begin ();
		}

		public override UIStatusBarStyle PreferredStatusBarStyle ()
		{
			return UIStatusBarStyle.LightContent;
		}

		CancellationTokenSource cs;

		void Begin ()
		{
			if (cs != null) {
				cs.Cancel ();
				cs = null;
			}
			NSTimer.CreateScheduledTimer (TimeSpan.FromSeconds(1), async _ => {
				cs = new CancellationTokenSource ();
				await PlayContinuouslyAsync (cs.Token);
			});
		}

		async Task PlayContinuouslyAsync (CancellationToken token)
		{
			while (!token.IsCancellationRequested) {
				using (var graph = CreateAudioGraph())
				{
					var song = new Song(graph, mixNode);
					graph.Initialize();
					graph.Start();
					try
					{
						await Task.Run(() => song.PlayAsync(token).Wait(token));
					}
					catch (OperationCanceledException)
					{
					}
					finally
					{
						graph.Stop();
					}
				}
			}
		}

		AUGraph CreateAudioGraph ()
		{
			var graph = new AUGraph ();

			var ioNode = graph.AddNode (AudioComponentDescription.CreateOutput (AudioTypeOutput.Remote));
			var mix = AudioComponentDescription.CreateMixer (AudioTypeMixer.MultiChannel);
			mixNode = graph.AddNode (mix);

			graph.ConnnectNodeInput (mixNode, 0, ioNode, 0);

			graph.Open ();

			var mixUnit = graph.GetNodeInfo (mixNode);
			mixUnit.SetElementCount (AudioUnitScopeType.Input, 5);
//			mixUnit.SetParameter (AudioUnitParameterType.MultiChannelMixerVolume, 1, AudioUnitScopeType.Input, 0);
//			mixUnit.SetParameter (AudioUnitParameterType.MultiChannelMixerVolume, 1, AudioUnitScopeType.Input, 1);
			mixUnit.SetMaximumFramesPerSlice (4096, AudioUnitScopeType.Global, 0);

			return graph;
		}
	}
}

