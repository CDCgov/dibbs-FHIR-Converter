// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Dibbs.Fhir.Liquid.Converter.FileSystems;
using Fluid;

namespace Dibbs.Fhir.Liquid.Converter
{
    public class TemplateProvider : ITemplateProvider
    {
        private readonly IFhirConverterTemplateFileSystem _fileSystem;
        private readonly bool _isDefaultTemplateProvider = false;

        public TemplateProvider(string templateDirectory)
        {
            _fileSystem = new TemplateLocalFileSystem(templateDirectory);
        }

        // TODO: We don't use MemoryFileSystem in our implementation but I'm keeping support for it for now in case it is helpful in our testing
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