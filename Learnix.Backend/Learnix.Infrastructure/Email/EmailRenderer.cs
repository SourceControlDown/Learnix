using RazorLight;

namespace Learnix.Infrastructure.Email;

internal sealed class EmailRenderer
{
    private readonly IRazorLightEngine _engine;

    public EmailRenderer()
    {
        var templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Email", "Templates");
        _engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(templatesPath)
            .UseMemoryCachingProvider()
            .Build();
    }

    public Task<string> RenderAsync<TModel>(string templateFileName, TModel model)
        => _engine.CompileRenderAsync(templateFileName, model);
}
