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

using ccl;
using ccl.ShaderNodes;
using Rhino.Display;
using RhinoCyclesCore.Core;

namespace RhinoCyclesCore.Shaders
{
	public class RhinoBackground : RhinoShader
	{

		public RhinoBackground(Client client, CyclesBackground intermediate, Shader existing) : this(client, intermediate, existing, "background", true)
		{
		}

		public RhinoBackground(Client client, CyclesBackground intermediate, Shader existing, string name, bool recreate) : base(client, intermediate, name, existing, recreate)
		{
		}

		public override Shader GetShader()
		{
			if(RcCore.It.EngineSettings.DebugSimpleShaders)
			{
				var bg = new BackgroundNode();
				bg.ins.Color.Value = new float4(0.7f);
				bg.ins.Strength.Value = 1.0f;

				m_shader.AddNode(bg);

				bg.outs.Background.Connect(m_shader.Output.ins.Surface);
				//m_shader.FinalizeGraph();
				//m_shader.Tag();
			}
			else if (!string.IsNullOrEmpty(m_original_background.Xml))
			{
				var xml = m_original_background.Xml;
				Shader.ShaderFromXml(m_shader, xml, true);
			}
			else
			{
				if (!m_original_background.PreviewBg) {
					var texcoord210 = new TextureCoordinateNode("texcoord");

					var bg_env_texture255 = new EnvironmentTextureNode("bg_env_texture");
					bg_env_texture255.Projection = TextureNode.EnvironmentProjection.Equirectangular;
					bg_env_texture255.ColorSpace = TextureNode.TextureColorSpace.None;
					bg_env_texture255.Extension = TextureNode.TextureExtension.Repeat;
					bg_env_texture255.Interpolation = InterpolationType.Linear;
					bg_env_texture255.IsLinear = false;

					var bg_color_or_texture259 = new MixNode("bg_color_or_texture");
					bg_color_or_texture259.ins.Color1.Value = m_original_background.Color1AsFloat4;
					bg_color_or_texture259.ins.Fac.Value = m_original_background.HasBgEnvTextureAsFloat;
					bg_color_or_texture259.BlendType = MixNode.BlendTypes.Blend;
					bg_color_or_texture259.UseClamp = false;

					var separate_bg_color265 = new SeparateRgbNode("separate_bg_color");

					var skylight_strength_factor299 = new MathMultiply("skylight_strength_factor");
					skylight_strength_factor299.ins.Value1.Value = m_original_background.BgStrength;
					skylight_strength_factor299.ins.Value2.Value = m_original_background.NonSkyEnvStrengthFactor;
					skylight_strength_factor299.Operation = MathNode.Operations.Multiply;
					skylight_strength_factor299.UseClamp = false;

					var factor_r262 = new MathMultiply("factor_r");
					factor_r262.Operation = MathNode.Operations.Multiply;
					factor_r262.UseClamp = false;

					var factor_g263 = new MathMultiply("factor_g");
					factor_g263.Operation = MathNode.Operations.Multiply;
					factor_g263.UseClamp = false;

					var factor_b264 = new MathMultiply("factor_b");
					factor_b264.Operation = MathNode.Operations.Multiply;
					factor_b264.UseClamp = false;

					var gradienttexture278 = new GradientTextureNode("gradienttexture");

					var factored_bg_color266 = new CombineRgbNode("factored_bg_color");

					var gradient_colorramp279 = new ColorRampNode("gradient_colorramp");
					gradient_colorramp279.ColorBand.Stops.Add(new ccl.ShaderNodes.ColorStop() { Color = new ccl.float4(0.9411765f, 0.5803922f, 0.07843138f, 1f), Position = 0f });
					gradient_colorramp279.ColorBand.Stops.Add(new ccl.ShaderNodes.ColorStop() { Color = new ccl.float4(0.5019608f, 0f, 0f, 1f), Position = 1f });

					var light_path235 = new LightPathNode("light_path");

					var maximum303 = new MathMaximum("maximum");
					maximum303.Operation = MathNode.Operations.Maximum;
					maximum303.UseClamp = true;

					var maximum305 = new MathMaximum("maximum");
					maximum305.Operation = MathNode.Operations.Maximum;
					maximum305.UseClamp = true;

					var gradient_or_other280 = new MixNode("gradient_or_other");
					gradient_or_other280.ins.Fac.Value = m_original_background.UseGradientAsFloat;
					gradient_or_other280.BlendType = MixNode.BlendTypes.Blend;
					gradient_or_other280.UseClamp = false;

					var maximum306 = new MathMaximum("maximum");
					maximum306.Operation = MathNode.Operations.Maximum;
					maximum306.UseClamp = true;

					var bg_no_customs301 = new BackgroundNode("bg_no_customs");

					var refl_env_texture256 = new EnvironmentTextureNode("refl_env_texture");
					refl_env_texture256.Projection = TextureNode.EnvironmentProjection.Equirectangular;
					refl_env_texture256.ColorSpace = TextureNode.TextureColorSpace.None;
					refl_env_texture256.Extension = TextureNode.TextureExtension.Repeat;
					refl_env_texture256.Interpolation = InterpolationType.Linear;
					refl_env_texture256.IsLinear = false;

					var refl_color_or_texture260 = new MixNode("refl_color_or_texture");
					refl_color_or_texture260.ins.Color1.Value = m_original_background.ReflectionColorAs4float;
					refl_color_or_texture260.ins.Fac.Value = m_original_background.HasReflEnvTextureAsFloat;
					refl_color_or_texture260.BlendType = MixNode.BlendTypes.Blend;
					refl_color_or_texture260.UseClamp = false;

					var separate_refl_color270 = new SeparateRgbNode("separate_refl_color");

					var skylight_strength_factor300 = new MathMultiply("skylight_strength_factor");
					skylight_strength_factor300.ins.Value1.Value = m_original_background.ReflStrength;
					skylight_strength_factor300.ins.Value2.Value = m_original_background.NonSkyEnvStrengthFactor;
					skylight_strength_factor300.Operation = MathNode.Operations.Multiply;
					skylight_strength_factor300.UseClamp = false;

					var factor_refl_r267 = new MathMultiply("factor_refl_r");
					factor_refl_r267.Operation = MathNode.Operations.Multiply;
					factor_refl_r267.UseClamp = false;

					var factor_refl_g268 = new MathMultiply("factor_refl_g");
					factor_refl_g268.Operation = MathNode.Operations.Multiply;
					factor_refl_g268.UseClamp = false;

					var factor_refl_b269 = new MathMultiply("factor_refl_b");
					factor_refl_b269.Operation = MathNode.Operations.Multiply;
					factor_refl_b269.UseClamp = false;

					var use_reflect_refract_when_glossy_and_reflection282 = new MathMultiply("use_reflect_refract_when_glossy_and_reflection");
					use_reflect_refract_when_glossy_and_reflection282.Operation = MathNode.Operations.Multiply;
					use_reflect_refract_when_glossy_and_reflection282.UseClamp = false;

					var factored_refl_color271 = new CombineRgbNode("factored_refl_color");

					var refl_env_when_enabled283 = new MathMultiply("refl_env_when_enabled");
					refl_env_when_enabled283.ins.Value1.Value = m_original_background.UseCustomReflectionEnvironmentAsFloat;
					refl_env_when_enabled283.Operation = MathNode.Operations.Multiply;
					refl_env_when_enabled283.UseClamp = false;

					var skycolor_or_final_bg281 = new MixNode("skycolor_or_final_bg");
					skycolor_or_final_bg281.ins.Color2.Value = m_original_background.SkyColorAs4float;
					skycolor_or_final_bg281.ins.Fac.Value = 0.0f; // m_original_background.UseSkyColorAsFloat;
					skycolor_or_final_bg281.BlendType = MixNode.BlendTypes.Blend;
					skycolor_or_final_bg281.UseClamp = false;

					var sky_env_texture257 = new EnvironmentTextureNode("sky_env_texture");
					sky_env_texture257.Projection = TextureNode.EnvironmentProjection.Equirectangular;
					sky_env_texture257.ColorSpace = TextureNode.TextureColorSpace.None;
					sky_env_texture257.Extension = TextureNode.TextureExtension.Repeat;
					sky_env_texture257.Interpolation = InterpolationType.Linear;
					sky_env_texture257.IsLinear = false;

					var sky_color_or_texture258 = new MixNode("sky_color_or_texture");
					sky_color_or_texture258.ins.Fac.Value = m_original_background.HasSkyEnvTextureAsFloat;
					sky_color_or_texture258.BlendType = MixNode.BlendTypes.Blend;
					sky_color_or_texture258.UseClamp = false;

					var separate_sky_color275 = new SeparateRgbNode("separate_sky_color");

					var sky_or_not261 = new MathMultiply("sky_or_not");
					sky_or_not261.ins.Value1.Value = m_original_background.SkyStrength;
					sky_or_not261.ins.Value2.Value = m_original_background.SkylightEnabledAsFloat;
					sky_or_not261.Operation = MathNode.Operations.Multiply;
					sky_or_not261.UseClamp = false;

					var factor_sky_r272 = new MathMultiply("factor_sky_r");
					factor_sky_r272.Operation = MathNode.Operations.Multiply;
					factor_sky_r272.UseClamp = false;

					var factor_sky_g273 = new MathMultiply("factor_sky_g");
					factor_sky_g273.Operation = MathNode.Operations.Multiply;
					factor_sky_g273.UseClamp = false;

					var factor_sky_b274 = new MathMultiply("factor_sky_b");
					factor_sky_b274.Operation = MathNode.Operations.Multiply;
					factor_sky_b274.UseClamp = false;

					var factored_sky_color276 = new CombineRgbNode("factored_sky_color");

					var non_camera_rays287 = new MathSubtract("non_camera_rays");
					non_camera_rays287.ins.Value1.Value = 1f;
					non_camera_rays287.Operation = MathNode.Operations.Subtract;
					non_camera_rays287.UseClamp = false;

					var camera_and_transmission291 = new MathAdd("camera_and_transmission");
					camera_and_transmission291.Operation = MathNode.Operations.Add;
					camera_and_transmission291.UseClamp = false;

					var invert_refl_switch297 = new MathSubtract("invert_refl_switch");
					invert_refl_switch297.ins.Value1.Value = 1f;
					invert_refl_switch297.Operation = MathNode.Operations.Subtract;
					invert_refl_switch297.UseClamp = false;

					var invert_cam_and_transm289 = new MathSubtract("invert_cam_and_transm");
					invert_cam_and_transm289.ins.Value1.Value = 1f;
					invert_cam_and_transm289.Operation = MathNode.Operations.Subtract;
					invert_cam_and_transm289.UseClamp = false;

					var refl_bg_or_custom_env288 = new MixNode("refl_bg_or_custom_env");
					refl_bg_or_custom_env288.BlendType = MixNode.BlendTypes.Blend;
					refl_bg_or_custom_env288.UseClamp = false;

					var light_with_bg_or_sky286 = new MixNode("light_with_bg_or_sky");
					light_with_bg_or_sky286.BlendType = MixNode.BlendTypes.Blend;
					light_with_bg_or_sky286.UseClamp = false;

					var if_not_cam_nor_transm_nor_glossyrefl298 = new MathMultiply("if_not_cam_nor_transm_nor_glossyrefl");
					if_not_cam_nor_transm_nor_glossyrefl298.Operation = MathNode.Operations.Multiply;
					if_not_cam_nor_transm_nor_glossyrefl298.UseClamp = false;

					var mix292 = new MixNode("mix");
					mix292.BlendType = MixNode.BlendTypes.Blend;
					mix292.UseClamp = false;

					var final_bg277 = new BackgroundNode("final_bg");
					final_bg277.ins.Strength.Value = 1f;

					m_shader.AddNode(texcoord210);
					m_shader.AddNode(bg_env_texture255);
					m_shader.AddNode(bg_color_or_texture259);
					m_shader.AddNode(separate_bg_color265);
					m_shader.AddNode(skylight_strength_factor299);
					m_shader.AddNode(factor_r262);
					m_shader.AddNode(factor_g263);
					m_shader.AddNode(factor_b264);
					m_shader.AddNode(gradienttexture278);
					m_shader.AddNode(factored_bg_color266);
					m_shader.AddNode(gradient_colorramp279);
					m_shader.AddNode(light_path235);
					m_shader.AddNode(maximum303);
					m_shader.AddNode(maximum305);
					m_shader.AddNode(gradient_or_other280);
					m_shader.AddNode(maximum306);
					m_shader.AddNode(bg_no_customs301);
					m_shader.AddNode(refl_env_texture256);
					m_shader.AddNode(refl_color_or_texture260);
					m_shader.AddNode(separate_refl_color270);
					m_shader.AddNode(skylight_strength_factor300);
					m_shader.AddNode(factor_refl_r267);
					m_shader.AddNode(factor_refl_g268);
					m_shader.AddNode(factor_refl_b269);
					m_shader.AddNode(use_reflect_refract_when_glossy_and_reflection282);
					m_shader.AddNode(factored_refl_color271);
					m_shader.AddNode(refl_env_when_enabled283);
					m_shader.AddNode(skycolor_or_final_bg281);
					m_shader.AddNode(sky_env_texture257);
					m_shader.AddNode(sky_color_or_texture258);
					m_shader.AddNode(separate_sky_color275);
					m_shader.AddNode(sky_or_not261);
					m_shader.AddNode(factor_sky_r272);
					m_shader.AddNode(factor_sky_g273);
					m_shader.AddNode(factor_sky_b274);
					m_shader.AddNode(factored_sky_color276);
					m_shader.AddNode(non_camera_rays287);
					m_shader.AddNode(camera_and_transmission291);
					m_shader.AddNode(invert_refl_switch297);
					m_shader.AddNode(invert_cam_and_transm289);
					m_shader.AddNode(refl_bg_or_custom_env288);
					m_shader.AddNode(light_with_bg_or_sky286);
					m_shader.AddNode(if_not_cam_nor_transm_nor_glossyrefl298);
					m_shader.AddNode(mix292);
					m_shader.AddNode(final_bg277);


					texcoord210.outs.Generated.Connect(bg_env_texture255.ins.Vector);
					bg_env_texture255.outs.Color.Connect(bg_color_or_texture259.ins.Color2);
					bg_color_or_texture259.outs.Color.Connect(separate_bg_color265.ins.Image);
					separate_bg_color265.outs.R.Connect(factor_r262.ins.Value1);
					skylight_strength_factor299.outs.Value.Connect(factor_r262.ins.Value2);
					separate_bg_color265.outs.G.Connect(factor_g263.ins.Value1);
					skylight_strength_factor299.outs.Value.Connect(factor_g263.ins.Value2);
					separate_bg_color265.outs.B.Connect(factor_b264.ins.Value1);
					skylight_strength_factor299.outs.Value.Connect(factor_b264.ins.Value2);
					texcoord210.outs.Window.Connect(gradienttexture278.ins.Vector);
					factor_r262.outs.Value.Connect(factored_bg_color266.ins.R);
					factor_g263.outs.Value.Connect(factored_bg_color266.ins.G);
					factor_b264.outs.Value.Connect(factored_bg_color266.ins.B);
					gradienttexture278.outs.Fac.Connect(gradient_colorramp279.ins.Fac);
					light_path235.outs.IsCameraRay.Connect(maximum303.ins.Value1);
					light_path235.outs.IsGlossyRay.Connect(maximum303.ins.Value2);
					maximum303.outs.Value.Connect(maximum305.ins.Value1);
					light_path235.outs.IsTransmissionRay.Connect(maximum305.ins.Value2);
					factored_bg_color266.outs.Image.Connect(gradient_or_other280.ins.Color1);
					gradient_colorramp279.outs.Color.Connect(gradient_or_other280.ins.Color2);
					maximum305.outs.Value.Connect(maximum306.ins.Value1);
					light_path235.outs.IsSingularRay.Connect(maximum306.ins.Value2);
					gradient_or_other280.outs.Color.Connect(bg_no_customs301.ins.Color);
					maximum306.outs.Value.Connect(bg_no_customs301.ins.Strength);
					texcoord210.outs.Generated.Connect(refl_env_texture256.ins.Vector);
					refl_env_texture256.outs.Color.Connect(refl_color_or_texture260.ins.Color2);
					refl_color_or_texture260.outs.Color.Connect(separate_refl_color270.ins.Image);
					separate_refl_color270.outs.R.Connect(factor_refl_r267.ins.Value1);
					skylight_strength_factor300.outs.Value.Connect(factor_refl_r267.ins.Value2);
					separate_refl_color270.outs.G.Connect(factor_refl_g268.ins.Value1);
					skylight_strength_factor300.outs.Value.Connect(factor_refl_g268.ins.Value2);
					separate_refl_color270.outs.B.Connect(factor_refl_b269.ins.Value1);
					skylight_strength_factor300.outs.Value.Connect(factor_refl_b269.ins.Value2);
					light_path235.outs.IsGlossyRay.Connect(use_reflect_refract_when_glossy_and_reflection282.ins.Value1);
					light_path235.outs.IsReflectionRay.Connect(use_reflect_refract_when_glossy_and_reflection282.ins.Value2);
					factor_refl_r267.outs.Value.Connect(factored_refl_color271.ins.R);
					factor_refl_g268.outs.Value.Connect(factored_refl_color271.ins.G);
					factor_refl_b269.outs.Value.Connect(factored_refl_color271.ins.B);
					use_reflect_refract_when_glossy_and_reflection282.outs.Value.Connect(refl_env_when_enabled283.ins.Value2);
					gradient_or_other280.outs.Color.Connect(skycolor_or_final_bg281.ins.Color1);
					texcoord210.outs.Generated.Connect(sky_env_texture257.ins.Vector);
					skycolor_or_final_bg281.outs.Color.Connect(sky_color_or_texture258.ins.Color1);
					sky_env_texture257.outs.Color.Connect(sky_color_or_texture258.ins.Color2);
					sky_color_or_texture258.outs.Color.Connect(separate_sky_color275.ins.Image);
					separate_sky_color275.outs.R.Connect(factor_sky_r272.ins.Value1);
					sky_or_not261.outs.Value.Connect(factor_sky_r272.ins.Value2);
					separate_sky_color275.outs.G.Connect(factor_sky_g273.ins.Value1);
					sky_or_not261.outs.Value.Connect(factor_sky_g273.ins.Value2);
					separate_sky_color275.outs.B.Connect(factor_sky_b274.ins.Value1);
					sky_or_not261.outs.Value.Connect(factor_sky_b274.ins.Value2);
					factor_sky_r272.outs.Value.Connect(factored_sky_color276.ins.R);
					factor_sky_g273.outs.Value.Connect(factored_sky_color276.ins.G);
					factor_sky_b274.outs.Value.Connect(factored_sky_color276.ins.B);
					light_path235.outs.IsCameraRay.Connect(non_camera_rays287.ins.Value2);
					light_path235.outs.IsCameraRay.Connect(camera_and_transmission291.ins.Value1);
					light_path235.outs.IsTransmissionRay.Connect(camera_and_transmission291.ins.Value2);
					refl_env_when_enabled283.outs.Value.Connect(invert_refl_switch297.ins.Value2);
					camera_and_transmission291.outs.Value.Connect(invert_cam_and_transm289.ins.Value2);
					gradient_or_other280.outs.Color.Connect(refl_bg_or_custom_env288.ins.Color1);
					factored_refl_color271.outs.Image.Connect(refl_bg_or_custom_env288.ins.Color2);
					refl_env_when_enabled283.outs.Value.Connect(refl_bg_or_custom_env288.ins.Fac);
					gradient_or_other280.outs.Color.Connect(light_with_bg_or_sky286.ins.Color1);
					factored_sky_color276.outs.Image.Connect(light_with_bg_or_sky286.ins.Color2);
					non_camera_rays287.outs.Value.Connect(light_with_bg_or_sky286.ins.Fac);
					invert_refl_switch297.outs.Value.Connect(if_not_cam_nor_transm_nor_glossyrefl298.ins.Value1);
					invert_cam_and_transm289.outs.Value.Connect(if_not_cam_nor_transm_nor_glossyrefl298.ins.Value2);
					refl_bg_or_custom_env288.outs.Color.Connect(mix292.ins.Color1);
					light_with_bg_or_sky286.outs.Color.Connect(mix292.ins.Color2);
					if_not_cam_nor_transm_nor_glossyrefl298.outs.Value.Connect(mix292.ins.Fac);
					mix292.outs.Color.Connect(final_bg277.ins.Color);

					// extra code

					gradient_colorramp279.ColorBand.Stops.Clear();
					// bottom color on 0.0f
					gradient_colorramp279.ColorBand.InsertColorStop(m_original_background.Color2AsFloat4, 0.0f);
					// top color on 1.0f
					gradient_colorramp279.ColorBand.InsertColorStop(m_original_background.Color1AsFloat4, 1.0f);

					// rotate the window vector
					gradienttexture278.Rotation = RenderEngine.CreateFloat4(0.0, 0.0, 1.570796);

					if (m_original_background.BackgroundFill == BackgroundStyle.Environment && m_original_background.HasBgEnvTexture)
					{
						RenderEngine.SetTextureImage(bg_env_texture255, m_original_background.BgTexture);
						_SetEnvironmentProjection(m_original_background.BgTexture, bg_env_texture255);
						bg_env_texture255.Translation = m_original_background.BgTexture.Transform.x;
						bg_env_texture255.Scale = m_original_background.BgTexture.Transform.y;
						bg_env_texture255.Rotation = m_original_background.BgTexture.Transform.z;
					}
					if (m_original_background.BackgroundFill == BackgroundStyle.WallpaperImage && m_original_background.Wallpaper.HasTextureImage)
					{
						RenderEngine.SetTextureImage(bg_env_texture255, m_original_background.Wallpaper);
						bg_env_texture255.Projection = TextureNode.EnvironmentProjection.Wallpaper;
					}
					if (m_original_background.HasReflEnvTexture)
					{
						RenderEngine.SetTextureImage(refl_env_texture256, m_original_background.ReflectionTexture);
						_SetEnvironmentProjection(m_original_background.ReflectionTexture, refl_env_texture256);
						refl_env_texture256.Translation = m_original_background.ReflectionTexture.Transform.x;
						refl_env_texture256.Scale = m_original_background.ReflectionTexture.Transform.y;
						refl_env_texture256.Rotation = m_original_background.ReflectionTexture.Transform.z;
					}
					if (m_original_background.HasSkyEnvTexture)
					{
						RenderEngine.SetTextureImage(sky_env_texture257, m_original_background.SkyTexture);
						_SetEnvironmentProjection(m_original_background.SkyTexture, sky_env_texture257);
						sky_env_texture257.Translation = m_original_background.SkyTexture.Transform.x;
						sky_env_texture257.Scale = m_original_background.SkyTexture.Transform.y;
						sky_env_texture257.Rotation = m_original_background.SkyTexture.Transform.z;
					}

					if (m_original_background.NoCustomsWithSkylightEnabled)
					{
						bg_no_customs301.ins.Strength.ClearConnections();
						bg_no_customs301.ins.Strength.Value = 1.0f;
						bg_no_customs301.outs.Background.Connect(m_shader.Output.ins.Surface);
					}
					else
					if (m_original_background.NoCustomsWithSkylightDisabled)
					{
						bg_no_customs301.outs.Background.Connect(m_shader.Output.ins.Surface);
					}
					else
					{
						final_bg277.outs.Background.Connect(m_shader.Output.ins.Surface);
					}
				} else {
					var final_bg277 = new BackgroundNode("final_bg");
					final_bg277.ins.Strength.Value = 1f;
					m_shader.AddNode(final_bg277);
					if (m_original_background.BgTexture.HasTextureImage)
					{
						var bg_env_texture255 = new EnvironmentTextureNode("bg_env_texture");
						bg_env_texture255.Projection = TextureNode.EnvironmentProjection.Equirectangular;
						bg_env_texture255.ColorSpace = TextureNode.TextureColorSpace.None;
						bg_env_texture255.Extension = TextureNode.TextureExtension.Repeat;
						bg_env_texture255.Interpolation = InterpolationType.Linear;
						bg_env_texture255.IsLinear = false;
						m_shader.AddNode(bg_env_texture255);
						RenderEngine.SetTextureImage(bg_env_texture255, m_original_background.BgTexture);
						_SetEnvironmentProjection(m_original_background.BgTexture, bg_env_texture255);
						bg_env_texture255.Translation = m_original_background.BgTexture.Transform.x;
						bg_env_texture255.Scale = m_original_background.BgTexture.Transform.y;
						bg_env_texture255.Rotation = m_original_background.BgTexture.Transform.z;
						bg_env_texture255.outs.Color.Connect(final_bg277.ins.Color);
					} else {
						final_bg277.ins.Color.Value = m_original_background.Color1AsFloat4;
					}
					final_bg277.ins.Strength.Value = m_original_background.BgStrength;

					final_bg277.outs.Background.Connect(m_shader.Output.ins.Surface);
				}
			}

			// phew, done.
			m_shader.FinalizeGraph();
			m_shader.Tag();

			return m_shader;
		}

