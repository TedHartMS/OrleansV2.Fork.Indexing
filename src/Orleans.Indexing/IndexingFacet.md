# Indexing Facet Design

<!-- markdown-toc inserts the Table of Contents between the toc/tocstop comments; commandline is: markdown-toc -i <this file> -->
<!-- markdown-toc truncates `IInterface<TProperties>` before the <TProperties>, so manually enter the anchor on the heading using the name markdown-toc generates (markdown-toc does not pick up names in anchor definitions on section headers; you must use the names it generates). Then after each TOC creation, re-add <TProperties> or <TGrainState> before the closing backtick. -->

<!-- toc -->

- [Overview of Indexing](#overview-of-indexing)
- [Application Level](#application-level)
  * [Application Properties Classes](#application-properties-classes)
    + [Property Attributes](#property-attributes)
      - [Index type](#index-type)
      - [Other Parameters](#other-parameters)
    + [Interaction of Indexed Properties and Grain State](#interaction-of-indexed-properties-and-grain-state)
      - [NullValue for Non-Nullable Property Types](#nullvalue-for-non-nullable-property-types)
  * [Application Grain Interfaces](#application-grain-interfaces)
  * [Facet Specification](#facet-specification)
    + [Attribute selection](#attribute-selection)
  * [Application Grain Implementation Classes](#application-grain-implementation-classes)
    + [Indexed Grain Implementation Requirements](#indexed-grain-implementation-requirements)
      - [Wrap the `TGrainState`](#wrap-the-tgrainstate)
      - [Register the Storage Provider](#register-the-storage-provider)
      - [Implement the Indexed Interface `TProperties` Accessors](#implement-the-indexed-interface-tproperties-accessors)
      - [Implement Required Overrides of Grain Methods](#implement-required-overrides-of-grain-methods)
      - [Implement IIndexableGrain Methods](#implement-iindexablegrain-methods)
  * [Querying Indexes](#querying-indexes)
  * [Testing Indexes](#testing-indexes)
- [Orleans Level](#orleans-level)
  * [Reading Property Attributes and Creating Indexes](#reading-property-attributes-and-creating-indexes)
  * [Orleans Indexing Interfaces](#orleans-indexing-interfaces)
    + [`IIndexableGrain<TProperties>`](#iindexablegrain)
  * [Orleans Indexing Implementation Classes](#orleans-indexing-implementation-classes)
    + [Inheritance-based (obsolete and removed)](#inheritance-based-obsolete-and-removed)
      - [`IndexableGrainNonFaultTolerant<TProperties>`](#indexablegrainnonfaulttolerant)
      - [`IndexableGrain<TProperties>`](#indexablegrain)
    + [Facets](#facets)
      - [Facet Attribute](#facet-attribute)
      - [`IIndexWriter<TGrainState>`](#iindexwriter)
- [Constraints on Indexing](#constraints-on-indexing)
  * [Incompatible definitions](#incompatible-definitions)
    + [Total Indexes Cannot Be Eager](#total-indexes-cannot-be-eager)
    + [Total Indexes Cannot be Partitioned Per-Silo](#total-indexes-cannot-be-partitioned-per-silo)
    + [Unique Indexes Cannot Be Active](#unique-indexes-cannot-be-active)
    + [Unique Indexes Cannot Be Partitioned Per-Silo](#unique-indexes-cannot-be-partitioned-per-silo)
    + [Fault-Tolerant Indexes Cannot Be Eager](#fault-tolerant-indexes-cannot-be-eager)
    + [Cannot Define Both Eager And Lazy Indexes on a Single Grain](#cannot-define-both-eager-and-lazy-indexes-on-a-single-grain)
  * [Only One Index Implementation (FT, NFT, TRX) per Grain](#only-one-index-implementation-ft-nft-trx-per-grain)
  * [Single Implementation Class per Grain Interface](#single-implementation-class-per-grain-interface)
  * [Only One Index per Query (==)](#only-one-index-per-query-)
    + [No Compound Indexes](#no-compound-indexes)
    + [No Conjunctions (&&)](#no-conjunctions-)
    + [No Disjunctions (||)](#no-disjunctions-)
    + [No Negations (!=)](#no-negations-)
  * [No Range Indexes (>, <, etc.)](#no-range-indexes---etc)
- [Possible Extensions to Current Design Proposal:](#possible-extensions-to-current-design-proposal)
  * [Compound Indexes](#compound-indexes)
  * [Index Conjunctions (&&)](#index-conjunctions-)
  * [Index Disjunctions (||)](#index-disjunctions-)
  * [Negations and Ranges](#negations-and-ranges)
  * [Adding Explicit TState-to-TProperties Name Mapping](#adding-explicit-tstate-to-tproperties-name-mapping)
  * [Unique Indexes Partitioned Per-Silo](#unique-indexes-partitioned-per-silo)

<!-- tocstop -->

## Overview of Indexing
Indexing enables grains to be efficiently queried by scalar properties. A research paper describing the interface and implementation can be found [here](http://cidrdb.org/cidr2017/papers/p29-bernstein-cidr17.pdf).

Indexing is defined at two levels: at the application level as property attributes on application properties classes, and by implementation classes supplied by Orleans to either update the indexes created from these attributes, or to translate LINQ queries into the retrieval of grains keyed by these indexes.

In this discussion, generic type arguments are prefaced with 'T'. `TProperties` is the type of an application-level class containing properties to be indexed (which are marked with `IndexAttribute` or a subclass). `TIProperties` is the type of an underlying interface for such a class. `TGrainState` is the type of the state parameter of `Grain<TGrainState>`, which an indexed grain inherits from. `TIIndexableGrain` is the type of an interface that has been marked as indexable (details below).

The examples in this discussion use the SportsTeamIndexing sample at Samples\2.1\SportsTeamIndexing. View the readme.md in that directory for instructions on running the sample. The SportsTeamIndexing.Interfaces project defines the grain and properties interfaces (and property classes, because the grain interface definition requires the property class definition), the SportsTeamIndexing.Grains directory defines the Grain (and GrainState) classes, and OrleansClient\ClientProgram.cs creates and queries the indexed grains.

The SportsTeamIndexing sample is intended to be simple, and implements only a single indexed interface on the SportsTeamGrain. More complicated scenarios that implement multiple indexed interfaces on a single grain are also supported. This discussion will also refer to the Indexing Unit Tests for MultipleInterface, which implement three indexed interfaces on TestEmployeeGrain; those interfaces use TProperties classes that implement IPersonProperties, IJobProperties, and IEmployeeProperties. These classes are defined in test\Orleans.Indexing.Tests\Grains\MultiInterface.

Transactional indexes are not yet defined; some references are here as currently best-guess placeholders.
## Application Level
This section describes the indexing interface that is presented to the application developer.
### Application Properties Classes
Applications define the data classes whose properties are indexed, for example:
```c#
    [Serializable]
    public class SportsTeamIndexedProperties : ISportsTeamIndexedProperties
    {
        [Index(typeof(ActiveHashIndexPartitionedPerKey<string, ISportsTeamGrain>), IsEager = true, IsUnique = false)]
        public string Name { get; set; }
        // ...
    }
```
#### Property Attributes
As shown above, an index on a property is defined by placing an attribute on it. While we support placing annotations on multiple properties in a `TProperties` class, each annotation defines a separate index; we currently do not support true compound indexes (they can be simulated by using computed properties such as QualifiedName in the sample). The name of the index is derived from the property name; e.g., the Location property will result in the creation of an index currently named "__Location", under the interface deriving from `IIndexableGrain<TProperties>`. In the sample, the Interface is ISportsTeamGrain, which implements `IIndexableGrain<SportsTeamIndexedProperties>`. Details of the indexable interface are presented below.

##### Index type
The Index attribute may be `Index` or one of the attribute classes inheriting from it; currently these are `ActiveIndex`, `TotalIndex`, or `StorageManagedIndex`.
##### Other Parameters
Index attributes have other parameters: the partitioning scheme (single grain, by key, or by silo); whether or not the index is unique; whether it is eager or lazy; and how many hash buckets to use for that index (the default is no limit; the actual number varies depending on the number of indexed grains and the distribution of the hash).
#### Interaction of Indexed Properties and Grain State
Generally, the `TGrainProperties` class is a base class of `Grain<TGrainState>`'s `TGrainState`, because the indexing implementation casts `TGrainState` to `TProperties` if possible. If there are multiple `TIIndexableGrain`s with multiple `TProperties`, then direct inheritance is not possible. For any `TProperties` that is not a base of `TGrainState`, the Indexing implementation creates an ephemeral `TProperties` instance and copies `TGrainState`'s property values using direct name matching. (Note that the application Grain does not usually create an instance of `TGrainProperties`; it creates an instance of `TGrainState` indirectly, by inheriting from `Grain<TGrainState>`, and populates this.) The properties of `TProperties` are then written to index storage.

The grain state is the backing storage for properties values and will be written to persistent storage; thus, the state class (and not the properties class) must have the [Serializable] attribute. Additionally, because the index specifications are not relevant to the state persistence, the same state class may be used as the backing storage specification for multiple properties classes.

For a single indexed interface, the `TGrainState` class can inherit from the `TProperties` class. For example:
```c#
    public interface ISportsTeamIndexedProperties
    {
        string Name { get; set; }
        string QualifiedName { get; set; }
        string Location { get; set; }
        string League { get; set; }
    }

    public class SportsTeamIndexedProperties : ISportsTeamIndexedProperties
    {
        [Index(typeof(ActiveHashIndexPartitionedPerKey<string, ISportsTeamGrain>), IsEager = true, IsUnique = false)]
        public string Name { get; set; }

        [Index(typeof(ActiveHashIndexPartitionedPerKey<string, ISportsTeamGrain>), IsEager = true, IsUnique = true)]
        public string QualifiedName { get => JoinName(this.League, this.Name); set => SplitName(value); }

        [ActiveIndex(ActiveIndexType.HashIndexPartitionedByKeyHash, IsEager = true, IsUnique = false)]
        public string Location { get; set; }

        [ActiveIndex(ActiveIndexType.HashIndexPartitionedByKeyHash, IsEager = true)]
        public string League { get; set; }
    }

    [Serializable]
    public class SportsTeamState : SportsTeamIndexedProperties
    {
        // This property is not indexed.
        public string Venue { get; set; }
    }
```

For multiple indexed interfaces, the `TGrainState` class and `TProperties` classes should implement common interfaces, since multiple inheritance is not available. For example:
```c#
    // This code block is a simplification of what is in the MultiInterface Unit Tests.
    public interface IPersonProperties
    {
        string Name { get; set; }
        int Age { get; set; }
    }

    public interface IJobProperties
    {
        string Title { get; set; }
        string Department { get; set; }
    }

    public interface IEmployeeProperties
    {
        int EmployeeId { get; set; }
    }

    public class PersonProperties : IPersonProperties
    {
        [Index(typeof(IActiveHashIndexSingleBucket<string, IPersonGrain>), IsEager = true, IsUnique = true)]
        public string Name { get; set; }

        [Index(typeof(IActiveHashIndexSingleBucket<int, IPersonGrain>), IsEager = true, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    [Serializable]
    public class JobProperties : IJobProperties
    {
        [Index(typeof(IActiveHashIndexSingleBucket<string, IJobGrain>), IsEager = true, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(IActiveHashIndexSingleBucket<string, IJobGrain>), IsEager = true, IsUnique = false)]
        public string Department { get; set; }
    }

    [Serializable]
    public class EmployeeProperties : IEmployeeProperties
    {
        [Index(typeof(IActiveHashIndexSingleBucket<int, IEmployeeGrain>), IsEager = true, IsUnique = true, NullValue = "-1")]
        public int EmployeeId { get; set; }
    }
```
##### NullValue for Non-Nullable Property Types
In the foregoing code example, the IndexAttribute specifications for the properties of type int that are unique must have the "NullValue" attribute. This is because, unlike string properties, integers cannot be null, so Indexing will write an index entry for a new grain even when its values have not yet been set or the grain's state saved, which is bad in general and, for unique indexes, will result in erroneous uniqueness violations when more than one such grain is created. Because 0 may be a valid value, the index must specify the value for which the property is to be considered null.

### Application Grain Interfaces
Indexes are grouped by the interface for which they are defined, although the indexes are named for the `TProperties` properties, not any properties defined on the interfaces. Essentially, the interface name functions as a namespace for the group of indexes defined on the `TProperties` of the interface's base `IIndexableGrain<TProperties>` specification.

`TIIndexableGrain`s defined by the application must inherit from [`IIndexableGrain<TProperties>`](#iindexablegrain). Additionally, it should define Task-based properties to set the underlying properties; the properties are Task-based properties because they are implemented on the [Grain implementation class](#application-grain-implementation-classes). For example:
```c#
    public interface ISportsTeamGrain : IGrainWithIntegerKey, IIndexableGrain<SportsTeamIndexedProperties>
    {
        Task<string> GetQualifiedName();
        Task SetQualifiedName(string name);

        Task<string> GetName();
        Task SetName(string name);

        Task<string> GetLocation();
        Task SetLocation(string location);

        Task<string> GetLeague();
        Task SetLeague(string league);

        Task<string> GetVenue();
        Task SetVenue(string venue);
        #endregion not indexed

        Task SaveAsync();
    }
```
In addition to the Task-based property accessors, this interface also defines a method SaveAsync to save the state of the Grain and its indexes.

### Facet Specification
The grain developer specifies which implementation of indexing to use via a Facet: this is done by selecting an attribute on a constructor "index writer" parameter, and selecting an Indexing implementation as the type of that parameter. The Indexing implementation ensures that the selected attribute and implementation are consistent.
#### Attribute selection
The attribute determines whether workflow (either fault-tolerant or non-fault-tolerant) or transactional indexing is to be used, and specifies any necessary parameters. The attribute must also implement the Orleans IFacetMetadata marker interface; this tells the Orleans infrastructure to instantiate the facet implementation. The Orleans-supplied attributes are listed here briefly, and are described in detail in the [Index Implementation](#orleans-indexing-implementation-classes) section:
```c#
    public class NonFaultTolerantWorkflowIndexWriterAttribute : Attribute, IFacetMetadata, INonFaultTolerantWorkflowIndexWriterAttribute, IIndexWriterConfiguration {...}

    public class FaultTolerantWorkflowIndexWriterAttribute : Attribute, IFacetMetadata, IFaultTolerantWorkflowIndexWriterAttribute, IIndexWriterConfiguration {...}

    public class TransactionalIndexedPropertiesAttribute : TransactionalStateAttribute, IFacetMetadata, IFaultTolerantWorkflowIndexWriterAttribute, IIndexWriterConfiguration {...}
```

Here is an example of its use; the IIndexWriter constructor parameter is instantiated by the Orleans Facet infrastructure based upon the Attribute type and is stored in the Grain class for later use. For more information see the [Grain implementation class](#application-grain-implementation-classes) section below.
```c#
    IIndexWriter<SportsTeamState> indexWriter;

    public SportsTeamGrain(
        [NonFaultTolerantWorkflowIndexWriter]
        IIndexWriter<SportsTeamState> indexWriter) => this.indexWriter = indexWriter;
```
### <a name="application-grain-implementation-classes"></a>Application Grain Implementation Classes
Grain classes that implement a `TIIndexableGrain` usually derive (directly or indirectly) from `Grain<TGrainState>`, as the `TGrainState` instance stores the actual data (the `TProperties` instance is ephemeral, created only during the process of index writing). For example: 
```c#
    [StorageProvider(ProviderName = SportsTeamGrain.GrainStore)]
    public class SportsTeamGrain : Grain<IndexableGrainStateWrapper<SportsTeamState>>, ISportsTeamGrain, IIndexableGrain<SportsTeamIndexedProperties>
    {
        // This must be configured when setting up the Silo; see SiloHost.cs StartSilo().
        public const string GrainStore = "SportsTeamGrainMemoryStore";

        IIndexWriter<SportsTeamState> indexWriter;

        SportsTeamState TeamState => base.State.UserState;

        public SportsTeamGrain(
            [NonFaultTolerantWorkflowIndexWriter]
            IIndexWriter<SportsTeamState> indexWriter) => this.indexWriter = indexWriter;

        public Task<string> GetName() => Task.FromResult(this.TeamState.Name);
        public Task SetName(string name) => this.SetProperty(() => this.TeamState.Name = name);

        public Task<string> GetQualifiedName() => Task.FromResult(this.TeamState.QualifiedName);
        public Task SetQualifiedName(string name) => this.SetProperty(() => this.TeamState.QualifiedName = name);

        public Task<string> GetLocation() => Task.FromResult(this.TeamState.Location);
        public Task SetLocation(string location) => this.SetProperty(() => this.TeamState.Location = location);

        public Task<string> GetLeague() => Task.FromResult(this.TeamState.League);
        public Task SetLeague(string role) => this.SetProperty(() => this.TeamState.League = role);

        public Task<string> GetVenue() => Task.FromResult(this.TeamState.Venue);
        public Task SetVenue(string venue) => this.SetProperty(() => this.TeamState.Venue = venue);

        public async Task SaveAsync() => await this.WriteStateAsync();

        ...

        #region Facet methods - required overrides of Grain<TGrainState>
        public override Task OnActivateAsync() => this.indexWriter.OnActivateAsync(this, base.State, () => base.WriteStateAsync(), () => Task.CompletedTask);
        public override Task OnDeactivateAsync() => this.indexWriter.OnDeactivateAsync(() => Task.CompletedTask);
        protected override Task WriteStateAsync() => this.indexWriter.WriteAsync();
        #endregion Facet methods - required overrides of Grain<TGrainState>

        #region Required implementations of IIndexableGrain methods; they are only called for fault-tolerant index writing
        public Task<Immutable<System.Collections.Generic.HashSet<Guid>>> GetActiveWorkflowIdsSet() => this.indexWriter.GetActiveWorkflowIdsSet();
        public Task RemoveFromActiveWorkflowIds(System.Collections.Generic.HashSet<Guid> removedWorkflowId) => this.indexWriter.RemoveFromActiveWorkflowIds(removedWorkflowId);
        #endregion required implementations of IIndexableGrain methods
    }
```
Grain classes may implement multiple `TIIndexableGrain`s. For example:
```c#
    // This code block is a simplification of what is in the MultiInterface Unit Tests.
    public interface IPersonGrain : IIndexableGrain<PersonProperties>, IGrainWithIntegerKey
    {
    }

    public interface IJobGrain : IIndexableGrain<JobProperties>, IGrainWithIntegerKey
    {
    }

    public interface IEmployeeGrain : IIndexableGrain<EmployeeProperties>, IGrainWithIntegerKey
    {
    }

    public class EmployeeGrain<EmployeeGrainState, IndexableGrainStateWrapper<EmployeeGrainState>>,
                                               IPersonGrain, IJobGrain, IEmployeeGrain
    {
        ...
    }
```
#### Indexed Grain Implementation Requirements
Using the SportsTeamGrain example above, there are a few things the grain implementation must do for indexing to work properly.
##### Wrap the `TGrainState`
Indexing stores some information along with the grain state. For non-fault-tolerant indexing, this is done by wrapping `TGrainState` with `IndexableGrainStateWrapper`, whose additional state simply indicates whether the State was persisted. If it was not, then the `IIndexWriter` implementation will assign the NullValues specified for all `TProperties` used by indexable interfaces of that Grain.

Fault-tolerant indexing defines `FaultTolerantIndexableGrainStateWrapper`, a subclass of `IndexableGrainStateWrapper` that also stores the in-flight workflow IDs for that Grain.

This example illustrates using a utility property, TeamState, to obtain the unwrapped `TGrainState` instance.
##### Register the Storage Provider
The above example defines a name for the Storage Provider that will be used for the grain. A Storage Provider with this name must be registered during Silo startup.
##### Implement the Indexed Interface `TProperties` Accessors
As shown above, the grain must implement the property setting in its Task-based property wrappers. As with all Orleans Grains, its methods must be Task-based as part of the remoting system. The grain simply implements an interface method call as a property get or set on its contained `TGrainState` instrance. See the actual code files for the implementation of the SetProperty() private method.

Again, notice that there is no `TProperties` instantiated; the index interface implementations operate on the `TGrainState` instance. The IIndexWriter creates ephemeral `TProperties` instances as part of its index-writing operation.
##### Implement Required Overrides of Grain Methods
As shown  above, because indexed Grains no longer inherit from a `Grain` subclass, an indexed Grain must implement overrides of the following methods, which are simply write-throughs to the matching implementation method of the `indexWriter`:
- `Grain` methods: `OnActivateAsync()` and `OnDeactivateAsync`. These must add the grain to and remove it from the list of active indexes.
- `Grain<TProperties>` method: `WriteStateAsync()`. `indexWriter.WriteAsync()` ensures the correct sequence of task executions to preserve our invariants of Grain state and Index persistence.
##### Implement IIndexableGrain Methods
The `IIndexableGrain` methods `GetActiveWorkflowIdsSet()` and `RemoveFromActiveWorkflowIds()` are necessary for the fault-tolerant internal implementation to communicate back to the grain to retrieve and update the set of in-flight workflows. As shown above, these are simply write-throughs to the matching implementation method of the `indexWriter`:
### Querying Indexes
Querying is done by LINQ:
```c#
    var indexFactory = (IIndexFactory)client.ServiceProvider.GetService(typeof(IIndexFactory));

    var query = from team in indexFactory.GetActiveGrains<ISportsTeamGrain, SportsTeamIndexedProperties>()
                 where team.Name == "Seahawks"
                 select team;

    await query.ObserveResults(new QueryResultStreamObserver<ISportsTeamGrain>(async team =>
    {
        Console.WriteLine($"\n\n{await team.GetName()} location = {await team.GetLocation()}, pk = {team.GetPrimaryKeyLong()}");
    }));
```
Orleans.Indexing reads the ExpressionTrees created by LINQ to determine the property that is being requested and translates this into a read operation on the index.
### Testing Indexes
See test\Orleans.Indexing.Tests for the Unit Tests. These are clustered into 3 groups:
- Players: These are the original tests, modified for the Facet implementation. They provide a single indexable interface on a grain and, usually, only a single indexed property: a Player's Location.
- MultiIndex: These provide a single indexed interface on a grain: One unique and one non-unique int and string index.
- MultiInterface: This capability is new with the Facet implementation. It provides 3 indexed interfaces per grain: A person, a job, and an employee, with a mix of unique and non-unique indexes.

Additionally, the SportsTeamIndexing sample at Samples\2.1\SportsTeamIndexing illustrates creating tests outside the Unit Testing framework.

## Orleans Level
This section defines the Indexing implementation within Orleans.Indexing.

### Reading Property Attributes and Creating Indexes
At Silo startup, the Indexing implementation uses reflection on each Grain class in all assemblies in its ApplicationParts. It reads the list of implemented interfaces for the Grain class and for any Indexable interfaces, it creates and validates the indexes defined on the properties of the `TProperties` of the `IIndexableGrain<TProperties>` underlying that `TIIndexableGrain`. (There is also a public API, `IndexValidator.Validate(Assembly assembly)`, to allow indexes to be validated during Unit Tests.) These indexes are cached, and the cache is keyed on `TIIndexableGrain`, with the value being the list of indexes for that interface. The cache also contains a dictionary of grain implementation classes to their indexed interfaces, for efficient retrieval during `IIndexWriter` instantiation.
### Orleans Indexing Interfaces
#### <a name="iindexablegrain"></a>`IIndexableGrain<TProperties>`
In both the old inheritance-based system and the new Facet system, this not only marks the interface as being indexable, but it also defines a couple of methods necessary for the internal fault-tolerant indexing to communicate with the grain to manage the set of in-flight workflows. In the inheritance-based system, the indexing-specific subclass of `Grain<TGrainState>` implemented these; in the Facet system, the grain must implement these as simple call-throughs to the IIndexWriter implementation.
```c#
    // Interface for a grain interface that will "contain" indexed properties (which are the properties of TProperties).
    public interface IIndexableGrain<TProperties> : IGrain where TProperties: new()
    {
    }

    // Non-generic base interface for indexable grains; provides methods that allow the fault-tolerant indexing
    // implementation to call back to the grain to retrieve and update the list of in-flight workflows.
    public interface IIndexableGrain : IGrain
    {
        /// <summary>
        /// This method returns the set of active workflow IDs for a Total Index
        /// </summary>
        Task<Immutable<HashSet<Guid>>> GetActiveWorkflowIdsSet();

        /// <summary>
        /// This method removes a workflow ID from the list of active workflow IDs for a Total Index
        /// </summary>
        Task RemoveFromActiveWorkflowIds(HashSet<Guid> removedWorkflowId);
    }
```
### Orleans Indexing Implementation Classes
Orleans.Indexing is changing from inheritance to containment; rather than inheriting from an intermediate grain class implementation that overrides WriteStateAsync, facets provide the ability to utilize a contained implementation through constructor injection.
#### Inheritance-based (obsolete and removed)
Under the inheritance scheme, each indexed grain had to inherit from one of these grain classes, which override WriteStateAsync to provide persistence. This approach has been entirely removed, along with the implementation classes listed here; this section remains in order to assist in migration.
##### <a name="indexablegrainnonfaulttolerant"></a>`IndexableGrainNonFaultTolerant<TProperties>`
An application grain inherits from this if its indexes do not have to be fault-tolerant. This is also the base class for `IndexableGrain<TProperties>`.
##### <a name="indexablegrain"></a>`IndexableGrain<TProperties>`
An application grain inherits from this if its indexes must be fault-tolerant. With fault-tolerant indexing, the in-flight workflows and queues are persisted along with the Grain state (the implementation swaps the base State with a wrapper that includes the original base State as well as the set of in-flight workflow IDs and the active queues). When the grain is reactivated (such as if a server crashed during a previous call), the in-flight workflow state is restored with it, and any pending workflows resume executing. 
#### Facets
##### Facet Attribute
The attribute determines whether workflow (fault-tolerant or non-fault-tolerant) or transactional indexing is to be used, and specifies any necessary parameters between the two. See [Facet Specification](#facet-specification) above for more information.
##### <a name="iindexwriter"></a>`IIndexWriter<TGrainState>`
This is the base class for the indexing Facet implementation. One `IIndexWriter` parameter must be on at least one constructor of the indexed grain (there is validation to ensure that there is exactly one for any constructor of a grain that implements a `TIIndexableGrain`, and that there is at least one constructor that has such a parameter). 

The implementation of this interface coordinates the writing of all indexed interfaces defined on the grain. It will retrieve the list of indexed interfaces for the grain from caches that are created during assembly load when indexes are read, validated, and created. It uses the IndexRegistry to maintain cached per-grain-class lists of interfaces and their indexes and properties to do the mapping from `TGrainState` to `TProperties`. If `TGrainState` inherits from `TProperties`, then a simple assignment to the `TProperties` instance is possible; otherwise, an ephemeral instance of `TProperties` is created and the `TGrainState`'s properties are mapped to the corresponding `TProperties` properties. If the index is workflow-based, the writer includes the grain state update in the workflow appropriately.

Orleans supplies three interfaces (and their implementation classes) deriving from `IIndexWriter`; the workflow implementations contain the implementation moved from the previous inheritance-based approach. An indexed Grain class should store the IIndexWriter as a data member assigned from the constructor's facet parameter. The Orleans implementations for the Orleans-provided interfaces are injected at Silo startup time, and an application can define its own as well.

With the exception of the workflow ID set methods on IIndexedGrain, which an indexed Grain implements by simply passing the call along to the matching methods on the IIndexWriter, the details of the various IIndexWriter implementations are completely opaque to the indexed grain. The indexed grain class specifies the desired implementation on the Facet attribute of the IIndexWriter constructor parameter, and the IIndexWriter is instantiated with the specified implementation.

```c#
    // The base interface definition for a class that implements the indexing facet of a grain.
    public interface IIndexWriter<TGrainState> where TGrainState : new()
    {
        /// <summary>
        /// The <see cref="Grain.OnActivateAsync()"/> implementation must call this; in turn, this
        /// calls <paramref name="writeGrainStateFunc"/> if needed and then <paramref name="onGrainActivateFunc"/>, in
        /// which the grain implementation should do any additional activation logic if desired.
        /// </summary>
        /// <param name="grain">The grain to manage indexes for</param>
        /// <param name="wrappedState">The state for <paramref name="grain"/>, which is either the base grain state from
        ///     <see cref="Grain{TGrainState}"/> or a local state member if <paramref name="grain"/> inherits directly
        ///     from <see cref="Grain"/>.</param>
        /// <param name="writeGrainStateFunc">If <paramref name="grain"/>" inherits from <see cref="Grain{TGrainState}"/>,
        ///     this is usually a lambda that calls "base.WriteStateAsync()". Otherwise it is either a custom persistence
        ///     call or simply "() => Task.CompletedTask". It is called in parallel with inserting grain update operations
        ///     into the silo index workflows.</param>
        /// <param name="onGrainActivateFunc">If <paramref name="grain"/> implements custom activation logic, it supplies
        ///     a lambda to do so here, or may simply pass "() => Task.CompletedTask". It is called in parallel with
        ///     inserting grain indexes into the silo index collections and later during <see cref="WriteAsync()"/>.</param>
        Task OnActivateAsync(Grain grain, IndexableGrainStateWrapper<TGrainState> wrappedState, Func<Task> /writeGrainStateFunc,
                             Func<Task> onGrainActivateFunc);

        /// <summary>
        /// The <see cref="Grain.OnDeactivateAsync()"/> implementation must call this; in turn, this
        /// calls <paramref name="onGrainDeactivateFunc"/>, in which the grain implementation should
        /// do any additional deactivation logic, if desired.
        /// </summary>
        /// <param name="onGrainDeactivateFunc">If the grain implements custom deactivation logic, it supplies
        ///     a lambda to do so here, or may simply pass "() => Task.CompletedTask". It is called in parallel with
        ///     removing grain indexes from the silo index collections.</param>
        Task OnDeactivateAsync(Func<Task> onGrainDeactivateFunc);

        /// <summary>
        /// The <see cref="Grain{TGrainState}.WriteStateAsync()"/> implementation must call this; in turn, this calls
        /// onGrainActivateFunc passed to <see cref="OnActivateAsync(Grain, IndexableGrainStateWrapper{TGrainState}, Func{Task}, Func{Task})"/> 
        /// in parallel with inserting grain update operations into the silo index workflows.
        /// </summary>
        /// <remarks>
        /// Coordinates the writing of all indexed interfaces defined on the grain. It will retrieve this from cached
        /// per-grain-class list of indexes and properties to do the mapping, and maps the State structure to the various
        /// TProperties structures. If workflow-based, it includes the grain state update in the workflow appropriately.
        /// </remarks>
        Task WriteAsync();

        /// <summary>
        /// This method returns the set of active workflow IDs for a fault-tolerant Total Index
        /// </summary>
        Task<Immutable<HashSet<Guid>>> GetActiveWorkflowIdsSet();

        /// <summary>
        /// This method removes a workflow ID from the list of active workflow IDs for a fault-tolerant Total Index
        /// </summary>
        Task RemoveFromActiveWorkflowIds(HashSet<Guid> removedWorkflowId);
    }

    public interface INonFaultTolerantWorkflowIndexWriter<TGrainState> : IIndexWriter<TGrainState> where TGrainState : new()
    {
    }

    public interface IFaultTolerantWorkflowIndexWriter<TGrainState> : IIndexWriter<TGrainState> where TGrainState : new()
    {
    }

    public interface ITransactionalIndexWriter<TGrainState> : IIndexWriter<TGrainState> where TGrainState : new()
    {
    }
```

## Constraints on Indexing
### Incompatible definitions
Some index definitions are not compatible with certain partitioning schemes or Active vs. Total indexing.
#### Total Indexes Cannot Be Eager
Eager indexes can only be Active indexes, because they must update the appropriate bucket on the same silo as the activated grain. 
#### Total Indexes Cannot be Partitioned Per-Silo
The indexing paper describes this limitation in detail in section 5.2: Per-Silo (physically partitioned) indexes would have to do fan-out operations to update the status of an index entry on a grain, because the grain may have previously been active on a different silo. The API does not contain definitions that allow such an index to be specified. There appears to be no need to implement this.
#### Unique Indexes Cannot Be Active
An Active Index cannot be defined as unique for the following reason:
1. Suppose there's a unique Active Index over persistent objects.
2. The activation of an initialized object could create a conflict in the Active Index.
   E.g., there's an active player PA with email foo and a non-active persistent player PP with email foo.
3. An attempt to activate PP will cause a violation of the Active Index on email.
   In other words, having a Total unique index prevents the possibility of such a conflict; having an Active unique index does not, because one could activate a Grain, set its email to something already there and persist it (and then deactivate it and activate a new one, etc.). The only use case would be "only one such value can be active at a time", but this would lead to more issues than gain. This implies we should disallow such indexes.
#### Unique Indexes Cannot Be Partitioned Per-Silo
As with Total indexes, Unique Indexes partitioned per Silo (physically) would require fan-out operations to all silos to ensure that the indexed property value is unique. This is currently not implemented.
#### Fault-Tolerant Indexes Cannot Be Eager
Fault-Tolerant Indexes are based on the workflow queues, so they cannot be Eager.
#### Cannot Define Both Eager And Lazy Indexes on a Single Grain
Allowing both Eager and Lazy indexes on a single grain would lead to potential difficulties in ensuring correctness.
### Only One Index Implementation (FT, NFT, TRX) per Grain
Orleans only supports a single index implementation per Grain class: fault-tolerant or non-fault-tolerant workflow, or transactional. This is reflected in the presence of only a single `IIndexWriter` facet parameter.
### Single Implementation Class per Grain Interface
Because GetGrain() must find only one implementation class for a given grain interface, we cannot support some scenarios such as having the same TIIndexedGrainInterface on multiple Grain implementation classes or hierarchies of `TIIndexableGrain`s.

There is an overload of GetGrain() that allows specifying the prefix of the implementation class. This would allow resolution of a single implementation class, but the capability of passing this through the indexing queries has not yet been added.
### <a name="only-one-index-per-query-"></a>Only One Index per Query (==)
The Orleans query syntax currently allows only a single equivalence condition, e.g.: where team.Name == "Seahawks". Following are some specific scenarios that are not supported, together with workarounds where possible.
#### No Compound Indexes
Orleans currently does not have a syntax on property annotations (or property classes) to support indexes across multiple properties, such as specifying an index on League and then on Name. The SportsTeamIndexing sample illustrates how this can be simulated at the application level by defining a computed property (QualifiedName) on the properties class. Note that the class also exposes static methods to compose the component property strings into a single compound string, and to decompose the compound string into its component parts. This ensures consistency and hides the implementation details from the consumer of the class.
#### <a name="no-conjunctions-"></a>No Conjunctions (&&)
Orleans currently does not support intersecting multiple index-query `GrainReference` result sets, such as: where team.Location == "New York, NY" && team.League == "NFL". The SportsTeamIndexing sample illustrates how this can be done at the application level by intersecting HashSets.
#### <a name="no-disjunctions-"></a>No Disjunctions (||)
Orleans currently does not support unioning multiple index-query `GrainReference` result sets, such as: where team.League == "WNBA" || team.League == "NWSL". The SportsTeamIndexing sample illustrates how this can be done at the application level by unioning the result sets.
#### <a name="no-negations-"></a>No Negations (!=)
Orleans currently does not support returning all Grains that do not match a predicate, such as: where team.League != "MLB". There is no workaround for this.
### <a name="no-range-indexes---etc"></a>No Range Indexes (>, <, etc.)
Orleans currently does not support returning all Grains in a range, such as: where player.Level >= 5. There is no workaround for this.

## Possible Extensions to Current Design Proposal:
### Compound Indexes
As noted above, the application can hack together its own implementation of computed properties to provide indexes on more than one property. This would be much more convenient if supplied by Orleans.Indexing. To do so, Orleans would have to define the syntax of the annotations, provide the encoding on write, process the LINQ expression tree to obtain the requested properties, and determine which of the available indexes to apply.
### <a name="index-conjunctions-"></a>Index Conjunctions (&&)
As noted above, the application can intersect the returned `GrainReference`s from multiple index queries. Again, this would be much more convenient if done in Orleans.Indexing. To do so, Orleans would have to process the LINQ expression tree to obtain the requested properties, issue multiple index lookups, and intersect the results.
### <a name="index-disjunctions-"></a>Index Disjunctions (||)
As noted above, the application can union the returned `GrainReference`s from multiple index queries. Again, this would be much more convenient if done in Orleans.Indexing. To do so, Orleans would have to process the LINQ expression tree to obtain the requested properties, issue multiple index lookups, and union the results.
### Negations and Ranges
These cannot be done using the current hash-based approach.
### Adding Explicit TState-to-TProperties Name Mapping
Currently, if `TGrainState` does not inherit from `TProperties`, Indexing maps them by assuming the same property names. We could add an explicit mapping, either by attributes on `TState` and/or `TProperties`, by passing a Dictionary to IIndexWriter.Write(), or by some other scheme.
### Unique Indexes Partitioned Per-Silo
As stated above, Unique Indexes partitioned per Silo (physically) would require fan-out operations to all silos to ensure that the indexed property value is unique. It is not clear how useful this would be.
