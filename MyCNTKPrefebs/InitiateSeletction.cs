using CNTK;

namespace MyCNTKPrefebs
{
    public enum InitiateSeletction : byte
    {
        None_Sel = 0,
        Orth_Sel = 0x1,
        RC_Sel = 0x2,
        Normalized_Sel = 0x4,
        Scale_Sel = 0x8
    };
}
