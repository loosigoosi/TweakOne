using System;
using System.IO;
using System.Linq;
using TweakOne.IsoXml.V3;
using Xunit;

namespace TweakOne.Tests.IsoXml.V3
{
    public sealed class IsoXmlGuidancePathGeneratorTests
    {
        [Fact]
        public void Load_WhenBaselineDocumentIsProvided_ReturnsTheExpectedPartfield()
        {
            var document = IsoXmlTaskDataSerializer.Load(GetTaskDataPath());

            Assert.Equal("TEST", document.GetRequiredPartfield("PFD2").Designator);
        }

        [Fact]
        public void CreateSimpleCopy_WhenGuidancePathIsProvided_AppendsADeepCopyToTheTargetPartfield()
        {
            var generator = new IsoXmlGuidancePathGenerator();
            var target = new IsoXmlPartfield();
            var source = new IsoXmlLineString
            {
                Type = IsoXmlLineString.GuidancePathType,
                Designator = "AB source",
                Points =
                {
                    new IsoXmlPoint { Type = 2, North = 45d, East = 7d },
                    new IsoXmlPoint { Type = 2, North = 45d, East = 7.001d }
                }
            };

            var copy = generator.CreateSimpleCopy(target, source, "AB copy");

            Assert.NotSame(source.Points[0], copy.Points[0]);
        }

        [Fact]
        public void CreateTranslatedCopy_WhenOffsetIsProvided_ShiftsTheCopiedPathInParallel()
        {
            var generator = new IsoXmlGuidancePathGenerator();
            var target = new IsoXmlPartfield();
            var source = new IsoXmlLineString
            {
                Type = IsoXmlLineString.GuidancePathType,
                Designator = "AB source",
                Points =
                {
                    new IsoXmlPoint { Type = 2, North = 45d, East = 7d },
                    new IsoXmlPoint { Type = 2, North = 45d, East = 7.001d }
                }
            };

            var copy = generator.CreateTranslatedCopy(target, source, 10d, "AB shifted");
            var northShiftMeters = (copy.Points[0].North - source.Points[0].North) * (Math.PI / 180d) * 6378137d;

            Assert.InRange(northShiftMeters, 9.5d, 10.5d);
        }

        [Fact]
        public void CreateTranslatedCopy_WhenTargetBoundaryIsOnTheOppositeSide_SelectsTheOffsetDirectionThatMovesTowardTheField()
        {
            var generator = new IsoXmlGuidancePathGenerator();
            var target = new IsoXmlPartfield
            {
                Polygons = { CreateRectanglePolygon(44.99985d, 44.99995d, 7.0002d, 7.0012d) }
            };
            var source = new IsoXmlLineString
            {
                Type = IsoXmlLineString.GuidancePathType,
                Designator = "AB source",
                Points =
                {
                    new IsoXmlPoint { Type = 2, North = 45d, East = 7d },
                    new IsoXmlPoint { Type = 2, North = 45d, East = 7.001d }
                }
            };

            var copy = generator.CreateTranslatedCopy(target, source, 10d, "AB shifted");
            var northShiftMeters = (copy.Points[0].North - source.Points[0].North) * (Math.PI / 180d) * 6378137d;

            Assert.InRange(northShiftMeters, -10.5d, -9.5d);
        }

        [Fact]
        public void CreateTranslatedCopy_WhenAbSegmentIsCompletelyOutside_SlidesTheAbPointsAlongTheLineTowardTheTargetField()
        {
            var generator = new IsoXmlGuidancePathGenerator();
            var target = new IsoXmlPartfield
            {
                Polygons = { CreateRectanglePolygon(44.99995d, 45.00005d, 7.0010d, 7.0012d) }
            };
            var source = new IsoXmlLineString
            {
                Type = IsoXmlLineString.GuidancePathType,
                Designator = "AB source",
                Points =
                {
                    new IsoXmlPoint { Type = 2, North = 45d, East = 7d },
                    new IsoXmlPoint { Type = 2, North = 45d, East = 7.0001d }
                }
            };

            var copy = generator.CreateTranslatedCopy(target, source, 0d, "AB shifted");
            var midpointEast = (copy.Points[0].East + copy.Points[1].East) / 2d;

            Assert.InRange(midpointEast, 7.00105d, 7.00115d);
        }

