using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Carbon.Extensions;
using CarbonCommunity.HookEditor.src.Forms;
using Newtonsoft.Json;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace CarbonCommunity.HookEditor
{
	public partial class Main : Form
	{
		public static string FilePath { get; set; }

		public static Main Instance { get; set; }

		public Main()
		{
			Instance = this;

			InitializeComponent();

			hooksView.NodeMouseDoubleClick += (object sender, TreeNodeMouseClickEventArgs e) =>
			{
				if (e.Node.Tag == null) return;

				var dyn = e.Node.Tag as dynamic;

				new HookPanel(dyn.Key, dyn.hook).Show();
			};
		}

		private void Load(object sender, EventArgs e)
		{
			var dialog = new OpenFileDialog()
			{
				CheckFileExists = true,
				Multiselect = false,
				RestoreDirectory = true,
				Title = "Select hook file"
			};

			if (dialog.ShowDialog() == DialogResult.OK)
			{
				Manifest.Current = JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(FilePath = dialog.FileName));
				hooksToolStripMenuItem.Enabled = saveToolStripMenuItem.Enabled = true;
				Rebuild();
			}
		}
		private void Exit(object sender, EventArgs e)
		{
			Application.Exit();
		}

		public void Rebuild()
		{
			groupBox1.Text = $"Hooks ({Manifest.Current.Hooks.Sum(x => x.Value.Count)})";

			hooksView.Nodes.Clear();
			foreach (var category in Manifest.Current.Hooks)
			{
				var group = hooksView.Nodes.Add($"{category.Key} ({category.Value.Count:n0})");

				foreach (var hook in category.Value)
				{
					var hookNode = group.Nodes.Add($"{hook.HookName} — [{hook.PatchType}.{hook.PatchMethod}]");
					hookNode.Tag = new { hook, category.Key };
				}
			}

			hooksView.ExpandAll();
		}

		public void Save(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(FilePath))
			{
				var dialog = new SaveFileDialog()
				{
					CheckFileExists = true,
					RestoreDirectory = true,
					DefaultExt = "json",
					Title = "Save hook file"
				};

				if (dialog.ShowDialog() == DialogResult.OK)
				{
					FilePath = dialog.FileName;
					hooksToolStripMenuItem.Enabled = saveToolStripMenuItem.Enabled = true;
					Rebuild();
				}
			}

			if(!string.IsNullOrEmpty(FilePath))
				File.WriteAllText(FilePath, JsonConvert.SerializeObject(Manifest.Current, Formatting.Indented));
		}

		private void CreateNewHook(object sender, EventArgs e)
		{
			new HookPanel(Manifest.Current.Hooks.FirstOrDefault().Key, new Manifest.Hook(), isNew: true).Show();
		}

		private void ReorderAlphabetically(object sender, EventArgs e)
		{
			var temp = new Dictionary<string, List<Manifest.Hook>>();

			var dick = Manifest.Current.Hooks.OrderBy(x => x.Key);
			foreach (var element in dick)
			{
				temp.Add(element.Key, element.Value.OrderBy(x => x.HookName).ToList());
			}

			Manifest.Current.Hooks = temp;

			Rebuild();
		}

		private void New(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(FilePath))
			{
				if(MessageBox.Show("All your progress will be lost.", "Are you sure?", MessageBoxButtons.YesNo) == DialogResult.No)
				{
					return;
				}
			}

			FilePath = null;
			Manifest.Current = new();
			Rebuild();
		}
	}
}
