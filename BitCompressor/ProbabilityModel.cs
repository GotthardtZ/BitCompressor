using System;

namespace BitCompressor
{
    class ProbabilityModel
    {
        readonly SharedState sharedState;
        public ProbabilityModel(SharedState sharedState)
        {
            this.sharedState = sharedState;
        }

        Stat[] stats0 = new Stat[255];
        Stat[] stats1 = new Stat[255 * 256];
        Stat[] stats2 = new Stat[255 * 256 * 256];
        Stat[] stats3 = new Stat[255 * 256 * 256];
        
        uint context0 = 0;
        uint context1 = 0;
        uint context2 = 0;
        uint context3 = 0;
        string word = "";

        public void SetContexts()
        {
            uint c0 = sharedState.c0;
            uint c1 = sharedState.c1;
            uint c2 = sharedState.c2;
            
            context0 = (c0 - 1);
            context1 = (c0 - 1) << 8 | c1;
            context2 = (c0 - 1) << 16 | c1 << 8 | c2;
            context3 = (c0 - 1) << 16 | (word.Hash() & 0xffff);
        }

        public double p()
        {
            var p0 = stats0[context0].p;
            var p1 = stats1[context1].p;
            var p2 = stats2[context2].p;
            var p3 = stats3[context3].p;

            var px =
                stats3[context3].IsCertain ? p3 :
                stats2[context2].IsMature ? p2 :
                stats1[context1].IsMature ? p1 : p0;

            return px;
        }

        public void UpdateModel(uint bit)
        {
            stats0[context0].Update(bit);
            stats1[context1].Update(bit);
            stats2[context2].Update(bit);
            stats3[context3].Update(bit);

            if (sharedState.bitpos == 0)
            {
                char c1 = (char)sharedState.c1;
                if (char.IsLetter(c1))
                    word += c1;
                else
                    word = "";
            }
        }
    }
}
