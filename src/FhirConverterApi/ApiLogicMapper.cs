// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Liquid.Converter;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Models.Hl7v2;
using Microsoft.Health.Fhir.Liquid.Converter.Processors;
using Microsoft.Health.Fhir.Liquid.Converter.Tool.Models;
using FhirConverterApi.Models;
using Newtonsoft.Json;

namespace FhirConverterApi
{
    public static class ApiLogicMapper
    {
        private static string TemplateDirectory = "/app/template_data/Templates/Hl7v2";
        private const string MetadataFileName = "metadata.json";
        private static readonly List<string> CcdaExtensions = new List<string> { ".ccda", ".xml" };
        private static readonly ProcessorSettings DefaultProcessorSettings = new ProcessorSettings();

        public static string Convert(FhirApiModel options)
        {
            if (!IsValidOptions(options))
            {
                throw new InputParameterException("Invalid/missing parameters set in api request.");
            }
            string resultString = "One of the required parameter is missing/empty";
            var dataType = GetDataTypes(TemplateDirectory);
            var dataProcessor = CreateDataProcessor(dataType);
            var templateProvider = CreateTemplateProvider(dataType, TemplateDirectory);
            DefaultProcessorSettings.EnableTelemetryLogger = options.IsVerboseEnabled;

            if (!string.IsNullOrEmpty(options.inputDataString))
            {
                resultString = ConvertSingleFile(dataProcessor, templateProvider, dataType, options.rootTemplateName, options.inputDataString, options.IsTraceInfo);
            }
            return resultString;
            // SystemTextJson($"Conversion completed!");
        }

        private static string ConvertSingleFile(IFhirConverter dataProcessor, ITemplateProvider templateProvider, DataType dataType, string rootTemplate, string inputContent, bool isTraceInfo)
        {
            var traceInfo = CreateTraceInfo(dataType, isTraceInfo);
            var resultString = dataProcessor.Convert(inputContent, rootTemplate, templateProvider, traceInfo);
            var result = new ConverterResult(ProcessStatus.OK, resultString, traceInfo);
            return resultString;
        }

        // private static void ConvertBatchFiles(IFhirConverter dataProcessor, ITemplateProvider templateProvider, DataType dataType, string rootTemplate, string inputFolder, string outputFolder, bool isTraceInfo)
        // {
        //     var files = GetInputFiles(dataType, inputFolder);
        //     foreach (var file in files)
        //     {
        //         Console.WriteLine($"Processing {Path.GetFullPath(file)}");
        //         var fileContent = File.ReadAllText(file);
        //         var outputFileDirectory = Path.Join(outputFolder, Path.GetRelativePath(inputFolder, Path.GetDirectoryName(file)));
        //         var outputFilePath = Path.Join(outputFileDirectory, Path.GetFileNameWithoutExtension(file) + ".json");
        //         ConvertSingleFile(dataProcessor, templateProvider, dataType, rootTemplate, fileContent, outputFilePath, isTraceInfo);
        //     }
        // }

        private static DataType GetDataTypes(string templateDirectory)
        {
            if (!Directory.Exists(templateDirectory))
            {
                throw new DirectoryNotFoundException($"Could not find template directory: {templateDirectory}");
            }

            var metadataPath = Path.Join(templateDirectory, MetadataFileName);
            if (!File.Exists(metadataPath))
            {
                throw new FileNotFoundException($"Could not find metadata.json in template directory: {templateDirectory}.");
            }

            var content = File.ReadAllText(metadataPath);
            var metadata = JsonConvert.DeserializeObject<Metadata>(content);
            if (Enum.TryParse<DataType>(metadata?.Type, ignoreCase: true, out var type))
            {
                return type;
            }

            throw new NotImplementedException($"The conversion from data type '{metadata?.Type}' to FHIR is not supported");
        }

        private static IFhirConverter CreateDataProcessor(DataType dataType)
        {
            return dataType switch
            {
                DataType.Hl7v2 => new Hl7v2Processor(DefaultProcessorSettings, ConsoleLoggerFactory.CreateLogger<Hl7v2Processor>()),
                DataType.Ccda => new CcdaProcessor(DefaultProcessorSettings, ConsoleLoggerFactory.CreateLogger<CcdaProcessor>()),
                DataType.Json => new JsonProcessor(DefaultProcessorSettings, ConsoleLoggerFactory.CreateLogger<JsonProcessor>()),
                DataType.Fhir => new FhirProcessor(DefaultProcessorSettings, ConsoleLoggerFactory.CreateLogger<FhirProcessor>()),
                _ => throw new NotImplementedException($"The conversion from data type {dataType} to FHIR is not supported")
            };
        }

        private static ITemplateProvider CreateTemplateProvider(DataType dataType, string templateDirectory)
        {
            return new TemplateProvider(templateDirectory, dataType);
        }

        private static TraceInfo CreateTraceInfo(DataType dataType, bool isTraceInfo)
        {
            return isTraceInfo ? (dataType == DataType.Hl7v2 ? new Hl7v2TraceInfo() : new TraceInfo()) : null;
        }

        private static List<string> GetInputFiles(DataType dataType, string inputDataFolder)
        {
            return dataType switch
            {
                DataType.Hl7v2 => Directory.EnumerateFiles(inputDataFolder, "*.hl7", SearchOption.AllDirectories).ToList(),
                DataType.Ccda => Directory.EnumerateFiles(inputDataFolder, "*.*", SearchOption.AllDirectories)
                    .Where(x => CcdaExtensions.Contains(Path.GetExtension(x).ToLower())).ToList(),
                DataType.Json => Directory.EnumerateFiles(inputDataFolder, "*.json", SearchOption.AllDirectories).ToList(),
                DataType.Fhir => Directory.EnumerateFiles(inputDataFolder, "*.json", SearchOption.AllDirectories).ToList(),
                _ => new List<string>(),
            };
        }

        private static void SaveConverterResult(string outputFilePath, ConverterResult result)
        {
            var outputFileDirectory = Path.GetDirectoryName(outputFilePath);
            Directory.CreateDirectory(outputFileDirectory);

            var content = JsonConvert.SerializeObject(result, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            File.WriteAllText(outputFilePath, content);
        }

        private static bool IsValidOptions(FhirApiModel options)
        {
            var contentToFile = !string.IsNullOrEmpty(options.inputDataString) &&
                                !string.IsNullOrEmpty(options.rootTemplateName) &&
                                !string.IsNullOrEmpty(options.inputDataFormat);

            return contentToFile;
        }

        private static bool IsSameFile(string inputFile, string outputFile)
        {
            return string.Equals(inputFile, outputFile, StringComparison.InvariantCultureIgnoreCase);
        }

        private static bool IsSameDirectory(string inputFolder, string outputFolder)
        {
            string inputFolderPath = Path.GetFullPath(inputFolder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string outputFolderPath = Path.GetFullPath(outputFolder)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return string.Equals(inputFolderPath, outputFolderPath, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
