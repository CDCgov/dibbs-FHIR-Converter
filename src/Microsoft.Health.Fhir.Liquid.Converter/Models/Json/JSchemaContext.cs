// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using Fluid;
using NJsonSchema;

namespace Microsoft.Health.Fhir.Liquid.Converter.Models.Json
{
    public class JSchemaContext : TemplateContext
    {
        public JSchemaContext(object data, TemplateOptions options)
             : base(data, options)
        {
            ValidateSchemas = new List<JsonSchema>();
        }

        public List<JsonSchema> ValidateSchemas { get; set; }
    }
}
