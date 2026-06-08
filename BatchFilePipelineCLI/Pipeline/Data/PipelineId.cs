using BatchFilePipelineCLI.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace BatchFilePipelineCLI.Pipeline.Data
{
    /// <summary>
    /// Define a unique identifier for a pipeline that can be linked to for running contained graph
    /// </summary>
    public readonly struct PipelineId : IEquatable<PipelineId>
    {
        /*----------Variables----------*/
        //PUBLIC

        /// <summary>
        /// The unique identifier that will be used to reference a pipeline
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// The full system path to the pipeline asset that this ID represents
        /// </summary>
        public readonly string Path;

        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Represents an empty pipeline identifier
        /// </summary>
        public static PipelineId Empty => new(0, string.Empty);

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the pipeline ID with the provided path that will represent the asset
        /// </summary>
        /// <param name="path">The path to the asset that is to be processed</param>
        /// <param name="root">The root path of execution that will be used to resolve relative paths</param>
        public PipelineId(string path, string root)
        {
            Path = IOUtility.GetFullPath(path, root);
            Id = Path.GetHashCode();
        }

        /// <summary>
        /// Use the ID as the unique identifier for checks of this struct
        /// </summary>
        /// <returns>Returns the underlying id value for comparisons</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => Id;

        /// <summary>
        /// Use the path as the string representation of this pipeline id for easier debugging and logging
        /// </summary>
        /// <returns>Returns the full path of the referenced asset for display</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => Path;

        /// <summary>
        /// Check to see if this id is equal to another
        /// </summary>
        /// <param name="other">The other object that is to be compared against</param>
        /// <returns>Returns true if both are referencing the same asset</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals([NotNullWhen(true)] object? other) => other is PipelineId id && Equals(id);

        /// <summary>
        /// Check to see if this id is equal to another
        /// </summary>
        /// <param name="other">The other object that is to be compared against</param>
        /// <returns>Returns true if both are referencing the same asset</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(PipelineId other) => Id == other.Id;

        /// <summary>
        /// Allow for equality checks between two pipeline IDs to be performed with the == operator for easier comparisons
        /// </summary>
        /// <returns>Returns true if both ids are referencing the same asset</returns>
        public static bool operator ==(PipelineId left, PipelineId right) => left.Equals(right);

        /// <summary>
        /// Allow for inequality checks between two pipeline IDs to be performed with the != operator for easier comparisons
        /// </summary>
        /// <returns>Returns true if both ids are referencing different assets</returns>
        public static bool operator !=(PipelineId left, PipelineId right) => left.Equals(right) == false;

        //PRIVATE

        /// <summary>
        /// Create a specific pipeline ID with the provided values for internal use when the path is already known and the ID has been precomputed
        /// </summary>
        private PipelineId(int id, string path)
        {
            Id = id;
            Path = path;
        }
    }
}
