// CiTo.cs - Ci translator
//
// Copyright (C) 2011  Piotr Fusik
//
// This file is part of CiTo, see http://cito.sourceforge.net
//
// CiTo is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// CiTo is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with CiTo.  If not, see http://www.gnu.org/licenses/

using System;
using System.Collections.Generic;
using System.IO;

namespace Foxoft.Ci
{

public class CiTo
{
	static void Usage()
	{
		Console.WriteLine("Usage: cito [OPTIONS] -l LANG -o PATH INPUT.ci");
		Console.WriteLine("Options:");
		Console.WriteLine("-l c     Translate to C89");
		Console.WriteLine("-l c99   Translate to C99");
		Console.WriteLine("-l cs    Translate to C#");
		Console.WriteLine("-l java  Translate to Java");
		Console.WriteLine("-l js    Translate to JavaScript");
		Console.WriteLine("-o PATH  Write to the specified directory (Java) or file (other languages)");
		Console.WriteLine("-n NAME  Specify C# namespace or Java package");
		Console.WriteLine("-D NAME  Define conditional compilation symbol");
		Console.WriteLine("-I DIR   Add directory for BinaryResource");
		Console.WriteLine("--help   This help");
	}

	public static int Main(string[] args)
	{
		HashSet<string> preSymbols = new HashSet<string>();
		preSymbols.Add("true");
		List<string> inputFiles = new List<string>();
		List<string> searchDirs = new List<string>();
		string lang = null;
		string outputPath = null;
		string namespace_ = null;
		for (int i = 0; i < args.Length; i++) {
			string arg = args[i];
			if (arg[0] == '-') {
				switch (arg) {
				case "--help":
					Usage();
					return 0;
				case "--version":
					Console.WriteLine("cito 0.1.0");
					return 0;
				case "-l":
					lang = args[++i];
					break;
				case "-o":
					outputPath = args[++i];
					break;
				case "-n":
					namespace_ = args[++i];
					break;
				case "-D":
					string symbol = args[++i];
					if (symbol == "true" || symbol == "false")
						throw new ApplicationException(symbol + " is reserved");
					preSymbols.Add(symbol);
					break;
				case "-I":
					searchDirs.Add(args[++i]);
					break;
				default:
					throw new ApplicationException("Unknown option: " + arg);
				}
			}
			else {
				inputFiles.Add(arg);
			}
		}
		if (lang == null || outputPath == null || inputFiles.Count == 0) {
			Usage();
			return 1;
		}

		CiParser parser = new CiParser();
		parser.PreSymbols = preSymbols;
		foreach (string inputFile in inputFiles) {
			try {
				parser.Parse(File.OpenText(inputFile));
			} catch (Exception ex) {
				Console.Error.WriteLine("{0}({1}): ERROR: {2}", inputFile, parser.InputLineNo, ex.Message);
				parser.PrintMacroStack();
				if (parser.CurrentMethod != null)
					Console.Error.WriteLine("   in method {0}", parser.CurrentMethod.Name);
//				return 1;
				throw;
			}
		}
		CiProgram program = parser.Program;

		CiResolver resolver = new CiResolver();
		resolver.SearchDirs = searchDirs;
		try {
			resolver.Resolve(program);
		} catch (Exception ex) {
			if (resolver.CurrentMethod != null)
				Console.Error.WriteLine("   in method {0}", resolver.CurrentMethod.Name);
//			return 1;
			throw;
		}

		SourceGenerator gen;
		switch (lang) {
		case "c": gen = new GenC89(); break;
		case "c99": gen = new GenC(); break;
		case "cs": gen = new GenCs(namespace_); break;
		case "java": gen = new GenJava(namespace_); break;
		case "js": gen = new GenJs(); break;
		default: throw new ApplicationException("Unknown language: " + lang);
		}
		gen.OutputPath = outputPath;
		gen.Write(program);
		return 0;
	}
}
}
