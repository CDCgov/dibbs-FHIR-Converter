// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Dibbs.Fhir.Liquid.Converter.FileSystems;
using Fluid;

namespace Dibbs.Fhir.Liquid.Converter
{
    public interface ITemplateProvider
    {
        public bool IsDefaultTemplateProvider { get; }

        public IFluidTemplate GetTemplate(string templateName);

        public IFhirConverterTemplateFileSystem GetTemplateFileSystem();
    }
}