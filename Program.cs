using System.Diagnostics;

namespace UnluacExecutor
{
    class Program
    {
        private static readonly byte[] LuaHeader = [0x4c, 0x75, 0x61, 0x51];
        static void Main(string[] args)
        {
            string inputPath = args[0];
            if (string.IsNullOrEmpty(inputPath))
            {
                Console.WriteLine("输入路径为空。");
                return;
            }

            if (Directory.Exists(inputPath))
            {
                var outputDirectory = inputPath + ".src";
                ProcessDirectory(inputPath, outputDirectory);
            }
            else if (File.Exists(inputPath))
            {
                ProcessFile(inputPath, Path.ChangeExtension(inputPath, ".src.lua"));
            }
            else
            {
                Console.WriteLine("输入路径无效。");
            }
        }

        private static void ProcessDirectory(string directoryPath, string outputDirectory)
        {
            string[] files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(directoryPath, file);
                ProcessFile(file, Path.Combine(outputDirectory, relativePath));
            }
        }

        private static void ProcessFile(string filePath, string outputPath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                if (IsLuaCompiled(filePath))
                    ExecuteUnluac(filePath, outputPath);
                else
                    File.Copy(filePath, outputPath, true);
                Console.WriteLine($"{filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理文件失败：{filePath} 错误信息：{ex.Message}");
            }
        }

        private static bool IsLuaCompiled(string filePath)
        {
            using (var fs = File.OpenRead(filePath))
            {
                if (fs.Length < 5)
                    return false;
                BinaryReader br = new(fs);
                br.BaseStream.Seek(1, SeekOrigin.Begin);
                var header = br.ReadBytes(4);
                return header.SequenceEqual(LuaHeader);
            }

        }

        private static void ExecuteUnluac(string inputPath, string outputPath)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                FileName = "java",
                Arguments = $"-jar unluac.jar --rawstring \"{inputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            using (Process process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();

                using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                {
                    using (Stream s = process.StandardOutput.BaseStream)
                    {
                        s.CopyTo(fs);
                    }
                    process.WaitForExit();
                }

                if (process.ExitCode != 0)
                {
                    Console.WriteLine("Standard Output:");
                    Console.WriteLine(process.StandardOutput.ReadToEnd());
                    Console.WriteLine("Standard Error:");
                    Console.WriteLine(process.StandardError.ReadToEnd());
                    throw new Exception($"unluac.jar 执行失败，返回值 {process.ExitCode}。");
                }
            }
        }
    }
}