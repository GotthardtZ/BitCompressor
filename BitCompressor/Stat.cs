namespace BitCompressor
{
    struct Stat
    {
        public ulong n0;
        public ulong n1;
        public double p=>(n1 + 0.5d) / (n0 + n1 + 1.0d);
        public void Update(uint bit)
        {
            n0 += bit == 0 ? 1u : 0u;
            n1 += bit == 1 ? 1u : 0u;
        }
        public int StatCertainty =>
            (n0 + n1 == 0) ? 0 : // we haven't encountered this context yet
            (n0 == 0 || n1 == 0) ? 1 : // trend of only 0s or 1s
            (n0 + n1 >= 16) ? 2 : // we encounteder this context many times already            3; //otherwise
            3; //otherwise
    }
}
