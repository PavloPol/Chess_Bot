using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace ChessBot.AI
{
    public class NeuralNetwork
    {
        private int inputNodes;
        private int outputNodes;
        private List<int> hiddenLayers;

        private List<Matrix> weights;
        private List<Matrix> biases;

        private double learningRate = 0.1;

        public NeuralNetwork(int inputNodes, List<int> hiddenLayers, int outputNodes)
        {
            this.inputNodes = inputNodes;
            this.hiddenLayers = new List<int>(hiddenLayers);
            this.outputNodes = outputNodes;

            weights = new List<Matrix>();
            biases = new List<Matrix>();

            // Input to first hidden layer
            weights.Add(new Matrix(hiddenLayers[0], inputNodes));
            biases.Add(new Matrix(hiddenLayers[0], 1));

            // Hidden layers
            for (int i = 1; i < hiddenLayers.Count; i++)
            {
                weights.Add(new Matrix(hiddenLayers[i], hiddenLayers[i - 1]));
                biases.Add(new Matrix(hiddenLayers[i], 1));
            }

            // Last hidden to output
            weights.Add(new Matrix(outputNodes, hiddenLayers[^1]));
            biases.Add(new Matrix(outputNodes, 1));

            // Randomize
            foreach (var w in weights) w.Randomize();
            foreach (var b in biases) b.Randomize();
        }

        public double[] Predict(double[] inputArray)
        {
            Matrix output = Matrix.ConvertArrayToMatrix(inputArray);

            for (int i = 0; i < weights.Count; i++)
            {
                output = Matrix.Multiply(weights[i], output);
                output.Add(biases[i]);
                output.ApplyFunction(ActivationFunctions.Sigmoid);
            }

            return Matrix.ConvertMatrixToArray(output);
        }

        public void Train(double[] inputArray, double[] targetArray)
        {
            Matrix input = Matrix.ConvertArrayToMatrix(inputArray);
            Matrix target = Matrix.ConvertArrayToMatrix(targetArray);

            List<Matrix> layerOutputs = new List<Matrix> { input };

            // Forward pass
            Matrix current = input;
            for (int i = 0; i < weights.Count; i++)
            {
                current = Matrix.Multiply(weights[i], current);
                current.Add(biases[i]);
                current.ApplyFunction(ActivationFunctions.Sigmoid);
                layerOutputs.Add(current);
            }

            // Calculate error
            Matrix output = layerOutputs[^1];
            Matrix error = Matrix.Substract(target, output);

            // Backward pass
            for (int i = weights.Count - 1; i >= 0; i--)
            {
                Matrix gradient = Matrix.ApplyFunction(layerOutputs[i + 1], ActivationFunctions.SigmoidDerivative);
                gradient.Multiply(error);
                gradient.Multiply(learningRate);

                Matrix transposed = Matrix.Transpose(layerOutputs[i]);
                Matrix delta = Matrix.Multiply(gradient, transposed);

                weights[i].Add(delta);
                biases[i].Add(gradient);

                if (i != 0)
                {
                    Matrix weightT = Matrix.Transpose(weights[i]);
                    error = Matrix.Multiply(weightT, error);
                }
            }
        }

        public void SaveWeights(string path)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.WriteLine($"InputNodes: {inputNodes}");
                writer.WriteLine($"HiddenLayers: {string.Join(",", hiddenLayers)}");
                writer.WriteLine($"OutputNodes: {outputNodes}");

                for (int i = 0; i < weights.Count; i++)
                {
                    writer.WriteLine($"# Layer {i} Weights");
                    WriteMatrix(writer, weights[i]);

                    writer.WriteLine($"# Layer {i} Biases");
                    WriteMatrix(writer, biases[i]);
                }
            }
        }

        private void WriteMatrix(StreamWriter writer, Matrix matrix)
        {
            for (int i = 0; i < matrix.Rows; i++)
            {
                for (int j = 0; j < matrix.Columns; j++)
                {
                    writer.Write(matrix[i, j].ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                    if (j < matrix.Columns - 1) writer.Write(",");
                }
                writer.WriteLine();
            }
        }
    }
}
