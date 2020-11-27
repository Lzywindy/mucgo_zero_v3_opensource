using CNTK;

namespace MyCNTKPrefebs
{
    public enum RandomType : byte
    {
        use_gamma_distribution,
        use_uniform_real_distribution,
        use_normal_distribution,
        use_lognormal_distribution,
        use_studentT_distribution,
        use_beta_distribution
    };
}
