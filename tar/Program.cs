using System;
using System.IO;
using System.Threading.Tasks;
using tar_cs;

namespace tar
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("USAGE: ArchiveMaker fileName.tar <fileToAdd.ext> [. more files..]");
                return;
            }
            using (var archUsTar = File.Create(args[0]))
            using (var tar = new TarWriter(archUsTar))
            {
                await tar.WriteDirectoryEntryAsync("test_dir");
                for (int i = 1; i < args.Length; ++i)
                {
                    await tar.WriteAsync(args[i]);
                }
                
            }

            Console.WriteLine("Examine tar file: {0}", args[0]);
            using (var examiner = File.OpenRead(args[0]))
            {
                TarReader tar = new TarReader(examiner);
                while (await tar.MoveNextAsync(true))
                {
                    Console.WriteLine("File: {0}, Owner: {1}", tar.FileInfo.FileName, tar.FileInfo.UserName);
                }
            }

            using (var unarchFile = File.OpenRead(args[0]))
            {
                TarReader reader = new TarReader(unarchFile);
                await reader.ReadToEndAsync("out_dir\\data");
            }
        }
    }
}