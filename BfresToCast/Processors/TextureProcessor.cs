using BfresLibrary.Switch;
using BfresToCast.Utils;
using ImageLibrary;
using ImageLibrary.Formats.Encoders;
using Syroot.NintenTools.NSW.Bntx;

namespace BfresToCast.Processors;

public static class TextureProcessor
{
    public static bool ProcessBNTX(BntxFile bntx, string outDir)
    {
        foreach (Texture texture in bntx.Textures)
        {
            SwitchTexture switchTexture = new SwitchTexture(bntx, texture);
            ExportSettings exportSettings = new ExportSettings { UseComponentChannels = true };

            GenericTextureBase generic = TextureUtils.SwitchTextureToGeneric(switchTexture, false);
            if (TextureUtils.IsHDR(switchTexture.Format) || generic.Type == TextureType.TextureCube)
            {
                generic.Export(Path.Combine(outDir, $"{switchTexture.Name}.dds"), exportSettings);
            }
            else
            {
                generic.Export(Path.Combine(outDir, $"{switchTexture.Name}.png"), exportSettings);
            }
            
            Console.WriteLine($"Saved texture {switchTexture.Name}");
        }
        
        return true;
    }

    public static bool ProcessWiiU(BfresLibrary.WiiU.Texture gx2Tex, string outDir)
    {
        return false; // TODO: WiiU textures not supported atm
    }
}