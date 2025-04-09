//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace ChessBotNoAI.AI
//{
//    public class Matrix
//    {
//        public double[,] Data { get; private set; }
//        public int Rows { get; private set; }
//        public int Columns { get; private set; }

//        public Matrix(int rows, int columns)
//        {
//            Rows = rows;
//            Columns = columns;
//            Data = new double[rows, columns];
//        }

//        public void SetMatrix(Matrix matrix)
//        {
//            Rows = matrix.Rows;
//            Columns = matrix.Columns; 
//            Data = matrix.Data;
//        }

//        public void Randomize()
//        {
//            Random rand = new Random();
//            for (int i = 0; i < Rows; i++)
//            {
//                for (int j = 0; j < Columns; j++)
//                {
//                    Data[i, j] = rand.NextDouble() * 2 - 1;
//                }
//            }
//        }

//        public void Add(Matrix other)
//        {
//            if (Rows != other.Rows || Columns != other.Columns)
//                throw new Exception("Matrix dimensions must match for addition!");

//            for (int i = 0; i < Rows; i++)
//            {
//                for (int j = 0; j < Columns; j++)
//                {
//                    Data[i, j] += other.Data[i, j];
//                }
//            }
//        }

//        public static Matrix Substract(Matrix a, Matrix b)
//        {
//            if (a.Rows != b.Rows || a.Columns != b.Columns) throw new Exeption("Matrix dimensions must match for substract!");

//            Matrix result = new Matrix(a.Rows, a.Columns);

//            for(int i = 0; i < a.Rows; i++)
//            {
//                for (int j = 0; j < a.Columns; j++)
//                {
//                    result.Data[i, j] = a.Data[i, j] - b.Data[i, j];
//                }
//            }

//            return result;
//        }

//        public void Multiply(Matrix matrix)
//        {
//            if (Columns != matrix.Columns) throw new Exception("Incompatible matrix dimensions for multiplucations!");

//            SetMatrix(Multiply(this, matrix));
//        }

//        public static Matrix Multiply(Matrix a, Matrix b)
//        {
//            if (a.Columns != b.Columns) throw new Exception("Incompatible matrix dimensions for multiplications!");

//            Matrix result = new Matrix(a.Rows, b.Columns);
//            for (int i = 0; i < a.Rows; i++)
//            {
//                for (int j = 0; j < b.Columns; j++)
//                {
//                    for (int k = 0; k < a.Columns; k++)
//                    {
//                        result.Data[i, j] += a.Data[i, k] * b.Data[k, j];
//                    }
//                }
//            }

//            return result;
//        }

//        public void Multiply(double multiplier)
//        {
//            for(int i  = 0; i < Rows; i++)
//            {
//                for(int j = 0; j < Columns; j++)
//                {
//                    Data[i, j] *= multiplier;
//                }
//            }
//        }

//        public void ApplyFunction(Func<double, double> function)
//        {
//            for (int i = 0; i < Rows; i++)
//            {
//                for (int j = 0; j < Columns; j++)
//                {
//                    Data[i, j] = function(Data[i, j]);
//                }
//            }
//        }

//        public static Matrix ApplyFunction(Matrix matrix, Func<double, double> function)
//        {
//            for(int i = 0; i < matrix.Rows; i++)
//            {
//                for(int j = 0; j < matrix.Columns; j++)
//                {
//                    matrix.Data[i, j] = function(matrix.Data[i, j]);
//                }
//            }
//            return matrix;
//        }

//        public static Matrix Transpose(Matrix matrix)
//        {
//            Matrix result = new Matrix(matrix.Columns, matrix.Rows);

//            for(int j = 0; j < matrix.Columns; j++)
//            {
//                for(int i = 0; i < matrix.Rows; i++)
//                {
//                    result.Data[j, i] = matrix.Data[i, j];
//                }
//            }

//            return result;
//        }

//        public static Matrix ConvertArrayToMatrix(double[] data)
//        {
//            Matrix result = new Matrix(data.Length, 1);

//            for(int i =  0; i < data.Length; i++)
//            {
//                result.Data[i, 0] = data[i];
//            }

//            return result;
//        }

//        public static double[] ConvertMatrixToArray(Matrix matrix)
//        {
//            double[] array = new double[matrix.Rows];
//            for(int i = 0; i < matrix.Rows; i++)
//            {
//                array[i] = matrix.Data[i, 0];
//            }
//            return array;
//        }
//    }
//}
