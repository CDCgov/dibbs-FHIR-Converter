// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Fluid;

namespace Microsoft.Health.Fhir.Liquid.Converter.DotLiquids
{
    public interface IFhirConverterTemplateFileSystem
    {
        public IFluidTemplate GetTemplate(string templateName, string rootTemplatePath = "");
    }
}
