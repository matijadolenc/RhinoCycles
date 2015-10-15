﻿using System;
using ccl;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Render;
using Rhino.Render.ChangeQueue;
using RhinoCycles.Materials;
using Light = Rhino.Render.ChangeQueue.Light;

using sdd = System.Diagnostics.Debug;

namespace RhinoCycles
{
	public partial class Plugin
	{
		private static bool IsCyclesMaterial(RenderMaterial rm)
		{
			var isit = rm is SimpleBrickMaterial
				|| rm is BrickWithCheckeredMortarMaterial
				|| rm is TestMaterial
				|| rm is TranslucentMaterial
				|| rm is PhongTestMaterial
				|| rm is FlakedCarPaintMaterial
				|| rm is GlassMaterial
				|| rm is SimplePlasticMaterial
				|| rm is DiffuseMaterial;

			return isit;
		}

		/// <summary>
		/// Create a CyclesShader based on given Material m
		/// </summary>
		/// <param name="m">Material to convert to CyclesShader</param>
		/// <returns>The CyclesShader</returns>
		internal static CyclesShader CreateCyclesShader(RenderMaterial rm, float gamma)
		{
			var is_cycles_material = IsCyclesMaterial(rm);
			var mid = rm.RenderHash;
			CyclesShader shader = null;
			ICyclesMaterial crm;

			crm = rm as ICyclesMaterial;

			if (crm == null)
			{
				if (rm.TypeName == "Plaster")
				{
					var plaster = new DiffuseMaterial();
					Color4f c;
					if (rm.Fields.TryGetValue("diffuse", out c))
					{
						plaster.Fields.Set("diffuse", c);
					}
					crm = plaster;
				}
				else if (rm.TypeName == "Glass")
				{
					var glass = new GlassMaterial();
					Color4f c;
					double frost;
					double ior;
					if (rm.Fields.TryGetValue("transparency-color", out c))
					{
						glass.Fields.Set("glass_color", c);
					}
					if (rm.Fields.TryGetValue("frost-amount", out frost))
					{
						glass.Fields.Set("frost-amount", (float) frost);
					}
					if (rm.Fields.TryGetValue("ior", out ior))
					{
						glass.Fields.Set("ior", (float) ior);
					}
					crm = glass;
				}
				else if (rm.TypeName == "Plastic")
				{
					var plastic = new SimplePlasticMaterial();
					Color4f c;
					double f;
					if (rm.Fields.TryGetValue("diffuse", out c))
					{
						plastic.Fields.Set("diffuse", c);
					}
					if (rm.Fields.TryGetValue("frost-amount", out f))
					{
						plastic.Fields.Set("frost-amount", (float) f);
					}
					if (rm.Fields.TryGetValue("polish-amount", out f))
					{
						plastic.Fields.Set("polish-amount", (float) f);
					}
					if (rm.Fields.TryGetValue("reflectivity", out f))
					{
						plastic.Fields.Set("reflectivity", (float) f);
					}
					if (rm.Fields.TryGetValue("transparency", out f))
					{
						plastic.Fields.Set("transparency", (float) f);
					}
					crm = plastic;
				}
				else
				{
					var m = rm.SimulateMaterial(true);
					var dcl = m.DiffuseColor;
					var scl = m.SpecularColor;
					var rcl = m.ReflectionColor;
					var rfcl = m.TransparentColor;
					var emcl = m.EmissionColor;

					var difftex_alpha = m.AlphaTransparency;

					var col = RenderEngine.CreateFloat4(dcl.R, dcl.G, dcl.B, 255);
					var spec = RenderEngine.CreateFloat4(scl.R, scl.G, scl.B, 255);
					var refl = RenderEngine.CreateFloat4(rcl.R, rcl.G, rcl.B, 255);
					var transp = RenderEngine.CreateFloat4(rfcl.R, rfcl.G, rfcl.B, 255);
					var refr = RenderEngine.CreateFloat4(rfcl.R, rfcl.G, rfcl.B, 255);
					var emis = RenderEngine.CreateFloat4(emcl.R, emcl.G, emcl.B, 255);

					var polish = (float) m.ReflectionGlossiness*EngineSettings.PolishFactor;
					shader = new CyclesShader
					{
						Id = mid,
						Type = CyclesShader.Shader.Diffuse,
						CyclesMaterialType = CyclesShader.CyclesMaterial.No,

						Shadeless = m.DisableLighting,

						DiffuseColor = col,
						SpecularColor = spec,
						ReflectionColor = refl,
						ReflectionRoughness = polish,
						RefractionColor = refr,
						RefractionRoughness = (float) m.RefractionGlossiness,
						TransparencyColor = transp,
						EmissionColor = emis,


						FresnelIOR = (float) m.FresnelIndexOfRefraction,
						IOR = (float) m.IndexOfRefraction,
						Roughness = (float) m.Reflectivity, // TODO: expose roughness...
						Reflectivity = (float) m.Reflectivity,
						Transparency = (float) m.Transparency,
						Shine = (float) (m.Shine/Rhino.DocObjects.Material.MaxShine)*2.0f,

						IsCyclesMaterial = false,

						FresnelReflections = m.FresnelReflections,

						Name = m.Name ?? ""
					};

					if (rm != null)
					{
						var diffchan = rm.TextureChildSlotName(RenderMaterial.StandardChildSlots.Diffuse);
						var difftex = rm.FindChild(diffchan) as RenderTexture;
						MaterialBitmapFromEvaluator(ref shader, rm, difftex, diffchan, RenderMaterial.StandardChildSlots.Diffuse);
						if (shader.HasDiffuseTexture)
						{
							shader.DiffuseTexture.UseAlpha = difftex_alpha;
							shader.DiffuseTexture.Amount = (float) Math.Min(rm.ChildSlotAmount(diffchan)/100.0f, 1.0f);
						}

						var bumpchan = rm.TextureChildSlotName(RenderMaterial.StandardChildSlots.Bump);
						var bumptex = rm.FindChild(bumpchan) as RenderTexture;
						MaterialBitmapFromEvaluator(ref shader, rm, bumptex, bumpchan, RenderMaterial.StandardChildSlots.Bump);
						if (shader.HasBumpTexture)
						{
							shader.BumpTexture.Amount = (float) Math.Min(rm.ChildSlotAmount(bumpchan)/100.0f, 1.0f);
						}

						var transchan = rm.TextureChildSlotName(RenderMaterial.StandardChildSlots.Transparency);
						var transtex = rm.FindChild(transchan) as RenderTexture;
						MaterialBitmapFromEvaluator(ref shader, rm, transtex, transchan, RenderMaterial.StandardChildSlots.Transparency);
						if (shader.HasTransparencyTexture)
						{
							shader.TransparencyTexture.Amount = (float) Math.Min(rm.ChildSlotAmount(transchan)/100.0f, 1.0f);
						}
					}
				}
			}
			if (crm != null)
			{
				shader = new CyclesShader
				{
					Id = mid,
					CyclesMaterialType = crm.MaterialType,
					IsCyclesMaterial = true,
					Gamma = gamma,
					Crm = crm
				};
			}

			if(shader != null) shader.Gamma = gamma;

			return shader;
		}

