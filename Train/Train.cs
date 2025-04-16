using ChessBot.AI;
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public partial class Train : Node2D
{
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

    public static void TrainFromCsv(string path, NeuralNetwork nn)
    {
        string[] lines = File.ReadAllLines(path);

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] split = line.Split(',');

            string fen = split[0];
            double target = double.Parse(split[1], System.Globalization.CultureInfo.InvariantCulture);

            double[] input = ParseFenToInput(fen);
            double[] targetArr = new double[] { target / 100.0 }; // Normalize if needed

            nn.Train(input, targetArr);
        }
    }

    public override void _Ready()
    {
        var hidden = new List<int> { 64, 32 };
        var nn = new NeuralNetwork(64, hidden, 1);

        TrainFromCsv("C:/Univ/MastersWork/chessData.csv", nn);

        nn.SaveWeights("trained_weights.txt");
    }
}
