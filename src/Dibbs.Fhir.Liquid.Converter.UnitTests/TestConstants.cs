// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.IO;

namespace Dibbs.Fhir.Liquid.Converter.UnitTests
{
    public static class TestConstants
    {
        public static readonly string SampleDataDirectory = Path.Join("..", "..", "data", "SampleData");
        public static readonly string TemplateDirectory = Path.Join("..", "..", "data", "Templates");
        public static readonly string ECRTemplateDirectory = Path.Join(TemplateDirectory, "eCR");
        public static readonly string TestTemplateDirectory = "TestData/TestTemplates";
        public static readonly string ExpectedDirectory = "TestData/Expected/";
    }
}
