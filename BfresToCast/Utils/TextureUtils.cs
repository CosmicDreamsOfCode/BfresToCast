using BfresLibrary.Switch;
using BfresLibrary.Swizzling;
using ImageLibrary;
using ImageLibrary.PlatformSwizzle;
using Syroot.NintenTools.NSW.Bntx.GFX;

namespace BfresToCast.Utils;

public static class TextureUtils
{
    #region Switch

    public static GenericTextureBase SwitchTextureToGeneric(SwitchTexture switchTexture, bool isOrin)
    {
        GenericTextureBase genericTex = new GenericTextureBase();
        genericTex.PlatformSwizzle = new PlatformSwizzleSwitch() { IsOrin = isOrin };

        genericTex.Name = switchTexture.Name;
        genericTex.Width = switchTexture.Width;
        genericTex.Height = switchTexture.Height;
        genericTex.Depth = switchTexture.Depth;
        genericTex.Data = GetSwizzledData(switchTexture);
        genericTex.RedComponent = ChannelSwapList[switchTexture.Texture.ChannelRed];
        genericTex.GreenComponent = ChannelSwapList[switchTexture.Texture.ChannelGreen];
        genericTex.BlueComponent = ChannelSwapList[switchTexture.Texture.ChannelBlue];
        genericTex.AlphaComponent = ChannelSwapList[switchTexture.Texture.ChannelAlpha];
        genericTex.MipCount = switchTexture.MipCount;
        genericTex.ArrayCount = switchTexture.ArrayLength;
        genericTex.ImageFormat = new ImageFormat(FormatList[switchTexture.Format]);
        genericTex.Type = SurfaceDimList[switchTexture.Texture.SurfaceDim];

        return genericTex;
    }

    public static byte[] GetSwizzledData(SwitchTexture texture)
    {
        List<byte[]> list = new List<byte[]>();

        for (int m = 0; m < texture.MipCount; m++)
        {
            for (int a = 0; a < texture.ArrayLength; a++)
            {
                list.Add(texture.Texture.TextureData[a][m]);
            }
        }

        return ByteUtils.CombineArray(list.ToArray());
    }

