using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace ChessBot.AI
{
    public enum ActivationType
    {
        Sigmoid,
        Tanh,
        ReLU,
        Softmax,
        Linear
    }

    public static class ActivationFunctions
    {
        public static double Sigmoid(double x) => 1 / (1 + Math.Exp(-x));

        public static double SigmoidDerivative(double x) => x * (1 - x);

        public static double Tanh(double x) => Math.Tanh(x);

        public static double TanhDerivative(double x) => 1 - x * x;

        public static double ReLU(double x) => Math.Max(0, x);

        public static double ReLUDerivative(double x) => x > 0 ? 1 : 0;

        public static double Linear(double x) => x;

        public static double LinearDerivative(double x) => 1;

        public static Matrix Softmax(Matrix input)
        {
            if (input.Columns != 1)
                throw new ArgumentException("Softmax expects a column‐vector");
            double max = Double.NegativeInfinity;
            for (int i = 0; i < input.Rows; i++)
                max = Math.Max(max, input[i, 0]);

            double sum = 0;
            for (int i = 0; i < input.Rows; i++)
                sum += Math.Exp(input[i, 0] - max);

            var result = new Matrix(input.Rows, 1);
            for (int i = 0; i < input.Rows; i++)
                result[i, 0] = Math.Exp(input[i, 0] - max) / sum;
            return result;
        }

    }
}
