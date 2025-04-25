using ChessBot.AI;
using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using static System.Formats.Asn1.AsnWriter;

public partial class Train : Node2D
{
    [Export]
    string rawCsv = "C:/Univ/MastersWork/chessData.csv";

    [Export]
    string AllPreparedData = "C:/Univ/MastersWork/AllChessData1000.txt";

    [Export]
    string RegularPreparedData = "C:/Univ/MastersWork/RegualrChessData1000.txt";

    [Export]
    string MatingPreparedData = "C:/Univ/MastersWork/MatingChessData1000.txt";

    [Export]
    string SaveFolderPath = "Train";

    [Export]
    string SaveModelPath = "alldatatrained";

    public double[] ConvertClassToOneHot(double value)
    {
        return value switch
        {
            -1 => new double[] { 1, 0, 0 },
            0 => new double[] { 0, 1, 0 },
            1 => new double[] { 0, 0, 1 },
            _ => throw new Exception("Invalid class value")
        };
    }

    public static double[] ParseFenToInput(string fen)
    {
        string[] parts = fen.Split(' ');
        string board = parts[0];

        List<double> input = new List<double>();

        Dictionary<char, double> pieceMap = new Dictionary<char, double>
        {
            ['p'] = -1,
            ['r'] = -2,
            ['n'] = -3,
            ['b'] = -4,
            ['q'] = -5,
            ['k'] = -6,
            ['P'] = 1,
            ['R'] = 2,
            ['N'] = 3,
            ['B'] = 4,
            ['Q'] = 5,
            ['K'] = 6,
        };

        foreach (char c in board)
        {
            if (char.IsDigit(c))
            {
                int empty = c - '0';
                input.AddRange(Enumerable.Repeat(0.0, empty));
            }
            else if (pieceMap.ContainsKey(c))
            {
                input.Add(pieceMap[c]);
            }
        }

        // Normalize board size
        while (input.Count < 64) input.Add(0);

        return input.ToArray(); // Length: 64
    }

    public void PrepareDataAndSave()
    {
        string[] lines = File.ReadAllLines(rawCsv).Skip(1).ToArray();
        using var Allwriter = new StreamWriter(AllPreparedData);
        using var Regularwriter = new StreamWriter(RegularPreparedData);
        using var Matingwriter = new StreamWriter(MatingPreparedData);

        double cap = 1000;

        double target;

        double[] input;

        double[] targetArr;

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] split = line.Split(',');

            string fen = split[0];

            if (split[1][0] == '#')
            {
                target = (split[1][1] == '-') ? -32000 : 32000;

                input = ParseFenToInput(fen);
                targetArr = new double[] { Math.Max(-cap, Math.Min(target, cap)) / cap };

                Allwriter.WriteLine(string.Join(";", input) + "|" + string.Join(";", targetArr));
                Matingwriter.WriteLine(string.Join(";", input) + "|" + string.Join(";", targetArr));

                continue;
            }
            else if (!double.TryParse(split[1], out target))
            {
                GD.PrintErr($"Invalid number: '{split[1]}'");
                continue;
            }

            input = ParseFenToInput(fen);
            targetArr = [Math.Max(-cap, Math.Min(target, cap)) / cap];

