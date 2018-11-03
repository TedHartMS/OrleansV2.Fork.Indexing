namespace Orleans.Indexing.Tests.MultiInterface
{
    public interface IEmployeeGrainState : IPersonProperties, IJobProperties
    {
        int Tenure { get; set; }
    }
}
