using System.Diagnostics;

namespace BitCompressor
{
    class StateMap
    {
        readonly RNG _rnd;
        Stat[] stats = new Stat[256];
        public StateMap(RNG rnd)
        {
            _rnd = rnd;
            for (int i = 0; i < 256; i++)
            {
                byte state = (byte)i;
                uint n0 = StateTable.next(state, 2);
                uint n1 = StateTable.next(state, 3);

                if (state < 205)
                {
                    n0 = n0 * 3;
                    n1 = n1 * 3;
                }
                else if (state < 253)
                {
                    int incremental_state = (state - 205) >> 2;
                    if (((state - 205) & 3) <= 1)
                        n0 = 29u + (1u << incremental_state);
                    else
                        n1 = 29u + (1u << incremental_state);
                    n0 = n0 * 3;
                    n1 = n1 * 3;
                    Debug.Assert(n0 < 16384 && n1 < 16384);
                }
                else
                { // 253, 254, 255
                    n0 = 0;
                    n1 = 0;
                }

                stats[state].n0 = n0;
                stats[state].n1 = n1;
            }
        }
        public double p(byte state) => state == 0 ? 0.5 : stats[state].p;
        public void Update(ref byte state, uint bit)
        {
            //update stats
            if (state != 0)
                stats[state].Update(bit, adaptationRate: 1 - 1 / 128f);
            //update state
            state = StateTable.next(state, bit, _rnd);
        }
    }
}
