using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using TweakOne.Localization;

namespace TweakOne.IsoXml.V3;

/// <summary>
/// Builds and prepares ZIP-first single-field ISOXML exports from an existing package or document.
/// </summary>
public sealed class IsoXmlSingleFieldPackageExportService
{
    private static readonly LocalizedStrings Strings = LocalizedStrings.Instance;

    /// <summary>
    /// Creates a task data document that keeps only the selected partfield and its required references.
    /// </summary>
    public IsoXmlTaskDataDocument CreateDocument(IsoXmlTaskDataDocument sourceDocument, string? preferredPartfieldIdentifier, string? preferredPartfieldDesignator)
    {
        ArgumentNullException.ThrowIfNull(sourceDocument);

        var selectedPartfield = ResolvePartfield(sourceDocument, preferredPartfieldIdentifier, preferredPartfieldDesignator);
        var customerIds = new HashSet<string>(StringComparer.Ordinal);
        var farmIds = new HashSet<string>(StringComparer.Ordinal);
        var cropTypeIds = new HashSet<string>(StringComparer.Ordinal);

        AddIfNotEmpty(customerIds, selectedPartfield.CustomerIdRef);
        AddIfNotEmpty(farmIds, selectedPartfield.FarmIdRef);
        AddIfNotEmpty(cropTypeIds, selectedPartfield.CropTypeIdRef);

        foreach (var farm in sourceDocument.Farms.Where(farm => !string.IsNullOrWhiteSpace(farm.Id) && farmIds.Contains(farm.Id!)))
        {
            AddIfNotEmpty(customerIds, farm.CustomerIdRef);
        }

        var result = new IsoXmlTaskDataDocument
        {
            VersionMajor = sourceDocument.VersionMajor,
            VersionMinor = sourceDocument.VersionMinor,
            ManagementSoftwareManufacturer = sourceDocument.ManagementSoftwareManufacturer,
            ManagementSoftwareVersion = sourceDocument.ManagementSoftwareVersion,
            DataTransferOrigin = sourceDocument.DataTransferOrigin,
            CropTypes = sourceDocument.CropTypes
                .Where(cropType => !string.IsNullOrWhiteSpace(cropType.Id) && cropTypeIds.Contains(cropType.Id!))
                .Select(CloneCropType)
                .ToList(),
            Customers = sourceDocument.Customers
                .Where(customer => !string.IsNullOrWhiteSpace(customer.Id) && customerIds.Contains(customer.Id!))
                .Select(CloneCustomer)
                .ToList(),
            Farms = sourceDocument.Farms
                .Where(farm => !string.IsNullOrWhiteSpace(farm.Id) && farmIds.Contains(farm.Id!))
                .Select(CloneFarm)
                .ToList(),
            Partfields = new List<IsoXmlPartfield> { ClonePartfield(selectedPartfield) },
            AdditionalElements = CloneElements(sourceDocument.AdditionalElements),
            AdditionalAttributes = CloneAttributes(sourceDocument.AdditionalAttributes)
        };

        return result;
    }

    /// <summary>
    /// Rewrites the extracted package so it contains only the selected field and removes stale proprietary sidecar files.
    /// </summary>
    internal void PreparePackage(IsoXmlTaskDataPackage package, string? preferredPartfieldIdentifier, string? preferredPartfieldDesignator)
    {
        ArgumentNullException.ThrowIfNull(package);

        var sourceDocument = IsoXmlTaskDataSerializer.Load(package.TaskDataXmlPath);
        var singleFieldDocument = CreateDocument(sourceDocument, preferredPartfieldIdentifier, preferredPartfieldDesignator);
        IsoXmlTaskDataSerializer.Save(singleFieldDocument, package.TaskDataXmlPath);

        DeleteIfExists(package.LinkListXmlPath);
        DeleteIfExists(package.AgcoPropJsonPath);
    }

    private static IsoXmlPartfield ResolvePartfield(IsoXmlTaskDataDocument sourceDocument, string? preferredPartfieldIdentifier, string? preferredPartfieldDesignator)
    {
        if (!string.IsNullOrWhiteSpace(preferredPartfieldIdentifier))
        {
            var byIdentifier = sourceDocument.Partfields.FirstOrDefault(partfield => string.Equals(partfield.Id, preferredPartfieldIdentifier, StringComparison.Ordinal));
            if (byIdentifier is not null)
            {
                return byIdentifier;
            }
        }

        if (!string.IsNullOrWhiteSpace(preferredPartfieldDesignator))
        {
            var byDesignator = sourceDocument.Partfields.FirstOrDefault(partfield => string.Equals(partfield.Designator, preferredPartfieldDesignator, StringComparison.CurrentCultureIgnoreCase));
            if (byDesignator is not null)
            {
                return byDesignator;
            }
        }

        throw new InvalidOperationException(Strings.SingleFieldExportPartfieldNotFoundError);
    }

