using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Carbon.Extensions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace CarbonCommunity.HookEditor.src.Forms
{
	public partial class HookPanel : Form
	{
		internal string _originalHookCategory;
		internal string _hookCategory;
		internal Manifest.Hook _hook;

		internal static readonly char[] _delimiterComma = new char[] { ',' };
		internal static readonly char[] _delimiterNewline= new char[] { '\n' };

		public HookPanel()
		{
			InitializeComponent();
		}

		public HookPanel(string category, Manifest.Hook hook, bool isNew = false) : base()
		{			
			InitializeComponent();
			_originalHookCategory = _hookCategory = category;
			_hook = hook;
			Refresh();

			if(isNew) deleteButton.Hide();

			comboBox1.Validated += (object sender, EventArgs e) =>
			{
				if (!Manifest.Current.Hooks.ContainsKey(comboBox1.Text))
				{
					Manifest.Current.Hooks.Add(comboBox1.Text, new ());
					MessageBox.Show(comboBox1.Text);
				}

				for(int i = 0; i < Manifest.Current.Hooks.Count; i++)
				{
					var element = Manifest.Current.Hooks.ElementAt(i);

					if (element.Key != comboBox1.Text && element.Value.Count == 0)
					{
						Manifest.Current.Hooks.Remove(element.Key);
					}
				}
			};
		}

		public void Refresh()
		{
			comboBox1.Items.Clear();
			comboBox1.Items.AddRange(Manifest.Current.Hooks.Select(x => x.Key).ToArray());
			comboBox1.SelectedItem = _hookCategory;

			hookNameBox.Text = _hook.HookName;
			hookParametersBox.Text = _hook.HookParameters?.ToString(", ");
			returnNonNullCheck.Checked = _hook.ReturnNonNull;

			patchTypeBox.Text = _hook.PatchType;
			patchMethodBox.Text = _hook.PatchMethod;
			patchReturnTypeBox.Text = _hook.PatchReturnType;
			patchParametersBox.Text += _hook.PatchParameters?.ToString(", ");

			descriptionBox.Text = _hook.MetadataDescription?.ToString('\n');
			displayParametersBox.Text = _hook.MetadataParameters?.ToString(", ");

			Text = $"Editing {_hook.HookName}";
		}

		private void Save(object sender, EventArgs e)
		{
			_hook.HookName = hookNameBox.Text;
			_hook.HookParameters = hookParametersBox.Text?.Replace(" ", string.Empty).Split(_delimiterComma, StringSplitOptions.RemoveEmptyEntries);
			_hook.ReturnNonNull = returnNonNullCheck.Checked;

			_hook.PatchType = patchTypeBox.Text;
			_hook.PatchMethod = patchMethodBox.Text;
			_hook.PatchReturnType = patchReturnTypeBox.Text;
			_hook.PatchParameters = patchParametersBox.Text?.Replace(" ", string.Empty).Split(_delimiterComma, StringSplitOptions.RemoveEmptyEntries);

			_hook.MetadataDescription = descriptionBox.Text?.Split(_delimiterNewline, StringSplitOptions.RemoveEmptyEntries);
			_hook.MetadataParameters = displayParametersBox.Text?.Split(_delimiterComma, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();

			_hookCategory = comboBox1.Text;

			var oldCategory = Manifest.Current.Hooks[_originalHookCategory];
			var newCategory = Manifest.Current.Hooks[_hookCategory];

			if (oldCategory.Contains(_hook))
			{
				if(_hookCategory != _originalHookCategory)
					oldCategory.Remove(_hook);
			}

			if (!newCategory.Contains(_hook)) newCategory.Add(_hook);

			Main.Instance.Save(null, null);
			Main.Instance.Rebuild();
			Close();
		}

		private void Cancel(object sender, EventArgs e)
		{
			Main.Instance.Rebuild();
			Close();
		}

		private void Delete(object sender, EventArgs e)
		{
			var times = 0;
			var sure = "sure";

			Loop();

			void Loop()
			{
				if (MessageBox.Show(
					$"You'll remove {_hookCategory}'s '{_hook.HookName}'.\n\nThis action has consequences.",
					$"Are you {sure}?", MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					if (times >= 0)
					{
						_hookCategory = comboBox1.Text;

						Manifest.Current.Hooks[_originalHookCategory].Remove(_hook);
						Manifest.Current.Hooks[_hookCategory].Remove(_hook);

						Main.Instance.Rebuild();
						Close();
						return;
					}

					sure += ", sure";
					times++;

					Loop();
				}
			}
		}
	}
}
