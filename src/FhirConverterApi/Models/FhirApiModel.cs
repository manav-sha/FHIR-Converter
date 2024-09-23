// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using CommandLine;

namespace FhirConverterApi.Models
{
    [Verb("convert", HelpText = "Convert input data to FHIR resources")]
    public class FhirApiModel
    {
        [Option("inputDataString", Required = true, HelpText = "Input data string of Hl7")]
        public string inputDataString { get; set; }

        [Option("rootTemplateName", Required = true, HelpText = "Name of root template")]
        public string rootTemplateName { get; set; }

        [Option("inputDataFormat", Required = true, HelpText = "Input data format type")]
        public string inputDataFormat { get; set; }

        [Option("isTraceInfo", Required = false, HelpText = "Provide trace information in the output")]
        public bool IsTraceInfo { get; set; }

        [Option("Verbose", Required = false, HelpText = "Output detailed processor diagnostics and performance data.")]
        public bool IsVerboseEnabled { get; set; }
    }
}
