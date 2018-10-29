namespace Orleans.Indexing.Tests
{
    public static class IndexingTestConstants
    {
        // storage providers
        public const string GrainStore = "GrainStore";
        public const string MemoryStore = "MemoryStore";
        public const string CosmosDBGrainStorage = "CosmosDBGrainStorage";

        public const string Seattle = "Seattle";
        public const string SanFrancisco = "San Francisco";
        public const string SanJose = "SanJose";
        public const string Bellevue = "Bellevue";
        public const string Redmond = "Redmond";
        public const string Kirkland = "Kirkland";
        public const string Tehran = "Tehran";
        public const string Yazd = "Yazd";

        public const string LocationProperty = "Location";
        public const string UniqueIntProperty = "UniqueInt";
        public const string UniqueStringProperty = "UniqueString";
        public const string NonUniqueIntProperty = "NonUniqueInt";
        public const string NonUniqueStringProperty = "NonUniqueString";

        public const int DelayUntilIndexesAreUpdatedLazily = 1000; // One-second delay for writes to the in-memory indexes should be enough
    }
}
