namespace BitCompressor
{
    class SharedState
    {
        public byte bitpos { get; private set; } = 0; //0..7, for the most significan bit it's 0, for the least significant bit it's 7

        public byte bit { get; private set; } = 0; //0 or 1 - the bit in the current byte we just encoded/decoded
        public byte c0 { get; private set; } = 1; //1..255 - the current byte to be predicted (all the bits known so far are filled from the left, with a leading '1' bit)
        public byte c1 { get; private set; } = 0; //0..255 - the last byte
        public byte c2 { get; private set; } = 0; //0..255 - the byte preceding the last byte

        public void UpdateState(uint bit)
        {
            this.bit = (byte)bit;
            c0 = (byte)((uint)c0 << 1 | bit);
            bitpos = (byte)((bitpos + 1) & 7);
            if (bitpos == 0) {
                c2 = c1;
                c1 = c0;
                c0 = 1;
            }
        }
    }
}
