﻿/**
Copyright 2014-2015 Robert McNeel and Associates

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

using System.Drawing;

namespace RhinoCycles
{
	partial class RenderEngine
	{
		/// <summary>
		/// Get or set the resolution for rendering.
		/// </summary>
		public Size RenderDimension { get; set; }

		/// <summary>
		/// Hold instance specific EngineSettings.
		/// </summary>
		private EngineSettings m_settings;

		/// <summary>
		/// Get or set EngineSettings for this instance. If you set
		/// EngineSettings a deep-copy will be made.
		/// </summary>
		public EngineSettings Settings
		{
			get
			{
				return m_settings ?? (m_settings = new EngineSettings());
			}
			set
			{
				m_settings = new EngineSettings(value);
			}
		}
	}
}
