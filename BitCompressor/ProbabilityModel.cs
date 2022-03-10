using System;

namespace BitCompressor
{
    class ProbabilityModel
    {
        enum TokenType { Word, Number, Other}

        readonly SharedState sharedState;
        public ProbabilityModel(SharedState sharedState)
        {
            this.sharedState = sharedState;
            stats[0] = stats0;
            stats[1] = stats1;
            stats[2] = stats2;
            stats[3] = stats3;
        }

        //context statistics
        Stat[] stats0 = new Stat[255];
        Stat[] stats1 = new Stat[255 * 256];
        Stat[] stats2 = new Stat[255 * 256 * 256];
        Stat[] stats3 = new Stat[255 * 256 * 256];
        Stat[][] stats = new Stat[4][];

        //model contexts
        //0: order-0 context (bits in the current byte)
        //1: order-1 context (bits in the current byte + previous byte)
        //2: order-3 context (bits in the current byte + previous 2 bytes)
        //3: token context   (variable length context to model words and word gaps)
        uint[] contexts = new uint[4];
        TokenType tokenType;
        uint tokenHash = 0;

        double[,] weights = new double[64, 4]; //model weights
        double[] stretchedInputs = new double[4]; //stretched probabilities
        int selectedWeightSet = 0;

        double px = 0.5;

        public void SetContexts()
        {
            uint c0 = sharedState.c0;
            uint c1 = sharedState.c1;
            uint c2 = sharedState.c2;

            contexts[0] = (c0 - 1);
            contexts[1] = (c0 - 1) << 8 | c1;
            contexts[2] = (c0 - 1) << 16 | c1 << 8 | c2;
            contexts[3] = (c0 - 1) << 16 | (tokenHash & 0xffff);
        }

        private void printChar(byte b)
        {
            if (b < 32) Console.Write(" ");
            else if (b > 127) Console.Write(" ");
            else Console.Write((char)b);
        }
        private void printBinary(byte b, int n)
        {
            for (int i = 0; i < n; i++)
            {
                int bit = (b >> (7 - i))&1;
                Console.Write(bit);
            }
        }
        private void printState(SharedState sharedState, double p0, double p1, double p2, double p3, double w0, double w1, double w2, double w3)
        {
            //end of previous line
            Console.WriteLine(sharedState.bit);

            //new line
            Console.WriteLine();
            printChar(sharedState.c2);
            printChar(sharedState.c1);
            Console.Write(" = ");
            printBinary(sharedState.c2, 8);
            Console.Write(" ");
            printBinary(sharedState.c1, 8);
            Console.Write(" ");
            printBinary((byte)(sharedState.c0 << (8 - sharedState.bitpos)), sharedState.bitpos);
            Console.Write("?");
            Console.Write(new string(' ', 10 - sharedState.bitpos));

            Console.Write(
                sharedState.bitpos +
                String.Format(" w0={0:+#0.000;-#0.000; #0.000} w1={1:+#0.000;-#0.000; #0.000} w2={2:+#0.000;-#0.000; #0.000} w3={3:+#0.000;-#0.000; #0.000}   |  ", w0, w1, w2, w3) +
                String.Format(" p0={0:0.000} p1={1:0.000} p2={2:0.000} p3={3:0.000} px={4:0.0000000} ", p0, p1, p2, p3, px)
                );

        }
        public double p()
        {
            selectedWeightSet =
                stats1[contexts[1]].StatCertainty << 0 |
                stats2[contexts[2]].StatCertainty << 2 |
                stats3[contexts[3]].StatCertainty << 4;

            double dotProduct = 0.0;
            for (int i = 0; i < 4; i++)
            {
                var stat = stats[i][contexts[i]];
                var p = stat.p; //p range: 0.0 .. 0.5 .. 1.0, excluding the ends (0.0 and 1.0)
                var d = MixerFunctions.Stretch(p); // typical d range: -8.3 .. 0.0 .. +8.3 when p is between 1/4096 and 4095/4096 
                var w = weights[selectedWeightSet, i];
                dotProduct += (w * d);
                stretchedInputs[i] = d;
            }

            const double scalingFactor = 0.2; //tunable parameter
            dotProduct *= scalingFactor;
            px = MixerFunctions.Squash(dotProduct);

            //uncomment the following line to print state bit by bit
            //printState(sharedState, p0, p1, p2, p3, w0, w1, w2, w3);

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
            //update state for tokenmodel
            if (sharedState.bitpos == 0)
            {
                byte c1 = sharedState.c1;
                var thisTokenType =
                    ((c1 >= 'A' && c1 <= 'Z') || (c1 >= 'a' && c1 <= 'z') || c1 >= 128) ? TokenType.Word :
                    c1 >= '0' && c1 <= '9' ? TokenType.Number :
                    TokenType.Other;
                if (thisTokenType != tokenType)
                {
                    tokenType = thisTokenType;
                    tokenHash = 0;
                }
                tokenHash = tokenHash.Hash(c1);
            }

            //update mixing weights

            var error = bit - px; //target probability vs predicted probability

            const double learningRate = 0.02; //tunable parameter

            for (int i = 0; i < 4; i++)
            {
                var d = stretchedInputs[i];
                var w = weights[selectedWeightSet, i];
                w += d * error * learningRate; //the larger the error - the larger of the weight change
                weights[selectedWeightSet, i] = Clip(w);
                stats[i][contexts[i]].Update(bit);
            }
        }
    }
}
