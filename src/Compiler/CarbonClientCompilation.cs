using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Carbon.Base;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UnityEngine;
using Debug = UnityEngine.Debug;

/*
 *
 * Copyright (c) 2023 Carbon Community 
 * All rights reserved.
 *
 */

namespace Carbon.Client;

#pragma warning disable CS8618

public class CompileThread : BaseThreadedJob
{
	public string FileName { get; set; }
	public string FilePath { get; set; }
	public string Source { get; set; }
	public float CompileTime { get; set; }
	public string Hash { get; set; }

	public Assembly Assembly { get; set; }

	private static List<MetadataReference> references = new();
	private static bool initedReferences;

	public static void CheckReferences()
	{
		if (initedReferences) return;
		initedReferences = true;

		var loadedHarmony0 = false;

		LoadFolder(Path.Combine(Entrypoint.Home, "BepInEx", "system-libs"));
		LoadFolder(Path.Combine(Entrypoint.Home, "BepInEx", "plugins"));
		LoadFolder(Path.Combine(Entrypoint.Home, "BepInEx", "core"));
		LoadFolder(Path.Combine(Entrypoint.Home, "BepInEx", "interop"));

		void LoadFolder(string folder)
		{
			folder = Path.GetFullPath(folder);

			foreach (var file in Directory.GetFiles(folder, "*.dll"))
			{
				if (file.Contains("dobby") || references.Any(x =>
				{
					return Path.GetFileNameWithoutExtension(file.ToLower()) == Path.GetFileNameWithoutExtension(x.Display.ToLower());
				})) continue;

				if (file.Contains("0Harmony"))
				{
					if (loadedHarmony0) continue;
					loadedHarmony0 = true;
				}

				try
				{
					var source = File.ReadAllBytes(file);

					using (var stream = new MemoryStream(source))
					{
						var reference = MetadataReference.CreateFromStream(stream, filePath: file);
						references.Add(reference);
						Debug.Log($"Loading reference: {Path.GetFileName(reference.Display)}");
					}

					Array.Clear(source, 0, source.Length);
					source = null;
				}
				catch { }
			}
		}
	}

	public override void Start()
	{
		CheckReferences();
		base.Start();
	}
	public override void ThreadFunction()
	{
		try
		{
			var start = new TimeSince();
			start = 0;
			var trees = new List<SyntaxTree>();
			var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
			var tree = CSharpSyntaxTree.ParseText(
				Source, options: parseOptions);
			trees.Add(tree);

			var options = new CSharpCompilationOptions(
				OutputKind.DynamicallyLinkedLibrary,
				optimizationLevel: OptimizationLevel.Release,
				deterministic: true, warningLevel: 4);
			var compilation = CSharpCompilation.Create(
				$"Script.{FileName}.{Guid.NewGuid():N}", trees, references, options);

			using (var dllStream = new MemoryStream())
			{
				var emit = compilation.Emit(dllStream);

				foreach (var error in emit.Diagnostics)
				{
					var span = error.Location.GetMappedLineSpan().Span;

					switch (error.Severity)
					{
						case DiagnosticSeverity.Error:
							Debug.LogWarning($"Failed compiling ({FileName}):{span.Start.Character + 1} line {span.Start.Line + 1} [{error.Id}]: {error.GetMessage(CultureInfo.InvariantCulture)}");
							break;
					}
				}

				if (emit.Success)
				{
					var assembly = dllStream.ToArray();

					if (assembly != null)
					{
						Assembly = Assembly.Load(assembly);
					}
				}
			}

			CompileTime = (float)start * 1000f;
		}
		catch (Exception ex)
		{
			Debug.LogWarning($"Compilation failure: {ex}");
		}

		OnFinished();
	}

	public override void OnFinished()
	{
		var info = new FileInfo(FilePath);

		if (Assembly != null)
		{
			foreach (var type in Assembly.GetTypes())
			{
				var attribute = type.GetCustomAttribute<Info>();
				if (attribute == null) continue;

				if (CarbonClientPlugin.Plugins.TryGetValue(attribute.Name, out var existent))
				{
					existent.IUnload();
				}

				Console.WriteLine($"Loaded plugin {attribute.Name}");

				var instance = Activator.CreateInstance(type) as CarbonClientPlugin;
				instance.Info = attribute;
				instance.Info.FilePath = FilePath;
				instance.Info.MD = CompilerHelper.GetMD(info.LastWriteTime.ToString());
				instance._pluginType = type;
				instance.ILoad();

				try
				{
					instance.OnInit();
				}
				catch (Exception ex)
				{
					instance.Log($"Failed OnInit: {ex}");
				}

				CarbonClientPlugin.Plugins[attribute.Name] = instance;
			}
		}
	}
}

