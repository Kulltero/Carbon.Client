using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Carbon.Extensions;
using Microsoft.CodeAnalysis;

namespace Carbon.Client
{
	internal class References
	{
		internal static bool initedReferences = false;

		public static async Task Load()
		{
			if (initedReferences) return;
			initedReferences = true;

			var references = new List<string>();
			var loadedHarmony0 = false;

			LoadFolder(Path.Combine(Entrypoint.Home, "BepInEx", "system-libs"));
			LoadFolder(Path.Combine(Entrypoint.Home, "BepInEx", "unity-libs"));
			// LoadFolder(Path.Combine(Entrypoint.Home, "BepInEx", "plugins"));
			// LoadFolder(Path.Combine(Entrypoint.Home, "BepInEx", "core"));
			// LoadFolder(Path.Combine(Entrypoint.Home, "BepInEx", "interop"));

			async void LoadFolder(string folder)
			{
				folder = Path.GetFullPath(folder);

				foreach (var file in Directory.GetFiles(folder, "*.dll"))
				{
					if (file.Contains("dobby") || references.Any(x =>
					{
						return Path.GetFileNameWithoutExtension(file.ToLower()) == Path.GetFileNameWithoutExtension(x);
					})) continue;

					if (file.Contains("0Harmony"))
					{
						if (loadedHarmony0) continue;
						loadedHarmony0 = true;
					}

					try
					{
						var source = File.ReadAllBytes(file);
						Assembly.Load(source);

						references.Add(file);

						Array.Clear(source, 0, source.Length);
						source = null;
					}
					catch { }

					await AsyncEx.WaitForSeconds(0.1f);
				}
			}
		}
	}
}
