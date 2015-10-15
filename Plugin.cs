using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ccl;
using Rhino;
using Rhino.Render;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rhino.PlugIns;
using Transform = ccl.Transform;

namespace RhinoCycles
{
	public partial class Plugin : Rhino.PlugIns.RenderPlugIn
	{
		#region helper functions to get relative path between two paths
		private const int FILE_ATTRIBUTE_DIRECTORY = 0x10;
		public static string GetRelativePath(string fromPath, string toPath)
		{

			var path = new StringBuilder();
			if (PathRelativePathTo(path,
				fromPath, FILE_ATTRIBUTE_DIRECTORY,
				toPath, FILE_ATTRIBUTE_DIRECTORY) == 0)
			{
				throw new ArgumentException("Paths must have a common prefix");
			}
			return path.ToString();
		}

		[DllImport("shlwapi.dll", SetLastError = true)]
		private static extern int PathRelativePathTo(StringBuilder pszPath,
				string pszFrom, int dwAttrFrom, string pszTo, int dwAttrTo);
		#endregion

		/// <summary>
		/// Flag to keep track of CSycles initialisation
		/// </summary>
		public static bool Initialised { get; set; }

		/// <summary>
		/// Get the path used to look up .cubins (absolute)
		/// </summary>
		public static string KernelPath { get; private set; }

		/// <summary>
		/// Get the path where runtime created data like compiled kernels and BVH caches are stored.
		/// </summary>
		public static string DataUserPath { get; private set; }

		/// <summary>
		/// Get the path used to look up .cubins (relative)
		/// </summary>
		public static string KernelPathRelative { get; private set; }


		public static string PluginPath { get; private set; }

		public static string AppPath { get; private set; }

		/// <summary>
		/// Make sure we load AtStartup so that our view mode is
		/// available even when RhinoCycles isn't the current renderer
		/// </summary>
		public override PlugInLoadTime LoadTime
		{
			get
			{
				return PlugInLoadTime.AtStartup;
			}
		}

		protected override Rhino.PlugIns.LoadReturnCode OnLoad(ref string errorMessage)
		{
			RhinoApp.WriteLine("RhinoCycles uses open source libraries that are available at www.rhino3d.com/opensource");

			RenderContent.RegisterContent(this);
			RenderedDisplayMode.RegisterDisplayModes(this);
			RenderedDisplayMode.InstallCyclesDisplayAttributes();

			RhinoApp.WriteLine("RhinoCycles {0}", RhinoBuildConstants.VERSION_STRING);

			RenderContent.ContentFieldChanged += RenderContentOnContentFieldChanged;

			EngineSettings = new EngineSettings();
			EngineSettings.DefaultSettings();

			var path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
			PluginPath = path;
			var kernel_path = Path.Combine(path, "RhinoCycles");
			KernelPath = kernel_path;
			var app_path = Path.GetDirectoryName(System.Reflection.Assembly.GetCallingAssembly().Location);
			AppPath = app_path;
			kernel_path = GetRelativePath(app_path, kernel_path);
			KernelPathRelative = kernel_path;

			var dataPath = RhinoApp.GetDataDirectory(true, true);
			var user_path = Path.Combine(dataPath, "RhinoCycles", "data");

			DataUserPath = user_path;

			CSycles.path_init(KernelPath, DataUserPath);

			Initialised = false;

			return Rhino.PlugIns.LoadReturnCode.Success;
		}

		/// <summary>
		/// Initialise Cycles if necessary.
		/// </summary>
		public static void InitialiseCSycles()
		{
			if (!Initialised)
			{
				CSycles.initialise();
				Initialised = true;
			}
		}

		private void RenderContentOnContentFieldChanged(object sender, RenderContentFieldChangedEventArgs renderContentFieldChangedEventArgs)
		{
			//RhinoApp.WriteLine("... {0}", renderContentFieldChangedEventArgs.FieldName);
		}

		protected override void OnShutdown()
		{
			/* Clean up everything from C[CS]?ycles. */
			CSycles.shutdown();
			base.OnShutdown();
		}

		protected override bool SupportsFeature(RenderFeature feature)
		{
			if (feature == RenderFeature.CustomDecalProperties)
				return false;

			return true;
		}

		protected override Rhino.Commands.Result RenderWindow(RhinoDoc doc, Rhino.Commands.RunMode modes, bool fastPreview, Rhino.Display.RhinoView view, Rectangle rect, bool inWindow)
		{
			return Rhino.Commands.Result.Failure;
		}

