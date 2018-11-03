using System;

namespace Orleans.Indexing.Tests.MultiInterface
{
    [Serializable]
    public class EmployeeGrainState : IEmployeeGrainState
    {
        #region IPersonProperties
        public string Location { get; set; }
        public int Age { get; set; }
        #endregion IPersonProperties

        #region IJobProperties
        public string Title { get; set; }
        public string Department { get; set; }
        #endregion IJobProperties

        #region Not Indexed
        public int Tenure { get; set; }
        #endregion Not Indexed
    }
}
