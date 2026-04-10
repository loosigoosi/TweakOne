using System;
using System.Collections.Generic;
using System.Linq;
using TweakOne.Localization;

namespace TweakOne.IsoXml.V3;

public sealed class IsoXmlCenteredRectificationTemplateInjectionService
{
    private static readonly LocalizedStrings Strings = LocalizedStrings.Instance;
    private readonly IsoXmlGuidancePathGenerator _generator = new();

    /// <summary>
    /// Injects the centered rectification guidance lines and marker cuts into a template task document.
    /// </summary>
    public IsoXmlCenteredRectificationTemplateInjectionResult Inject(
        IsoXmlTaskDataDocument templateDocument,
        string? preferredPartfieldIdentifier,
        string? preferredPartfieldDesignator,
        IsoXmlCenteredRectificationPlan plan,
        IReadOnlyList<IsoXmlLineString> correctionLines)
    {
        ArgumentNullException.ThrowIfNull(templateDocument);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(correctionLines);

        if (correctionLines.Count != plan.OffsetRows.Count)
        {
            throw new ArgumentException(Strings.CenteredRectificationCorrectionLineCountError, nameof(correctionLines));
        }

        var targetPartfield = ResolveTargetPartfield(templateDocument, preferredPartfieldIdentifier, preferredPartfieldDesignator);
        var existingGuidanceIds = new HashSet<string>(EnumerateGuidanceIds(templateDocument), StringComparer.Ordinal);
        var injectedMarkerLines = new List<IsoXmlLineString>(2)
        {
            plan.MarkerLineA.DeepClone(),
            plan.MarkerLineB.DeepClone()
        };

        foreach (var markerLine in injectedMarkerLines)
        {
            targetPartfield.LineStrings.Add(markerLine);
        }

        var injectedGuidanceLines = new List<IsoXmlLineString>(correctionLines.Count);
        var injectedGuidanceGroups = new List<IsoXmlGuidanceGroup>(correctionLines.Count);

        for (var index = 0; index < correctionLines.Count; index++)
        {
            var guidanceLine = correctionLines[index].DeepClone();
            guidanceLine.Designator = plan.OffsetRows[index].SuggestedDesignator;
            injectedGuidanceLines.Add(guidanceLine);
            var guidanceGroup = _generator.CreateGroupedRectification(targetPartfield, new[] { guidanceLine }, guidanceLine.Designator);
            AssignDocumentWideGuidanceIds(guidanceGroup, existingGuidanceIds);
            injectedGuidanceGroups.Add(guidanceGroup);
        }

        return new IsoXmlCenteredRectificationTemplateInjectionResult(
            targetPartfield,
            correctionLines.Count,
            2,
            injectedMarkerLines.Concat(injectedGuidanceLines).ToArray(),
            injectedGuidanceGroups);
    }

    private static IsoXmlPartfield ResolveTargetPartfield(IsoXmlTaskDataDocument templateDocument, string? preferredPartfieldIdentifier, string? preferredPartfieldDesignator)
    {
        if (templateDocument.Partfields.Count == 0)
        {
            throw new InvalidOperationException(Strings.CenteredRectificationTemplateMissingPartfieldError);
        }

        if (!string.IsNullOrWhiteSpace(preferredPartfieldIdentifier))
        {
            var matchingByIdentifier = templateDocument.Partfields.FirstOrDefault(partfield => string.Equals(partfield.Id, preferredPartfieldIdentifier, StringComparison.Ordinal));
            if (matchingByIdentifier is not null)
            {
                return matchingByIdentifier;
            }
        }

        if (!string.IsNullOrWhiteSpace(preferredPartfieldDesignator))
        {
            var matchingByDesignator = templateDocument.Partfields.FirstOrDefault(partfield => string.Equals(partfield.Designator, preferredPartfieldDesignator, StringComparison.CurrentCultureIgnoreCase));
            if (matchingByDesignator is not null)
            {
                return matchingByDesignator;
            }
        }

        return templateDocument.Partfields[0];
    }

    private static IEnumerable<string> EnumerateGuidanceIds(IsoXmlTaskDataDocument templateDocument)
    {
        return templateDocument.Partfields
            .SelectMany(static partfield => partfield.GuidanceGroups)
            .Select(static group => group.Id)
            .Concat(templateDocument.Partfields.SelectMany(static partfield => partfield.GuidanceGroups.SelectMany(group => group.GuidancePatterns.Select(pattern => pattern.Id))))
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id!);
    }

    private static void AssignDocumentWideGuidanceIds(IsoXmlGuidanceGroup guidanceGroup, ISet<string> existingGuidanceIds)
    {
        guidanceGroup.Id = GetNextGuidanceId(existingGuidanceIds, "GGP");

        foreach (var guidancePattern in guidanceGroup.GuidancePatterns)
        {
            guidancePattern.Id = GetNextGuidanceId(existingGuidanceIds, "GPN");
        }
    }

    private static string GetNextGuidanceId(ISet<string> existingGuidanceIds, string prefix)
    {
        for (var index = 1; ; index++)
        {
            var id = $"{prefix}{index}";
            if (existingGuidanceIds.Add(id))
            {
                return id;
            }
        }
    }
}

public sealed record IsoXmlCenteredRectificationTemplateInjectionResult(
    IsoXmlPartfield TargetPartfield,
    int InjectedGuidanceLineCount,
    int InjectedMarkerLineCount,
    IReadOnlyList<IsoXmlLineString> InjectedLines,
    IReadOnlyList<IsoXmlGuidanceGroup> InjectedGuidanceGroups);
