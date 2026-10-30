using System.Text;
using ZstdNet;

namespace BfresToCast.Utils
{
    public class DecompressionHelpers
    {
        public static byte[] DecompressIfNeeded(byte[] data)
        {
            byte[] readId = data.Take(4).ToArray();
            byte[] zstdId = [0x28, 0xB5, 0x2F, 0xFD];
            byte[] yaz0Id = Encoding.ASCII.GetBytes("Yaz0");

            if (readId.SequenceEqual(zstdId))
            {
                Decompressor decompressor = new Decompressor();
                return decompressor.Unwrap(data);
            }
            else if (readId.SequenceEqual(yaz0Id))
            {
                return DecompressYaz0(data);
            }

            return data;
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
