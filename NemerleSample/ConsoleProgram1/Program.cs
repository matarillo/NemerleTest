using Nemerle.Compiler;
using Nemerle.Completion2;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Reflection;

namespace ConsoleProgram1
{
	class MainClass
	{
		public static void Main(string[] args)
		{
            Foo();
		}

        public static void Foo()
        {
            var currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var targetPath = Path.GetFullPath(Path.Combine(currentPath, @"..\..\..\..\NemerleSolution1\ConsoleApplication1"));
            string[] sources = {
                                   Path.Combine(targetPath, @"MethodTip.n"),
                                   Path.Combine(targetPath, @"Main.n"),
                                   Path.Combine(targetPath, @"Properties\AssemblyInfo.n")
                               };
            string[] assemblies = {
                                      "System.Data, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                                      "System.Drawing, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
                                      "System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
                                  };

            var stub = new EngineCallbackStub(assemblies, sources);
            var engine = EngineFactory.Create(stub, Console.Out, true);

            var init = engine.BeginReloadProject();
            init.AsyncWaitHandle.WaitOne(20000);
            Console.WriteLine(engine.IsProjectAvailable);
            
            var srcIndex = Location.GetFileIndex(sources[0]);
            var src = engine.GetSource(srcIndex);
            var tuple = ReadLocation(sources[0], "CompareTo_inField");
            var result = engine.BeginGetMethodTipInfo(src, tuple.Item1, tuple.Item2);
            result.AsyncWaitHandle.WaitOne();
            var info = result.MethodTipInfo;
            if (info == null)
            {
                Console.WriteLine("not found");
                return;
            }
            for (var i = 0; i < info.GetCount(); i++)
            {
                Console.WriteLine(info.GetName(i));
            }
        }

        public static Tuple<int, int> ReadLocation(string filePath, string tagName)
        {
            var tagName2 = "/*" + tagName;
            var lines = File.ReadAllLines(filePath);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var index = line.IndexOf(tagName);
                if (index >= 0)
                {
                    var str = line.Substring(index + tagName.Length);
                    var offset = 0;
                    var isNegative = false;
                    Match m;
                    if ((m = Regex.Match(str, @":\+{0,1}(\d+)\*/.*")).Success)
                    {
                        offset = int.Parse(m.Groups[1].Value);
                    }
                    else if ((m = Regex.Match(str, @":\-(\d+)\*/.*")).Success)
                    {
                        offset = -1 * int.Parse(m.Groups[1].Value);
                        isNegative = true;
                    }
                    var len = str.IndexOf("*/");
                    var col = (isNegative) ? index + offset : index + len + 2 + offset + tagName.Length;
                    return Tuple.Create(i + 1, col + 1);
                }
            }
            throw new Exception("Tag not found.");
        }
    }
}
