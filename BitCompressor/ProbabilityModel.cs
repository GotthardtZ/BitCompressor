using System;

namespace BitCompressor
{
    class ProbabilityModel
    {
        enum TokenType { Word, Number, Other}

        readonly RNG _rnd = new RNG();
        readonly SharedState sharedState;
        public ProbabilityModel(SharedState sharedState)
        {
            this.sharedState = sharedState;

            stats[0] = stats0; //order-0 context
            stats[1] = stats1; //order-1 context
            stats[2] = stats2; //order-2 context
            stats[3] = stats3; //order-3 context
            stats[4] = stats4; //token context

            states[0] = states0; //order-0 context
            states[1] = states1; //order-1 context
            states[2] = states2; //order-2 context
            states[3] = states3; //order-3 context
            states[4] = states4; //token context

            for (int i = 0; i < stateMaps.Length; i++)
                stateMaps[i] = new StateMap(_rnd);
        }

        //context statistics
        Stat[] stats0 = new Stat[255]; //2KB
        Stat[] stats1 = new Stat[255 * 256]; //512k
        Stat[] stats2 = new Stat[255 * 256 * 256]; //132MB
        Stat[] stats3 = new Stat[256 * 256 * 256]; //132MB
        Stat[] stats4 = new Stat[256 * 256 * 256]; //132MB
        Stat[][] stats = new Stat[5][];

        StateMap[] stateMaps = new StateMap[5];
        byte[] states0 = new byte[255]; //255 bytes
        byte[] states1 = new byte[255 * 256]; //64k
        byte[] states2 = new byte[255 * 256 * 256]; //16MB
        byte[] states3 = new byte[256 * 256 * 256]; //16MB
        byte[] states4 = new byte[256 * 256 * 256]; //16MB
        byte[][] states = new byte[5][];

        //model contexts
        //0: order-0 context (bits in the current byte)
        //1: order-1 context (bits in the current byte + previous byte)
        //2: order-2 context (bits in the current byte + previous 2 bytes)
        //3: order-3 context (bits in the current byte + previous 3 bytes)
        //4: token context   (variable length context to model words and word gaps)
        uint[] contexts = new uint[5];
        
        //token model state
        TokenType tokenType;
        ulong tokenHash = 0;

        Mixer mixer = new Mixer(
            numberOfInputs: 5 + 5, // number of mixer inputs (contexts)
            numberOfSelectedWeightSets: 4,
            numberOfAllWeightSets: (1) + (4 * 4 * 4 * 4) + 255 + (256 * 8), // model weights
            learningRate: 0.02, // tunable parameter
            scalingFactor: 0.3 // tunable parameter
        );

        Mixer secondMixer = new Mixer(
            numberOfInputs: 4, // inputs coming from the first mixer layer
            numberOfSelectedWeightSets: 1, // as the last layer it has a single weight set
            numberOfAllWeightSets: 1, // as the last layer it has a single weight set
            learningRate: 0.01, // tunable parameter
            scalingFactor: 0.3 // tunable parameter
        );
        public void SetContexts()
        {
            uint c0 = sharedState.c0;
            uint c1 = sharedState.c1;
            uint c2 = sharedState.c2;
            uint c3 = sharedState.c3;

            contexts[0] = (c0 - 1);
            contexts[1] = (c0 - 1) << 8 | c1;
            contexts[2] = (c0 - 1) << 16 | c1 << 8 | c2;
            contexts[3] = ((ulong)c0).Hash((byte)c1).Hash((byte)c2).Hash((byte)c3).FinalizeHash(24);
            contexts[4] = tokenHash.Hash((byte)c0).FinalizeHash(24);
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
                String.Format(" p0={0:0.000} p1={1:0.000} p2={2:0.000} p3={3:0.000} px={4:0.0000000} ", p0, p1, p2, p3, mixer.px)
                );

        }
        public double p()
        {
            int selectedWeightSet0 = 0;

            int selectedWeightSet1 =
                stats1[contexts[1]].StatCertainty << 0 |
                stats2[contexts[2]].StatCertainty << 2 |
                stats3[contexts[3]].StatCertainty << 4 |
                stats4[contexts[4]].StatCertainty << 6; //select one from the 4*4*4*4 = 256 weight sets

            int selectedWeightSet2 = this.sharedState.c0 - 1; //select one from the 255 weight sets

            int selectedWeightSet3 = this.sharedState.c1 << 3 | sharedState.bitpos; //select one from the 256*8 weight sets

            const int base0 = 0;
            const int base1 = 0 + 1;
            const int base2 = 0 + 1 + 256;
            const int base3 = 0 + 1 + 256 + 255;

            mixer.SetSelectedWeigthSet(0, base0 + selectedWeightSet0);
            mixer.SetSelectedWeigthSet(1, base1 + selectedWeightSet1);
            mixer.SetSelectedWeigthSet(2, base2 + selectedWeightSet2);
            mixer.SetSelectedWeigthSet(3, base3 + selectedWeightSet3);

            for (int i = 0; i < 5; i++)
            {
                var stat = stats[i][contexts[i]];
                var p = stat.p; //p range: 0.0 .. 0.5 .. 1.0, excluding the ends (0.0 and 1.0)
                mixer.SetInput(i, p);

                var state = states[i][contexts[i]];
                var stateMap = stateMaps[i];
                p = stateMap.p(state);
                mixer.SetInput(i + 5, p);
            }

            //predict next bit: get its probability by mixing contextual probabilities
            mixer.Mix();
            mixer.FeedForwardTo(secondMixer);
            secondMixer.Mix();
            double px = secondMixer.px[0];

            //uncomment the following line to print state bit by bit
            //printState(sharedState, p0, p1, p2, p3, w0, w1, w2, w3);

            return px;
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

            // update context statistics
            for (int i = 0; i < 5; i++)
            {
                stats[i][contexts[i]].Update(bit, adaptationRate: 1.0f);
                stateMaps[i].Update(ref states[i][contexts[i]], bit);
            }

            // update mixing weights
            mixer.Update(bit);
            secondMixer.Update(bit);
        }
    }
}
