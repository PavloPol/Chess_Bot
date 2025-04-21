using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace ChessBot.AI
{
    public class Matrix
    {
        public double[,] Data { get; private set; }
        public int Rows { get; private set; }
        public int Columns { get; private set; }

        public double this[int row, int col]
        {
            get => Data[row, col];
            set => Data[row, col] = value;
        }

        public Matrix(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
            Data = new double[rows, columns];
        }

        public void SetMatrix(Matrix matrix)
        {
            Rows = matrix.Rows;
            Columns = matrix.Columns;
            Data = matrix.Data;
        }

        public void Randomize(int fanIn = -1)
        {
            Random rand = new Random();
            double scale = fanIn > 0 ? Math.Sqrt(1.0 / fanIn) : 1.0;

            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                    Data[i, j] = rand.NextDouble() * 2 * scale - scale;
        }

        public void Add(Matrix other)
        {
            if (Rows != other.Rows || Columns != other.Columns)
                GD.PrintErr("Matrix dimensions must match for addition!");

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    Data[i, j] += other.Data[i, j];
                }
            }
        }

        public static Matrix Substract(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Columns != b.Columns) GD.PrintErr("Matrix dimensions must match for substract!");

            Matrix result = new Matrix(a.Rows, a.Columns);

            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < a.Columns; j++)
                {
                    result.Data[i, j] = a.Data[i, j] - b.Data[i, j];
                }
            }

            return result;
        }

        public void Multiply(Matrix matrix)
        {
            if (Columns != matrix.Rows) GD.PrintErr("Incompatible matrix dimensions for multiplucations!");

            SetMatrix(Multiply(this, matrix));
        }

        public static Matrix Multiply(Matrix a, Matrix b)
        {
            if (a.Columns != b.Rows)
            {
                GD.PrintErr($"Matrix dimensions mismatch: A is {a.Rows}x{a.Columns}, B is {b.Rows}x{b.Columns}");
                throw new Exception("Matrix A columns must match Matrix B rows!");
            }

            Matrix result = new Matrix(a.Rows, b.Columns);
            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < b.Columns; j++)
                {
                    for (int k = 0; k < a.Columns; k++)
                    {
                        result.Data[i, j] += a.Data[i, k] * b.Data[k, j];
                    }
                }
            }

            //for (int i = 0; i < result.Rows; i++)
            //    for (int j = 0; j < result.Columns; j++)
            //        if (double.IsNaN(result[i, j]) || double.IsInfinity(result[i, j]))
            //            GD.PrintErr($"Matrix.Multiply -> NaN at ({i},{j}) = {result[i, j]}");

            return result;
        }

        public void Multiply(double multiplier)
        {
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    Data[i, j] *= multiplier;
                }
            }
        }

        public static Matrix Multiply(Matrix a, double scalar)
        {
            var result = new Matrix(a.Rows, a.Columns);
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Columns; j++)
                    result[i, j] = a[i, j] * scalar;

            //for (int i = 0; i < result.Rows; i++)
            //    for (int j = 0; j < result.Columns; j++)
            //        if (double.IsNaN(result[i, j]) || double.IsInfinity(result[i, j]))
            //            GD.PrintErr($"Matrix.Multiply scalar -> NaN at ({i},{j}) = {result[i, j]}");

            return result;
        }

        public void Hadamard(Matrix other)
        {
            if (Rows != other.Rows || Columns != other.Columns)
                GD.PrintErr("Matrix dimensions must match for Hadamard product!");

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    Data[i, j] *= other.Data[i, j];
                }
            }
        }


        public void ApplyFunction(Func<double, double> function)
        {
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    Data[i, j] = function(Data[i, j]);
                }
            }
        }

        public static Matrix ApplyFunction(Matrix matrix, Func<double, double> function)
        {
            Matrix result = new Matrix(matrix.Rows, matrix.Columns);
            for (int i = 0; i < matrix.Rows; i++)
                for (int j = 0; j < matrix.Columns; j++)
                    result.Data[i, j] = function(matrix.Data[i, j]);
            return result;
        }

        public static Matrix Transpose(Matrix matrix)
        {
            Matrix result = new Matrix(matrix.Columns, matrix.Rows);

            for (int j = 0; j < matrix.Columns; j++)
            {
                for (int i = 0; i < matrix.Rows; i++)
                {
                    result.Data[j, i] = matrix.Data[i, j];
                }
            }

            return result;
        }

        public static Matrix ConvertArrayToMatrix(double[] data)
        {
            Matrix result = new Matrix(data.Length, 1);

            for (int i = 0; i < data.Length; i++)
            {
                result.Data[i, 0] = data[i];
            }

            return result;
        }

        public static double[] ConvertMatrixToArray(Matrix matrix)
        {
            double[] array = new double[matrix.Rows];
            for (int i = 0; i < matrix.Rows; i++)
            {
                array[i] = matrix.Data[i, 0];
            }
            return array;
        }

        public static Matrix DivideElementWise(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Columns != b.Columns)
                GD.PrintErr("Matrix dimensions must match for element-wise division!");

            Matrix result = new Matrix(a.Rows, a.Columns);
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Columns; j++)
                    result[i, j] = a[i, j] / b[i, j];
            return result;
        }

        public void Substract(Matrix other)
        {
            if (Rows != other.Rows || Columns != other.Columns)
                GD.PrintErr("Matrix dimensions must match for subtraction!");

            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                    Data[i, j] -= other[i, j];
        }

    }
}