        [Fact]
        public void CreateTranslatedCopy_WhenTranslatedAbLineCrossesTheTargetBoundary_SnapsEndpointsToBoundaryIntersections()
        {
            var generator = new IsoXmlGuidancePathGenerator();
            var target = new IsoXmlPartfield
            {
                Polygons = { CreateRectanglePolygon(44.99995d, 45.00005d, 7.00020d, 7.00080d) }
            };
            var source = new IsoXmlLineString
            {
                Type = IsoXmlLineString.GuidancePathType,
                Designator = "AB source",
                Points =
                {
                    new IsoXmlPoint { Type = 2, North = 45d, East = 7d },
                    new IsoXmlPoint { Type = 2, North = 45d, East = 7.001d }
                }
            };

            var copy = generator.CreateTranslatedCopy(target, source, 0d, "AB fitted");

            Assert.InRange(copy.Points[0].East, 7.00019d, 7.00021d);
            Assert.InRange(copy.Points[1].East, 7.00079d, 7.00081d);
            Assert.InRange(copy.Points[0].North, 44.99999d, 45.00001d);
            Assert.InRange(copy.Points[1].North, 44.99999d, 45.00001d);
        }

        [Fact]
        public void CreateGroupedRectification_WhenLinesAreProvided_AddsOneGroupWithOrderedNestedGuidancePatterns()
        {
            var generator = new IsoXmlGuidancePathGenerator();
            var target = new IsoXmlPartfield();
            var lines = new[]
            {
                CreateGuidanceLine("Passata_01", 45.000000d, 7.000000d, 45.000020d, 7.000200d, 45.000000d, 7.000400d),
                CreateGuidanceLine("Passata_02", 45.000010d, 7.000000d, 45.000030d, 7.000200d, 45.000010d, 7.000400d),
                CreateGuidanceLine("AB_Finale", 45.000020d, 7.000000d, 45.000020d, 7.000400d)
            };

            var group = generator.CreateGroupedRectification(target, lines, "Rettifica_Graduale_CampoX");

            Assert.Single(target.GuidanceGroups);
            Assert.Equal("Rettifica_Graduale_CampoX", group.Designator);
            Assert.Equal(lines.Length, group.GuidancePatterns.Count);
            Assert.Equal(IsoXmlGuidancePattern.CurveGuidancePatternType, group.GuidancePatterns[0].Type);
            Assert.Equal(IsoXmlGuidancePattern.CurveGuidancePatternType, group.GuidancePatterns[1].Type);
            Assert.Equal(IsoXmlGuidancePattern.AbGuidancePatternType, group.GuidancePatterns[2].Type);
            Assert.NotNull(group.GuidancePatterns[0].LineString);
            Assert.NotSame(lines[0].Points[0], group.GuidancePatterns[0].LineString!.Points[0]);
            Assert.Equal(6, group.GuidancePatterns[0].LineString.Points[0].Type);
            Assert.Equal(9, group.GuidancePatterns[0].LineString.Points[1].Type);
            Assert.Equal(7, group.GuidancePatterns[0].LineString.Points[^1].Type);
            Assert.Equal(6, group.GuidancePatterns[2].LineString!.Points[0].Type);
            Assert.Equal(7, group.GuidancePatterns[2].LineString.Points[^1].Type);
        }

        [Fact]
        public void Save_WhenDocumentIsRoundTripped_PreservesTheAddedGuidancePath()
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xml");
            var document = IsoXmlTaskDataSerializer.Load(GetTaskDataPath());
            var generator = new IsoXmlGuidancePathGenerator();
            var partfield = document.GetRequiredPartfield("PFD2");
            var source = partfield.GuidancePaths.Single();

