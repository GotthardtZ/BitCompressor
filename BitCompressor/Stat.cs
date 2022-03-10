namespace BitCompressor
{
    struct Stat
    {
        public float n0;
        public float n1;
        public double p => (n1 + 0.2d) / (n0 + n1 + 0.4d);
        public void Update(uint bit)
        {
            n0 *= 1 - 1 / 33f;
            n1 *= 1 - 1 / 33f;
            n0 += bit == 0 ? 1f : 0f;
            n1 += bit == 1 ? 1f : 0f;
        }
        public int StatCertainty =>
            (n0 + n1 == 0) ? 0 : // we haven't encountered this context yet
            (n0 == 0 || n1 == 0) ? 1 : // trend of only 0s or 1s
            (n0 + n1 >= 12) ? 2 : // we encounteder this context many times already
            3; //otherwise
    }
}
