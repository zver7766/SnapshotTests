using System;
using CalculatorSample;
using Shouldly;
using SnapshotTests.Attributes;
using Xunit;

namespace SnapshotTests.Tests.CalculatorTests
{
    public class CalculatorTests
    {
        [Theory]
        [JsonTestCases]
        public void AddOperation_ShouldReturn_ValidResult(Tuple<double, double> input, double expectedResult, string testCaseFilePath)
        {
            var fistOperand = input.Item1;
            var secondOperand = input.Item2;

            var result = Calculactor.Add(fistOperand, secondOperand);
            
            result.ShouldBe(expectedResult, testCaseFilePath);
        }
    }
}