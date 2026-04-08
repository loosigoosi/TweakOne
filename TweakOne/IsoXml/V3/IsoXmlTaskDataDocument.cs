using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace TweakOne.IsoXml.V3
{
    [XmlRoot("ISO11783_TaskData")]
    public sealed class IsoXmlTaskDataDocument
    {
        [XmlAttribute("VersionMajor")]
        public string? VersionMajor { get; set; }

        [XmlAttribute("VersionMinor")]
        public string? VersionMinor { get; set; }

        [XmlAttribute("ManagementSoftwareManufacturer")]
        public string? ManagementSoftwareManufacturer { get; set; }

        [XmlAttribute("ManagementSoftwareVersion")]
        public string? ManagementSoftwareVersion { get; set; }

        [XmlAttribute("DataTransferOrigin")]
        public string? DataTransferOrigin { get; set; }

        [XmlElement("CTP")]
        public List<IsoXmlCropType> CropTypes { get; set; } = new();

        [XmlElement("CTR")]
        public List<IsoXmlCustomer> Customers { get; set; } = new();

        [XmlElement("FRM")]
        public List<IsoXmlFarm> Farms { get; set; } = new();

        [XmlElement("PFD")]
        public List<IsoXmlPartfield> Partfields { get; set; } = new();

        [XmlAnyElement]
        public XmlElement[]? AdditionalElements { get; set; }

        [XmlAnyAttribute]
        public XmlAttribute[]? AdditionalAttributes { get; set; }

        public IsoXmlPartfield GetRequiredPartfield(string partfieldId)
        {
            if (string.IsNullOrWhiteSpace(partfieldId))
            {
                throw new ArgumentException("A partfield id is required.", nameof(partfieldId));
            }

            return Partfields.FirstOrDefault(p => string.Equals(p.Id, partfieldId, StringComparison.Ordinal))
                ?? throw new InvalidOperationException($"Partfield '{partfieldId}' was not found.");
        }
    }

    public sealed class IsoXmlCropType
    {
        [XmlAttribute("A")]
        public string? Id { get; set; }

        [XmlAttribute("B")]
        public string? Designator { get; set; }

        [XmlAnyAttribute]
        public XmlAttribute[]? AdditionalAttributes { get; set; }

        [XmlAnyElement]
        public XmlElement[]? AdditionalElements { get; set; }
    }

    public sealed class IsoXmlCustomer
    {
        [XmlAttribute("A")]
        public string? Id { get; set; }

        [XmlAttribute("B")]
        public string? LastNameOrDesignator { get; set; }

        [XmlAttribute("C")]
        public string? FirstName { get; set; }

        [XmlAttribute("D")]
        public string? Street { get; set; }

        [XmlAttribute("E")]
        public string? PoBox { get; set; }

        [XmlAttribute("F")]
        public string? PostalCode { get; set; }

        [XmlAttribute("G")]
        public string? City { get; set; }

        [XmlAttribute("H")]
        public string? State { get; set; }

        [XmlAttribute("I")]
        public string? Country { get; set; }

        [XmlAttribute("J")]
        public string? Phone { get; set; }

        [XmlAttribute("K")]
        public string? Mobile { get; set; }

        [XmlAttribute("L")]
        public string? Fax { get; set; }

        [XmlAttribute("M")]
        public string? Email { get; set; }

        [XmlAttribute("N")]
        public string? CustomerNumber { get; set; }

        [XmlAnyAttribute]
        public XmlAttribute[]? AdditionalAttributes { get; set; }
    }

    public sealed class IsoXmlFarm
    {
        [XmlAttribute("A")]
        public string? Id { get; set; }

        [XmlAttribute("B")]
        public string? Designator { get; set; }

        [XmlAttribute("C")]
        public string? Street { get; set; }

        [XmlAttribute("D")]
        public string? PoBox { get; set; }

        [XmlAttribute("E")]
        public string? PostalCode { get; set; }

        [XmlAttribute("F")]
        public string? City { get; set; }

        [XmlAttribute("G")]
        public string? State { get; set; }

        [XmlAttribute("H")]
        public string? Country { get; set; }

        [XmlAttribute("I")]
        public string? CustomerIdRef { get; set; }

        [XmlAnyAttribute]
        public XmlAttribute[]? AdditionalAttributes { get; set; }
    }

    public sealed class IsoXmlPartfield
    {
        [XmlAttribute("A")]
        public string? Id { get; set; }

        [XmlAttribute("B")]
        public string? Code { get; set; }

        [XmlAttribute("C")]
        public string? Designator { get; set; }

        [XmlAttribute("D")]
        public ulong Area { get; set; }

        [XmlAttribute("E")]
        public string? CustomerIdRef { get; set; }

        [XmlAttribute("F")]
        public string? FarmIdRef { get; set; }

        [XmlAttribute("G")]
        public string? CropTypeIdRef { get; set; }

        [XmlAttribute("H")]
        public string? CropVarietyIdRef { get; set; }

        [XmlAttribute("I")]
        public string? FieldIdRef { get; set; }

        [XmlElement("PLN")]
        public List<IsoXmlPolygon> Polygons { get; set; } = new();

        [XmlElement("LSG")]
        public List<IsoXmlLineString> LineStrings { get; set; } = new();

        [XmlElement("GGP")]
        public List<IsoXmlGuidanceGroup> GuidanceGroups { get; set; } = new();

        [XmlElement("PNT")]
        public List<IsoXmlPoint> Points { get; set; } = new();

        [XmlAnyElement]
        public XmlElement[]? AdditionalElements { get; set; }

        [XmlAnyAttribute]
        public XmlAttribute[]? AdditionalAttributes { get; set; }

        public IEnumerable<IsoXmlLineString> GuidancePaths => LineStrings.Where(static line => line.Type == IsoXmlLineString.GuidancePathType);
    }

    public sealed class IsoXmlGuidanceGroup
    {
        [XmlAttribute("A")]
        public string? Id { get; set; }

        [XmlAttribute("B")]
        public string? Designator { get; set; }

        [XmlElement("GPN")]
        public List<IsoXmlGuidancePattern> GuidancePatterns { get; set; } = new();

        [XmlAnyElement]
        public XmlElement[]? AdditionalElements { get; set; }

        [XmlAnyAttribute]
        public XmlAttribute[]? AdditionalAttributes { get; set; }
    }

    public sealed class IsoXmlGuidancePattern
    {
        public const int AbGuidancePatternType = 1;
        public const int CurveGuidancePatternType = 3;

        [XmlAttribute("A")]
        public string? Id { get; set; }

        [XmlAttribute("B")]
        public string? Designator { get; set; }

        [XmlAttribute("C")]
        public int Type { get; set; }

        [XmlElement("LSG")]
        public IsoXmlLineString? LineString { get; set; }

        [XmlAnyElement]
        public XmlElement[]? AdditionalElements { get; set; }

        [XmlAnyAttribute]
        public XmlAttribute[]? AdditionalAttributes { get; set; }
    }

    public sealed class IsoXmlPolygon
    {
        [XmlAttribute("A")]
        public int Type { get; set; }

        [XmlAttribute("B")]
        public string? Designator { get; set; }

        [XmlIgnore]
        public ulong? Area { get; set; }

        [XmlAttribute("C")]
        public string? AreaText
        {
            get => IsoXmlAttributeValueConverter.Format(Area);
            set => Area = IsoXmlAttributeValueConverter.ParseNullableUInt64(value);
        }

        [XmlIgnore]
        public byte? Colour { get; set; }

        [XmlAttribute("D")]
        public string? ColourText
        {
            get => IsoXmlAttributeValueConverter.Format(Colour);
            set => Colour = IsoXmlAttributeValueConverter.ParseNullableByte(value);
        }

        [XmlElement("LSG")]
        public List<IsoXmlLineString> LineStrings { get; set; } = new();

        [XmlAnyElement]
        public XmlElement[]? AdditionalElements { get; set; }

        [XmlAnyAttribute]
        public XmlAttribute[]? AdditionalAttributes { get; set; }
    }

    public sealed class IsoXmlLineString
    {
        public const int GuidancePathType = 5;
        public const int MarkerLineType = 7;

        [XmlAttribute("A")]
        public int Type { get; set; }

        [XmlAttribute("B")]
        public string? Designator { get; set; }

        [XmlIgnore]
        public ulong? Width { get; set; }

        [XmlAttribute("C")]
        public string? WidthText
        {
            get => IsoXmlAttributeValueConverter.Format(Width);
            set => Width = IsoXmlAttributeValueConverter.ParseNullableUInt64(value);
        }

        [XmlIgnore]
        public ulong? Length { get; set; }

        [XmlAttribute("D")]
        public string? LengthText
        {
            get => IsoXmlAttributeValueConverter.Format(Length);
            set => Length = IsoXmlAttributeValueConverter.ParseNullableUInt64(value);
        }

        [XmlIgnore]
        public byte? Colour { get; set; }

        [XmlAttribute("E")]
        public string? ColourText
        {
            get => IsoXmlAttributeValueConverter.Format(Colour);
            set => Colour = IsoXmlAttributeValueConverter.ParseNullableByte(value);
        }

        [XmlElement("PNT")]
        public List<IsoXmlPoint> Points { get; set; } = new();

        [XmlAnyElement]
        public XmlElement[]? AdditionalElements { get; set; }

        [XmlAnyAttribute]
        public XmlAttribute[]? AdditionalAttributes { get; set; }

        public IsoXmlLineString DeepClone()
        {
            return new IsoXmlLineString
            {
                Type = Type,
                Designator = Designator,
                Width = Width,
                Length = Length,
                Colour = Colour,
                Points = Points.Select(static point => point.DeepClone()).ToList()
            };
        }
    }

    public sealed class IsoXmlPoint
    {
        [XmlAttribute("A")]
        public int Type { get; set; }

        [XmlAttribute("B")]
        public string? Designator { get; set; }

        [XmlAttribute("C")]
        public double North { get; set; }

        [XmlAttribute("D")]
        public double East { get; set; }

        [XmlIgnore]
        public long? Up { get; set; }

        [XmlAttribute("E")]
        public string? UpText
        {
            get => IsoXmlAttributeValueConverter.Format(Up);
            set => Up = IsoXmlAttributeValueConverter.ParseNullableInt64(value);
        }

        [XmlIgnore]
        public byte? Colour { get; set; }

        [XmlAttribute("F")]
        public string? ColourText
        {
            get => IsoXmlAttributeValueConverter.Format(Colour);
            set => Colour = IsoXmlAttributeValueConverter.ParseNullableByte(value);
        }

        [XmlAnyAttribute]
        public XmlAttribute[]? AdditionalAttributes { get; set; }

        public IsoXmlPoint DeepClone()
        {
            return new IsoXmlPoint
            {
                Type = Type,
                Designator = Designator,
                North = North,
                East = East,
                Up = Up,
                Colour = Colour
            };
        }
    }

    internal static class IsoXmlAttributeValueConverter
    {
        public static string? Format<T>(T? value)
            where T : struct, ISpanFormattable
        {
            return value?.ToString(null, CultureInfo.InvariantCulture);
        }

        public static ulong? ParseNullableUInt64(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return ulong.Parse(value, CultureInfo.InvariantCulture);
        }

        public static long? ParseNullableInt64(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return long.Parse(value, CultureInfo.InvariantCulture);
        }

        public static byte? ParseNullableByte(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return byte.Parse(value, CultureInfo.InvariantCulture);
        }
    }
}
