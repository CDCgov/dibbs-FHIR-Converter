// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Fluid;
using Microsoft.Health.Fhir.Liquid.Converter.DotLiquids;
using Microsoft.Health.Fhir.Liquid.Converter.Models;

namespace Microsoft.Health.Fhir.Liquid.Converter
{
    public class TemplateProvider : ITemplateProvider
    {
        private readonly IFhirConverterTemplateFileSystem _fileSystem;
        private readonly bool _isDefaultTemplateProvider = false;

        public TemplateProvider(string templateDirectory, DataType dataType)
        {
            _fileSystem = new TemplateLocalFileSystem(templateDirectory, dataType);
        }

        public TemplateProvider(List<Dictionary<string, IFluidTemplate>> templateCollection, bool isDefaultTemplateProvider = false)
        {
            _fileSystem = new MemoryFileSystem(templateCollection);
            _isDefaultTemplateProvider = isDefaultTemplateProvider;
        }

        public bool IsDefaultTemplateProvider => _isDefaultTemplateProvider;

        public IFluidTemplate GetTemplate(string templateName)
        {
            return _fileSystem.GetTemplate(templateName);
        }

        public IFhirConverterTemplateFileSystem GetTemplateFileSystem()
        {
            return _fileSystem;
        }
    }
}
