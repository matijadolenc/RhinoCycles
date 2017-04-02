/**
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
using System.Linq;
using Eto.Forms;
using Rhino.UI;
using RhinoCyclesCore.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RhinoCycles.Settings
{
	public class SelectionChangedEventArgs : EventArgs
	{
		public ObservableCollection<DeviceItem> Collection { get; private set; }
		public SelectionChangedEventArgs(ObservableCollection<DeviceItem> col)
		{
			Collection = col;
		}
	}

	public class GridDevicePage : TabPage
	{
		private GridView m_gv;

		public GridView Grid => m_gv;

		private ObservableCollection<DeviceItem> m_col;

		public ObservableCollection<DeviceItem> Collection => m_col;

		public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

		public GridDevicePage()
		{
			m_col = new ObservableCollection<DeviceItem>();
			m_col.CollectionChanged += M_col_CollectionChanged;
			m_gv = new GridView { DataStore = m_col };
			m_gv.Columns.Add(new GridColumn {
				DataCell = new TextBoxCell { Binding = Binding.Property<DeviceItem, string>(r => r.Text) },
				HeaderText = "Device"
			});

			m_gv.Columns.Add(new GridColumn {
				DataCell = new CheckBoxCell { Binding = Binding.Property<DeviceItem, bool?>(r => r.Selected) },
				HeaderText = "Use",
				Editable = true
			});
			Content = new StackLayout
			{
				Spacing = 5,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				Items = {
					new StackLayoutItem(m_gv, true)
				}
			};
		}

		public void ClearSelection()
		{
			foreach(var di in m_col)
			{
				di.Selected = false;
			}
		}

		public string DeviceSelectionString()
		{
			var str = string.Join(",", (from d in m_col where d.Selected select d.Id).ToList());

			return string.IsNullOrEmpty(str) ? "-1" : str;

		}

		public void RegisterEventHandlers()
		{
			foreach(var di in m_col)
			{
				di.PropertyChanged += Di_PropertyChanged;
			}
		}

		public void UnregisterEventHandlers()
		{
			foreach(var di in m_col)
			{
				di.PropertyChanged -= Di_PropertyChanged;
			}
		}

		private void M_col_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
			{
				foreach(var i in e.NewItems)
				{
					if (i is DeviceItem di)
					{
						di.PropertyChanged -= Di_PropertyChanged;
						di.PropertyChanged += Di_PropertyChanged;
					}
				}
			}
		}

		private void Di_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var di = sender as DeviceItem;
			if (e.PropertyName.CompareTo("Selected") == 0)
			{
				SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(m_col));
			}
		}
	}

	public class DeviceItem : INotifyPropertyChanged
	{
		public string Text { get; set; }

		private bool _selected;
		public bool Selected
		{
			get { return _selected; }
			set
			{
				if(value!=_selected)
				{
					_selected = value;
					OnPropertyChanged();
				}
			}
		}

		ccl.Device _dev;
		int _id;
		public int Id { get {
				return _id;
			}
			set
			{
				_id = value;
				_dev = ccl.Device.GetDevice(_id);
			}
		}

		public ccl.Device Device => _dev;

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string memberName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
		}
	}
	///<summary>
	/// The UI implementation of device section
	///</summary>
	public class DeviceSection: Section
	{
		private LocalizeStringPair m_caption;
		private TabControl m_tc;
		private Label m_lb_curdev;
		private Label m_curdev;
		private Label m_lb_newdev;
		private Label m_newdev;
		private GridDevicePage m_tabpage_cpu;
		private GridDevicePage m_tabpage_cuda;
		private GridDevicePage m_tabpage_opencl;
		private Button m_reset;
		private Button m_select;
		private ccl.Device m_currentDevice;
		private ccl.Device m_newDevice;
		private readonly string m_nodeviceselected = LOC.STR("No device selected, default device will be used");

		public override LocalizeStringPair Caption
		{
			get { return m_caption; }
		}

		///<summary>
		/// The Heigth of the section
		///</summary>
		public override int SectionHeight
		{
			get
			{
				return Content.Height;
			}
		}

		///<summary>
		/// Constructor for SectionOne
		///</summary>
		public DeviceSection()
		{
			RcCore.It.InitialisationCompleted += It_InitialisationCompleted;
			m_caption = new LocalizeStringPair("Device settings", LOC.STR("Device settings"));
			InitializeComponents();
			InitializeLayout();
			RegisterControlEvents();
			ViewportSettingsReceived += DeviceSection_ViewportSettingsReceived;
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			var vud = Plugin.GetActiveViewportSettings();
			var rd = ActiveDevice(vud);
			if (rd.IsCpu) m_tc.SelectedPage = m_tabpage_cpu;
			if (rd.IsCuda || rd.IsMultiCuda) m_tc.SelectedPage = m_tabpage_cuda;
			if (rd.IsOpenCl || rd.IsMultiOpenCl) m_tc.SelectedPage = m_tabpage_opencl;
		}

		private static ccl.Device ActiveDevice(ViewportSettings vud)
		{
			ccl.Device rd;
			if (vud == null)
			{
				rd = RcCore.It.EngineSettings.RenderDevice;
			}
			else
			{
				rd = ccl.Device.DeviceFromString(vud.SelectedDevice);
			}
			return rd;
		}
		private static void SetupListbox(ViewportSettings vud, ObservableCollection<DeviceItem> lb, ccl.DeviceType t)
		{
			var rd = ActiveDevice(vud);
			lb.Clear();
			foreach (var d in ccl.Device.Devices)
			{
				if (d.Type == t)
				{
					lb.Add(new DeviceItem { Text = d.NiceName, Selected = rd.HasId(d.Id), Id = (int)d.Id });
				}
			}
		}

		private void It_InitialisationCompleted(object sender, EventArgs e)
		{
			Application.Instance.AsyncInvoke(() =>
			{
				var vud = Plugin.GetActiveViewportSettings();
				m_currentDevice = RcCore.It.EngineSettings.RenderDevice;
				m_newDevice = vud!=null ? ccl.Device.DeviceFromString(vud.SelectedDevice) : m_currentDevice;
				SuspendLayout();
				UnRegisterControlEvents();
				ShowDeviceData();
				SetupListbox(vud, m_tabpage_cpu.Collection, ccl.DeviceType.CPU);
				SetupListbox(vud, m_tabpage_cuda.Collection, ccl.DeviceType.CUDA);
				SetupListbox(vud, m_tabpage_opencl.Collection, ccl.DeviceType.OpenCL);
				ActivateDevicePage(vud);
				RegisterControlEvents();
				ResumeLayout();
			}
			);
		}

		private void ActivateDevicePage(ViewportSettings vud)
		{
			var rd = ActiveDevice(vud);
			if (rd.IsCuda || rd.IsMultiCuda) m_tc.SelectedPage = m_tabpage_cuda;
			else if (rd.IsOpenCl || rd.IsMultiOpenCl) m_tc.SelectedPage = m_tabpage_opencl;
			else m_tc.SelectedPage = m_tabpage_cpu;
		}

		private void DeviceSection_ViewportSettingsReceived(object sender, ViewportSettingsReceivedEventArgs e)
		{
			if (e.ViewportSettings != null)
			{
				m_currentDevice = RcCore.It.EngineSettings.RenderDevice;
				m_newDevice = ccl.Device.DeviceFromString(e.ViewportSettings.SelectedDevice);
				SuspendLayout();
				UnRegisterControlEvents();
				ShowDeviceData();
				SetupListbox(e.ViewportSettings, m_tabpage_cpu.Collection, ccl.DeviceType.CPU);
				SetupListbox(e.ViewportSettings, m_tabpage_cuda.Collection, ccl.DeviceType.CUDA);
				SetupListbox(e.ViewportSettings, m_tabpage_opencl.Collection, ccl.DeviceType.OpenCL);
				ActivateDevicePage(e.ViewportSettings);
				RegisterControlEvents();
				ResumeLayout();
			}
		}

		private void InitializeComponents()
		{
			m_reset = new Button { Text = LOC.STR("Reset device selection"), ToolTip = LOC.STR("Reset the current selection to that corresponding to the application-level render device selection.") };
			m_select = new Button { Text = LOC.STR("Use current device selection"), ToolTip = LOC.STR("Sets the current selection as application level render device.") };
			m_tc = new TabControl();
			m_tabpage_cpu = new GridDevicePage { Text = "CPU", ToolTip = LOC.STR("Show all the render devices in the CPU category.") };
			m_tabpage_cuda = new GridDevicePage { Text = "CUDA", ToolTip = LOC.STR("Show all the render devices in the CUDA category. These are the NVidia graphics and compute cards.") };
			m_tabpage_opencl = new GridDevicePage { Text = "OpenCL", ToolTip = LOC.STR("Show all the render devices in the OpenCL category. These include all devices that support the OpenCL technology, including CPUs and most graphics cards.") };
			m_tc.Pages.Add(m_tabpage_cpu);
			m_tc.Pages.Add(m_tabpage_cuda);
			m_tc.Pages.Add(m_tabpage_opencl);

			m_lb_curdev = new Label { Text = LOC.STR("Current render device:") };
			m_curdev = new Label { Text = "...", Wrap = WrapMode.Word };
			m_lb_newdev = new Label { Text = LOC.STR("New render device:") };
			m_newdev = new Label { Text = "...", Wrap = WrapMode.Word };
		}


		private void InitializeLayout()
		{
			StackLayout layout = new StackLayout()
			{
				Padding = 10,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				Orientation = Orientation.Vertical,
				Items =
				{
					TableLayout.Horizontal(15, m_lb_curdev, m_curdev, null),
					new StackLayoutItem(m_tc, true),
					TableLayout.Horizontal(15, m_lb_newdev, m_newdev, null),
					TableLayout.Horizontal(15, null, m_reset, m_select, null),
				}
			};
			Content = layout;
		}

		private void RegisterControlEvents()
		{
			m_reset.Click += HandleResetClick;
			m_select.Click += HandleSelectClick;
			m_tabpage_cpu.SelectionChanged += DeviceSelectionChanged;
			m_tabpage_cpu.RegisterEventHandlers();
			m_tabpage_cuda.SelectionChanged += DeviceSelectionChanged;
			m_tabpage_cuda.RegisterEventHandlers();
			m_tabpage_opencl.SelectionChanged += DeviceSelectionChanged;
			m_tabpage_opencl.RegisterEventHandlers();
		}

		private void ShowDeviceData()
		{
			var nodev = "-";
			if(m_currentDevice!=null)
			{
				m_curdev.Text = $"{m_currentDevice.NiceName} ({m_currentDevice.Type})";
			}
			else
			{
				m_curdev.Text = nodev;
			}
			if(m_newDevice != null && m_currentDevice != m_newDevice)
			{
				m_newdev.Text = $"{m_newDevice.NiceName} ({m_newDevice.Type})";
			}
			else
			{
				m_newdev.Text = nodev;
			}
		}

		private void HandleResetClick(object sender, EventArgs e)
		{
			var vud = Plugin.GetActiveViewportSettings();
			if (vud != null)
			{
				vud.SelectedDevice = RcCore.It.EngineSettings.SelectedDeviceStr;
				It_InitialisationCompleted(this, EventArgs.Empty);
			}
		}

		private void HandleSelectClick(object sender, EventArgs e)
		{
			var vud = Plugin.GetActiveViewportSettings();
			if (vud != null)
			{
				RcCore.It.EngineSettings.SelectedDeviceStr = vud.SelectedDevice;
				It_InitialisationCompleted(this, EventArgs.Empty);
			}
		}

		private void DeviceSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			UnRegisterControlEvents();

			if (sender is GridDevicePage senderpage)
			{

				foreach (var page in m_tc.Pages)
				{
					if (page is GridDevicePage p && p != sender) p.ClearSelection();
				}
				var vud = Plugin.GetActiveViewportSettings();

				m_newDevice = ccl.Device.DeviceFromString(senderpage.DeviceSelectionString());
				if(vud!=null) vud.SelectedDevice = m_newDevice.DeviceString;
				
			}

			RegisterControlEvents();

			It_InitialisationCompleted(this, EventArgs.Empty);
		}

		private void UnRegisterControlEvents()
		{
			m_reset.Click -= HandleResetClick;
			m_select.Click -= HandleSelectClick;
			m_tabpage_cpu.SelectionChanged -= DeviceSelectionChanged;
			m_tabpage_cpu.UnregisterEventHandlers();
			m_tabpage_cuda.SelectionChanged -= DeviceSelectionChanged;
			m_tabpage_cuda.UnregisterEventHandlers();
			m_tabpage_opencl.SelectionChanged -= DeviceSelectionChanged;
			m_tabpage_opencl.UnregisterEventHandlers();
		}
	}
}
