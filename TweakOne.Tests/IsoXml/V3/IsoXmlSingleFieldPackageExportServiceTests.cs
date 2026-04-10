using System;
using TweakOne.IsoXml.V3;
using Xunit;

namespace TweakOne.Tests.IsoXml.V3;

public sealed class IsoXmlSingleFieldPackageExportServiceTests
{
    [Fact]
    public void CreateDocument_WhenMatchingPartfieldExists_KeepsOnlyThatFieldAndItsReferences()
    {
        var service = new IsoXmlSingleFieldPackageExportService();
        var sourceDocument = new IsoXmlTaskDataDocument
        {
            VersionMajor = "4",
            VersionMinor = "3",
            CropTypes =
            {
                new IsoXmlCropType { Id = "CTP1", Designator = "Corn" },
                new IsoXmlCropType { Id = "CTP2", Designator = "Wheat" }
            },
            Customers =
            {
                new IsoXmlCustomer { Id = "CTR1", LastNameOrDesignator = "Ronco" },
                new IsoXmlCustomer { Id = "CTR2", LastNameOrDesignator = "Other" }
            },
            Farms =
            {
                new IsoXmlFarm { Id = "FRM1", Designator = "Main", CustomerIdRef = "CTR1" },
                new IsoXmlFarm { Id = "FRM2", Designator = "Other", CustomerIdRef = "CTR2" }
            },
            Partfields =
            {
                new IsoXmlPartfield { Id = "PFD1", Designator = "Keep me", CustomerIdRef = "CTR1", FarmIdRef = "FRM1", CropTypeIdRef = "CTP1" },
                new IsoXmlPartfield { Id = "PFD2", Designator = "Drop me", CustomerIdRef = "CTR2", FarmIdRef = "FRM2", CropTypeIdRef = "CTP2" }
            }
        };
        sourceDocument.Partfields[0].LineStrings.Add(new IsoXmlLineString
        {
            Type = IsoXmlLineString.GuidancePathType,
            Designator = "AB",
            Points =
            {
                new IsoXmlPoint { Type = 2, North = 45d, East = 7d },
                new IsoXmlPoint { Type = 2, North = 45.0001d, East = 7.0001d }
            }
        });

        var result = service.CreateDocument(sourceDocument, "PFD1", "Keep me");

        var partfield = Assert.Single(result.Partfields);
        Assert.Equal("PFD1", partfield.Id);
        Assert.Single(partfield.LineStrings);
        Assert.Equal("AB", partfield.LineStrings[0].Designator);
        Assert.Collection(result.CropTypes, cropType => Assert.Equal("CTP1", cropType.Id));
        Assert.Collection(result.Farms, farm => Assert.Equal("FRM1", farm.Id));
        Assert.Collection(result.Customers, customer => Assert.Equal("CTR1", customer.Id));
    }

    [Fact]
    public void CreateDocument_WhenPartfieldDoesNotExist_ThrowsInvalidOperationException()
    {
        var service = new IsoXmlSingleFieldPackageExportService();
        var sourceDocument = new IsoXmlTaskDataDocument
        {
            Partfields =
            {
                new IsoXmlPartfield { Id = "PFD1", Designator = "Keep me" }
            }
        };

        Assert.Throws<InvalidOperationException>(() => service.CreateDocument(sourceDocument, "PFD2", "Missing"));
    }
}