    public static bool IsHDR(SurfaceFormat fmt)
    {
        string fmtName = fmt.ToString();
        if (fmtName.Contains("FLOAT") || fmtName.Contains("INT"))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private static Dictionary<ChannelType, TextureChannelType> ChannelSwapList = new Dictionary<ChannelType, TextureChannelType>()
    {
        { ChannelType.Zero, TextureChannelType.Zero },
        { ChannelType.One, TextureChannelType.One },
        { ChannelType.Red, TextureChannelType.Red },
        { ChannelType.Green, TextureChannelType.Green },
        { ChannelType.Blue, TextureChannelType.Blue },
        { ChannelType.Alpha, TextureChannelType.Alpha },
    };

    public static Dictionary<SurfaceDim, TextureType> SurfaceDimList = new Dictionary<SurfaceDim, TextureType>()
    {
        { SurfaceDim.Dim1D, TextureType.Texture1D },
        { SurfaceDim.Dim2D, TextureType.Texture2D },
        { SurfaceDim.Dim3D, TextureType.Texture3D },
        { SurfaceDim.DimCube, TextureType.TextureCube },
        { SurfaceDim.Dim1DArray, TextureType.Texture2DArray}, //close enough ig
        { SurfaceDim.Dim2DArray, TextureType.Texture2DArray },
        { SurfaceDim.Dim2DMsaa, TextureType.Texture2D },
        { SurfaceDim.Dim2DMsaaArray, TextureType.Texture2DArray },
        { SurfaceDim.DimCubeArray, TextureType.TextureCubeArray },
    };

    public static Dictionary<SurfaceFormat, TextureFormat> FormatList = new Dictionary<SurfaceFormat, TextureFormat>()
    {
        { SurfaceFormat.R8_G8_B8_A8_UNORM, TextureFormat.RGBA8_UNORM },
        { SurfaceFormat.R8_G8_B8_A8_SRGB, TextureFormat.RGBA8_SRGB },
        { SurfaceFormat.R8_G8_UNORM, TextureFormat.RG8_UNORM },
        { SurfaceFormat.R8_G8_SNORM, TextureFormat.RG8_SNORM},
        { SurfaceFormat.R8_UNORM, TextureFormat.R8_UNORM },
        { SurfaceFormat.R16_UNORM, TextureFormat.R16_UNORM },
        { SurfaceFormat.R16_UINT, TextureFormat.R16_UINT },
        { SurfaceFormat.R16_FLOAT, TextureFormat.R16_FLOAT },
        { SurfaceFormat.R32_UNORM, TextureFormat.R32_FLOAT }, //?
        { SurfaceFormat.R32_G32_B32_A32_UNORM, TextureFormat.RGBA32_FLOAT }, //?

        { SurfaceFormat.D32_FLOAT_S8X24_UINT, TextureFormat.D32_FLOAT_S8X24_UINT },

        { SurfaceFormat.R11_G11_B10_UNORM,  TextureFormat.RG11B10_FLOAT }, //?
        { SurfaceFormat.R5_G5_B5_A1_UNORM,  TextureFormat.RGB5A1_UNORM }, //?
        { SurfaceFormat.R5_G6_B5_UNORM, TextureFormat.RGB565_UNORM }, //?
        { SurfaceFormat.R4_G4_UNORM, TextureFormat.RG4_UNORM },
        { SurfaceFormat.R4_G4_B4_A4_UNORM, TextureFormat.RGBA4_UNORM },

        { SurfaceFormat.B8_G8_R8_A8_UNORM, TextureFormat.BGRA8_UNORM },
        { SurfaceFormat.B8_G8_R8_A8_SRGB,  TextureFormat.BGRA8_SRGB },

        { SurfaceFormat.BC1_UNORM, TextureFormat.BC1_UNORM },
        { SurfaceFormat.BC1_SRGB,  TextureFormat.BC1_SRGB },
        { SurfaceFormat.BC2_UNORM, TextureFormat.BC2_UNORM },
        { SurfaceFormat.BC2_SRGB,  TextureFormat.BC2_SRGB },
        { SurfaceFormat.BC3_UNORM, TextureFormat.BC3_UNORM  },
        { SurfaceFormat.BC3_SRGB,  TextureFormat.BC3_SRGB },
        { SurfaceFormat.BC4_UNORM, TextureFormat.BC4_UNORM },
        { SurfaceFormat.BC4_SNORM, TextureFormat.BC4_SNORM },
        { SurfaceFormat.BC5_UNORM, TextureFormat.BC5_UNORM   },
        { SurfaceFormat.BC5_SNORM, TextureFormat.BC5_SNORM },
        { SurfaceFormat.BC6_UFLOAT, TextureFormat.BC6H_UF16 }, //?
        { SurfaceFormat.BC6_FLOAT, TextureFormat.BC6H_SF16 }, //?
        { SurfaceFormat.BC7_UNORM, TextureFormat.BC7_UNORM },
        { SurfaceFormat.BC7_SRGB, TextureFormat.BC7_SRGB },

        { SurfaceFormat.ASTC_4x4_UNORM, TextureFormat.ASTC_4x4_UNORM },
        { SurfaceFormat.ASTC_4x4_SRGB,  TextureFormat.ASTC_4x4_SRGB },
        { SurfaceFormat.ASTC_5x4_UNORM, TextureFormat.ASTC_5x4_UNORM },
        { SurfaceFormat.ASTC_5x4_SRGB,  TextureFormat.ASTC_5x4_SRGB },
        { SurfaceFormat.ASTC_5x5_UNORM, TextureFormat.ASTC_5x5_UNORM },
        { SurfaceFormat.ASTC_5x5_SRGB,  TextureFormat.ASTC_5x5_SRGB },
        { SurfaceFormat.ASTC_6x5_UNORM, TextureFormat.ASTC_6x5_UNORM },
        { SurfaceFormat.ASTC_6x5_SRGB,  TextureFormat.ASTC_6x5_SRGB },
        { SurfaceFormat.ASTC_6x6_UNORM, TextureFormat.ASTC_6x6_UNORM },
        { SurfaceFormat.ASTC_6x6_SRGB,  TextureFormat.ASTC_6x6_SRGB },
        { SurfaceFormat.ASTC_8x5_UNORM, TextureFormat.ASTC_8x5_UNORM },
        { SurfaceFormat.ASTC_8x5_SRGB,  TextureFormat.ASTC_8x5_SRGB },
        { SurfaceFormat.ASTC_8x6_UNORM, TextureFormat.ASTC_8x6_UNORM },
        { SurfaceFormat.ASTC_8x6_SRGB,  TextureFormat.ASTC_8x6_SRGB },
        { SurfaceFormat.ASTC_8x8_UNORM, TextureFormat.ASTC_8x8_UNORM },
        { SurfaceFormat.ASTC_8x8_SRGB,  TextureFormat.ASTC_8x8_SRGB },
        { SurfaceFormat.ASTC_10x5_UNORM, TextureFormat.ASTC_10x5_UNORM },
        { SurfaceFormat.ASTC_10x5_SRGB,  TextureFormat.ASTC_10x5_SRGB },
        { SurfaceFormat.ASTC_10x6_UNORM, TextureFormat.ASTC_10x6_UNORM },
        { SurfaceFormat.ASTC_10x6_SRGB,  TextureFormat.ASTC_10x6_SRGB },
        { SurfaceFormat.ASTC_10x8_UNORM, TextureFormat.ASTC_10x8_UNORM },
        { SurfaceFormat.ASTC_10x8_SRGB,  TextureFormat.ASTC_10x8_SRGB },
        { SurfaceFormat.ASTC_10x10_UNORM, TextureFormat.ASTC_10x10_UNORM },
        { SurfaceFormat.ASTC_10x10_SRGB,  TextureFormat.ASTC_10x10_SRGB },
        { SurfaceFormat.ASTC_12x10_UNORM, TextureFormat.ASTC_12x10_UNORM },
        { SurfaceFormat.ASTC_12x10_SRGB,  TextureFormat.ASTC_12x10_SRGB },
        { SurfaceFormat.ASTC_12x12_UNORM, TextureFormat.ASTC_12x12_UNORM },
        { SurfaceFormat.ASTC_12x12_SRGB,  TextureFormat.ASTC_12x12_SRGB },
    }; 
    #endregion
}