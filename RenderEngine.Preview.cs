﻿using System;
using System.Drawing;
using ccl;
using ccl.ShaderNodes;
using sdd = System.Diagnostics.Debug;

namespace RhinoCycles
{
	public partial class RenderEngine
	{
		/// <summary>
		/// Renderer entry point for preview rendering
		/// </summary>
		/// <param name="oPipe"></param>
		public static void PreviewRenderer(object oPipe)
		{
			var cycles_engine = (RenderEngine)oPipe;

			var client = cycles_engine.Client;

			var size = cycles_engine.RenderDimension;
			var samples = cycles_engine.Settings.Samples;

			cycles_engine.m_measurements.Reset();

#region pick a render device

			var render_device = cycles_engine.Settings.SelectedDevice == -1
				? Device.FirstCuda
				: Device.GetDevice(cycles_engine.Settings.SelectedDevice);

			if (cycles_engine.Settings.Verbose) sdd.WriteLine(String.Format("Using device {0}", render_device.Name + " " + render_device.Description));
#endregion

			if (cycles_engine.CancelRender) return;

			var scene = CreateScene(client, render_device, cycles_engine);

			#region set up session parameters
			var session_params = new SessionParameters(client, render_device)
			{
				Experimental = false,
				Samples = samples,
				TileSize = render_device.IsCuda ? new Size(256, 256) : new Size(32, 32),
				Threads = (uint)(render_device.IsCuda ? 0 : cycles_engine.Settings.Threads),
				ShadingSystem = ShadingSystem.SVM,
				Background = true,
				ProgressiveRefine = false,
				Progressive = false,
			};
#endregion

			if (cycles_engine.CancelRender) return;

#region create session for scene
			cycles_engine.Session = new Session(client, session_params, scene);
#endregion

			// register callbacks before starting any rendering
			cycles_engine.SetCallbacks();

			// main render loop, including restarts
			cycles_engine.ChangeQueue.OneShot();
			cycles_engine.m_flush = false;
			cycles_engine.UploadData();

			// lets first reset session
			cycles_engine.Session.Reset((uint)size.Width, (uint)size.Height, (uint)samples);
			// then reset scene
			cycles_engine.Session.Scene.Reset();
			// and actually start
			// we're rendering again
			cycles_engine.Session.Start();
			// ... aaaaand we wait
			cycles_engine.Session.Wait();

			cycles_engine.CancelRender = true;

			// we're done now, so lets clean up our session.
			cycles_engine.Session.Destroy();
		}

	}

}
