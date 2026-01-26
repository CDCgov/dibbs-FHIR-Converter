// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

namespace Dibbs.Fhir.Liquid.Converter.Processors
{
    public interface IFhirConverter
    {
        public string Convert(string data, string rootTemplate, string templatesPath, ITemplateProvider templateProvider);
    }
}
