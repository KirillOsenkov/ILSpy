﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.Tests.Helpers;
using ICSharpCode.Decompiler.TypeSystem;
using NUnit.Framework;

namespace ICSharpCode.Decompiler.Tests.Util
{
	[TestFixture]
	public class SequencePointTests
	{
		[Test]
		public void BasicSequencePoints()
		{
			TestCreateSequencePoints(@"class C
{
	void M()
	{
		int i = 0;
		int j = 1;

		System.Action a = () => 
		{
			int k = 2;
		};
	}
}",
				"int num = 0;",
				"int num2 = 1;");
		}

		private DecompilerSettings decompilerSettings = new DecompilerSettings {
			ThrowOnAssemblyResolveErrors = false,
			AsyncAwait = false,
			AnonymousMethods = false,
			AnonymousTypes = false,
			ArrayInitializers = false,
			AwaitInCatchFinally = false,
			ShowDebugInfo = true
		};

		[Test]
		public void RealDll()
		{
			var decompiler = new CSharp.CSharpDecompiler(@"C:\temp\1\src\console\bin\Debug\net472\console.exe", decompilerSettings);
			TestCreateSequencePoints(decompiler, "console.Program");
		}

		private void TestCreateSequencePoints(string code, params string[] expectedSequencePoints)
		{
			var decompiler = Tester.GetDecompilerForSnippet(code);
			TestCreateSequencePoints(decompiler, "C", expectedSequencePoints);
		}

		private void TestCreateSequencePoints(CSharp.CSharpDecompiler decompiler, string typeName, params string[] expectedSequencePoints)
		{
			var tree = decompiler.DecompileType(new FullTypeName(typeName));

			var output = new StringWriter();
			tree.AcceptVisitor(new InsertParenthesesVisitor { InsertParenthesesForReadability = true });
			TokenWriter tokenWriter = new TextWriterTokenWriter(output);
			tokenWriter = new InsertMissingTokensDecorator(tokenWriter, (ILocatable)tokenWriter);
			var formattingOptions = FormattingOptionsFactory.CreateSharpDevelop();
			tree.AcceptVisitor(new CSharpOutputVisitor(tokenWriter, formattingOptions));

			var functionsWithSequencePoints = decompiler.CreateSequencePoints(tree);
			var finalText = output.ToString();

			var lines = finalText.Split(new[] { output.NewLine }, StringSplitOptions.None);

			var actualSequencePoints = new List<string>();
			foreach (var sequencePoint in functionsWithSequencePoints.Values.SelectMany(f => f)) {
				if (sequencePoint.IsHidden) {
					continue;
				}

				var line = lines[sequencePoint.StartLine - 1];
				var text = line.Substring(sequencePoint.StartColumn - 1, sequencePoint.EndColumn - sequencePoint.StartColumn);
				actualSequencePoints.Add(text);
			}

			Assert.True(Enumerable.SequenceEqual(expectedSequencePoints, actualSequencePoints));
		}
	}
}