            Regularwriter.WriteLine(string.Join(";", input) + "|" + string.Join(";", targetArr));
            Allwriter.WriteLine(string.Join(";", input) + "|" + string.Join(";", targetArr));
            targetArr = [0];
            Matingwriter.WriteLine(string.Join(";", input) + "|" + string.Join(";", targetArr));
        }
    }

    public List<(double[] input, double[] target)> LoadClassificationData(string path, int number = int.MaxValue, int startPos = 0)
    {
        var dataset = new List<(double[] input, double[] target)>();

        int ptr = -1;
        foreach (string line in File.ReadLines(path))
        {
            ptr++;
            if (ptr <= startPos) continue;
            if (ptr > startPos+number) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split('|');
            var input = parts[0].Split(';').Select(double.Parse).Select(x => x / 6).ToArray();
            var raw = double.Parse(parts[1], CultureInfo.InvariantCulture);

            // Очікуємо значення -1, 0, 1
            int classValue = (int)Math.Round(raw); // якщо раніше було щось типу -1.0/1.0
            var target = ConvertClassToOneHot(classValue);
            dataset.Add((input, target));
        }

        return dataset;
    }

    public List<(double[] input, double[] target)> LoadPreparedData(string preparedPath, int number = int.MaxValue, int startPos = 0)
    {
        List<(double[] input, double[] target)> dataset = new();


        int ptr = -1;
        foreach (string line in File.ReadLines(preparedPath))
        {
            ptr++;
            if (ptr <= startPos) continue;
            if (ptr > startPos+number) break;

            var parts = line.Split('|');
            var input = parts[0].Split(';').Select(double.Parse).Select(x => x/6).ToArray();
            var target = parts[1].Split(';').Select(double.Parse).ToArray();
            dataset.Add((input, target));
        }

        return dataset;
    }

    public void TrainFromPrepared(List<(double[] input, double[] target)> dataset, NeuralNetwork nn, int epochs, int batchSize)
    {
        var rnd = new Random();

        for (int epoch = 0; epoch < epochs; epoch++)
        {
            double totalError = 0.0;
            // shuffle
            dataset = dataset
                .OrderBy(_ => rnd.Next())
                .ToList();

            // process in batches
            for (int i = 0; i < dataset.Count; i += batchSize)
            {
                // take a slice: last batch may be smaller
                var batch = dataset
                    .Skip(i)
                    .Take(batchSize)
                    .ToList();

                // build arrays for inputs and targets
                var inputs = batch.Select(x => x.input).ToArray();
                var targets = batch.Select(x => x.target).ToArray();

                // accumulate error for reporting
                foreach (var (inp, tgt) in batch)
                {
                    var outp = nn.Predict(inp);
                    var err = tgt[0] - outp[0];
                    totalError += err * err;
                }

                // one batch‐training step
                nn.TrainBatch(inputs, targets);
                
            }

            var mse = totalError / dataset.Count;
            GD.Print($"Epoch {epoch + 1}/{epochs} - MSE: {mse:F6}");

            if ((epoch + 1) % 10 == 0 && dataset.Count > 0)
            {
                var (exampleInput, exampleTarget) = dataset[0];
                var pred = nn.Predict(exampleInput);
                GD.Print($"[DEBUG] Target: {exampleTarget[0]:F3}, Output: {pred[0]:F3}");
            }
        }
    }
   
    public void TrainClassification(List<(double[] input, double[] target)> dataset, NeuralNetwork nn, int epochs, int batchSize)
    {
        var rnd = new Random();

        for (int epoch = 0; epoch < epochs; epoch++)
        {
            double totalError = 0.0;

            // Shuffle dataset once per epoch
            dataset = dataset.OrderBy(x => rnd.Next()).ToList();

            for (int i = 0; i < dataset.Count; i += batchSize)
            {
                var batch = dataset.Skip(i).Take(batchSize).ToList();
                var inputs = batch.Select(x => x.input).ToArray();
                var targets = batch.Select(x => x.target).ToArray();

                // First do forward pass to compute error
                for (int j = 0; j < batch.Count; j++)
                {
                    var output = nn.Predict(inputs[j]);
                    var target = targets[j];

                    for (int k = 0; k < target.Length; k++)
                    {
                        totalError += -target[k] * Math.Log(Math.Max(output[k], 1e-8));
                    }
                }

                // Then do backpropagation for the batch
                nn.TrainBatch(inputs, targets);
            }

            double avgError = totalError / dataset.Count;
            GD.Print($"Epoch {epoch + 1}/{epochs} - CrossEntropy: {avgError:F6}");

            if ((epoch + 1) % 10 == 0 && dataset.Count > 0)
            {
                var (input, target) = dataset[epoch % dataset.Count];
                var prediction = nn.Predict(input);
                int predictedIndex = Array.IndexOf(prediction, prediction.Max());
                int targetIndex = Array.IndexOf(target, target.Max());
                GD.Print($"Target: {targetIndex - 1}, Predicted: {predictedIndex - 1}");
            }
        }
    }

    public void TestPrediction(List<(double[] input, double[] target)> dataset, NeuralNetwork nn)
    {
        double totalError = 0.0;

        foreach (var (input, target) in dataset)
        {
            double[] output = nn.Predict(input);
            double error = target[0] - output[0];
            totalError += error * error;
            GD.Print($"Target: {target[0]:F3}, Output: {output[0]:F3}");
        }

        double mse = totalError / dataset.Count;
        GD.Print($"Total MSE: {mse:F6}");
    }

    public void TestClassification(List<(double[] input, double[] target)> dataset, NeuralNetwork nn)
    {
        int correct = 0;

        foreach (var (input, target) in dataset)
        {
            double[] output = nn.Predict(input);
            int predictedIndex = Array.IndexOf(output, output.Max());
            int targetIndex = Array.IndexOf(target, target.Max());

            if (predictedIndex == targetIndex)
                correct++;

            GD.Print($"Expected: {targetIndex - 1}, Got: {predictedIndex - 1}");
        }

        GD.Print($"Accuracy: {(double)correct / dataset.Count:P2}");
    }

    public override void _Ready()
    {
        const int epochs = 30;
        const int batchSize = 256;

        //GD.Print("Preparing data");
        //PrepareDataAndSave();

        var activations = new List<ActivationType>{
            ActivationType.ReLU,
            ActivationType.ReLU,
            ActivationType.ReLU,
            ActivationType.ReLU,
            ActivationType.Softmax
        };

        var nn = new NeuralNetwork(
            inputNodes: 64,
            hiddenLayers: new List<int> { 128, 256, 64, 32 },
            outputNodes: 3,
            activations: activations,
            learningRate: 0.0003
        );

        //var nn = new NeuralNetwork(SaveFolderPath + "/" + SaveModelPath + ".json");


        var dataset = LoadClassificationData(MatingPreparedData, 20000);

        var dataset1 = LoadClassificationData(MatingPreparedData, 20000, 200000);

        GD.Print($"Training on data: {dataset.Count}");
        TrainClassification(dataset, nn, epochs, batchSize);

        GD.Print($"Testing on data: {dataset1.Count}");
        TestClassification(dataset1, nn);

        //GD.Print($"Saving");
        //nn.SaveToJson(SaveFolderPath + "/" + SaveModelPath + ".json");

        GD.Print("Done!\n\n");
    }


}
