// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;

namespace Microsoft.Health.Fhir.Liquid.Converter.FunctionalTests
{
    public static class Constants
    {
        public static readonly string TemplateDirectory = Path.Join("..", "..", "data", "Templates");
        public static readonly string SampleDataDirectory = Path.Join("..", "..", "data", "SampleData");
        // Roundabout path to make sure we reference/update the source expected test data and not a copy
        // in the build folder
        public static readonly string ExpectedDataFolder = Path.Join("..", "..", "src", "Microsoft.Health.Fhir.Liquid.Converter.FunctionalTests", "TestData", "Expected");
    }
}
