using System;
using System.Collections.Generic;
using System.Linq;
using TweakOne.Localization;

namespace TweakOne.IsoXml.V3;

public sealed class IsoXmlCenteredRectificationTemplateInjectionService
{
    private static readonly LocalizedStrings Strings = LocalizedStrings.Instance;

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
        var injectedGuidanceLines = new List<IsoXmlLineString>(correctionLines.Count + 2)
        {
            plan.MarkerLineA.DeepClone(),
            plan.MarkerLineB.DeepClone()
        };

        for (var index = 0; index < correctionLines.Count; index++)
        {
            var guidanceLine = correctionLines[index].DeepClone();
            guidanceLine.Designator = plan.OffsetRows[index].SuggestedDesignator;
            injectedGuidanceLines.Add(guidanceLine);
        }

        foreach (var line in injectedGuidanceLines)
        {
            targetPartfield.LineStrings.Add(line);
        }

        return new IsoXmlCenteredRectificationTemplateInjectionResult(
            targetPartfield,
            correctionLines.Count,
            2,
            injectedGuidanceLines);
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
}

public sealed record IsoXmlCenteredRectificationTemplateInjectionResult(
    IsoXmlPartfield TargetPartfield,
    int InjectedGuidanceLineCount,
    int InjectedMarkerLineCount,
    IReadOnlyList<IsoXmlLineString> InjectedLines);