		private void _SetEnvironmentProjection(CyclesTextureImage img, EnvironmentTextureNode envtexture)
		{
			switch (img.EnvProjectionMode)
			{
				case Rhino.Render.TextureEnvironmentMappingMode.Automatic:
				case Rhino.Render.TextureEnvironmentMappingMode.EnvironmentMap:
					envtexture.Projection = TextureNode.EnvironmentProjection.EnvironmentMap;
					break;
				case Rhino.Render.TextureEnvironmentMappingMode.Box:
					envtexture.Projection = TextureNode.EnvironmentProjection.Box;
					break;
				case Rhino.Render.TextureEnvironmentMappingMode.LightProbe:
					envtexture.Projection = TextureNode.EnvironmentProjection.LightProbe;
					break;
				case Rhino.Render.TextureEnvironmentMappingMode.Cube:
					envtexture.Projection = TextureNode.EnvironmentProjection.CubeMap;
					break;
				case Rhino.Render.TextureEnvironmentMappingMode.HorizontalCrossCube:
					envtexture.Projection = TextureNode.EnvironmentProjection.CubeMapHorizontal;
					break;
				case Rhino.Render.TextureEnvironmentMappingMode.VerticalCrossCube:
					envtexture.Projection = TextureNode.EnvironmentProjection.CubeMapVertical;
					break;
				case Rhino.Render.TextureEnvironmentMappingMode.Hemispherical:
					envtexture.Projection = TextureNode.EnvironmentProjection.Hemispherical;
					break;
				case Rhino.Render.TextureEnvironmentMappingMode.Spherical:
					envtexture.Projection = TextureNode.EnvironmentProjection.Spherical;
					break;
				default: // non-existing planar environment projection, value 4
					envtexture.Projection = TextureNode.EnvironmentProjection.Wallpaper;
					break;
			}
		}
	}
}
