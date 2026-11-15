using Atlas.Generated;

namespace Atlas
{
    public sealed partial class AtlasClient
    {
        private AtlasCatalog? _catalog;

        /// <summary>The generated long tail of the Backend and Platform APIs, grouped by resource namespace.</summary>
        public AtlasCatalog Catalog => _catalog ??= new AtlasCatalog(_transport, _platformTransport);
    }
}
