// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Dibbs.Fhir.Liquid.Converter.Models;
using Xunit;
using Xunit.Abstractions;

namespace Dibbs.Fhir.Liquid.Converter.FunctionalTests
{
    public class RuleBasedTemplateDirectoryProviderTests : BaseRuleBasedFunctionalTests
    {
        private static readonly string _ccdaTemplateFolder = Path.Combine(Constants.TemplateDirectory, "eCR");

        private static readonly ITemplateProvider _ccdaTemplateProvider = new TemplateProvider(_ccdaTemplateFolder);

        private readonly ITestOutputHelper _output;

        public RuleBasedTemplateDirectoryProviderTests(ITestOutputHelper output)
            : base(output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(GetCcdaRuleBasedTestCases))]
        public async Task GivenDataAndTemplateDirectoryProvider_WhenConvertDataCalled_ThenValidateOnePatient(string templateName, string samplePath, string rootTemplate)
        {
            await ConvertAndValidatePatientCount(_ccdaTemplateProvider, templateName, samplePath, rootTemplate);
        }

        [Theory]
        [MemberData(nameof(GetCcdaRuleBasedTestCases))]
        public async Task GivenCCDAData_WhenConvertData_ThenValidateReferenceResourceId(string templateName, string samplePath, string rootTemplate)
        {
            await ConvertAndValidateReferenceResourceId(_ccdaTemplateProvider, templateName, samplePath, rootTemplate);
        }

        [Theory]
        [MemberData(nameof(GetCcdaRuleBasedTestCases))]
        public async Task GivenDataAndTemplateDirectoryProvider_WhenConvertDataCalled_ThenValidateNonemptyResource(string templateName, string samplePath, string rootTemplate)
        {
            await ConvertAndValidateNonemptyResource(_ccdaTemplateProvider, templateName, samplePath, rootTemplate);
        }

        // We have a previously existing bug that can cause identical Practitioner and PractitionerRole IDs.
        // Once that is addressed we can uncomment this test.
        // [Theory]
        // [MemberData(nameof(GetCcdaRuleBasedTestCases))]
        // public async Task GivenDataAndTemplateDirectoryProvider_WhenConvertDataCalled_ThenValidateNonidenticalResources(string templateName, string samplePath, string rootTemplate)
        // {
        //     await ConvertAndValidateNonidenticalResources(_ccdaTemplateProvider, templateName, samplePath, rootTemplate);
        // }

        [Fact]
        public async Task GivenDataAndTemplateDirectoryProvider_WhenConvertDataCalled_ThenValidateParserFunctionality()
        {
            await ConvertAndValidateParserFunctionality();
        }

        [Theory]
        [MemberData(nameof(GetCcdaRuleBasedTestCases))]
        public async Task GivenDataAndTemplateDirectoryProvider_WhenConvertDataCalled_ThenValidatePassFhirParser(string templateName, string samplePath, string rootTemplate)
        {
            await ConvertAndValidatePassFhirParser(_ccdaTemplateProvider, templateName, samplePath, rootTemplate);
        }
    }
}