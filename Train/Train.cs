using ChessBot.AI;
using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static System.Formats.Asn1.AsnWriter;

public partial class Train : Node2D
{
    [Export]
    string AllPreparedData = "C:/Univ/MastersWork/AllChessData.txt";

    [Export]
    string RegularPreparedData = "C:/Univ/MastersWork/RegualrChessData.txt";

    [Export]
    string MatingPreparedData = "C:/Univ/MastersWork/MatingChessData.txt";

    [Export]
    string rawCsv = "C:/Univ/MastersWork/chessData.csv";

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

        double cap = 100;

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
                targetArr = [Math.Max(-cap, Math.Min(target, cap)) / cap];

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
            var input = parts[0].Split(';').Select(double.Parse).ToArray();
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
            var input = parts[0].Split(';').Select(double.Parse).ToArray();
            var target = parts[1].Split(';').Select(double.Parse).ToArray();
            dataset.Add((input, target));
        }

        return dataset;
    }

    public void TrainFromPrepared(List<(double[] input, double[] target)> dataset, NeuralNetwork nn, int epochs)
    {
        for (int epoch = 0; epoch < epochs; epoch++)
        {
            double totalError = 0.0;

            var rnd = new Random();
            dataset = dataset.OrderBy(x => rnd.Next()).ToList();

            foreach (var (input, target) in dataset)
            {
                double[] output = nn.Predict(input);
                double error = target[0] - output[0];
                totalError += error * error;
                nn.Train(input, target);
            }

            double mse = totalError / dataset.Count;
            GD.Print($"Epoch {epoch + 1}/{epochs} - MSE: {mse:F6}");

            if ((epoch + 1) % 10 == 0)
            {
                var (exampleInput, exampleTarget) = dataset[0];
                var prediction = nn.Predict(exampleInput);
                GD.Print($"[DEBUG] Target: {exampleTarget[0]:F3}, Output: {prediction[0]:F3}");
            }
        }
    }

    public void TrainClassification(List<(double[] input, double[] target)> dataset, NeuralNetwork nn, int epochs)
    {
        for (int epoch = 0; epoch < epochs; epoch++)
        {
            double totalError = 0.0;

            var rnd = new Random();
            dataset = dataset.OrderBy(x => rnd.Next()).ToList();

            foreach (var (input, target) in dataset)
            {
                double[] output = nn.Predict(input);

                // Cross-entropy loss (simplified): сумуємо -target[i] * log(output[i])
                for (int i = 0; i < target.Length; i++)
                {
                    totalError += -target[i] * Math.Log(Math.Max(output[i], 1e-8));
                }

                nn.Train(input, target);

            }

            double avgError = totalError / dataset.Count;
            GD.Print($"Epoch {epoch + 1}/{epochs} - CrossEntropy: {avgError:F6}");

            //if (epoch % 10 == 0)
            //{
            //    var (input, target) = dataset[epoch % dataset.Count];
            //    var prediction = nn.Predict(input);
            //    int predictedIndex = Array.IndexOf(prediction, prediction.Max());
            //    int targetIndex = Array.IndexOf(target, target.Max());
            //    GD.Print($"Target: {targetIndex - 1}, Predicted: {predictedIndex - 1}");
            //}
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
        int epoch = 30;

        var activations = new List<ActivationType> { ActivationType.ReLU, ActivationType.ReLU, ActivationType.ReLU, ActivationType.Tanh };
        var nn = new NeuralNetwork(64, new List<int> { 128, 64, 32 }, 1, activations);

        GD.Print("Preparing data");
        var dataset = LoadPreparedData(AllPreparedData, 5000);
        var TestDataset = LoadPreparedData(AllPreparedData, 1000, 5000);

        GD.Print("Training on size: " + dataset.Count);
        TrainFromPrepared(dataset, nn, epoch);

        GD.Print("Testing on size: " + TestDataset.Count);
        TestPrediction(TestDataset, nn);

        nn.SaveToJson("test_all_nn.json");

        GD.Print("Testing on size: " + TestDataset.Count);
        var nn2 = new NeuralNetwork("test_all_nn.json");
        TestPrediction(TestDataset, nn2);

        //var hidden = new List<int> { 64, 32 };
        //var nn = new NeuralNetwork(64, hidden, 1);

        // Не чіпати всі дані завантажено
        //GD.Print("Preparing and saving data...");
        //PrepareDataAndSave();

        //GD.Print("Preparing data");
        //var dataset = LoadPreparedData(RegularPreparedData);

        //foreach (var (input, target) in dataset)
        //{
        //    GD.Print($"input:{input[0]}, {input.Length}, target:{target[0]}");
        //}

        //GD.Print("Training on size:" + dataset.Count);
        //TrainFromPrepared(dataset, nn, epoch);

        //GD.Print("SavingWeigths");
        //nn.SaveWeights("trained_weights_2.txt");

        //var nn2 = new NeuralNetwork("trained_weights_2.txt");

        //GD.Print("Testing...");
        //TestPrediction(dataset, nn2);

        GD.Print("All Done!");
    }

}