            generator.CreateSimpleCopy(partfield, source, "AB roundtrip");
            IsoXmlTaskDataSerializer.Save(document, tempFilePath);
            var reloaded = IsoXmlTaskDataSerializer.Load(tempFilePath);

            Assert.Contains(reloaded.GetRequiredPartfield("PFD2").GuidancePaths, line => line.Designator == "AB roundtrip");
        }

        [Fact]
        public void Save_WhenGroupedRectificationIsRoundTripped_PreservesTheAddedGuidanceGroup()
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xml");
            var document = IsoXmlTaskDataSerializer.Load(GetTaskDataPath());
            var generator = new IsoXmlGuidancePathGenerator();
            var partfield = document.GetRequiredPartfield("PFD2");
            var lines = new[]
            {
                CreateGuidanceLine("Passata_01", 45.000000d, 7.000000d, 45.000020d, 7.000200d, 45.000000d, 7.000400d),
                CreateGuidanceLine("Passata_02", 45.000010d, 7.000000d, 45.000030d, 7.000200d, 45.000010d, 7.000400d),
                CreateGuidanceLine("AB_Finale", 45.000020d, 7.000000d, 45.000020d, 7.000400d)
            };

            generator.CreateGroupedRectification(partfield, lines, "Rettifica_Graduale_CampoX");
            IsoXmlTaskDataSerializer.Save(document, tempFilePath);
            var reloaded = IsoXmlTaskDataSerializer.Load(tempFilePath);
            var reloadedPartfield = reloaded.GetRequiredPartfield("PFD2");
            var reloadedGroup = Assert.Single(reloadedPartfield.GuidanceGroups);

            Assert.Equal("Rettifica_Graduale_CampoX", reloadedGroup.Designator);
            Assert.Equal(3, reloadedGroup.GuidancePatterns.Count);
            Assert.Equal(IsoXmlGuidancePattern.AbGuidancePatternType, reloadedGroup.GuidancePatterns[^1].Type);
            Assert.All(reloadedGroup.GuidancePatterns, static pattern => Assert.NotNull(pattern.LineString));
        }

        [Fact]
        public void Save_WhenGuidancePathIsRemoved_RoundTripDoesNotContainTheRemovedGuidancePath()
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xml");
            var document = IsoXmlTaskDataSerializer.Load(GetTaskDataPath());
            var partfield = document.GetRequiredPartfield("PFD2");
            var existing = partfield.GuidancePaths.Single();

            Assert.True(partfield.LineStrings.Remove(existing));

            IsoXmlTaskDataSerializer.Save(document, tempFilePath);
            var reloaded = IsoXmlTaskDataSerializer.Load(tempFilePath);

            Assert.Empty(reloaded.GetRequiredPartfield("PFD2").GuidancePaths);
        }

        private static string GetTaskDataPath()
        {
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TASKDATA", "TestISOXML_v3", "TASKDATA.XML"));
        }

        private static IsoXmlPolygon CreateRectanglePolygon(double south, double north, double west, double east)
        {
            return new IsoXmlPolygon
            {
                Type = 1,
                LineStrings =
                {
                    new IsoXmlLineString
                    {
                        Type = 1,
                        Points =
                        {
                            new IsoXmlPoint { Type = 2, North = south, East = west },
                            new IsoXmlPoint { Type = 2, North = south, East = east },
                            new IsoXmlPoint { Type = 2, North = north, East = east },
                            new IsoXmlPoint { Type = 2, North = north, East = west },
                            new IsoXmlPoint { Type = 2, North = south, East = west }
                        }
                    }
                }
            };
        }

        private static IsoXmlLineString CreateGuidanceLine(string designator, params double[] northEastPairs)
        {
            var line = new IsoXmlLineString
            {
                Type = IsoXmlLineString.GuidancePathType,
                Designator = designator
            };

            for (var index = 0; index < northEastPairs.Length; index += 2)
            {
                line.Points.Add(new IsoXmlPoint
                {
                    Type = 2,
                    North = northEastPairs[index],
                    East = northEastPairs[index + 1]
                });
            }

            return line;
        }
    }
}
