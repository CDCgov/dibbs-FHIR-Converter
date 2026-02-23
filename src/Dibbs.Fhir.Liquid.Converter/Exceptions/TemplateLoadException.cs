// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Dibbs.Fhir.Liquid.Converter.Models;

namespace Dibbs.Fhir.Liquid.Converter.Exceptions
{
    public class TemplateLoadException : FhirConverterException
    {
        public TemplateLoadException(FhirConverterErrorCode fhirConverterErrorCode, string message)
            : base(fhirConverterErrorCode, message)
        {
        }

        public TemplateLoadException(FhirConverterErrorCode fhirConverterErrorCode, string message, Exception innerException)
            : base(fhirConverterErrorCode, message, innerException)
        {
        }
    }
}
