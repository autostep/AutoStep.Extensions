namespace AutoStep.Extensions
{
    public interface IPackageMetadata
    {
        public string PackageFolder { get; }

        public string PackageId { get; }

        public string PackageVersion { get; }
    }
}
