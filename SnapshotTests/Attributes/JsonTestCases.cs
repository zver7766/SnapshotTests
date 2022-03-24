using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Xunit.Sdk;

namespace SnapshotTests.Attributes
{
    // Class inherits from DataAttribute
    public class JsonTestCases : DataAttribute
    {
        public string TestCasesDirectoryPath { get; set; }

        public JsonTestCases()
        {
        }
        
        // Second constructor which allows to specify test case directory path
        public JsonTestCases(string testCasesDirectoryPath)
        {
            TestCasesDirectoryPath = testCasesDirectoryPath;
        }

        // Implementation of DataAttribute methods
        public override IEnumerable<object[]> GetData(MethodInfo currentTestMethodInfo)
        {
            if (currentTestMethodInfo == null)
            {
                throw new ArgumentNullException(nameof(currentTestMethodInfo));
            }
            
            if (string.IsNullOrEmpty(TestCasesDirectoryPath))
            {
                TestCasesDirectoryPath = GetTestCasesDirectoryRelativePath(currentTestMethodInfo);
            }
            
            var testCasesFileNames = GetTestCasesFileNames(TestCasesDirectoryPath);

            return GetAllTestCasesMethodParameters(testCasesFileNames, currentTestMethodInfo);
        }

        // Returns raw json data from specified file names
        private List<object[]> GetAllTestCasesMethodParameters(IEnumerable<string> testCasesFileNames, MethodInfo currentTestMethodInfo)
        {
            var result = new List<object[]>();

            foreach (var testCaseFileName in testCasesFileNames)
            {
                // Reading all data from file into a string
                var testCaseRawJson = File.ReadAllText(testCaseFileName);

                // Getting test method parameter types (e.g. CreateClaim_BadRequest(string request, string expectedResult, string testCaseFilePath)
                // output string, string, string)
                var testParameterTypes = currentTestMethodInfo.GetParameters().Select(x => x.ParameterType).ToArray();

                // Trimming last type because its always must be string type (testCaseFilePath)
                var testParameterTypesWithoutFileLocation = testParameterTypes.TrimLast();
                var testMethodTypedParameters = DeserializeRawJsonToObjectArray(testCaseRawJson, testParameterTypesWithoutFileLocation, testCaseFileName);
                
                result.Add(testMethodTypedParameters);
            }
            return result;
        }
        
        // Gets all file names with specified searchPattern (by default *.json) by Directory methods
        private IEnumerable<string> GetTestCasesFileNames(string directoryPath, string searchPattern = "*.json")
        {
            var fileNames = Directory.GetFiles(directoryPath, searchPattern);

            if (!fileNames.Any())
            {
                throw new ArgumentNullException($"Directory with root {TestCasesDirectoryPath} does not contain any {searchPattern} files");
            }

            return fileNames;
        }

        // In case if TestCasesDirectoryPath property empty by using of reflection getting relative path to the directory
        // by signature of method like Post_CreateClaim_ShouldReturn_BadRequest method and the same folder
        private string GetTestCasesDirectoryRelativePath(MethodInfo currentTestMethodInfo)
        {
            var testAssemblyName = currentTestMethodInfo.DeclaringType.Assembly.FullName.Split(",").First() + ".";
            var testMethodBasePart = currentTestMethodInfo.Name.Split("_When").First();
            
            var directoryPath = currentTestMethodInfo.DeclaringType.Namespace
                .Remove(0, testAssemblyName.Length)
                .Replace('.', '\\') + "\\" + testMethodBasePart;
            
            return directoryPath;
        }

        // Deserializes raw json data to the helper class and returns object array of it
        private object[] DeserializeRawJsonToObjectArray(string jsonData, Type[] testParameterTypes, string fileName)
        {
            // Using # to highlight file name
            var highlightedFileName = "#### " + fileName + " ####";
            // Specify wrapper by specific type
            var contentContainer = typeof(RequestResponseContentContainer<,>).MakeGenericType(testParameterTypes);
            
            dynamic deserializedContentContainer = JsonConvert.DeserializeObject(jsonData, contentContainer);

            var result = new object[] {deserializedContentContainer?.Request, deserializedContentContainer?.Response, highlightedFileName};
            return result;
        }

        // Helper class, like wrapper or container to typed deserialize input and output of file
        private class RequestResponseContentContainer<TRequest, TResponse>
        {
            public TRequest Request { get; set; }
            
            public TResponse Response { get; set; }
        }
        
    }

    public static class TypeExtensions
    {
        // Trimming last type
        public static Type[] TrimLast(this Type[] types)
        {
            return types.Take(types.Length - 1).ToArray();
        }
    }
    
}