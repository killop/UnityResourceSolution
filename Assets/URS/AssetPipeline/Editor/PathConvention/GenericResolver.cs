using System.Collections.Generic;

namespace Daihenka.AssetPipeline.NamingConvention
{
    public class GenericResolver : ITemplateResolver
    {
        public List<Template> templates = new List<Template>();

        public GenericResolver()
        {
        }

        public GenericResolver(List<Template> templates)
        {
            this.templates.AddRange(templates);
        }

        public Template Get(string templateName, Template defaultValue = null)
        {
            foreach (var template in templates)
            {
                if (template.Name == templateName)
                {
                    return template;
                }
            }

            return defaultValue;
        }
    }
}