		/// <summary>
		/// Convert a Rhino.Render.ChangeQueue.Light to a CyclesLight
		/// </summary>
		/// <param name="light"></param>
		/// <returns></returns>
		internal static CyclesLight ConvertLight(ChangeQueue changequeue, Rhino.Render.ChangeQueue.Light light, ViewInfo view, float gamma)
		{
			if (changequeue != null && view != null)
			{
				if (light.Data.LightStyle == LightStyle.CameraDirectional)
				{
					ChangeQueue.ConvertCameraBasedLightToWorld(changequeue, light, view);
				}
			}
			var cl = ConvertLight(light.Data, gamma);
			cl.Id = light.Id;

			if (light.ChangeType == Light.Event.Deleted)
			{
				cl.Strength = 0;
			}

			return cl;
		}

		/// <summary>
		/// Convert a Rhino light into a <c>CyclesLight</c>.
		/// </summary>
		/// <param name="lg">The Rhino light to convert</param>
		/// <returns><c>CyclesLight</c></returns>
		internal static CyclesLight ConvertLight(Rhino.Geometry.Light lg, float gamma)
		{
			var enabled = lg.IsEnabled ? 1.0 : 0.0;

			var spotangle = 0.0;
			var smooth = 0.0;
			var size = 0.0f;
			var strength = (float)(lg.Intensity * EngineSettings.PointlightFactor * enabled);
			var axisu = new float4(0.0f);
			var axisv = new float4(0.0f);
			var use_mis = false;
			var sizeu = 0.0f;
			var sizev = 0.0f;

			var co = RenderEngine.CreateFloat4(lg.Location.X, lg.Location.Y, lg.Location.Z);
			var dir = RenderEngine.CreateFloat4(lg.Direction.X, lg.Direction.Y, lg.Direction.Z);
			var color = RenderEngine.CreateFloat4(lg.Diffuse.R, lg.Diffuse.G, lg.Diffuse.B, lg.Diffuse.A);

			var lt = LightType.Point;
			if (lg.IsDirectionalLight)
			{
				lt = LightType.Distant;
				strength = (float)(lg.Intensity * EngineSettings.SunlightFactor * enabled);
				//size = 0.01f;
			}
			else if (lg.IsSpotLight)
			{
				lt = LightType.Spot;
				spotangle = lg.SpotAngleRadians * 2;
				smooth = 1.0 / Math.Max(lg.HotSpot, 0.001f) - 1.0;
				strength = (float)(lg.Intensity * EngineSettings.SpotlightFactor * enabled);
			}
			else if (lg.IsRectangularLight)
			{
				lt = LightType.Area;

				strength = (float)(lg.Intensity * EngineSettings.ArealightFactor * enabled);

				var width = lg.Width;
				var length = lg.Length;

				sizeu = (float)width.Length;
				sizev = (float)length.Length;

				size = 1.0f;

				var rect_loc = lg.Location + (lg.Width * 0.5) + (lg.Length * 0.5);

				co = RenderEngine.CreateFloat4(rect_loc.X, rect_loc.Y, rect_loc.Z);

				width.Unitize();
				length.Unitize();

				axisu = RenderEngine.CreateFloat4(width.X, width.Y, width.Z);
				axisv = RenderEngine.CreateFloat4(length.X, length.Y, length.Z);

				use_mis = true;
			}

			var clight = new CyclesLight
				{
					Type = lt,
					Co = co,
					Dir = dir,
					DiffuseColor = color,
					Size = size,

					SizeU = sizeu,
					SizeV = sizeu,

					AxisU = axisu,
					AxisV = axisv,

					UseMis = use_mis,

					SpotAngle = (float)spotangle,
					SpotSmooth = (float)smooth,

					Strength = strength,

					CastShadow = true,

					Gamma = gamma,

					Id = lg.Id
				};

			return clight;
		}
	}
}
