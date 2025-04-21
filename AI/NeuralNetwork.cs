using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using Godot;
using System.Text.Json;
using System.Linq;


namespace ChessBot.AI
{
    public class NetworkData
    {
        public int InputNodes { get; set; }
        public List<int> HiddenLayers { get; set; }
        public int OutputNodes { get; set; }
        public List<string> Activations { get; set; }
        public List<List<List<double>>> Weights { get; set; }
        public List<List<List<double>>> Biases { get; set; }
    }

    public class NeuralNetwork
    {
        private int inputNodes;
        private int outputNodes;
        private List<int> hiddenLayers;

        private List<Matrix> weights;
        private List<Matrix> biases;

        private List<ActivationType> activations;

        private List<Matrix> mWeights;
        private List<Matrix> vWeights;
        private List<Matrix> mBiases;
        private List<Matrix> vBiases;

        private double beta1 = 0.9;
        private double beta2 = 0.999;
        private double epsilon = 1e-8;
        private int timestep = 0;

        private double learningRate = 0.001;

        public NeuralNetwork(int inputNodes, List<int> hiddenLayers, int outputNodes, List<ActivationType> activations)
        {
            this.inputNodes = inputNodes;
            this.hiddenLayers = new List<int>(hiddenLayers);
            this.outputNodes = outputNodes;

            weights = new List<Matrix>();
            biases = new List<Matrix>();

            // Input to first hidden layer
            weights.Add(new Matrix(hiddenLayers[0], inputNodes));
            biases.Add(new Matrix(hiddenLayers[0], 1));
            weights[0].Randomize(inputNodes);

            // Hidden layers
            for (int i = 1; i < hiddenLayers.Count; i++)
            {
                weights.Add(new Matrix(hiddenLayers[i], hiddenLayers[i - 1]));
                biases.Add(new Matrix(hiddenLayers[i], 1));
                weights[i].Randomize(hiddenLayers[i - 1]);
            }

            if (activations.Count != hiddenLayers.Count + 1)
                throw new ArgumentException("Activation count must match number of layers (hidden + output)");

            this.activations = new List<ActivationType>(activations);

            // Last hidden to output
            weights.Add(new Matrix(outputNodes, hiddenLayers[^1]));
            biases.Add(new Matrix(outputNodes, 1));
            weights[^1].Randomize(hiddenLayers[^1]);

            //foreach (var w in weights)
            //{
            //    for (int i = 0; i < w.Rows; i++)
            //        for (int j = 0; j < w.Columns; j++)
            //            if (double.IsNaN(w[i, j]) || double.IsInfinity(w[i, j]))
            //                GD.PrintErr($"Weight NaN at ({i},{j}) = {w[i, j]}");
            //}

            mWeights = weights.Select(w => new Matrix(w.Rows, w.Columns)).ToList();
            vWeights = weights.Select(w => new Matrix(w.Rows, w.Columns)).ToList();
            mBiases = biases.Select(b => new Matrix(b.Rows, b.Columns)).ToList();
            vBiases = biases.Select(b => new Matrix(b.Rows, b.Columns)).ToList();

            //// Randomize
            //foreach (var w in weights) w.Randomize();
            //foreach (var b in biases) b.Randomize();
        }

        public NeuralNetwork(string path)
        {
            string json = File.ReadAllText(path);
            try
            {
                var data = JsonSerializer.Deserialize<NetworkData>(json);

                inputNodes = data.InputNodes;
                hiddenLayers = data.HiddenLayers;
                outputNodes = data.OutputNodes;
                activations = data.Activations.Select(s => Enum.Parse<ActivationType>(s)).ToList();

                weights = data.Weights.Select(list =>
                {
                    int rows = list.Count;
                    int cols = list[0].Count;
                    var m = new Matrix(rows, cols);
                    for (int i = 0; i < rows; i++)
                        for (int j = 0; j < cols; j++)
                            m[i, j] = list[i][j];
                    return m;
                }).ToList();

                biases = data.Biases.Select(list =>
                {
                    int rows = list.Count;
                    int cols = list[0].Count;
                    var m = new Matrix(rows, cols);
                    for (int i = 0; i < rows; i++)
                        for (int j = 0; j < cols; j++)
                            m[i, j] = list[i][j];
                    return m;
                }).ToList();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to load neural network from {path}: {ex.Message}");
                throw;
            }
        }



