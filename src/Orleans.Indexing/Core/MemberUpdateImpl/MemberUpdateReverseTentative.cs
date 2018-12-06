using System;
using System.Collections.Generic;
using System.Linq;

namespace Orleans.Indexing
{
    /// <summary>
    /// This class is a wrapper around another IMemberUpdate, which reverses
    /// the operation in the actual update
    /// </summary>
    [Serializable]
    internal class MemberUpdateReverseTentative : IMemberUpdate
    {
        private IMemberUpdate _update;

        public MemberUpdateReverseTentative(IMemberUpdate update) => this._update = update;

        public object GetBeforeImage() => this._update.GetAfterImage();

        public object GetAfterImage() => this._update.GetBeforeImage();

        public IndexOperationType OperationType
        {
            get
            {
                IndexOperationType op = this._update.OperationType;
                switch (op)
                {
                    case IndexOperationType.Delete: return IndexOperationType.Insert;
                    case IndexOperationType.Insert: return IndexOperationType.Delete;
                    default: return op;
                }
            }
        }

        public override string ToString() => MemberUpdate.ToString(this);

        /// <summary>
        /// Reverses a dictionary of updates by converting all updates to MemberUpdateReverseTentative
        /// </summary>
        /// <param name="updates">the dictionary of updates to be reverse</param>
        /// <returns>the reversed dictionary of updates</returns>
        internal static IReadOnlyDictionary<string, IMemberUpdate> Reverse(IReadOnlyDictionary<string, IMemberUpdate> updates)
            => updates.ToDictionary(kvp => kvp.Key, kvp => new MemberUpdateReverseTentative(kvp.Value)) as IReadOnlyDictionary<string, IMemberUpdate>;
    }
}