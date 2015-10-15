﻿using System.Runtime.InteropServices;
using Rhino.Display;
using Rhino.Render;

namespace RhinoCycles.Materials
{
	[Guid("E64050E9-521F-44F3-BFDA-EFEAFA73625E")]
	public class TranslucentMaterial : RenderMaterial, ICyclesMaterial
	{
		public override string TypeName { get { return "Translucent Material (DEV)"; } }
		public override string TypeDescription { get { return "Translucent Material (DEV)"; } }

		public float Gamma { get; set; }

		public TranslucentMaterial()
		{
			Fields.Add("diffuse_color", Color4f.White, "Diffuse Color");
		}

		protected override void OnAddUserInterfaceSections()
		{
			AddAutomaticUserInterfaceSection("Parameters", 0);
		}

		public override void SimulateMaterial(ref Rhino.DocObjects.Material simulatedMaterial, bool forDataOnly)
		{
			base.SimulateMaterial(ref simulatedMaterial, forDataOnly);

			Color4f color;
			if (Fields.TryGetValue("diffuse_color", out color))
				simulatedMaterial.DiffuseColor = color.AsSystemColor();
		}

		public override Rhino.DocObjects.Material SimulateMaterial(bool isForDataOnly)
		{
			var m = base.SimulateMaterial(isForDataOnly);

			SimulateMaterial(ref m, isForDataOnly);

			return m;
		}


		public string MaterialXml
		{
			get {
				Color4f color;

				Fields.TryGetValue("diffuse_color", out color);
				color = Color4f.ApplyGamma(color, Gamma);

				return string.Format("<diffuse_bsdf color=\"{0} {1} {2}\" name=\"diff\" />" +
					"<translucent_bsdf color=\"{0} {1} {2}\" name=\"translucent\" />" +
					"<mix_closure name=\"mix\" fac=\"0.5\" />" +
					"<connect from=\"diff bsdf\" to=\"mix closure1\" />" +
					"<connect from=\"translucent bsdf\" to=\"mix closure2\" />" +
					"<connect from=\"mix closure\" to=\"output surface\" />" +
			             " ", color.R, color.G, color.B); }
		}

		public CyclesShader.CyclesMaterial MaterialType
		{
			get { return CyclesShader.CyclesMaterial.Translucent; }
		}
	}
}