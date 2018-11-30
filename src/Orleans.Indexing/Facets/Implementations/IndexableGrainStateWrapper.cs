using System;

namespace Orleans.Indexing.Facets
{
    /// <summary>
    /// A wrapper around a user-defined state, TState, which indicates whether the grain has been persisted.
    /// </summary>
    /// <typeparam name="TGrainState">the type of user state</typeparam>
    [Serializable]
    public class IndexableGrainStateWrapper<TGrainState>
    {
        /// <summary>
        /// Indicates whether the grain was read from storage (used on startup to set null values).
        /// </summary>
        internal bool IsPersisted;

        /// <summary>
        /// The actual user state.
        /// </summary>
        public TGrainState UserState = (TGrainState)Activator.CreateInstance(typeof(TGrainState));
    }
}
