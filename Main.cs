#if NETFRAMEWORK
using System.Windows.Forms;
#endif

using Newtonsoft.Json;

namespace ZZZRPCDumper
{

    internal class Program
    {

        private const string HelpParam = "--help";
        private const string HelpParamShort = "-h";
        private const string FullDumpParam = "--fulldump";
        private const string FullDumpParamShort = "-fd";

        [STAThread]
        static int Main(string[] args)
        {
            // var assemblyPath = "";
            var assemblyPath = @"D:\source-code-repos\Il2CppDumper-ZZZ\Il2CppDumper\bin\Debug\net472\DummyDll\Foundation.dll";
            var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "");

#if NETFRAMEWORK
            // If args are empty and assembly path is not defined, make a pop up asking for the assembly (ONLY .NET 4.7.2)
            if (args.Length == 0 && assemblyPath == "") {
                var openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Foundation.dll|*";
                Console.WriteLine("Please select the Assembly with the RPC definitions.");
                if (openFileDialog.ShowDialog() == DialogResult.OK) {
                    assemblyPath = openFileDialog.FileName;
                }
            }
#endif

            if (args.Length > 0)
            {
                foreach (var arg in args)
                {
                    if (arg == HelpParam || arg == HelpParamShort)
                    {
                        ShowHelp();
                        return 1;
                    }
                    else
                    {
                        Console.WriteLine($"Unrecognized option {arg}, use -h for help.");
                    }
                }
            }

            if (assemblyPath == "")
            {
                Console.WriteLine("Assembly path not found!");
                Console.WriteLine();
                ShowHelp();
                Console.WriteLine();
                Exit(-1);
                return -1;
            }

            var parser = new Parser.RPCParser(assemblyPath);
            var rpcs = parser.Parse();

            Console.WriteLine("Found RPCs: " + rpcs.Count);

            foreach (var rpc in rpcs)
            {
                // Console.WriteLine(rpc);
            }

            File.WriteAllText(Path.Combine(outputPath, "rpcs.json"), JsonConvert.SerializeObject(rpcs, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            File.WriteAllText(Path.Combine(outputPath, "types.json"), JsonConvert.SerializeObject(parser.RPCTypes, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

            Console.WriteLine("Done!");
            Exit();
            return 0;
        }

        static void ShowHelp()
        {
            Console.WriteLine("Usage: ZZZRPCDumper [parameters]");
            Console.WriteLine("Possible parameters:");
            Console.WriteLine($"\t{HelpParam}, {HelpParamShort} - Optional. Show this help");
        }

        static void Exit(int code = 0)
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(code);
        }
    }

}