    private static IsoXmlCropType CloneCropType(IsoXmlCropType source)
    {
        return new IsoXmlCropType
        {
            Id = source.Id,
            Designator = source.Designator,
            AdditionalAttributes = CloneAttributes(source.AdditionalAttributes),
            AdditionalElements = CloneElements(source.AdditionalElements)
        };
    }

    private static IsoXmlCustomer CloneCustomer(IsoXmlCustomer source)
    {
        return new IsoXmlCustomer
        {
            Id = source.Id,
            LastNameOrDesignator = source.LastNameOrDesignator,
            FirstName = source.FirstName,
            Street = source.Street,
            PoBox = source.PoBox,
            PostalCode = source.PostalCode,
            City = source.City,
            State = source.State,
            Country = source.Country,
            Phone = source.Phone,
            Mobile = source.Mobile,
            Fax = source.Fax,
            Email = source.Email,
            CustomerNumber = source.CustomerNumber,
            AdditionalAttributes = CloneAttributes(source.AdditionalAttributes)
        };
    }

    private static IsoXmlFarm CloneFarm(IsoXmlFarm source)
    {
        return new IsoXmlFarm
        {
            Id = source.Id,
            Designator = source.Designator,
            Street = source.Street,
            PoBox = source.PoBox,
            PostalCode = source.PostalCode,
            City = source.City,
            State = source.State,
            Country = source.Country,
            CustomerIdRef = source.CustomerIdRef,
            AdditionalAttributes = CloneAttributes(source.AdditionalAttributes)
        };
    }

    private static IsoXmlPartfield ClonePartfield(IsoXmlPartfield source)
    {
        return new IsoXmlPartfield
        {
            Id = source.Id,
            Code = source.Code,
            Designator = source.Designator,
            Area = source.Area,
            CustomerIdRef = source.CustomerIdRef,
            FarmIdRef = source.FarmIdRef,
            CropTypeIdRef = source.CropTypeIdRef,
            CropVarietyIdRef = source.CropVarietyIdRef,
            FieldIdRef = source.FieldIdRef,
            Polygons = source.Polygons.Select(ClonePolygon).ToList(),
            LineStrings = source.LineStrings.Select(static line => line.DeepClone()).ToList(),
            GuidanceGroups = source.GuidanceGroups.Select(CloneGuidanceGroup).ToList(),
            Points = source.Points.Select(static point => point.DeepClone()).ToList(),
            AdditionalElements = CloneElements(source.AdditionalElements),
            AdditionalAttributes = CloneAttributes(source.AdditionalAttributes)
        };
    }

    private static IsoXmlPolygon ClonePolygon(IsoXmlPolygon source)
    {
        return new IsoXmlPolygon
        {
            Type = source.Type,
            Designator = source.Designator,
            Area = source.Area,
            Colour = source.Colour,
            LineStrings = source.LineStrings.Select(static line => line.DeepClone()).ToList(),
            AdditionalElements = CloneElements(source.AdditionalElements),
            AdditionalAttributes = CloneAttributes(source.AdditionalAttributes)
        };
    }

    private static IsoXmlGuidanceGroup CloneGuidanceGroup(IsoXmlGuidanceGroup source)
    {
        return new IsoXmlGuidanceGroup
        {
            Id = source.Id,
            Designator = source.Designator,
            GuidancePatterns = source.GuidancePatterns.Select(CloneGuidancePattern).ToList(),
            AdditionalElements = CloneElements(source.AdditionalElements),
            AdditionalAttributes = CloneAttributes(source.AdditionalAttributes)
        };
    }

    private static IsoXmlGuidancePattern CloneGuidancePattern(IsoXmlGuidancePattern source)
    {
        return new IsoXmlGuidancePattern
        {
            Id = source.Id,
            Designator = source.Designator,
            Type = source.Type,
            LineString = source.LineString?.DeepClone(),
            AdditionalElements = CloneElements(source.AdditionalElements),
            AdditionalAttributes = CloneAttributes(source.AdditionalAttributes)
        };
    }

    private static XmlElement[]? CloneElements(XmlElement[]? source)
    {
        return source?.Select(static element => (XmlElement)element.CloneNode(deep: true)).ToArray();
    }

    private static XmlAttribute[]? CloneAttributes(XmlAttribute[]? source)
    {
        if (source is null)
        {
            return null;
        }

        var document = new XmlDocument();
        return source
            .Select(attribute =>
            {
                var clone = document.CreateAttribute(attribute.Prefix, attribute.LocalName, attribute.NamespaceURI);
                clone.Value = attribute.Value;
                return clone;
            })
            .ToArray();
    }

    private static void AddIfNotEmpty(ISet<string> values, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values.Add(value);
        }
    }

    private static void DeleteIfExists(string filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
