// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using DotLiquid.FileSystems;
using Fluid;

namespace Microsoft.Health.Fhir.Liquid.Converter
{
    public interface ITemplateProvider
    {
        public bool IsDefaultTemplateProvider { get; }

        public IFluidTemplate GetTemplate(string templateName);

        public ITemplateFileSystem GetTemplateFileSystem();
    }
}
