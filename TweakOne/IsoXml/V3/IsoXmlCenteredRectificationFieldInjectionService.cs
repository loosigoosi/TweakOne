using System;
using System.Collections.Generic;
using System.Linq;
using TweakOne.Localization;

namespace TweakOne.IsoXml.V3;

/// <summary>
/// Injects centered rectification guidance lines and marker cuts directly into a partfield for field-package exports.
/// </summary>
public sealed class IsoXmlCenteredRectificationFieldInjectionService
{
    private static readonly LocalizedStrings Strings = LocalizedStrings.Instance;

    /// <summary>
    /// Injects centered rectification lines and marker cuts into the matching partfield as direct line strings.
    /// </summary>
    public IsoXmlCenteredRectificationFieldInjectionResult Inject(
        IsoXmlTaskDataDocument document,
        string? preferredPartfieldIdentifier,
        string? preferredPartfieldDesignator,
        IsoXmlCenteredRectificationPlan plan,
        IReadOnlyList<IsoXmlLineString> correctionLines)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(correctionLines);

        if (correctionLines.Count != plan.OffsetRows.Count)
        {
            throw new ArgumentException(Strings.CenteredRectificationCorrectionLineCountError, nameof(correctionLines));
        }

        var targetPartfield = ResolveTargetPartfield(document, preferredPartfieldIdentifier, preferredPartfieldDesignator);
        var injectedLines = new List<IsoXmlLineString>(correctionLines.Count + 2)
        {
            plan.MarkerLineA.DeepClone(),
            plan.MarkerLineB.DeepClone()
        };

        for (var index = 0; index < correctionLines.Count; index++)
        {
            var guidanceLine = correctionLines[index].DeepClone();
            guidanceLine.Designator = plan.OffsetRows[index].SuggestedDesignator;
            injectedLines.Add(guidanceLine);
        }

        foreach (var line in injectedLines)
        {
            targetPartfield.LineStrings.Add(line);
        }

        return new IsoXmlCenteredRectificationFieldInjectionResult(targetPartfield, correctionLines.Count, 2, injectedLines);
    }

    private static IsoXmlPartfield ResolveTargetPartfield(IsoXmlTaskDataDocument document, string? preferredPartfieldIdentifier, string? preferredPartfieldDesignator)
    {
        if (document.Partfields.Count == 0)
        {
            throw new InvalidOperationException(Strings.CenteredRectificationTemplateMissingPartfieldError);
        }

        if (!string.IsNullOrWhiteSpace(preferredPartfieldIdentifier))
        {
            var matchingByIdentifier = document.Partfields.FirstOrDefault(partfield => string.Equals(partfield.Id, preferredPartfieldIdentifier, StringComparison.Ordinal));
            if (matchingByIdentifier is not null)
            {
                return matchingByIdentifier;
            }
        }

        if (!string.IsNullOrWhiteSpace(preferredPartfieldDesignator))
        {
            var matchingByDesignator = document.Partfields.FirstOrDefault(partfield => string.Equals(partfield.Designator, preferredPartfieldDesignator, StringComparison.CurrentCultureIgnoreCase));
            if (matchingByDesignator is not null)
            {
                return matchingByDesignator;
            }
        }

        return document.Partfields[0];
    }
}

public sealed record IsoXmlCenteredRectificationFieldInjectionResult(
    IsoXmlPartfield TargetPartfield,
    int InjectedGuidanceLineCount,
    int InjectedMarkerLineCount,
    IReadOnlyList<IsoXmlLineString> InjectedLines);