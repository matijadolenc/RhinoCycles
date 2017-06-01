﻿/**
Copyright 2014-2017 Robert McNeel and Associates

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
**/

using System;
using System.Drawing;
using ccl;
using Rhino.Render;
using RhinoCyclesCore.Core;
using sdd = System.Diagnostics.Debug;

namespace RhinoCyclesCore.RenderEngines
{
	public class PreviewRenderEngine : RenderEngine
	{

		/// <summary>
		/// Construct a render engine for preview rendering
		/// </summary>
		/// <param name="createPreviewEventArgs"></param>
		/// <param name="pluginId">Id of the plugin for which the render engine is created</param>
		public PreviewRenderEngine(CreatePreviewEventArgs createPreviewEventArgs, Guid pluginId) : base (pluginId, createPreviewEventArgs, false)
		{
			Client = new Client();
			State = State.Rendering;

#region create callbacks for Cycles
			m_update_callback = UpdateCallback;
			m_update_render_tile_callback = PreviewRendererUpdateRenderTileCallback;
			m_write_render_tile_callback = PreviewRendererWriteRenderTileCallback;
			m_test_cancel_callback = TestCancel;

			CSycles.log_to_stdout(false);
#endregion
		}

		public void PreviewRendererUpdateRenderTileCallback(uint sessionId, uint x, uint y, uint w, uint h, uint sample, uint depth, PassType passtype, float[] pixels, int pixlen)
		{
			if (IsStopped || sample < 5 || (Session.Scene.Device.IsCpu && sample % 10 != 0)) return;
			DisplayBuffer(sessionId, x, y, w, h, passtype, ref pixels, pixlen, (int)depth);
			PreviewEventArgs.PreviewNotifier.NotifyIntermediateUpdate(RenderWindow);
		}

		public void PreviewRendererWriteRenderTileCallback(uint sessionId, uint x, uint y, uint w, uint h, uint sample, uint depth, PassType passtype, float[] pixels, int pixlen)
		{
			if (IsStopped || sample < 5 || (Session.Scene.Device.IsCpu && sample % 10 != 0)) return;
			DisplayBuffer(sessionId, x, y, w, h, passtype, ref pixels, pixlen, (int)depth);
			PreviewEventArgs.PreviewNotifier.NotifyIntermediateUpdate(RenderWindow);
		}
		/// <summary>
		/// Renderer entry point for preview rendering
		/// </summary>
		/// <param name="oPipe"></param>
		public static void Renderer(object oPipe)
		{
			var cyclesEngine = (PreviewRenderEngine)oPipe;

			var client = cyclesEngine.Client;

			var size = cyclesEngine.RenderDimension;
			var samples = 500;

			#region pick a render device
			var renderDevice = RcCore.It.EngineSettings.RenderDevice;

			if (RcCore.It.EngineSettings.Verbose) sdd.WriteLine(
				$"Using device {renderDevice.Name + " " + renderDevice.Description}");
#endregion

			if (cyclesEngine.CancelRender) return;

			var scene = CreateScene(client, renderDevice, cyclesEngine);

			#region set up session parameters
			var sessionParams = new SessionParameters(client, renderDevice)
			{
				Experimental = false,
				Samples = samples,
				TileSize = renderDevice.IsCpu ? new Size(32, 32) : new Size(256, 256),
				TileOrder = TileOrder.Center,
				Threads = (uint)(renderDevice.IsCuda ? 0 : RcCore.It.EngineSettings.Threads),
				ShadingSystem = ShadingSystem.SVM,
				Background = true,
				DisplayBufferLinear = true,
				ProgressiveRefine = true,
				Progressive = true,
			};
#endregion

			if (cyclesEngine.CancelRender) return;

#region create session for scene
			cyclesEngine.Session = new Session(client, sessionParams, scene);
#endregion

			// register callbacks before starting any rendering
			cyclesEngine.SetCallbacks();

			// main render loop
			cyclesEngine.m_flush = false;
			cyclesEngine.UploadData();

			// lets first reset session
			cyclesEngine.Session.Reset((uint)size.Width, (uint)size.Height, (uint)samples);
			// then reset scene
			cyclesEngine.Session.Scene.Reset();
			// and actually start
			// we're rendering again
			cyclesEngine.Session.Start();
			// ... aaaaand we wait
			cyclesEngine.Session.Wait();

			cyclesEngine.CancelRender = true;

			// we're done now, so lets clean up our session.
			cyclesEngine.Session.Destroy();
		}

	}

}