public class CompilerHelper
{
	internal static Dictionary<string, string> _watcherSum = new();
	internal static Queue<string> _fileQueue = new();
	internal static string PluginsFolder;
	internal static System.Timers.Timer _timer;
	internal static WaitForSeconds _wfs = new(.05f);

	public static string FormatName(string name)
	{
		return name.Replace(" ", string.Empty).Replace("-", string.Empty);
	}

	public static void Fetch()
	{
		var files = Directory.GetFiles(PluginsFolder, "*.cs");

		foreach (var file in files)
		{
			var formattedFile = Path.GetFullPath(file);
			if (_fileQueue.Contains(formattedFile)) continue;

			var name = Path.GetFileNameWithoutExtension(formattedFile);
			if (CarbonClientPlugin.Plugins.TryGetValue(FormatName(name), out var existentPlugin))
			{
				var info = new FileInfo(file);
				var newMD = GetMD(info.LastWriteTime.ToString());

				if (newMD != existentPlugin.Info.MD)
				{
					existentPlugin.IUnload(true);
					_fileQueue.Enqueue(existentPlugin.Info.FilePath);
					continue;
				}
				else continue;
			}
			else
			{
				_fileQueue.Enqueue(formattedFile);
			}
		}

		var pool = new List<CarbonClientPlugin>();
		foreach (var plugin in CarbonClientPlugin.Plugins) pool.Add(plugin.Value);

		foreach (var plugin in pool)
		{
			if (plugin.Info == null) continue;

			if (_fileQueue.Contains(plugin.Info.FilePath)) continue;

			var exists = File.Exists(plugin.Info.FilePath);

			if (!exists)
			{
				plugin.IUnload(true);
				continue;
			}

			var info = new FileInfo(plugin.Info.FilePath);
			var newMD = GetMD(info.LastWriteTime.ToString());

			if (newMD != plugin.Info.MD)
			{
				plugin.Info.MD = newMD;
				_fileQueue.Enqueue(plugin.Info.FilePath);
			}
		}

		pool.Clear();
		pool = null;
	}

	public static IEnumerator CompileRoutine()
	{
		while (true)
		{
			yield return _wfs;

			try
			{
				Fetch();
			}
			catch (Exception ex) { Debug.LogWarning($"Fetch failed ({ex.Message})\n{ex.StackTrace}"); }

			yield return _wfs;

			if (_fileQueue.Count != 0)
			{
				var file = _fileQueue.Dequeue();
				try
				{
					Compile(file);
				}
				catch (Exception ex) { Debug.LogWarning($"Compilation failed for '{file}' ({ex.Message})\n{ex.StackTrace}"); }
				continue;
			}

			yield return _wfs;
			yield return null;
		}
	}

	public static void Compile(string file)
	{
		file = Path.GetFullPath(file);
		var info = new FileInfo(file);
		var name = Path.GetFileNameWithoutExtension(file);
		var md = GetMD(info.LastWriteTime.ToString());

		if (!_watcherSum.TryGetValue(file, out var existentMD))
		{
			_watcherSum[file] = md;
		}
		else
		{
			if (existentMD == md) return;

			_watcherSum[file] = md;
		}

		if (CarbonClientPlugin.Plugins.TryGetValue(name, out _))
		{
			return;
		}

		var source = File.ReadAllText(file);
		var thread = new CompileThread()
		{
			Source = source,
			FileName = FormatName(Path.GetFileNameWithoutExtension(file)),
			FilePath = file,
			Hash = md
		};
		thread.Start();
	}

	public static string GetMD(string content)
	{
		// var md5 = MD5.Create();
		// var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(content));
		// var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
		// md5.Dispose();
		// md5 = null;
		// return hashString;

		return content;
	}
}
