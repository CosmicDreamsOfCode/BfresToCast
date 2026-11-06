using System.Text;

namespace BfresToCast.Utils.Helpers;

public static class SarcHelper
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