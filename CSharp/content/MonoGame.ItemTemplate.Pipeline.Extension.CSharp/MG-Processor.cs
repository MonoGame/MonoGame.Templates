using Microsoft.Xna.Framework.Content.Pipeline;

using TInput = System.String;
using TOutput = System.String;

[ContentProcessor(DisplayName = "MGProcessor")]
public class MGProcessor : ContentProcessor<TInput, TOutput>
{
    public override TOutput Process(TInput input, ContentProcessorContext context)
    {
        return default(TOutput);
    }
}
