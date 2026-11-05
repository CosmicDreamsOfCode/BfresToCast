using BfresLibrary;
using BymlLibrary;
using SarcLibrary;
using System.Text;
using ZstdNet;

namespace BfresToCast.Utils
{
    public class ExternalFileHelpers
    {
        private static string GameRootDirectory = "";

        private static string ConfigName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GameRoots.cfg");

        public static List<string> Config = new List<string>();

        public static ResFile ExternalStringsFile;

        public static byte[] ZstdDictionary;

        public static void ProcessConfig(string pathToSearch) // TODO: fix it adding to Config for every time this is called (oops)
        {
            // User can put paths in this config to automatch to texture folder
            string configName = Path.GetFullPath(ConfigName);
            if (File.Exists(configName))
            {
                foreach (string line in File.ReadLines(configName))
                {
                    Config.Add(line);
                }

                foreach (string line in Config)
                {
                    if (line.StartsWith("#"))
                        continue;

                    if (pathToSearch.Contains(Path.GetDirectoryName(line)))
                    {
                        GameRootDirectory = line;
                        break;
                    }
                }
            }

            if (GameRootDirectory == "")
            {
                Console.WriteLine("Please provide the path to the root of your game dump. (Press Enter to skip)");

                GameRootDirectory = Console.ReadLine();

                if (GameRootDirectory == "")
                {
                    GameRootDirectory = "!skip";
                }
            }
        }

        public static void SaveConfig(List<string> lines)
        {
            File.WriteAllLines(Path.GetFullPath(ConfigName), lines);
        }

        public static void GetZstdDictionary(string path)
        {
            if (ZstdDictionary == null)
            {
                ProcessConfig(path);
                string dictSarc = Path.Combine(GameRootDirectory, @"Pack\ZsDic.pack.zs");

                if (File.Exists(dictSarc))
                {
                    byte[] data = File.ReadAllBytes(dictSarc);
                    data = DecompressionHelpers.DecompressIfNeeded(data);

                    Sarc sarc = Sarc.FromBinary(data);

                    if (sarc.TryGetValue("zs.zsdic", out ArraySegment<byte> dictData))
                    {
                        ZstdDictionary = dictData.ToArray();
                    }
                }
            }
        }

        public static void GetExternalBinaryStrings(string path)
        {
            if (ExternalStringsFile == null)
            {
                ProcessConfig(path);

                string ebsPath = Path.Combine(GameRootDirectory, @"Shader\ExternalBinaryString.bfres.zs");

                if (File.Exists(ebsPath))
                {
                    byte[] data = File.ReadAllBytes(ebsPath);
                    data = DecompressionHelpers.DecompressIfNeeded(data);

                    MemoryStream stream = new MemoryStream(data);
                    ExternalStringsFile = new ResFile(stream, true);
                }
            }
        }

        public static bool GetExternalTextures(ResFile modelRes, List<string> referencedTextures, string outputDir, out List<string> externalTextures)
        {
            bool success = true;
            externalTextures = new List<string>();

            // Check if a lookup BYML exists
            Byml byml = null;
            if (modelRes.ExternalFiles.TryGetValue("textureTable.byml", out ExternalFile file))
            {
                byml = Byml.FromBinary(file.Data);
                referencedTextures.Clear();

                foreach (Byml tex in byml.GetMap().Values)
                {
                    referencedTextures.Add(tex.GetString());
                }
            }

            bool refButNoTex = referencedTextures.Count > 0 && modelRes.Textures.Count == 0;

            if ((modelRes.VersionMajor >= 10 && refButNoTex) || byml != null)
            {

                ProcessConfig(outputDir);

                if (GameRootDirectory != "!skip")
                {
                    foreach (string texture in referencedTextures)
                    {
                        bool hasVariations = int.TryParse(Path.GetExtension(texture).TrimStart('.'), out int index);

                        string baseTextureName = Path.GetFileNameWithoutExtension(texture);
                        string textureDirectory = Path.Combine(GameRootDirectory, "Tex");

                        string fileName = Path.Combine(textureDirectory, $"{texture}.bntx.zs");

                        if (hasVariations)
                        {
                            foreach (string variant in Directory.GetFiles(textureDirectory, $"*{baseTextureName}*"))
                            {
                                externalTextures.Add(variant);
                            }
                            continue;
                        }

                        if (File.Exists(fileName))
                        {
                            externalTextures.Add(fileName);
                            continue;
                        }

                        Console.WriteLine($"Failed to find external texture : {texture}");

                        success = false;
                    }

                    if (success && !Config.Contains(GameRootDirectory))
                    {
                        // Add this path to the config if it's not already there
                        Config.Add(GameRootDirectory);
                        SaveConfig(Config);
                    }
                }
            }

            return success;
        }
    }

    public class DecompressionHelpers
    {
        public static byte[] DecompressIfNeeded(byte[] data)
        {
            byte[] readId = data.Take(4).ToArray();
            byte[] zstdId = [0x28, 0xB5, 0x2F, 0xFD];
            byte[] yaz0Id = Encoding.ASCII.GetBytes("Yaz0");

            if (readId.SequenceEqual(zstdId))
            {
                return DecompressZStd(data);
            }
            else if (readId.SequenceEqual(yaz0Id))
            {
                return DecompressYaz0(data);
            }

            return data;
        }

        public static byte[] DecompressZStd(byte[] data)
        {
            DecompressionOptions options = new DecompressionOptions();

            if (ExternalFileHelpers.ZstdDictionary != null)
            {
                options = new DecompressionOptions(ExternalFileHelpers.ZstdDictionary);
            }

            Decompressor decompressor = new Decompressor(options);

            return decompressor.Unwrap(data); ;
        }

        public unsafe static byte[] DecompressYaz0(byte[] Data)
        {
            UInt32 leng = (uint)(Data[4] << 24 | Data[5] << 16 | Data[6] << 8 | Data[7]);
            byte[] Result = new byte[leng];
            int Offs = 16;
            int dstoffs = 0;
            while (true)
            {
                byte header = Data[Offs++];
                for (int i = 0; i < 8; i++)
                {
                    if ((header & 0x80) != 0) Result[dstoffs++] = Data[Offs++];
                    else
                    {
                        byte b = Data[Offs++];
                        int offs = ((b & 0xF) << 8 | Data[Offs++]) + 1;
                        int length = (b >> 4) + 2;
                        if (length == 2) length = Data[Offs++] + 0x12;
                        for (int j = 0; j < length; j++)
                        {
                            Result[dstoffs] = Result[dstoffs - offs];
                            dstoffs++;
                        }
                    }
                    if (dstoffs >= leng) return Result;
                    header <<= 1;
                }
            }
        }
    }

    public class SarcHelper
    {
        public static bool CheckSarc(byte[] data)
        {
            if (data.Take(4).ToArray().SequenceEqual(Encoding.ASCII.GetBytes("SARC")))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool CheckBfres(byte[] data)
        {
            if (data.Take(4).ToArray().SequenceEqual(Encoding.ASCII.GetBytes("FRES")))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
