using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace ET
{
    public static class DynamicAssembly
    {
        public static Assembly Build(IEnumerable<string> csFiles)
        {
            List<SyntaxTree> syntaxTrees = new();
            foreach (string csFile in csFiles)
            {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(File.ReadAllText(csFile)));
            }

            List<PortableExecutableReference> references = new();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (assembly.IsDynamic)
                    {
                        continue;
                    }

                    if (assembly.Location == "")
                    {
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }

                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }

            CSharpCompilation compilation = CSharpCompilation.Create(null,
                syntaxTrees.ToArray(),
                references.ToArray(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using MemoryStream memStream = new();

            EmitResult emitResult = compilation.Emit(memStream);
            if (!emitResult.Success)
            {
                StringBuilder stringBuilder = new();
                foreach (Diagnostic diagnostic in emitResult.Diagnostics)
                {
                    stringBuilder.Append($"{diagnostic.GetMessage()}\n");
                }

                throw new Exception($"动态编译失败:\n{stringBuilder}");
            }

            memStream.Seek(0, SeekOrigin.Begin);

            return Assembly.Load(memStream.ToArray());
        }
    }
}
