namespace BitCompressor
{
    class Mixer
    {
        double[] inputs; // probabilities
        double[] stretchedInputs; // stretched probabilities
        double[,] weights; 
        double learningRate; // tunable parameter
        double scalingFactor; // tunable parameter

        int selectedWeightSet; // 0..255
        public double px; // last mixer output

        public Mixer(int numberOfInputs, int numberOfWeightSets, double learningRate, double scalingFactor)
        {
            this.inputs = new double[numberOfInputs];
            this.stretchedInputs = new double[numberOfInputs];
            this.weights = new double[numberOfWeightSets, numberOfInputs]; 
            this.learningRate = learningRate;
            this.scalingFactor = scalingFactor;
        }

        public void SetSelectedWeigthSet(int selectedWeightSet)
        {
            this.selectedWeightSet = selectedWeightSet;
        }

        public void SetInput(int idx, double probability)
        {
            inputs[idx] = probability;
            stretchedInputs[idx] = MixerFunctions.Stretch(probability); // typical d range: -8.3 .. 0.0 .. +8.3 when p is between 1/4096 and 4095/4096 
        }

        public double p()
        {
            double dotProduct = 0.0;

            for (int i = 0; i < stretchedInputs.Length; i++)
            {
                var d = stretchedInputs[i];
                var w = weights[selectedWeightSet, i];
                dotProduct += (w * d);
            }

            dotProduct *= scalingFactor;
            px = MixerFunctions.Squash(dotProduct);
            return px;
        }

        public void Update(uint bit)
        {
            var error = bit - px; // target value (0 or 1) vs predicted probability (0..1)

            for (int i = 0; i < stretchedInputs.Length; i++)
            {
                var d = stretchedInputs[i];
                var w = weights[selectedWeightSet, i];
                w += d * error * learningRate; // weight update rule for logistic regression
                weights[selectedWeightSet, i] = MixerFunctions.Clip(w);
            }
        }
    }
}
