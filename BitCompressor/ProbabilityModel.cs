﻿using System;

namespace BitCompressor
{
    class ProbabilityModel
    {
        enum TokenType { Word, Number, Other}

        readonly SharedState sharedState;
        public ProbabilityModel(SharedState sharedState)
        {
            this.sharedState = sharedState;
        }

        //context statistics
        Stat[] stats0 = new Stat[255];
        Stat[] stats1 = new Stat[255 * 256];
        Stat[] stats2 = new Stat[255 * 256 * 256];
        Stat[] stats3 = new Stat[255 * 256 * 256];

        //model contexts
        uint context0 = 0;
        uint context1 = 0;
        uint context2 = 0;
        uint context3 = 0;
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

            context0 = (c0 - 1);
            context1 = (c0 - 1) << 8 | c1;
            context2 = (c0 - 1) << 16 | c1 << 8 | c2;
            context3 = (c0 - 1) << 16 | (tokenHash & 0xffff);
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
            var p0 = stats0[context0].p; //p range: 0.0 .. 0.5 .. 1.0, excluding the ends (0.0 and 1.0)
            var p1 = stats1[context1].p;
            var p2 = stats2[context2].p;
            var p3 = stats3[context3].p;

            var d0 = MixerFunctions.Stretch(p0); // typical d range: -8.3 .. 0.0 .. +8.3 when p is between 1/4096 and 4095/4096 
            var d1 = MixerFunctions.Stretch(p1);
            var d2 = MixerFunctions.Stretch(p2);
            var d3 = MixerFunctions.Stretch(p3);

            selectedWeightSet =
                stats1[context1].StatCertainty << 0 |
                stats2[context2].StatCertainty << 2 |
                stats3[context3].StatCertainty << 4;
            
            var w0 = weights[selectedWeightSet, 0];
            var w1 = weights[selectedWeightSet, 1];
            var w2 = weights[selectedWeightSet, 2];
            var w3 = weights[selectedWeightSet, 3];

            const double scalingFactor = 0.2; //tunable parameter
            var dotProduct = (w0 * d0) + (w1 * d1) + (w2 * d2) + (w3 * d3);
            dotProduct *= scalingFactor;

            stretchedInputs[0] = d0;
            stretchedInputs[1] = d1;
            stretchedInputs[2] = d2;
            stretchedInputs[3] = d3;

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
            stats0[context0].Update(bit);
            stats1[context1].Update(bit);
            stats2[context2].Update(bit);
            stats3[context3].Update(bit);

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

            var d0 = stretchedInputs[0];
            var d1 = stretchedInputs[1];
            var d2 = stretchedInputs[2];
            var d3 = stretchedInputs[3];

            var w0 = weights[selectedWeightSet, 0];
            var w1 = weights[selectedWeightSet, 1];
            var w2 = weights[selectedWeightSet, 2];
            var w3 = weights[selectedWeightSet, 3];

            w0 += d0 * error * learningRate; //the larger the error - the larger of the weight change
            w1 += d1 * error * learningRate;
            w2 += d2 * error * learningRate;
            w3 += d3 * error * learningRate;

            weights[selectedWeightSet, 0] = Clip(w0);
            weights[selectedWeightSet, 1] = Clip(w1);
            weights[selectedWeightSet, 2] = Clip(w2);
            weights[selectedWeightSet, 3] = Clip(w3);
        }
    }
}
