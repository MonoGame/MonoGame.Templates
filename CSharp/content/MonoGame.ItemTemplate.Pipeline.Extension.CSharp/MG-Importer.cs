using Microsoft.Xna.Framework.Content.Pipeline;

using TImport = System.String;

[ContentImporter(".txt", DisplayName = "MGImporter", DefaultProcessor = "MGProcessor")]
public class MGImporter : ContentImporter<TImport>
{
    public override TImport Import(string filename, ContentImporterContext context)
    {
        return default(TImport);
    }
}
