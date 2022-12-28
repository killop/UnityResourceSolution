namespace Daihenka.AssetPipeline.NamingConvention
{
    public interface ITemplateResolver
    {
        Template Get(string templateName, Template defaultValue = null);
    }
}