        public double[] Predict(double[] inputArray)
        {
            Matrix output = Matrix.ConvertArrayToMatrix(inputArray);

            for (int i = 0; i < weights.Count; i++)
            {
                output = Matrix.Multiply(weights[i], output);
                output.Add(biases[i]);

                if (activations[i] == ActivationType.Softmax)
                {
                    output = ActivationFunctions.Softmax(output);
                }
                else
                {
                    output.ApplyFunction(GetActivation(activations[i]));
                }
                if (double.IsNaN(output[0, 0]) || double.IsInfinity(output[0, 0]))
                {
                    GD.PrintErr($"[Predict] NaN після шару {i}, активація: {activations[i]}");
                }
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

                if (activations[i] == ActivationType.Softmax)
                    current = ActivationFunctions.Softmax(current);
                else
                    current.ApplyFunction(GetActivation(activations[i]));

                layerOutputs.Add(current);
            }

            // Calculate error
            Matrix output = layerOutputs[^1];
            Matrix error = Matrix.Substract(target, output);

            // Backward pass
            for (int i = weights.Count - 1; i >= 0; i--)
            {
                Matrix gradient;
                if (activations[i] == ActivationType.Softmax)
                    gradient = Matrix.Substract(layerOutputs[i + 1], target);
                else
                {
                    gradient = Matrix.ApplyFunction(layerOutputs[i + 1], GetDerivative(activations[i]));
                    gradient.Hadamard(error);
                }

                gradient.Multiply(learningRate);
                Matrix transposed = Matrix.Transpose(layerOutputs[i]);
                Matrix delta = Matrix.Multiply(gradient, transposed);

                // Adam для weights
                mWeights[i].Multiply(beta1);
                mWeights[i].Add(Matrix.Multiply(delta, 1 - beta1));

                vWeights[i].Multiply(beta2);
                Matrix deltaSq = Matrix.ApplyFunction(delta, x => x * x);
                vWeights[i].Add(Matrix.Multiply(deltaSq, 1 - beta2));

                Matrix mHat = Matrix.Multiply(mWeights[i], 1.0 / (1 - Math.Pow(beta1, timestep)));
                Matrix vHat = Matrix.Multiply(vWeights[i], 1.0 / (1 - Math.Pow(beta2, timestep)));

                Matrix update = Matrix.DivideElementWise(mHat, Matrix.ApplyFunction(vHat, x => Math.Sqrt(x) + epsilon));
                weights[i].Substract(update);

                // Adam для biases
                mBiases[i].Multiply(beta1);
                mBiases[i].Add(Matrix.Multiply(gradient, 1 - beta1));

                vBiases[i].Multiply(beta2);
                Matrix gradSq = Matrix.ApplyFunction(gradient, x => x * x);
                vBiases[i].Add(Matrix.Multiply(gradSq, 1 - beta2));

                Matrix mHatB = Matrix.Multiply(mBiases[i], 1.0 / (1 - Math.Pow(beta1, timestep)));
                Matrix vHatB = Matrix.Multiply(vBiases[i], 1.0 / (1 - Math.Pow(beta2, timestep)));

                Matrix updateB = Matrix.DivideElementWise(mHatB, Matrix.ApplyFunction(vHatB, x => Math.Sqrt(x) + epsilon));
                biases[i].Substract(updateB);

                if (i != 0)
                    error = Matrix.Multiply(Matrix.Transpose(weights[i]), error);
            }
        }

        private Func<double, double> GetActivation(ActivationType type)
        {
            return type switch
            {
                ActivationType.Sigmoid => ActivationFunctions.Sigmoid,
                ActivationType.Tanh => ActivationFunctions.Tanh,
                ActivationType.ReLU => ActivationFunctions.ReLU,
                ActivationType.Linear => ActivationFunctions.Linear,
                _ => throw new ArgumentException("Unsupported activation")
            };
        }

        private Func<double, double> GetDerivative(ActivationType type)
        {
            return type switch
            {
                ActivationType.Sigmoid => ActivationFunctions.SigmoidDerivative,
                ActivationType.Tanh => ActivationFunctions.TanhDerivative,
                ActivationType.ReLU => ActivationFunctions.ReLUDerivative,
                ActivationType.Linear => ActivationFunctions.LinearDerivative,
                _ => throw new ArgumentException("Unknown activation")
            };
        }

        public void SaveToJson(string path)
        {
            var data = new NetworkData
            {
                InputNodes = inputNodes,
                HiddenLayers = hiddenLayers,
                OutputNodes = outputNodes,
                Activations = activations.Select(a => a.ToString()).ToList(),
                Weights = weights.Select(m => MatrixToList(m)).ToList(),
                Biases = biases.Select(m => MatrixToList(m)).ToList()
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(path, json);
        }

        private List<List<double>> MatrixToList(Matrix matrix)
        {
            var result = new List<List<double>>();
            for (int i = 0; i < matrix.Rows; i++)
            {
                var row = new List<double>();
                for (int j = 0; j < matrix.Columns; j++)
                {
                    row.Add(matrix[i, j]);
                }
                result.Add(row);
            }
            return result;
        }

    }
}
