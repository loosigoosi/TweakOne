using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TweakOne.IsoXml.V3
{
    public static class IsoXmlTaskDataSerializer
    {
        private static readonly XmlSerializer Serializer = new(typeof(IsoXmlTaskDataDocument));

        /// <summary>
        /// Loads an ISOXML v3 task data document from disk.
        /// </summary>
        public static IsoXmlTaskDataDocument Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("A file path is required.", nameof(filePath));
            }

            using var stream = File.OpenRead(filePath);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreComments = false, IgnoreWhitespace = true });
            var document = Serializer.Deserialize(reader) as IsoXmlTaskDataDocument;
            return document ?? throw new InvalidOperationException($"File '{filePath}' does not contain a valid ISOXML task data document.");
        }

        /// <summary>
        /// Saves an ISOXML v3 task data document to disk.
        /// </summary>
        public static void Save(IsoXmlTaskDataDocument document, string filePath)
        {
            ArgumentNullException.ThrowIfNull(document);

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("A file path is required.", nameof(filePath));
            }

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add(string.Empty, string.Empty);

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = Environment.NewLine,
                NewLineHandling = NewLineHandling.Replace,
                Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            };

            using var stream = File.Create(filePath);
            using var writer = XmlWriter.Create(stream, settings);
            Serializer.Serialize(writer, document, namespaces);
        }
    }
}
