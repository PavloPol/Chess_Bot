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

        private int adamT;
        private readonly double beta1 = 0.9;
        private readonly double beta2 = 0.999;
        private readonly double epsilon = 1e-8;
        private List<Matrix> mWeights, vWeights, mBiases, vBiases;

        private double learningRate;

        // Default learning rate lowered to 0.001 for stability
        public NeuralNetwork(int inputNodes, List<int> hiddenLayers, int outputNodes, List<ActivationType> activations, double learningRate = 0.01)
        {
            this.inputNodes = inputNodes;
            this.hiddenLayers = new List<int>(hiddenLayers);
            this.outputNodes = outputNodes;
            this.learningRate = learningRate;

            weights = new List<Matrix>();
            biases = new List<Matrix>();
            adamT = 0;

            // Xavier (fan-in) initialization for each weight matrix
            var prev = inputNodes;
            foreach (var layerSize in hiddenLayers)
            {
                var w = new Matrix(layerSize, prev);
                w.Randomize(fanIn: prev);
                weights.Add(w);
                biases.Add(new Matrix(layerSize, 1));
                prev = layerSize;
            }
            // output layer
            var outW = new Matrix(outputNodes, prev);
            outW.Randomize(fanIn: prev);
            weights.Add(outW);
            biases.Add(new Matrix(outputNodes, 1));

            // Adam moment vectors
            mWeights = weights.Select(w => new Matrix(w.Rows, w.Columns)).ToList();
            vWeights = weights.Select(w => new Matrix(w.Rows, w.Columns)).ToList();
            mBiases = biases.Select(b => new Matrix(b.Rows, b.Columns)).ToList();
            vBiases = biases.Select(b => new Matrix(b.Rows, b.Columns)).ToList();

            if (activations.Count != weights.Count)
                throw new ArgumentException("Activation count must match number of layers");
            this.activations = new List<ActivationType>(activations);
        }

        public NeuralNetwork(string path)
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<NetworkData>(json);

            inputNodes = data.InputNodes;
            hiddenLayers = data.HiddenLayers;
            outputNodes = data.OutputNodes;
            activations = data.Activations.Select(s => Enum.Parse<ActivationType>(s)).ToList();
            learningRate = 0.001;
            adamT = 0;

            weights = data.Weights.Select(layer =>
            {
                var m = new Matrix(layer.Count, layer[0].Count);
                for (int i = 0; i < layer.Count; i++)
                    for (int j = 0; j < layer[0].Count; j++)
                        m[i, j] = layer[i][j];
                return m;
            }).ToList();

            biases = data.Biases.Select(layer =>
            {
                var m = new Matrix(layer.Count, layer[0].Count);
                for (int i = 0; i < layer.Count; i++)
                    for (int j = 0; j < layer[0].Count; j++)
                        m[i, j] = layer[i][j];
                return m;
            }).ToList();

            mWeights = weights.Select(w => new Matrix(w.Rows, w.Columns)).ToList();
            vWeights = weights.Select(w => new Matrix(w.Rows, w.Columns)).ToList();
            mBiases = biases.Select(b => new Matrix(b.Rows, b.Columns)).ToList();
            vBiases = biases.Select(b => new Matrix(b.Rows, b.Columns)).ToList();
        }

        public double[] Predict(double[] inputArray)
        {
            var output = Matrix.ConvertArrayToMatrix(inputArray);
            for (int i = 0; i < weights.Count; i++)
            {
                output = Matrix.Multiply(weights[i], output);
                output = output.Add(biases[i]);
                if (activations[i] == ActivationType.Softmax)
                    output = ActivationFunctions.Softmax(output);
                else
                    output = output.ApplyFunction(GetActivation(activations[i]));
            }
            return Matrix.ConvertMatrixToArray(output);
        }

        public void TrainBatch(double[][] inputBatch, double[][] targetBatch)
        {
            int batchSize = inputBatch.Length;
            // Build batch input and target matrices: each column is one sample
            var input = new Matrix(inputNodes, batchSize);
            var target = new Matrix(outputNodes, batchSize);
            for (int j = 0; j < batchSize; j++)
            {
                var colIn = Matrix.ConvertArrayToMatrix(inputBatch[j]);
                var colTgt = Matrix.ConvertArrayToMatrix(targetBatch[j]);
                input.SetColumn(j, colIn);
                target.SetColumn(j, colTgt);
            }

            // Forward pass
            var outputs = new List<Matrix> { input };
            var current = input;
            for (int i = 0; i < weights.Count; i++)
            {
                current = Matrix.Multiply(weights[i], current);
                // replicate bias across batch columns
                var biasTile = biases[i].Repeat(1, batchSize);
                current = current.Add(biasTile);
                if (activations[i] == ActivationType.Softmax)
                    current = ActivationFunctions.Softmax(current);
                else
                    current = current.ApplyFunction(GetActivation(activations[i]));
                outputs.Add(current);
            }

            // Compute batch error
            var error = Matrix.Subtract(outputs[^1], target);
            adamT++;

            // Backprop
            for (int i = weights.Count - 1; i >= 0; i--)
            {
                // 1) local gradient
                Matrix grad = activations[i] == ActivationType.Softmax
                    ? error
                    : Matrix.ApplyFunction(outputs[i + 1], GetDerivative(activations[i])).Hadamard(error);

                // 2) weight gradient averaged over batch
                var deltaW = Matrix.Multiply(grad, Matrix.Transpose(outputs[i]));
                deltaW = deltaW.Multiply(1.0 / batchSize);

                // 3) Adam update for weights
                mWeights[i] = mWeights[i].Multiply(beta1).Add(deltaW.Multiply(1 - beta1));
                vWeights[i] = vWeights[i].Multiply(beta2).Add(Matrix.Hadamard(deltaW, deltaW).Multiply(1 - beta2));
                var mHat = mWeights[i].Multiply(1 / (1 - Math.Pow(beta1, adamT)));
                var vHat = vWeights[i].Multiply(1 / (1 - Math.Pow(beta2, adamT)));
                var denom = Matrix.ApplyFunction(vHat, x => Math.Sqrt(x) + epsilon);
                var stepW = Matrix.Hadamard(mHat, denom.ApplyFunction(x => 1 / x)).Multiply(learningRate);
                weights[i] = weights[i].Subtract(stepW);

                // 4) bias gradient averaged and Adam update
                var deltaB = grad.SumRows();             // returns a column vector of summed gradients per neuron
                deltaB = deltaB.Multiply(1.0 / batchSize);
                mBiases[i] = mBiases[i].Multiply(beta1).Add(deltaB.Multiply(1 - beta1));
                vBiases[i] = vBiases[i].Multiply(beta2).Add(Matrix.Hadamard(deltaB, deltaB).Multiply(1 - beta2));
                var mHatB = mBiases[i].Multiply(1 / (1 - Math.Pow(beta1, adamT)));
                var vHatB = vBiases[i].Multiply(1 / (1 - Math.Pow(beta2, adamT)));
                var denomB = Matrix.ApplyFunction(vHatB, x => Math.Sqrt(x) + epsilon);
                var stepB = Matrix.Hadamard(mHatB, denomB.ApplyFunction(x => 1 / x)).Multiply(learningRate);
                biases[i] = biases[i].Subtract(stepB);

                // 5) propagate error
                if (i > 0)
                    error = Matrix.Multiply(Matrix.Transpose(weights[i]), grad);
            }
        }


        private Func<double, double> GetActivation(ActivationType type) => type switch
        {
            ActivationType.Sigmoid => ActivationFunctions.Sigmoid,
            ActivationType.Tanh => ActivationFunctions.Tanh,
            ActivationType.ReLU => ActivationFunctions.ReLU,
            ActivationType.Linear => ActivationFunctions.Linear,
            _ => throw new ArgumentException("Unsupported activation")
        };

        private Func<double, double> GetDerivative(ActivationType type) => type switch
        {
            ActivationType.Sigmoid => ActivationFunctions.SigmoidDerivative,
            ActivationType.Tanh => ActivationFunctions.TanhDerivative,
            ActivationType.ReLU => ActivationFunctions.ReLUDerivative,
            ActivationType.Linear => ActivationFunctions.LinearDerivative,
            _ => throw new ArgumentException("Unknown activation")
        };

        public void SaveToJson(string path)
        {
            var data = new NetworkData
            {
                InputNodes = inputNodes,
                HiddenLayers = hiddenLayers,
                OutputNodes = outputNodes,
                Activations = activations.Select(a => a.ToString()).ToList(),
                Weights = weights.Select(m => Matrix.ToList(m)).ToList(),
                Biases = biases.Select(m => Matrix.ToList(m)).ToList()
            };
            var opts = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(data, opts));
        }

        public void PrintNetwork()
        {
            GD.Print("=== Neural Network ===");
            GD.Print($"Input nodes : {inputNodes}");
            GD.Print($"Hidden layers: {string.Join(", ", hiddenLayers)}");
            GD.Print($"Output nodes: {outputNodes}");
            GD.Print("-----------------------");

            for (int i = 0; i < weights.Count; i++)
            {
                GD.Print($"Layer {i + 1} ({activations[i]})");
                GD.Print($"  Weights [{weights[i].Rows}×{weights[i].Columns}]:");
                for (int r = 0; r < weights[i].Rows; r++)
                {
                    var rowVals = new double[weights[i].Columns];
                    for (int c = 0; c < weights[i].Columns; c++)
                        rowVals[c] = weights[i].Data[r, c];
                    GD.Print("    " + string.Join(", ", rowVals.Select(v => v.ToString("F4"))));
                }

                GD.Print($"  Biases [{biases[i].Rows}×{biases[i].Columns}]:");
                for (int r = 0; r < biases[i].Rows; r++)
                {
                    // biases[i].Columns is 1
                    GD.Print($"    {biases[i].Data[r, 0]:F4}");
                }
                GD.Print("-----------------------");
            }
        }
    }
}
