using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Xml.Linq;
using JetBrains.Annotations;
using Parlance.CLDR;
using Parlance.Project.Index;

namespace Parlance.Project.TranslationFiles.QtLinguist;

[TranslationFileType("qt", TranslationFileTypeAttribute.ExpectedTranslationFileNameFormat.Underscored)]
public class QtLinguistTranslationFile : ParlanceTranslationFile, IParlanceMonoTranslationFile
{
    private string _file = null!;
    private Locale _locale = null!;

    private async Task LoadFile(string file, Locale locale)
    {
        _file = file;
        _locale = locale;

        var fileContents = await File.ReadAllTextAsync(file);
        Hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(fileContents)));

        var pluralRules = locale.PluralRules().ToArray();
        
        var xmlDoc = XDocument.Parse(fileContents);
        Entries = xmlDoc.Descendants("message").Select((msg, idx) => new QtLinguistTranslationFileEntry
        {
            RealKey = (string)msg.Element("source")!,
            Context = ((string) msg.Parent!.Element("name"))!,
            Source = (string)msg.Element("source")!,
            Translation = msg.Attribute("numerus")?.Value == "yes" ? msg.Descendants("numerusform").Select((content, idx2) => new TranslationWithPluralType()
            {
                PluralType = pluralRules[idx2].Category,
                TranslationContent = (string) content
            }).ToList() : new List<TranslationWithPluralType>
            {
                new()
                {
                    PluralType = "singular",
                    TranslationContent = (string) msg.Element("translation")!
                }
            },
            RequiresPluralisation = msg.Attribute("numerus")?.Value == "yes",
            Locations = msg.Elements("location").Select(loc => new QtLinguistTranslationFileEntry.Location((string)loc.Attribute("filename")!, ((string)loc.Attribute("line"))!))
        }).Cast<IParlanceTranslationFileEntry>().ToList();
    }

    public override string Hash { get; internal set; } = null!;

    public override IList<IParlanceTranslationFileEntry> Entries { get; internal set; } = null!;

    public new async Task Save()
    {
        var pluralRules = _locale.PluralRules().ToList();
        
        var doc = new XDocument(
            new XElement("TS", new XAttribute("version", "2.1"),
                Entries.GroupBy(entry => entry.Context).Select(context =>
                    new XElement("context", 
                        new XElement("name", new XText(context.Key)),
                        context.Select(entry => new XElement("message",
                            entry.RequiresPluralisation
                                ? new XObject[] {
                                    new XAttribute("numerus", "yes"),
                                    new XElement("translation",
                                        entry.Translation.OrderBy(x => pluralRules.FindIndex(r => r.Category == x.PluralType)).Select(translation => new XElement("numerusform", new XText(translation.TranslationContent)))
                                    )
                                }
                                : new XElement("translation", new XText(entry.Translation.Single(x => x.PluralType == "singular").TranslationContent)),
                            ((QtLinguistTranslationFileEntry) entry).Locations.Select(location => new XElement("location", new XAttribute("filename", location.Filename), new XAttribute("line", location.Line))),
                            new XElement("source", new XText(((QtLinguistTranslationFileEntry) entry).RealKey))
                        ))
                    )
                )
            )
        );

        {
            //TODO: What if two people write to the file at the same time?
            await using var stream = File.Open(_file, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            await doc.SaveAsync(stream, SaveOptions.None, CancellationToken.None);
        }

        await LoadFile(_file, _locale);
        await base.Save();
    }

    public static async Task<ParlanceTranslationFile> CreateAsync(string file, Locale locale, IParlanceSubprojectLanguage? subprojectLanguage, IParlanceIndexingService? indexingService)
    {
        var translationFile = new QtLinguistTranslationFile(subprojectLanguage, indexingService);
        await translationFile.LoadFile(file, locale);
        return translationFile;
    }

    public QtLinguistTranslationFile(IParlanceSubprojectLanguage? subprojectLanguage, IParlanceIndexingService? indexingService) : base(subprojectLanguage, indexingService)
    {
    }
}