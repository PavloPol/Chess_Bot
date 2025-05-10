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

    private StreamWriter logWriter;

    private void Log(string message)
    {
        GD.Print(message);
        logWriter?.WriteLine(message);
    }

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

        int matingPtr = 0;

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] split = line.Split(',');

            string fen = split[0];

            bool isBlackMove = fen.Split(' ')[1] == "b";

            if (split[1][0] == '#') // Mating position
            {
                target = (split[1][1] == '-') ? -32000 : 32000;

                if (isBlackMove)
                {
                    fen = FlipFen(fen);
                    target = -target;
                }

                input = ParseFenToInput(fen);
                targetArr = new double[] { Math.Max(-cap, Math.Min(target, cap)) / cap };

                Matingwriter.WriteLine(string.Join(";", input) + "|" + string.Join(";", targetArr));
                continue;
            }
            else if (!double.TryParse(split[1], out target))
            {
                GD.PrintErr($"Invalid number: '{split[1]}'");
                continue;
            }

            if (isBlackMove)
            {
                fen = FlipFen(fen);
                target = -target;
            }

            input = ParseFenToInput(fen);
            targetArr = new double[] { Math.Max(-cap, Math.Min(target, cap)) / cap };

            Regularwriter.WriteLine(string.Join(";", input) + "|" + string.Join(";", targetArr));
            Allwriter.WriteLine(string.Join(";", input) + "|" + string.Join(";", targetArr));

            if (matingPtr == 67)
            {
                matingPtr = 0;
                targetArr = new double[] { 0 };
                Matingwriter.WriteLine(string.Join(";", input) + "|" + string.Join(";", targetArr));
            }
            else
            {
                matingPtr++;
            }
        }
    }

    private string FlipFen(string fen)
    {
        string[] parts = fen.Split(' ');
        if (parts.Length < 6)
            throw new ArgumentException("Invalid FEN: not enough parts");

        // Flip board rows
        string[] rows = parts[0].Split('/');
        Array.Reverse(rows);
        for (int i = 0; i < rows.Length; i++)
        {
            char[] row = rows[i].ToCharArray();
            for (int j = 0; j < row.Length; j++)
            {
                if (char.IsLetter(row[j]))
                {
                    row[j] = char.IsUpper(row[j]) ? char.ToLower(row[j]) : char.ToUpper(row[j]);
                }
            }
            rows[i] = new string(row);
        }
        string flippedBoard = string.Join("/", rows);

        // Flip castling rights
        string castling = parts[2];
        castling = castling.Replace('K', 't').Replace('Q', 'y').Replace('k', 'K').Replace('q', 'Q')
                           .Replace('t', 'k').Replace('y', 'q');
        if (castling == "-") castling = "-";

        // Flip en passant (if present)
        string ep = parts[3];
        if (ep != "-")
        {
            int rank = 8 - int.Parse(ep[1].ToString()) + 1;
            ep = ep[0] + rank.ToString();
        }

        // Side to move is always white after flipping
        string flippedFen = $"{flippedBoard} w {castling} {ep} {parts[4]} {parts[5]}";
        return flippedFen;
    }

    public int CountData(string preparedPath)
    {
        int ptr = 0;
        foreach (string line in File.ReadLines(preparedPath))
        {
            ptr++;
        }
        return ptr;
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
        var stopwatch = new System.Diagnostics.Stopwatch();

        for (int epoch = 0; epoch < epochs; epoch++)
        {
            stopwatch.Restart();

            double totalMSE = 0.0;
            double totalMAE = 0.0;

            // Shuffle the dataset once per epoch
            dataset = dataset.OrderBy(_ => rnd.Next()).ToList();

            for (int i = 0; i < dataset.Count; i += batchSize)
            {
                var batch = dataset.Skip(i).Take(batchSize).ToList();
                var inputs = batch.Select(x => x.input).ToArray();
                var targets = batch.Select(x => x.target).ToArray();

                double batchMSE = 0.0;
                double batchMAE = 0.0;

                foreach (var (inp, tgt) in batch)
                {
                    var outp = nn.Predict(inp);
                    double error = tgt[0] - outp[0];
                    batchMSE += error * error;
                    batchMAE += Math.Abs(error);
                }

                totalMSE += batchMSE;
                totalMAE += batchMAE;

                nn.TrainBatch(inputs, targets);

                Log($"Epoch {epoch + 1}, Batch {i / batchSize + 1}: Batch MSE: {batchMSE / batch.Count:F6}, Batch MAE: {batchMAE / batch.Count:F6}");
            }

            stopwatch.Stop();

            double epochMSE = totalMSE / dataset.Count;
            double epochMAE = totalMAE / dataset.Count;

            Log($"=== Epoch {epoch + 1}/{epochs} completed ===");
            Log($"Epoch MSE: {epochMSE:F6} | Epoch MAE: {epochMAE:F6} | Time: {stopwatch.ElapsedMilliseconds} ms");
            Log("---------------------------------------------");

            if ((epoch + 1) % 10 == 0 && dataset.Count > 0)
            {
                var (exampleInput, exampleTarget) = dataset[epoch % dataset.Count];
                var pred = nn.Predict(exampleInput);
                Log($"[DEBUG] Example Target: {exampleTarget[0]:F3}, Prediction: {pred[0]:F3}");
            }
        }
    }

    public void TrainClassification(List<(double[] input, double[] target)> dataset, NeuralNetwork nn, int epochs, int batchSize)
    {
        var rnd = new Random();

        for (int epoch = 0; epoch < epochs; epoch++)
        {
            double totalError = 0.0;
            int correct = 0;
            int totalPredictions = 0;

            dataset = dataset.OrderBy(x => rnd.Next()).ToList();

            for (int i = 0; i < dataset.Count; i += batchSize)
            {
                var batch = dataset.Skip(i).Take(batchSize).ToList();
                var inputs = batch.Select(x => x.input).ToArray();
                var targets = batch.Select(x => x.target).ToArray();

                double batchError = 0.0;
                int batchCorrect = 0;

                for (int j = 0; j < batch.Count; j++)
                {
                    var output = nn.Predict(inputs[j]);
                    var target = targets[j];

                    int predictedIndex = Array.IndexOf(output, output.Max());
                    int targetIndex = Array.IndexOf(target, target.Max());

                    if (predictedIndex == targetIndex)
                        batchCorrect++;

                    for (int k = 0; k < target.Length; k++)
                    {
                        batchError += -target[k] * Math.Log(Math.Max(output[k], 1e-8));
                    }
                }

                nn.TrainBatch(inputs, targets);

                totalError += batchError;
                correct += batchCorrect;
                totalPredictions += batch.Count;

                Log($"Epoch {epoch + 1}, Batch {i / batchSize + 1}: CrossEntropy {batchError / batch.Count:F6}, Accuracy {(double)batchCorrect / batch.Count:P2}");
            }

            double avgError = totalError / dataset.Count;
            double epochAccuracy = (double)correct / totalPredictions;

            Log($"=== Epoch {epoch + 1}/{epochs} completed ===");
            Log($"Epoch Loss: {avgError:F6} | Epoch Accuracy: {epochAccuracy:P2}");
            Log("---------------------------------------------");

            if ((epoch + 1) % 10 == 0 && dataset.Count > 0)
            {
                var (input, target) = dataset[epoch % dataset.Count];
                var prediction = nn.Predict(input);
                int predictedIndex = Array.IndexOf(prediction, prediction.Max());
                int targetIndex = Array.IndexOf(target, target.Max());
                Log($"[DEBUG] Target: {targetIndex - 1}, Predicted: {predictedIndex - 1}");
            }
        }
    }

    public void TestPrediction(List<(double[] input, double[] target)> dataset, NeuralNetwork nn)
    {
        double totalSquaredError = 0.0;
        double totalAbsoluteError = 0.0;
        double maxError = double.MinValue;
        double minError = double.MaxValue;

        int shownExamples = 0;
        int showLimit = 5; // Show 5 sample predictions

        foreach (var (input, target) in dataset)
        {
            double[] output = nn.Predict(input);
            double error = target[0] - output[0];

            totalSquaredError += error * error;
            totalAbsoluteError += Math.Abs(error);

            if (Math.Abs(error) > maxError)
                maxError = Math.Abs(error);
            if (Math.Abs(error) < minError)
                minError = Math.Abs(error);

            // Print a few example predictions
            if (shownExamples < showLimit)
            {
                Log($"Example {shownExamples + 1}: Target = {target[0]:F3}, Prediction = {output[0]:F3}, Error = {error:F3}");
                shownExamples++;
            }
        }

        double mse = totalSquaredError / dataset.Count;
        double mae = totalAbsoluteError / dataset.Count;

        Log("=== Prediction Test Summary ===");
        Log($"Total Samples: {dataset.Count}");
        Log($"Mean Squared Error (MSE): {mse:F6}");
        Log($"Mean Absolute Error (MAE): {mae:F6}");
        Log($"Minimum Error: {minError:F6}");
        Log($"Maximum Error: {maxError:F6}");
        Log("================================");
    }

    public void TestClassification(List<(double[] input, double[] target)> dataset, NeuralNetwork nn)
    {
        int correct = 0;
        int total = dataset.Count;

        // Counters for per-class statistics
        int[] correctPerClass = new int[3];  // classes: 0 = -1, 1 = 0, 2 = 1
        int[] totalPerClass = new int[3];

        // Confusion matrix: [actual][predicted]
        int[,] confusionMatrix = new int[3, 3];

        foreach (var (input, target) in dataset)
        {
            double[] output = nn.Predict(input);
            int predictedIndex = Array.IndexOf(output, output.Max());
            int targetIndex = Array.IndexOf(target, target.Max());

            if (predictedIndex == targetIndex)
                correct++;

            // Update per-class statistics
            totalPerClass[targetIndex]++;
            if (predictedIndex == targetIndex)
                correctPerClass[targetIndex]++;

            // Update confusion matrix
            confusionMatrix[targetIndex, predictedIndex]++;
        }

        Log($"=== Classification Test Results ===");
        Log($"Total Samples: {total}");
        Log($"Overall Accuracy: {(double)correct / total:P2}");
        Log("------------------------------------");

        // Per-class accuracy
        string[] classNames = { "Mate for Black (-1)", "No Mate (0)", "Mate for White (1)" };
        for (int i = 0; i < 3; i++)
        {
            double classAccuracy = totalPerClass[i] > 0 ? (double)correctPerClass[i] / totalPerClass[i] : 0;
            Log($"Class '{classNames[i]}': {correctPerClass[i]}/{totalPerClass[i]} correct ({classAccuracy:P2})");
        }

        Log("------------------------------------");

        // Print Confusion Matrix
        Log("Confusion Matrix (Rows = Actual, Columns = Predicted):");
        for (int i = 0; i < 3; i++)
        {
            string row = "";
            for (int j = 0; j < 3; j++)
            {
                row += $"{confusionMatrix[i, j],5}";
            }
            Log(row);
        }

        Log("====================================\n");
    }

    public override void _Ready()
    {
        string logPath = SaveFolderPath + "/" + SaveModelPath + "LOG.txt";
        logWriter = new StreamWriter(logPath, append: false); // overwrite old file
        logWriter.AutoFlush = true; // immediately write to disk

        const int epochs = 30;
        const int batchSize = 256;

        var activations = new List<ActivationType>{
            ActivationType.ReLU,
            ActivationType.ReLU,
            ActivationType.ReLU,
            ActivationType.ReLU,
            ActivationType.Tanh
        };

        var nn = new NeuralNetwork(
            inputNodes: 64,
            hiddenLayers: new List<int> { 128, 256, 64, 32 },
            outputNodes: 1,
            activations: activations,
            learningRate: 0.0003
        );

        var dataset = LoadPreparedData(AllPreparedData, 200000);

        var dataset1 = LoadPreparedData(AllPreparedData, 20000, 200000);

        GD.Print($"Training on data: {dataset.Count}");
        TrainFromPrepared(dataset, nn, epochs, batchSize);

        GD.Print($"Testing on data: {dataset1.Count}");
        TestPrediction(dataset1, nn);

        GD.Print($"Saving");
        nn.SaveToJson(SaveFolderPath + "/" + SaveModelPath + ".json");

        logWriter?.Close();
        GD.Print("Log saved successfully.");



        logPath = SaveFolderPath + "/" + SaveModelPath + "_Mating_LOG.txt";
        logWriter = new StreamWriter(logPath, append: false); // overwrite old file
        logWriter.AutoFlush = true; // immediately write to disk

        activations = new List<ActivationType>{
            ActivationType.ReLU,
            ActivationType.ReLU,
            ActivationType.ReLU,
            ActivationType.Softmax
        };

        nn = new NeuralNetwork(
            inputNodes: 64,
            hiddenLayers: new List<int> { 128, 64, 32 },
            outputNodes: 3,
            activations: activations,
            learningRate: 0.0001
        );

        dataset = LoadClassificationData(MatingPreparedData, 200000);

        dataset1 = LoadClassificationData(MatingPreparedData, 20000, 200000);

        GD.Print($"Training on data: {dataset.Count}");
        TrainClassification(dataset, nn, epochs, batchSize);

        GD.Print($"Testing on data: {dataset1.Count}");
        TestClassification(dataset1, nn);

        GD.Print($"Saving");
        nn.SaveToJson(SaveFolderPath + "/" + SaveModelPath + "_Mating.json");

        logWriter?.Close();
        GD.Print("Log saved successfully.");

        /*
        //GD.Print("Preparing data");
        //PrepareDataAndSave();

        //var activations = new List<ActivationType>{
        //    ActivationType.ReLU,
        //    ActivationType.ReLU,
        //    ActivationType.ReLU,
        //    ActivationType.ReLU,
        //    ActivationType.ReLU,
        //    ActivationType.ReLU,
        //    ActivationType.Tanh
        //};

        //var nn = new NeuralNetwork(
        //    inputNodes: 64,
        //    hiddenLayers: new List<int> { 128, 128, 64, 64, 32, 16 },
        //    outputNodes: 1,
        //    activations: activations,
        //    learningRate: 0.0003
        //);

        ////var nn = new NeuralNetwork(SaveFolderPath + "/" + SaveModelPath + ".json");


        //var dataset = LoadPreparedData(AllPreparedData, 200000);
        ////var dataset = LoadPreparedData(AllPreparedData, 200000);

        //var dataset1 = LoadPreparedData(AllPreparedData, 20000, 200000);
        ////var dataset1 = LoadPreparedData(AllPreparedData, 20000, 200000);

        ////GD.Print($"Mating Data size: {CountData(MatingPreparedData)}");

        //GD.Print($"Training on data: {dataset.Count}");
        //TrainFromPrepared(dataset, nn, epochs, batchSize);

        //GD.Print($"Testing on data: {dataset1.Count}");
        //TestPrediction(dataset1, nn);

        //GD.Print($"Saving");
        //nn.SaveToJson(SaveFolderPath + "/" + SaveModelPath + ".json");

        //logWriter?.Close();
        //GD.Print("Log saved successfully.");




        //logPath = SaveFolderPath + "/" + SaveModelPath + "2LOG.txt";
        //logWriter = new StreamWriter(logPath, append: false); // overwrite old file
        //logWriter.AutoFlush = true; // immediately write to disk

        //activations = new List<ActivationType>{
        //    ActivationType.ReLU,
        //    ActivationType.ReLU,
        //    ActivationType.ReLU,
        //    ActivationType.ReLU,
        //    ActivationType.ReLU,
        //    ActivationType.Tanh
        //};

        //nn = new NeuralNetwork(
        //    inputNodes: 64,
        //    hiddenLayers: new List<int> { 256, 128, 16, 64, 32 },
        //    outputNodes: 1,
        //    activations: activations,
        //    learningRate: 0.0003
        //);

        //GD.Print($"Training on data: {dataset.Count}");
        //TrainFromPrepared(dataset, nn, epochs, batchSize);

        //GD.Print($"Testing on data: {dataset1.Count}");
        //TestPrediction(dataset1, nn);

        //GD.Print($"Saving");
        //nn.SaveToJson(SaveFolderPath + "/" + SaveModelPath + "2.json");

        //logWriter?.Close();
        //GD.Print("Log saved successfully.");



        //logPath = SaveFolderPath + "/" + SaveModelPath + "3LOG.txt";
        //logWriter = new StreamWriter(logPath, append: false); // overwrite old file
        //logWriter.AutoFlush = true; // immediately write to disk

        //activations = new List<ActivationType>{
        //    ActivationType.ReLU,
        //    ActivationType.ReLU,
        //    ActivationType.Tanh
        //};

        //nn = new NeuralNetwork(
        //    inputNodes: 64,
        //    hiddenLayers: new List<int> { 512, 128 },
        //    outputNodes: 1,
        //    activations: activations,
        //    learningRate: 0.0003
        //);

        //GD.Print($"Training on data: {dataset.Count}");
        //TrainFromPrepared(dataset, nn, epochs, batchSize);

        //GD.Print($"Testing on data: {dataset1.Count}");
        //TestPrediction(dataset1, nn);

        //GD.Print($"Saving");
        //nn.SaveToJson(SaveFolderPath + "/" + SaveModelPath + "3.json");

        //logWriter?.Close();
        //GD.Print("Log saved successfully.");

        */

        GD.Print("Done!\n\n");
    }


}