		protected override Rhino.Commands.Result RenderQuiet(RhinoDoc doc, Rhino.Commands.RunMode mode, bool fastPreview)
		{
			return Rhino.Commands.Result.Failure;
		}

		/// <summary>
		/// Implement the render entry point.
		/// 
		/// Rhino data is prepared for further conversion in RenderEngine.
		/// </summary>
		/// <param name="doc">Rhino document for which the render command was given</param>
		/// <param name="mode">mode</param>
		/// <param name="fastPreview">True for fast preview.</param>
		/// <returns></returns>
		protected override Rhino.Commands.Result Render(RhinoDoc doc, Rhino.Commands.RunMode mode, bool fastPreview)
		{
			return RenderInternal(doc, mode, fastPreview, false);
		}

		public Rhino.Commands.Result RenderInternal(RhinoDoc doc, Rhino.Commands.RunMode mode, bool fastPreview, bool inViewport)
		{
			InitialiseCSycles();
			AsyncRenderContext a_rc = new RenderEngine(doc, Id);
			var engine = (RenderEngine)a_rc;

			//engine.Background = CreateCyclesBackgroundShader(doc.RenderSettings);

			engine.Settings = EngineSettings;
			engine.Settings.UseInteractiveRenderer = false;
			engine.Settings.SetQuality(doc.RenderSettings.AntialiasLevel);

			/* render only 3 samples if we are told to generate a fast preview. */
			if (fastPreview)
			{
				engine.Settings.Samples = 3;
			}
			// for now when using interactive renderer render indefinitely
			if(engine.Settings.UseInteractiveRenderer) engine.Settings.Samples = ushort.MaxValue;
			var renderSize = Rhino.Render.RenderPipeline.RenderSize(doc);

			var pipe = new RenderPipeline(doc, mode, this, ref a_rc);

			if (inViewport)
			{
				engine.RenderBitmap = new Bitmap(renderSize.Width, renderSize.Height);
			}
			else
			{
				engine.RenderWindow = pipe.GetRenderWindow();
				engine.RenderWindow.AddWireframeChannel(engine.Doc, engine.ViewportInfo, renderSize, new Rectangle(0, 0, renderSize.Width, renderSize.Height));
				engine.RenderWindow.SetSize(renderSize);
			}
			engine.RenderDimension = renderSize;

			engine.Settings.Verbose = true;

			engine.CreateWorld(); // has to be done on main thread, so lets do this just before starting render session

			/* since we're an asynchronous renderer plugin we start the render process
			 * here, but, apart from data conversion and pumping, we fall right through
			 * without a complete render result.
			 */
			var rc = pipe.Render();

			if (Rhino.Render.RenderPipeline.RenderReturnCode.Ok != rc)
			{
				RhinoApp.WriteLine("Rendering failed:" + rc.ToString());
				return Rhino.Commands.Result.Failure;
			}

			return Rhino.Commands.Result.Success;
			
		}

		/// <summary>
		/// Handler for rendering preview thumbnails.
		/// 
		/// The CreatePreviewEventArgs parameter contains a simple
		/// scene description to be rendered. It contains a set of meshes
		/// and lights. Meshes have RenderMaterials attached to them.
		/// </summary>
		/// <param name="scene">The scene description to render, along with the requested quality setting</param>
		protected override void CreatePreview(CreatePreviewEventArgs scene)
		{
			scene.SkipInitialisation();

			InitialiseCSycles();

			if (scene.Quality == PreviewSceneQuality.RealtimeQuick)
			{
				scene.PreviewImage = null;
				return;
			}

			AsyncRenderContext a_rc = new RenderEngine(scene, Id);
			var engine = (RenderEngine)a_rc;
			engine.Settings = EngineSettings;
			engine.Settings.SetQuality(scene.Quality);

			engine.RenderDimension = scene.PreviewImageSize;
			engine.RenderWindow = null;

			// New preview bitmap
			engine.RenderBitmap = new Bitmap(scene.PreviewImageSize.Width, scene.PreviewImageSize.Height);

			engine.CreateWorld();

			/* render the preview scene */
			RenderEngine.PreviewRenderer(engine);

			/* set final preview bitmap, or null if cancelled */
			scene.PreviewImage = scene.Cancel ? null : engine.RenderBitmap;
		}
	}
}
