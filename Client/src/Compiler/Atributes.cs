using System;
using Carbon.Client;

/*
 *
 * Copyright (c) 2023 Carbon Community 
 * All rights reserved.
 *
 */

[AttributeUsage(AttributeTargets.Class)]
public class Info : Attribute
{
	public string Name { get; set; }
	public string FilePath { get; set; }
	public string MD { get; set; }

	public Info() { }
	public Info(string name)
	{
		Name = CompilerHelper.FormatName(name);
	}
}

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
	public string Name { get; set; }

	public CommandAttribute() { }
	public CommandAttribute(string name)
	{
		Name = name;
	}
}
