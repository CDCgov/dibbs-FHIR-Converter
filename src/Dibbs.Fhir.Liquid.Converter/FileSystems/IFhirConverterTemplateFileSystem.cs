// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Fluid;

namespace Dibbs.Fhir.Liquid.Converter.FileSystems
{
    public interface IFhirConverterTemplateFileSystem
    {
        public IFluidTemplate GetTemplate(string templateName, string rootTemplatePath = "");
    }
}