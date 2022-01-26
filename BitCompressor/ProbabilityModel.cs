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
        uint wordhash = 0;

        double w0 = 0;
        double w1 = 0;
        double w2 = 0;
        double w3 = 0;

        double d0 = 0;
        double d1 = 0;
        double d2 = 0;
        double d3 = 0;

        double px = 0.5;

        public void SetContexts()
        {
            uint c0 = sharedState.c0;
            uint c1 = sharedState.c1;
            uint c2 = sharedState.c2;

            context0 = (c0 - 1);
            context1 = (c0 - 1) << 8 | c1;
            context2 = (c0 - 1) << 16 | c1 << 8 | c2;
            context3 = (c0 - 1) << 16 | (wordhash & 0xffff);
        }

        public double p()
        {
            var p0 = stats0[context0].p;
            var p1 = stats1[context1].p;
            var p2 = stats2[context2].p;
            var p3 = stats3[context3].p;

            d0 = MixerFunctions.Stretch(p0);
            d1 = MixerFunctions.Stretch(p1);
            d2 = MixerFunctions.Stretch(p2);
            d3 = MixerFunctions.Stretch(p3);

            const double scalingFactor = 0.2; //tunable parameter
            var dotProduct = (w0 * d0) + (w1 * d1) + (w2 * d2) + (w3 * d3);
            dotProduct *= scalingFactor;
            
            px = MixerFunctions.Squash(dotProduct);
            
            return px;
        }

        private static double Clip(double x)
        {
            if (x < -16.0) return -16.0;
            if (x > +16.0) return +16.0;
            return x;
        }

        public void UpdateModel(uint bit)
        {
            stats0[context0].Update(bit);
            stats1[context1].Update(bit);
            stats2[context2].Update(bit);
            stats3[context3].Update(bit);

            if (sharedState.bitpos == 0)
            {
                byte c1 = sharedState.c1;
                if (char.IsLetter((char)c1))
                    wordhash = wordhash.Hash(c1);
                else
                    wordhash = 0;
            }

            //update mixing weights

            var error = bit - px; //target probability vs predicted probability

            const double learningRate = 0.02; //tunable parameter
            w0 += d0 * error * learningRate;
            w1 += d1 * error * learningRate;
            w2 += d2 * error * learningRate;
            w3 += d3 * error * learningRate;

            w0 = Clip(w0);
            w1 = Clip(w1);
            w2 = Clip(w2);
            w3 = Clip(w3);
        }
    }
}
