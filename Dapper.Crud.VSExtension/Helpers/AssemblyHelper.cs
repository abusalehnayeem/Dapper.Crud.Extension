﻿using System;
using System.CodeDom.Compiler;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;

namespace Dapper.Crud.VSExtension.Helpers
{
    public static class AssemblyHelper
    {
        private static Assembly BuildAssembly(string code)
        {
            using (var compiler = new Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider())
            {
                //Clean up
                Environment.SetEnvironmentVariable("ROSLYN_COMPILER_LOCATION", null, EnvironmentVariableTarget.Process);

                var compilerparams = new CompilerParameters
                {
                    GenerateExecutable = false,
                    GenerateInMemory = true
                };

                var assemblies = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(a => !a.IsDynamic)
                    .Select(a => a.Location);

                compilerparams.ReferencedAssemblies.AddRange(assemblies.ToArray());

                compilerparams.ReferencedAssemblies.Add(@"C:\Users\thiago.azevedo\AppData\Local\Microsoft\VisualStudio\15.0_b7d486ffExp\Extensions\Thiago Loureiro\Dapper Crud Generator\2.8\EnumLibrary.dll");

                CompilerResults results = compiler.CompileAssemblyFromSource(compilerparams, code);

                if (results.Errors.HasErrors)
                {
                    StringBuilder errors = new StringBuilder("Compiler Errors :\r\n");
                    foreach (CompilerError error in results.Errors)
                    {
                        errors.Append($"Line {error.Line},{error.Column}\t: {error.ErrorText}\n");
                    }

                    compiler.Dispose();

                    throw new Exception(errors.ToString());
                }
                else
                {
                    return results.CompiledAssembly;
                }
            }
        }

        public static object ExecuteCode(string code, string namespacename, string classname, bool isstatic)
        {
            Assembly asm = BuildAssembly(code);
            classname = FixClassName(classname);

            object instance = null;
            Type type = null;

            if (isstatic)
            {
                type = asm.GetType(namespacename + "." + classname);
            }
            else
            {
                instance = asm.CreateInstance(namespacename + "." + classname);
                type = instance.GetType();
            }
            return instance;
        }

        private static string FixClassName(string classname)
        {
            if (classname.Contains("\\"))
            {
                var str = classname.Split('\\');
                classname = str.Last(); // last item of the array
            }
            return classname;
        }
    }
}