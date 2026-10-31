using BfresLibrary;
using BfresLibrary.Switch;
using BfresToCast.Utils;
using BymlLibrary;
using ImageLibrary;
using Syroot.NintenTools.NSW.Bntx;

namespace BfresToCast.Processors;

public static class TextureProcessor
{
    private static string ExternalTextureDirectory = "";

    private static string ConfigName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExternalTextureFolders.cfg");

    private static List<string> Config = new List<string>();

    public static bool Process(ResFile modelRes, List<string> referencedTextures, string outputDir)
    {
        bool success = true;

        // Prepare external textures
        List<string> externalTextures = new List<string>();
        try
        {
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

                        if (outputDir.Contains(Path.GetDirectoryName(line)))
                        {
                            ExternalTextureDirectory = line;
                            break;
                        }
                    }
                }

                if (ExternalTextureDirectory == "")
                {
                    Console.WriteLine("Please provide a path to search for textures. (Press Enter to skip)");

                    ExternalTextureDirectory = Console.ReadLine();

                    if (ExternalTextureDirectory == "")
                    {
                        ExternalTextureDirectory = "!skip";
                    }
                }

                if (ExternalTextureDirectory != "!skip")
                {
                    foreach (string texture in referencedTextures)
                    {
                        bool hasVariations = int.TryParse(Path.GetExtension(texture).TrimStart('.'), out int index);

                        string baseTextureName = Path.GetFileNameWithoutExtension(texture);
                        string fileName = Path.Combine(ExternalTextureDirectory, $"{texture}.bntx.zs");

                        if (hasVariations)
                        {
                            foreach (string variant in Directory.GetFiles(ExternalTextureDirectory, $"*{baseTextureName}*"))
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

                    if (success && !Config.Contains(ExternalTextureDirectory))
                    {
                        // Add this path to the config if it's not already there
                        Config.Add(ExternalTextureDirectory);
                        File.WriteAllLines(configName, Config);
                    }
                }
            } // if (modelRes.VersionMajor >= 10 && referencedTextures.Count > 0 && modelRes.Textures.Count == 0)
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to search for external textures: {e}");
            success = false;
        }
        

        if (externalTextures.Count > 0 || modelRes.Textures.Count > 0)
        {
            string textureOutputDir = Path.Combine(outputDir, "Textures");
            Directory.CreateDirectory(textureOutputDir);

            if (modelRes.IsPlatformSwitch)
            {
                if (modelRes.ExternalFiles.TryGetValue("textures.bntx", out ExternalFile file) && file.LoadedFileData is BntxFile bntx)
                {
                    success &= ProcessBNTX(bntx, textureOutputDir);
                }

                if (externalTextures.Count > 0)
                {
                    foreach (string texture in externalTextures)
                    {
                        MemoryStream stream = new MemoryStream();
                        byte[] data = File.ReadAllBytes(texture);
                        data = DecompressionHelpers.DecompressIfNeeded(data);
                        stream = new MemoryStream(data);

                        BntxFile exBntx = new BntxFile(stream);
                        try
                        {
                            success &= ProcessBNTX(exBntx, textureOutputDir);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Failed to process BNTX {exBntx.Name}: {e}");
                            success = false;
                        }
                    }
                }
            }
            else
            {
                try
                {
                    success &= ProcessWiiU(modelRes, textureOutputDir);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to process Wii U textures: {e}");
                    success = false;
                }
            }
        }

        return success;
    }

    private static bool ProcessBNTX(BntxFile bntx, string outDir)
    {
        ExportSettings exportSettings = new ExportSettings { UseComponentChannels = true };
        bool isOrin = bntx.PlatformTarget == "Ounc";

        foreach (Texture texture in bntx.Textures)
        {
            SwitchTexture switchTexture = new SwitchTexture(bntx, texture);
            GenericTextureBase generic = TextureUtils.SwitchTextureToGeneric(switchTexture, isOrin);

            if (TextureUtils.IsHDR(switchTexture.Format) || generic.Type == TextureType.TextureCube)
            {
                generic.Export(Path.Combine(outDir, $"{switchTexture.Name}.dds"), exportSettings);
            }
            else
            {
                generic.Export(Path.Combine(outDir, $"{switchTexture.Name}.png"), exportSettings);
            }
            
            Console.WriteLine($"Saved texture : {switchTexture.Name}");
        }
        
        return true;
    }

    private static bool ProcessWiiU(ResFile modelRes, string outDir)
    {
        ExportSettings exportSettings = new ExportSettings { UseComponentChannels = true };

        foreach (BfresLibrary.WiiU.Texture gx2Tex in modelRes.Textures.Values)
        {
            GenericTextureBase generic = TextureUtils.GX2TextureToGeneric(gx2Tex);
            generic.Export(Path.Combine(outDir, $"{gx2Tex.Name}.png"), exportSettings);

            Console.WriteLine($"Saved texture : {gx2Tex.Name}");
        }

        return true;
    }
}