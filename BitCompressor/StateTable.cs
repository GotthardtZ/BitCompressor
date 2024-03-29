﻿namespace BitCompressor
{
    /**
     * State table:
     *   nex(state, 0) = next state if bit y is 0, 0 <= state < 256
     *   nex(state, 1) = next state if bit y is 1
     *   nex(state, 2) = number of zeros in bit history represented by state
     *   nex(state, 3) = number of ones represented
     *
     * States represent a bit history within some context.
     * State 0 is the starting state (no bits seen).
     * States 1-30 represent all possible sequences of 1-4 bits.
     * States 31-252 represent a pair of counts, (n0,n1), the number
     *   of 0 and 1 bits respectively. If n0+n1 < 16 then there are
     *   two states for each pair, depending on if a 0 or 1 was the last bit seen.
     * If n0 and n1 are too large, then there is no state to represent this
     * pair, so another state with about the same ratio of n0/n1 is substituted.
     * Also, when a bit is observed and the count of the opposite bit is large,
     * then part of this count is discarded to favor newer data over old.
     */
    class StateTable
    {
        public static readonly byte[,] stateTable = new byte[256, 4]
        {
            {  1,  2, 0, 0},{  3,  5, 1, 0},{  4,  6, 0, 1},{  7, 10, 2, 0}, // 0-3
            {  8, 12, 1, 1},{  9, 13, 1, 1},{ 11, 14, 0, 2},{ 15, 19, 3, 0}, // 4-7
            { 16, 23, 2, 1},{ 17, 24, 2, 1},{ 18, 25, 2, 1},{ 20, 27, 1, 2}, // 8-11
            { 21, 28, 1, 2},{ 22, 29, 1, 2},{ 26, 30, 0, 3},{ 31, 33, 4, 0}, // 12-15
            { 32, 35, 3, 1},{ 32, 35, 3, 1},{ 32, 35, 3, 1},{ 32, 35, 3, 1}, // 16-19
            { 34, 37, 2, 2},{ 34, 37, 2, 2},{ 34, 37, 2, 2},{ 34, 37, 2, 2}, // 20-23
            { 34, 37, 2, 2},{ 34, 37, 2, 2},{ 36, 39, 1, 3},{ 36, 39, 1, 3}, // 24-27
            { 36, 39, 1, 3},{ 36, 39, 1, 3},{ 38, 40, 0, 4},{ 41, 43, 5, 0}, // 28-31
            { 42, 45, 4, 1},{ 42, 45, 4, 1},{ 44, 47, 3, 2},{ 44, 47, 3, 2}, // 32-35
            { 46, 49, 2, 3},{ 46, 49, 2, 3},{ 48, 51, 1, 4},{ 48, 51, 1, 4}, // 36-39
            { 50, 52, 0, 5},{ 53, 43, 6, 0},{ 54, 57, 5, 1},{ 54, 57, 5, 1}, // 40-43
            { 56, 59, 4, 2},{ 56, 59, 4, 2},{ 58, 61, 3, 3},{ 58, 61, 3, 3}, // 44-47
            { 60, 63, 2, 4},{ 60, 63, 2, 4},{ 62, 65, 1, 5},{ 62, 65, 1, 5}, // 48-51
            { 50, 66, 0, 6},{ 67, 55, 7, 0},{ 68, 57, 6, 1},{ 68, 57, 6, 1}, // 52-55
            { 70, 73, 5, 2},{ 70, 73, 5, 2},{ 72, 75, 4, 3},{ 72, 75, 4, 3}, // 56-59
            { 74, 77, 3, 4},{ 74, 77, 3, 4},{ 76, 79, 2, 5},{ 76, 79, 2, 5}, // 60-63
            { 62, 81, 1, 6},{ 62, 81, 1, 6},{ 64, 82, 0, 7},{ 83, 69, 8, 0}, // 64-67
            { 84, 71, 7, 1},{ 84, 71, 7, 1},{ 86, 73, 6, 2},{ 86, 73, 6, 2}, // 68-71
            { 44, 59, 5, 3},{ 44, 59, 5, 3},{ 58, 61, 4, 4},{ 58, 61, 4, 4}, // 72-75
            { 60, 49, 3, 5},{ 60, 49, 3, 5},{ 76, 89, 2, 6},{ 76, 89, 2, 6}, // 76-79
            { 78, 91, 1, 7},{ 78, 91, 1, 7},{ 80, 92, 0, 8},{ 93, 69, 9, 0}, // 80-83
            { 94, 87, 8, 1},{ 94, 87, 8, 1},{ 96, 45, 7, 2},{ 96, 45, 7, 2}, // 84-87
            { 48, 99, 2, 7},{ 48, 99, 2, 7},{ 88,101, 1, 8},{ 88,101, 1, 8}, // 88-91
            { 80,102, 0, 9},{103, 69,10, 0},{104, 87, 9, 1},{104, 87, 9, 1}, // 92-95
            {106, 57, 8, 2},{106, 57, 8, 2},{ 62,109, 2, 8},{ 62,109, 2, 8}, // 96-99
            { 88,111, 1, 9},{ 88,111, 1, 9},{ 80,112, 0,10},{113, 85,11, 0}, // 100-103
            {114, 87,10, 1},{114, 87,10, 1},{116, 57, 9, 2},{116, 57, 9, 2}, // 104-107
            { 62,119, 2, 9},{ 62,119, 2, 9},{ 88,121, 1,10},{ 88,121, 1,10}, // 108-111
            { 90,122, 0,11},{123, 85,12, 0},{124, 97,11, 1},{124, 97,11, 1}, // 112-115
            {126, 57,10, 2},{126, 57,10, 2},{ 62,129, 2,10},{ 62,129, 2,10}, // 116-119
            { 98,131, 1,11},{ 98,131, 1,11},{ 90,132, 0,12},{133, 85,13, 0}, // 120-123
            {134, 97,12, 1},{134, 97,12, 1},{136, 57,11, 2},{136, 57,11, 2}, // 124-127
            { 62,139, 2,11},{ 62,139, 2,11},{ 98,141, 1,12},{ 98,141, 1,12}, // 128-131
            { 90,142, 0,13},{143, 95,14, 0},{144, 97,13, 1},{144, 97,13, 1}, // 132-135
            { 68, 57,12, 2},{ 68, 57,12, 2},{ 62, 81, 2,12},{ 62, 81, 2,12}, // 136-139
            { 98,147, 1,13},{ 98,147, 1,13},{100,148, 0,14},{149, 95,15, 0}, // 140-143
            {150,107,14, 1},{150,107,14, 1},{108,151, 1,14},{108,151, 1,14}, // 144-147
            {100,152, 0,15},
            // contexts representing strong trend of 0s or 1s start from here
            {153, 95,16, 0},{154, 69,15, 1},{ 80,155, 1,15},{100,156, 0,16}, // 149-152
            {157, 95,17, 0},{158, 69,16, 1},{ 80,159, 1,16},{100,160, 0,17}, // 153-156
            {161, 95,18, 0},{162, 69,17, 1},{ 80,163, 1,17},{100,164, 0,18}, // 157-160
            {165, 95,19, 0},{166, 69,18, 1},{ 80,167, 1,18},{100,168, 0,19}, // 161-164
            {169, 95,20, 0},{170, 69,19, 1},{ 80,171, 1,19},{100,172, 0,20}, // 165-168
            {173, 95,21, 0},{174, 69,20, 1},{ 80,175, 1,20},{100,176, 0,21}, // 169-172
            {177, 95,22, 0},{178, 69,21, 1},{ 80,179, 1,21},{100,180, 0,22}, // 173-176
            {181, 95,23, 0},{182, 69,22, 1},{ 80,183, 1,22},{100,184, 0,23}, // 177-180
            {185, 95,24, 0},{186, 69,23, 1},{ 80,187, 1,23},{100,188, 0,24}, // 181-184
            {189, 95,25, 0},{190, 69,24, 1},{ 80,191, 1,24},{100,192, 0,25}, // 185-188
            {193, 95,26, 0},{194, 69,25, 1},{ 80,195, 1,25},{100,196, 0,26}, // 189-192
            {197, 95,27, 0},{198, 69,26, 1},{ 80,199, 1,26},{100,200, 0,27}, // 193-196
            {201, 95,28, 0},{202, 69,27, 1},{ 80,203, 1,27},{100,204, 0,28}, // 197-200
            {205, 95,29, 0},{206, 69,28, 1},{ 80,207, 1,28},{100,208, 0,29}, // 201-204
            // contexts with incremental counting start from here
            {209, 95,30, 0},{210, 69,29, 1},{ 80,211, 1,29},{100,212, 0,30}, // 205-208
            {213, 95,31, 0},{214, 69,30, 1},{ 80,215, 1,30},{100,216, 0,31}, // 209-212
            {217, 95,32, 0},{218, 69,31, 1},{ 80,219, 1,31},{100,220, 0,32}, // 213-216
            {221, 95,33, 0},{222, 69,32, 1},{ 80,223, 1,32},{100,224, 0,33}, // 217-220
            {225, 95,34, 0},{226, 69,33, 1},{ 80,227, 1,33},{100,228, 0,34}, // 221-224
            {229, 95,35, 0},{230, 69,34, 1},{ 80,231, 1,34},{100,232, 0,35}, // 225-228
            {233, 95,36, 0},{234, 69,35, 1},{ 80,235, 1,35},{100,236, 0,36}, // 229-232
            {237, 95,37, 0},{238, 69,36, 1},{ 80,239, 1,36},{100,240, 0,37}, // 233-236
            {241, 95,38, 0},{242, 69,37, 1},{ 80,243, 1,37},{100,244, 0,38}, // 237-240
            {245, 95,39, 0},{246, 69,38, 1},{ 80,247, 1,38},{100,248, 0,39}, // 241-244
            {249, 95,40, 0},{250, 69,39, 1},{ 80,251, 1,39},{100,252, 0,40}, // 245-248
            {249, 95,41, 0},{250, 69,40, 1},{ 80,251, 1,40},{100,252, 0,41}, // 249-252
            {1,2, 0,0},{1,2, 0,0},{1,2, 0,0}// 253-255 are unused (and in case we reach such a state we'll need to restart)
        };

        public static byte next(byte state, uint bit) =>
            stateTable[state, bit];

        public static byte next(byte oldState, uint bit, RNG rnd)
        {
            byte newState = stateTable[oldState, bit];
            if (newState >= 205 && newState >= oldState + 4)
            {
                // Apply probabilistic increment for contexts with strong trend of 0s or 1s.
                // Applicable to states 149 and above, but we'll start applying it from states (205-208).
                // That means...
                // For states 205..208, 209..212, ... 249..252 a group of 4 states is not represented by
                // the counts indicated in the state table. An exponential scale is used instead.
                // The highest group (249..252) represents the top of this scale, where we can not increment anymore.
                // Reaching the highest states (249-252) from states (201-204) takes 12 steps.
                // thus the probability of reaching the highest states (249-252) is ~ 1/2048 
                if (rnd.Next(1) != 0)
                { // random bit: p()=1/2 -> the probability to advance to a higher state is 1/2
                    return oldState; // don't advance
                }
            }
            return newState;
        }

        public void update(ref byte state, uint bit, RNG rnd)
        {
            state = next(state, bit, rnd);
        }
    }
}
