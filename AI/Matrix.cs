using System;
using System.Collections.Generic;
using Godot;
using static Godot.OpenXRInterface;

namespace ChessBot.AI
{
    public class Matrix
    {
        private static readonly Random _rand = new Random();    // ← reuse one Random

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

        public Matrix Copy()  // ← deep‐copy helper for back‑prop
        {
            var m = new Matrix(Rows, Columns);
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                    m.Data[i, j] = Data[i, j];
            return m;
        }

        public void Randomize(int fanIn = -1)
        {
            double scale = fanIn > 0 ? Math.Sqrt(1.0 / fanIn) : 1.0;
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                    Data[i, j] = _rand.NextDouble() * 2 * scale - scale;
        }

        public Matrix Add(Matrix other)
        {
            if (Rows != other.Rows || Columns != other.Columns)
                GD.PrintErr("Matrix dimensions must match for addition!");
            
            var result = new Matrix(this.Rows, this.Columns);
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                    result.Data[i, j] = this.Data[i, j] + other.Data[i, j];
            return result;
        }

        // Static add returning new matrix
        public static Matrix Add(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Columns != b.Columns) GD.PrintErr("Dimensions must match for Add");
            var result = new Matrix(a.Rows, a.Columns);
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Columns; j++)
                    result.Data[i, j] = a.Data[i, j] + b.Data[i, j];
            return result;
        }

        // Instance in-place subtract
        public Matrix Subtract(Matrix other)
        {
            if (Rows != other.Rows || Columns != other.Columns)
                GD.PrintErr("Matrix dimensions must match for subtraction!");
            var result = new Matrix(this.Rows, this.Columns);
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                    result.Data[i, j] = this.Data[i, j] - other.Data[i, j];
            return result;
        }

        // Static subtract returning new matrix
        public static Matrix Subtract(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Columns != b.Columns) GD.PrintErr("Dimensions must match for Subtract");
            var result = new Matrix(a.Rows, a.Columns);
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Columns; j++)
                    result.Data[i, j] = a.Data[i, j] - b.Data[i, j];
            return result;
        }

        // Instance chainable Hadamard
        public Matrix Hadamard(Matrix other)
        {
            if (Rows != other.Rows || Columns != other.Columns)
                GD.PrintErr("Matrix dimensions must match for Hadamard product!");
            var result = new Matrix(Rows, Columns);
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                    result.Data[i, j] = Data[i, j] * other.Data[i, j];
            return result;
        }

        public static Matrix Hadamard(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Columns != b.Columns) GD.PrintErr("Dimensions must match for Hadamard");
            var result = new Matrix(a.Rows, a.Columns);
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Columns; j++)
                    result.Data[i, j] = a.Data[i, j] * b.Data[i, j];
            return result;
        }

        public Matrix Multiply(double scalar)
        {
            var result = new Matrix(this.Rows, this.Columns);
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                    result.Data[i, j] = this.Data[i, j] * scalar;
            return result;
        }

        public static Matrix Multiply(Matrix a, double scalar)
        {
            var result = new Matrix(a.Rows, a.Columns);
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Columns; j++)
                    result.Data[i, j] = a.Data[i, j] * scalar;
            return result;
        }

        public Matrix Multiply(Matrix other)
        {
            var r = Multiply(this, other);
            return r;
        }

        public static Matrix Multiply(Matrix a, Matrix b)
        {
            if (a.Columns != b.Rows) GD.PrintErr("Incompatible dimensions for Multiply!");
            var result = new Matrix(a.Rows, b.Columns);
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < b.Columns; j++)
                    for (int k = 0; k < a.Columns; k++)
                        result.Data[i, j] += a.Data[i, k] * b.Data[k, j];
            return result;
        }

        public static Matrix Transpose(Matrix a)
        {
            var result = new Matrix(a.Columns, a.Rows);
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Columns; j++)
                    result.Data[j, i] = a.Data[i, j];
            return result;
        }

        public Matrix ApplyFunction(Func<double, double> f)
        {
            var result = new Matrix(Rows, Columns);
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                    result.Data[i, j] = f(Data[i, j]);
            return result;
        }

        public static Matrix ApplyFunction(Matrix a, Func<double, double> f)
        {
            var result = new Matrix(a.Rows, a.Columns);
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Columns; j++)
                    result.Data[i, j] = f(a.Data[i, j]);
            return result;
        }

        public static Matrix DivideElementWise(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Columns != b.Columns) GD.PrintErr("Dimensions must match for division!");
            var result = new Matrix(a.Rows, a.Columns);
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Columns; j++)
                    result.Data[i, j] = a.Data[i, j] / b.Data[i, j];
            return result;
        }

        public static Matrix ConvertArrayToMatrix(double[] arr)
        {
            var m = new Matrix(arr.Length, 1);
            for (int i = 0; i < arr.Length; i++) m.Data[i, 0] = arr[i];
            return m;
        }

        public static double[] ConvertMatrixToArray(Matrix m)
        {
            var arr = new double[m.Rows];
            for (int i = 0; i < m.Rows; i++) arr[i] = m.Data[i, 0];
            return arr;
        }

        public static List<List<double>> ToList(Matrix m)
        {
            var lst = new List<List<double>>();
            for (int i = 0; i < m.Rows; i++)
            {
                var row = new List<double>();
                for (int j = 0; j < m.Columns; j++) row.Add(m[i, j]);
                lst.Add(row);
            }
            return lst;
        }

        // Tile this matrix rowFactor times vertically and colFactor times horizontally
        public Matrix Repeat(int rowFactor, int colFactor)
        {
            var result = new Matrix(Rows * rowFactor, Columns * colFactor);
            for (int i = 0; i < rowFactor; i++)
                for (int j = 0; j < colFactor; j++)
                    for (int r = 0; r < Rows; r++)
                        for (int c = 0; c < Columns; c++)
                            result.Data[i * Rows + r, j * Columns + c] = Data[r, c];
            return result;
        }

        // Sum across columns, returning a column vector of shape (Rows x 1)
        public Matrix SumRows()
        {
            var result = new Matrix(Rows, 1);
            for (int i = 0; i < Rows; i++)
            {
                double sum = 0;
                for (int j = 0; j < Columns; j++)
                    sum += Data[i, j];
                result.Data[i, 0] = sum;
            }
            return result;
        }

        // Set an entire column from a column-vector matrix (Rows x 1)
        public void SetColumn(int colIndex, Matrix columnVector)
        {
            if (columnVector.Rows != Rows || columnVector.Columns != 1)
                GD.PrintErr($"Invalid column size: expected {Rows}x1, got {columnVector.Rows}x{columnVector.Columns}");
            for (int i = 0; i < Rows; i++)
                Data[i, colIndex] = columnVector.Data[i, 0];
        }

        // Set an entire row from a row-vector matrix (1 x Columns)
        public void SetRow(int rowIndex, Matrix rowVector)
        {
            if (rowVector.Rows != 1 || rowVector.Columns != Columns)
                GD.PrintErr($"Invalid row size: expected 1x{Columns}, got {rowVector.Rows}x{rowVector.Columns}");
            for (int j = 0; j < Columns; j++)
                Data[rowIndex, j] = rowVector.Data[0, j];
        }

        public Matrix GetColumn(int colIndex)
        {
            if (colIndex < 0 || colIndex >= Columns)
                GD.PrintErr($"Invalid column index: {colIndex}");

            var result = new Matrix(Rows, 1);
            for (int i = 0; i < Rows; i++)
                result.Data[i, 0] = this.Data[i, colIndex];
            return result;
        }
    }
}
