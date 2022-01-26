﻿using System;

namespace BitCompressor
{
    class MixerFunctions
    {
        //the logit: stretches a probability to be in the logistic domain ]-∞..∞[
        public static double Stretch(double p)
        {
            return Math.Log(p / (1.0 - p));
        }

        //the sigmoid (the inverse of the logit): squashes any input to be in the range of ]0..1[
        public static double Squash(double d)
        {
            return 1.0 / (1.0 + Math.Exp(-d));
        }
    }
}
