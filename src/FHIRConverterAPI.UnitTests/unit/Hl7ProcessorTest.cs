using Microsoft.Health.Fhir.Liquid.Converter.FHIRConverterAPI.Processors;

public class Hl7ProcessorTest
{
  [Fact]
  public void StandardizeHl7DateTimes_ShouldStandardizeDateTimes_WhenTimezoneIsIncluded()
  {
    var timezoneInput = File.ReadAllText("../../../TestData/FileSingleMessageLongTZ.hl7");
    var timezoneExpectedOutput = "MSH|^~\\&|WIR11.3.2^^|WIR^^||WIRPH^^|20200514010000-0400||VXU^V04"
         + "|2020051411020600|P^|2.4^^|||ER\n"
         + "PID|||3054790^^^^SR^~^^^^PI^||ZTEST^PEDIARIX^^^^^^|HEPB^DTAP^^^^^^"
         + "|20180808|M|||||||||||||||||||||\n"
         + "PD1|||||||||||02^^^^^|Y||||A\n"
         + "NK1|1||BRO^BROTHER^HL70063^^^^^|^^NEW GLARUS^WI^^^^^^^|\n"
         + "PV1||R||||||||||||||||||\n"
         + "RXA|0|999|20180809|20180809|08^HepB pediatric^CVX^90744^HepB pediatric^CPT"
         + "|1.0|||01^^^^^~38193939^WIR immunization id^IMM_ID^^^|||||||||||NA\n";

    Assert.Equal(timezoneExpectedOutput, Hl7Processor.StandardizeHl7DateTimes(timezoneInput));
  }

  [Fact]
  public void StandardizeHl7DateTimes_ShouldShortenDatetimes_WhenTheyAreTooLong()
  {
    var longDateInput = File.ReadAllText("../../../TestData/FileSingleMessageLongDate.hl7");
    var longDateExpectedOutput = "MSH|^~\\&|WIR11.3.2^^|WIR^^||WIRPH^^|20200514010000||VXU^V04"
         + "|2020051411020600|P^|2.4^^|||ER\n"
         + "PID|||3054790^^^^SR^~^^^^PI^||ZTEST^PEDIARIX^^^^^^|HEPB^DTAP^^^^^^"
         + "|20180808000000|M|||||||||||||||||||||\n"
         + "PD1|||||||||||02^^^^^|Y||||A\n"
         + "NK1|1||BRO^BROTHER^HL70063^^^^^|^^NEW GLARUS^WI^^^^^^^|\n"
         + "PV1||R||||||||||||||||||\n"
         + "RXA|0|999|20180809|20180809|08^HepB pediatric^CVX^90744^HepB pediatric^CPT"
         + "|1.0|||01^^^^^~38193939^WIR immunization id^IMM_ID^^^|\n";

    Assert.Equal(longDateExpectedOutput, Hl7Processor.StandardizeHl7DateTimes(longDateInput));
  }

  [Fact]
  public void StandardizeHl7DateTimes_ShouldReturnInputMessage_WhenInputIsValid()
  {
    var invalidSegmentsInput = File.ReadAllText("../../../TestData/FileSingleMessageInvalidSegments.hl7");
    var invalidSegmentsExpectedOutput = "AAA|^~\\&|WIR11.3.2^^|WIR^^||WIRPH^^|2020051401000000||ADT^A31|"
         + "2020051411020600|P^|2.4^^|||ER\n"
         + "BBB|||3054790^^^^SR^~^^^^PI^||ZTEST^PEDIARIX^^^^^^|HEPB^DTAP^^^^^^"
         + "|2018080800000000000|M|||||||||||||||||||||\n"
         + "CCC|||||||||||02^^^^^|Y||||A\n";

    Assert.Equal(invalidSegmentsExpectedOutput, Hl7Processor.StandardizeHl7DateTimes(invalidSegmentsInput));
  }
}