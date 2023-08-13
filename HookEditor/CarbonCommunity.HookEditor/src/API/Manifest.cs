using Carbon.Extensions;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System;

public class Manifest
{
	public static Manifest Current { get; set; } = new();

	public Dictionary<string, List<Hook>> Hooks { get; set; } = new();

	public class Hook
	{
		internal const BindingFlags _flags =
			BindingFlags.Instance
			| BindingFlags.Static
			| BindingFlags.Public
			| BindingFlags.NonPublic;

		public string HookName { get; set; }
		public string[] HookParameters { get; set; }

		public bool ReturnNonNull { get; set; }
		public string PatchType { get; set; }
		public string PatchMethod { get; set; }
		public string PatchReturnType { get; set; }
		public string[] PatchParameters { get; set; }

		public string[] MetadataDescription { get; set; }
		public string[] MetadataParameters { get; set; }

		[JsonIgnore]
		public bool IsInvalid { get; set; }

		[JsonIgnore]
		public string Category { get; set; }

		[JsonIgnore]
		public bool IsPostfix => string.IsNullOrEmpty(PatchReturnType);
	}
}
