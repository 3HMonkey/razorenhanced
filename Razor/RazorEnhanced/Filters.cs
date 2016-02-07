﻿using Assistant;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace RazorEnhanced
{
	public class Filters
	{
		[Serializable]
		public class GraphChangeData
		{
			private bool m_Selected;
			public bool Selected { get { return m_Selected; } }

			private int m_GraphReal;
			public int GraphReal { get { return m_GraphReal; } }

			private int m_GraphNew;
			public int GraphNew { get { return m_GraphNew; } }

			public GraphChangeData(bool selected, int graphreal, int graphnew)
			{
				m_Selected = selected;
				m_GraphReal = graphreal;
				m_GraphNew = graphnew;
			}
		}

		internal static int BoneCutterBlade
		{
			get
			{
				try
				{
					int serialblade = Convert.ToInt32(Assistant.Engine.MainWindow.BoneBladeLabel.Text, 16);
					if (serialblade == 0)
					{
						return 0;
					}

					Item blade = RazorEnhanced.Items.FindBySerial(serialblade);
					if (blade != null && blade.RootContainer == World.Player)
						return blade.Serial;
					else
						return 0;
				}
				catch
				{
					return 0;
				}
			}

			set
			{
				Assistant.Engine.MainWindow.BoneBladeLabel.Invoke(new Action(() => Assistant.Engine.MainWindow.BoneBladeLabel.Text = "0x" + value.ToString("X8")));
			}
		}

		internal static int AutoCarverBlade
		{
			get
			{
				try
				{
					int serialblade = Convert.ToInt32(Assistant.Engine.MainWindow.AutoCarverBladeLabel.Text, 16);
					if (serialblade == 0)
						return 0;

					Item blade = RazorEnhanced.Items.FindBySerial(serialblade);
					if (blade != null && blade.RootContainer == World.Player)
						return blade.Serial;
					else
						return 0;
				}
				catch
				{
					return 0;
				}
			}

			set
			{
				Assistant.Engine.MainWindow.AutoCarverBladeLabel.Invoke(new Action(() => Assistant.Engine.MainWindow.AutoCarverBladeLabel.Text = "0x" + value.ToString("X8")));
			}
		}

		internal static void ProcessMessage(Assistant.Mobile m)
		{
			if (m.Serial == World.Player.Serial)      // Skip Self
				return;

			if (Assistant.Engine.MainWindow.FlagsHighlightCheckBox.Checked)
			{
				if (m.Poisoned)
					RazorEnhanced.Mobiles.Message(m.Serial, 10, "[Poisoned]");
				if (m.IsGhost)
					RazorEnhanced.Mobiles.Message(m.Serial, 10, "[Dead]");
			}

			if (Assistant.Engine.MainWindow.HighlightTargetCheckBox.Checked)
			{
				if (Targeting.IsLastTarget(m))
					RazorEnhanced.Mobiles.Message(m.Serial, 10, "*[Target]*");
			}
		}

		//////////////// GRAPH FILTER ///////////////////////

		internal static void RefreshLists()
		{
			List<RazorEnhanced.Filters.GraphChangeData> graphdatas = Settings.GraphFilter.ReadAll();

			Assistant.Engine.MainWindow.MobFilterlistView.Items.Clear();

			foreach (RazorEnhanced.Filters.GraphChangeData graphdata in graphdatas)
			{
				ListViewItem listitem = new ListViewItem();

				listitem.Checked = graphdata.Selected;

				listitem.SubItems.Add("0x" + graphdata.GraphReal.ToString("X4"));
				listitem.SubItems.Add("0x" + graphdata.GraphNew.ToString("X4"));

				Assistant.Engine.MainWindow.MobFilterlistView.Items.Add(listitem);
			}
		}

		internal static void UpdateSelectedItems(int i)
		{
			List<RazorEnhanced.Filters.GraphChangeData> graphdatas = Settings.GraphFilter.ReadAll();

			if (graphdatas.Count != Assistant.Engine.MainWindow.MobFilterlistView.Items.Count)
			{
				return;
			}

			ListViewItem lvi = Assistant.Engine.MainWindow.MobFilterlistView.Items[i];
			GraphChangeData old = graphdatas[i];

			if (lvi != null && old != null)
			{
				GraphChangeData graph = new GraphChangeData(lvi.Checked, old.GraphReal, old.GraphNew);
				RazorEnhanced.Settings.GraphFilter.Replace(i, graph);
			}
		}

		//////////////// GRAPH FILTER END /////////////////////

		//////////////// AUTOCARVER START ////////////////

		private static Queue<int> m_IgnoreCutCorpiQueue = new Queue<int>();
		private static bool m_AutoCarver;

		internal static bool AutoCarver
		{
			get { return m_AutoCarver; }
			set { m_AutoCarver = value; }
		}

		internal static int AutoCarverEngine(Items.Filter filter)
		{
			if (World.Player == null)       // Esce se non loggato
				return 0;

			if (AutoCarverBlade == 0)       // Esce in caso di errore lettura blade
				return 0;

			List<Item> corpi = RazorEnhanced.Items.ApplyFilter(filter);

			foreach (RazorEnhanced.Item corpo in corpi)
			{
				if (!m_IgnoreCutCorpiQueue.Contains(corpo.Serial))
				{
					DragDropManager.CorpseToCutSerial.Enqueue(corpo.Serial);
					m_IgnoreCutCorpiQueue.Enqueue(corpo.Serial);
                }			
			}
			return 0;
		}

		internal static void CarveAutoRun()
		{
			int exit = Int32.MinValue;

			// Genero filtro per corpi
			Items.Filter corpseFilter = new Items.Filter();
			corpseFilter.RangeMax = 3;
			corpseFilter.Movable = false;
			corpseFilter.IsCorpse = 1;
			corpseFilter.OnGround = 1;
			corpseFilter.Enabled = true;

			exit = AutoCarverEngine(corpseFilter);
		}

		//////////////// AUTOCARVER STOP ////////////////

		//////////////// BONE CUTTER START ////////////////

		private static bool m_BoneCutter;

		internal static bool BoneCutter
		{
			get { return m_BoneCutter; }
			set { m_BoneCutter = value; }
		}

		internal static int BoneCutterEngine(Items.Filter filter)
		{
			if (World.Player == null)       // Esce se non loggato
				return 0;

			if (BoneCutterBlade == 0)       // Esce in caso di errore lettura blade
				return 0;

			List<Item> bones = RazorEnhanced.Items.ApplyFilter(filter);

			foreach (RazorEnhanced.Item bone in bones)
			{
				Target.Cancel();
				Items.UseItem(Items.FindBySerial(BoneCutterBlade));
				Target.WaitForTarget(1000);
				Target.TargetExecute(bone.Serial);
				Thread.Sleep(100);			
            }
			return 0;
		}

		internal static void BoneCutterRun()
		{
			int exit = Int32.MinValue;

			// Genero filtro per ossa
			Items.Filter bonesFilter = new Items.Filter();
			bonesFilter.Graphics.Add(0x3968);
			bonesFilter.RangeMax = 1;
			bonesFilter.Movable = false;
			bonesFilter.IsCorpse = -1;
			bonesFilter.OnGround = 1;
			bonesFilter.Enabled = true;

			exit = BoneCutterEngine(bonesFilter);
		}

		//////////////// BONE CUTTER STOP ////////////////

		//////////////// AUTOREMOUNT START ////////////////

		internal static int AutoRemountDelay
		{
			get
			{
				int delay = 100;
				Assistant.Engine.MainWindow.RemountDelay.Invoke(new Action(() => Int32.TryParse(Assistant.Engine.MainWindow.RemountDelay.Text, out delay)));
				return delay;
			}

			set
			{
				Assistant.Engine.MainWindow.RemountDelay.Invoke(new Action(() => Assistant.Engine.MainWindow.RemountDelay.Text = value.ToString()));
			}
		}

		internal static int AutoRemountEDelay
		{
			get
			{
				int delay = 100;
				Assistant.Engine.MainWindow.RemountEDelay.Invoke(new Action(() => Int32.TryParse(Assistant.Engine.MainWindow.RemountEDelay.Text, out delay)));
				return delay;
			}

			set
			{
				Assistant.Engine.MainWindow.RemountEDelay.Invoke(new Action(() => Assistant.Engine.MainWindow.RemountEDelay.Text = value.ToString()));
			}
		}

		internal static int AutoRemountSerial
		{
			get
			{
				int serial = 0;
				try
				{
					serial = Convert.ToInt32(Assistant.Engine.MainWindow.RemountSerialLabel.Text, 16);
				}
				catch
				{ }
				return serial;
			}

			set
			{
				Assistant.Engine.MainWindow.RemountSerialLabel.Invoke(new Action(() => Assistant.Engine.MainWindow.RemountSerialLabel.Text = "0x" + value.ToString("X8")));
			}
		}

		private static bool m_AutoModeRemount;

		internal static bool AutoModeRemount
		{
			get { return m_AutoModeRemount; }
			set { m_AutoModeRemount = value; }
		}

		internal static void RemountAutoRun()
		{
			if (World.Player == null)
				return;

			if (AutoRemountSerial == 0)
				return;

			if (World.Player.IsGhost)
				return;

			if (World.Player.GetItemOnLayer(Layer.Mount) != null)   // Gia su mount
				return;

			Assistant.Item etheralMount = Assistant.World.FindItem(AutoRemountSerial);
			if (etheralMount != null && etheralMount.Serial.IsItem)
			{
				RazorEnhanced.Items.UseItem(AutoRemountSerial);
				Thread.Sleep(AutoRemountEDelay);
			}
			else
			{
				Assistant.Mobile mount = Assistant.World.FindMobile(AutoRemountSerial);
				if (mount != null && mount.Serial.IsMobile)
				{
					RazorEnhanced.Mobiles.UseMobile(AutoRemountSerial);
					Thread.Sleep(AutoRemountDelay);
				}
			}
		}

		//////////////// AUTOREMOUNT STOP ////////////////

		//////////////// Load settings ////////////////
		internal static void LoadSettings()
		{
			Assistant.Engine.MainWindow.HighlightTargetCheckBox.Checked = RazorEnhanced.Settings.General.ReadBool("HighlightTargetCheckBox");
			Assistant.Engine.MainWindow.FlagsHighlightCheckBox.Checked = RazorEnhanced.Settings.General.ReadBool("FlagsHighlightCheckBox");
			Assistant.Engine.MainWindow.ShowStaticFieldCheckBox.Checked = RazorEnhanced.Settings.General.ReadBool("ShowStaticFieldCheckBox");
			Assistant.Engine.MainWindow.BlockTradeRequestCheckBox.Checked = RazorEnhanced.Settings.General.ReadBool("BlockTradeRequestCheckBox");
			Assistant.Engine.MainWindow.BlockPartyInviteCheckBox.Checked = RazorEnhanced.Settings.General.ReadBool("BlockPartyInviteCheckBox");
			Assistant.Engine.MainWindow.MobFilterCheckBox.Checked = RazorEnhanced.Settings.General.ReadBool("MobFilterCheckBox");
			Assistant.Engine.MainWindow.AutoCarverCheckBox.Checked = RazorEnhanced.Settings.General.ReadBool("AutoCarverCheckBox");
			Assistant.Engine.MainWindow.BoneCutterCheckBox.Checked = RazorEnhanced.Settings.General.ReadBool("BoneCutterCheckBox");
			Assistant.Engine.MainWindow.AutoCarverBladeLabel.Text = RazorEnhanced.Settings.General.ReadInt("AutoCarverBladeLabel").ToString("X8");
			Assistant.Engine.MainWindow.BoneBladeLabel.Text = RazorEnhanced.Settings.General.ReadInt("BoneBladeLabel").ToString("X8");
			Assistant.Engine.MainWindow.RemountCheckbox.Checked = RazorEnhanced.Settings.General.ReadBool("RemountCheckbox");
			Assistant.Engine.MainWindow.ShowHeadTargetCheckBox.Checked = RazorEnhanced.Settings.General.ReadBool("ShowHeadTargetCheckBox");
			Assistant.Engine.MainWindow.BlockHealPoisonCheckBox.Checked = RazorEnhanced.Settings.General.ReadBool("BlockHealPoison");
			Assistant.Engine.MainWindow.BlockChivalryHealCheckBox.Checked = RazorEnhanced.Settings.General.ReadBool("BlockChivalryHealCheckBox");
			Assistant.Engine.MainWindow.BlockBigHealCheckBox.Checked = RazorEnhanced.Settings.General.ReadBool("BlockBigHealCheckBox");
			Assistant.Engine.MainWindow.BlockMiniHealCheckBox.Checked = RazorEnhanced.Settings.General.ReadBool("BlockMiniHealCheckBox");
			Assistant.Engine.MainWindow.ColorFlagsHighlightCheckBox.Checked = RazorEnhanced.Settings.General.ReadBool("ColorFlagsHighlightCheckBox");

			AutoRemountDelay = RazorEnhanced.Settings.General.ReadInt("MountDelay");
			AutoRemountEDelay = RazorEnhanced.Settings.General.ReadInt("EMountDelay");
			AutoRemountSerial = RazorEnhanced.Settings.General.ReadInt("MountSerial");

			RefreshLists();
		}
	}
}