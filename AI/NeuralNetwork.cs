//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace ChessBot.AI
//{
//    public class NeuralNetwork
//    {
//        private int inputNodes;
//        private int hiddenNodes;
//        private int outputNodes;

//        private Matrix weightsInputHidden;
//        private Matrix weightsHiddenOutput;

//        private Matrix biasHidden;
//        private Matrix biasOutput;

//        private double learningRate = 0.1;

//        public NeuralNetwork(int inputNodes, int hiddenNodes, int outputNodes)
//        {
//            this.inputNodes = inputNodes;
//            this.hiddenNodes = hiddenNodes;
//            this.outputNodes = outputNodes;

//            weightsInputHidden = new Matrix(hiddenNodes, inputNodes);
//            weightsHiddenOutput = new Matrix(outputNodes, hiddenNodes);
//            biasHidden = new Matrix(hiddenNodes, 1);
//            biasOutput = new Matrix(outputNodes, 1);

//            weightsInputHidden.Randomize();
//            weightsHiddenOutput.Randomize();
//            biasHidden.Randomize();
//            biasOutput.Randomize();
//        }
        
//        //forward propagation
//        public double[] Predict(double[] inputs)
//        {
//            Matrix inputMatrix = Matrix.ConvertArrayToMatrix(inputs);

//            // Input to Hidden
//            Matrix hidden = Matrix.Multiply(weightsInputHidden, inputMatrix);
//            hidden.Add(biasHidden);
//            hidden.ApplyFunction(ActivationFunctions.Sigmoid);

//            // Hiiden to Output
//            Matrix output = Matrix.Multiply(weightsHiddenOutput, hidden);
//            output.Add(biasOutput);
//            output.ApplyFunction(ActivationFunctions.Sigmoid);

//            return Matrix.ConvertMatrixToArray(output);
//        }

//        // Training woth backpropagation
//        public void Train(double[] inputsArray, double[] targetArray)
//        {
//            Matrix inputs = Matrix.ConvertArrayToMatrix(inputsArray);
//            Matrix targets = Matrix.ConvertArrayToMatrix(targetArray);

//            //Forward Propagation
//            Matrix hidden = Matrix.Multiply(weightsInputHidden, inputs);
//            hidden.Add(biasHidden);
//            hidden.ApplyFunction(ActivationFunctions.Sigmoid);

//            Matrix outputs = Matrix.Multiply(weightsHiddenOutput, hidden);
//            outputs.Add(biasOutput);
//            outputs.ApplyFunction(ActivationFunctions.Sigmoid);

//            //Calculate output error
//            Matrix outputErrors = Matrix.Substract(targets, outputs);

//            //Backpropagation for hidden to output weights
//            Matrix outputGradients = Matrix.ApplyFunction(outputs, ActivationFunctions.SigmoidDerivative);
//            outputGradients.Multiply(outputErrors);
//            outputGradients.Multiply(learningRate);

//            Matrix hiddenTransposed = Matrix.Transpose(hidden);
//            Matrix weightsHioddenOutputDeltas = Matrix.Multiply(outputGradients, hiddenTransposed);

//            weightsHiddenOutput.Add(weightsHioddenOutputDeltas);
//            biasOutput.Add(outputGradients);

//            //Calculate hidden layer errors
//            Matrix weightsHiddenOutputTransposed = Matrix.Transpose(weightsHiddenOutput);
//            Matrix hiddenErrors = Matrix.Multiply(weightsHiddenOutputTransposed, outputErrors);

//            // Backpropagation for input to hiddden weights
//            Matrix hiddenGradients = Matrix.ApplyFunction(hidden, ActivationFunctions.SigmoidDerivative);
//            hiddenGradients.Multiply(hiddenErrors);
//            hiddenGradients.Multiply(learningRate);

//            Matrix inputsTransposed = Matrix.Transpose(inputs);
//            Matrix weightsInputHiddenDeltas = Matrix.Multiply(hiddenGradients, inputsTransposed);

//            weightsInputHidden.Add(weightsInputHiddenDeltas);
//            biasHidden.Add(hiddenGradients);
//        }
//    }
//}
