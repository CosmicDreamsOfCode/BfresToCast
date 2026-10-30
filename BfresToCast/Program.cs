using BfresLibrary;
using BfresToCast.Processors;
using BfresToCast.Utils;
using SarcLibrary;
using SarcHelper = BfresToCast.Utils.SarcHelper;

namespace BfresToCast;

public class Program
{
    static void Main(string[] args)
    {
        // this is a super niche use case, this is just for if the user opens the exe instead of drag-dropping files onto it
        // useful for debugging the program
        if (args.Length == 0)
        {
            string? path;
            while (true)
            {
                Console.WriteLine("Please input the path to the input file to convert.");
                path = Console.ReadLine().Trim('"');
                if (path == "exit")
                    return;

                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    break;

                Console.WriteLine("Invalid path.");
            }

            args = new string[] { path };
        }

        bool success = true;

        foreach (string file in args)
        {
            MemoryStream stream = new MemoryStream();
            byte[] data = File.ReadAllBytes(file);
            data = DecompressionHelpers.DecompressIfNeeded(data);
            bool isSarc = SarcHelper.CheckSarc(data);
            if (isSarc)
            {
                Sarc rootSarc = Sarc.FromBinary(data);
                ProcessSarc(rootSarc, Path.GetFileName(file));

                void ProcessSarc(Sarc sarc, string name)
                {
                    foreach (KeyValuePair<string, ArraySegment<byte>> sarcFile in sarc)
                    {
                        byte[] fileData = DecompressionHelpers.DecompressIfNeeded(sarcFile.Value.ToArray());
                        if (SarcHelper.CheckSarc(fileData))
                        {
                            Sarc subSarc = Sarc.FromBinary(fileData);
                            ProcessSarc(subSarc, sarcFile.Key);
                        }
                        else if (SarcHelper.CheckBfres(fileData))
                        {
                            stream = new MemoryStream(fileData);
                            ResFile modelRes = new ResFile(stream);
                            string newFile = file;
                            if (sarcFile.Key.ToLower() == "output.bfres")
                            {
                                newFile = Path.Combine(Path.GetDirectoryName(newFile), name);
                            }
                            
                            success &= BfresProcessor.Process(modelRes, newFile, true);
                        }
                    }
                }
            }
            else
            {
                stream = new MemoryStream(data);
                ResFile modelRes = new ResFile(stream);
                success &= BfresProcessor.Process(modelRes, file, false);
            }

            Console.WriteLine();
        }

        if (!success)
        {
            Console.WriteLine("Some errors occurred, See above for info.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}