namespace BitCompressor
{
    class Mixer
    {
        double[] inputs; // probabilities
        double[] stretchedInputs; // stretched probabilities
        double[,] weights; 
        double learningRate; // tunable parameter
        double scalingFactor; // tunable parameter

        int[] selectedWeightSets; // 0: 0..255, 1: 0..255
        public double[] px; // last mixer output per selected weight set

        public Mixer(int numberOfInputs, int numberOfSelectedWeightSets, int numberOfAllWeightSets, double learningRate, double scalingFactor)
        {
            this.inputs = new double[numberOfInputs];
            this.stretchedInputs = new double[numberOfInputs];
            this.weights = new double[numberOfAllWeightSets, numberOfInputs]; 
            this.learningRate = learningRate;
            this.scalingFactor = scalingFactor;
            this.selectedWeightSets = new int[numberOfSelectedWeightSets];
            this.px = new double[numberOfSelectedWeightSets];
        }

        public void SetSelectedWeigthSet(int idx, int selectedWeightSet)
        {
            this.selectedWeightSets[idx] = selectedWeightSet;
        }

        public void SetInput(int idx, double probability)
        {
            inputs[idx] = probability;
            stretchedInputs[idx] = MixerFunctions.Stretch(probability); // typical d range: -8.3 .. 0.0 .. +8.3 when p is between 1/4096 and 4095/4096 
        }

        public void Mix()
        {
            for (int s = 0; s < px.Length; s++)
            {
                var selectedWeightSet = selectedWeightSets[s];

                double dotProduct = 0.0;

                for (int i = 0; i < stretchedInputs.Length; i++)
                {
                    var d = stretchedInputs[i];
                    var w = weights[selectedWeightSet, i];
                    dotProduct += (w * d);
                }

                dotProduct *= scalingFactor;
                px[s] = MixerFunctions.Squash(dotProduct);
            }
        }

        public void FeedForwardTo(Mixer mixer)
        {
            System.Diagnostics.Debug.Assert(this.px.Length == mixer.inputs.Length);
            for(int i = 0; i < px.Length; i++)
            {
                mixer.SetInput(i, px[i]);
            }
            mixer.SetSelectedWeigthSet(0, 0);
        }

        public void Update(uint bit)
        {
            for (int s = 0; s < px.Length; s++)
            {
                var selectedWeightSet = selectedWeightSets[s];

                var error = bit - px[s]; // target value (0 or 1) vs predicted probability (0..1)

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
}
