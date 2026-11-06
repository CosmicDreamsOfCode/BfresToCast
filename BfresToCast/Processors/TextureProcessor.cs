using BfresLibrary;
using BfresLibrary.Switch;
using BfresToCast.Utils;
using BfresToCast.Utils.Helpers;
using ImageLibrary;
using Syroot.NintenTools.NSW.Bntx;

namespace BfresToCast.Processors;

public static class TextureProcessor
{
    public static bool Process(ResFile modelRes, List<string> referencedTextures, string outputDir)
    {
        bool success = true;

        // Prepare external textures
        List<string> externalTextures = new List<string>();
        try
        {
            success &= ExternalFileHelpers.GetExternalTextures(modelRes, referencedTextures, outputDir, out externalTextures);
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