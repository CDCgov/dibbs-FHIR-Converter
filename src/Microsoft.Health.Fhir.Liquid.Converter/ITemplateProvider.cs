// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Fluid;
using Microsoft.Health.Fhir.Liquid.Converter.DotLiquids;

namespace Microsoft.Health.Fhir.Liquid.Converter
{
    public interface ITemplateProvider
    {
        public bool IsDefaultTemplateProvider { get; }

        public IFluidTemplate GetTemplate(string templateName);

        public IFhirConverterTemplateFileSystem GetTemplateFileSystem();
    }
}
