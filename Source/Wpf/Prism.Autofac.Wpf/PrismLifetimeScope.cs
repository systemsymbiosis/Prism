namespace Prism.Autofac
{
    public enum PrismLifetimeScope
    {
        PrismLifetimeScoped,
        PrismSingletonScoped
    }
    public class PrismAutofacMetadata
    {
        public static readonly string Prism = "Prism";
        public PrismLifetimeScope LifetimeScope { get; set; }

        public PrismAutofacMetadataDTO PrismAutofacMetadataDTO { get; set; }
    }
}
