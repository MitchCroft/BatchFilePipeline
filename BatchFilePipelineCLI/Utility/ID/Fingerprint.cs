using System.Diagnostics.CodeAnalysis;

namespace BatchFilePipelineCLI.Utility.ID
{
    /// <summary>
    /// This struct represents the unique fingerprint of a file that can be processed within the pipeline
    /// </summary>
    internal readonly struct Fingerprint : IEquatable<Fingerprint>
    {
        /*----------Variables----------*/
        //CONST

        /// <summary>
        /// The expected number of characters that would exist in a fingerprint
        /// </summary>
        public const int Size = 32;

        /// <summary>
        /// Instance that marks an empty, invalid fingerprint
        /// </summary>
        public static readonly Fingerprint None = new Fingerprint(new string('0', Size));

        //PUBLIC

        /// <summary>
        /// The specific, unique identifier for this fingerprint
        /// </summary>
        public readonly string ID;

        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Shorthand means of checking if the instance is valid for use
        /// </summary>
        public readonly bool IsValid => string.IsNullOrWhiteSpace(ID) == false && ID.Length == 32;

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create the fingerprint with the specified string sequence
        /// </summary>
        /// <param name="id">The ID that will be used as the fingerprint</param>
        public Fingerprint(in string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }
            if (id.Length != Size)
            {
                throw new ArgumentException("Unexpected length of ID", nameof(id));
            }
            ID = id;
        }

        /// <summary>
        /// Copy the ID of the specified fingerprint
        /// </summary>
        /// <param name="other">The other fingerprint instance to copy</param>
        public Fingerprint(in Fingerprint other)
        {
            ID = other.ID;
        }

        /// <summary>
        /// Check if the other object is equal to this fingerprint
        /// </summary>
        /// <param name="other">The other object that is to be processed</param>
        /// <returns>Returns true if both instances have the same ID</returns>
        public override bool Equals([NotNullWhen(true)] object? other)
        {
            return other is Fingerprint fingerprint && Equals(fingerprint);
        }

        /// <summary>
        /// Check if the other object is equal to this fingerprint
        /// </summary>
        /// <param name="other">The other object that is to be processed</param>
        /// <returns>Returns true if both instances have the same ID</returns>
        public bool Equals(Fingerprint other)
        {
            return IsValid && other.IsValid && ID == other.ID;
        }

        /// <summary>
        /// Retrieve the hash code for this fingerprint element
        /// </summary>
        /// <returns>Returns the hash code identifier for the contained ID</returns>
        public override int GetHashCode()
        {
            return IsValid ? ID.GetHashCode() : 0;
        }

        /// <summary>
        /// Returns a string representation of this fingerprint
        /// </summary>
        /// <returns>Returns the internal string ID</returns>
        public override string ToString()
        {
            return $"[{(IsValid ? ID : "INVALID")}]";
        }

        /// <summary>
        /// Check if both fingerprints are equal
        /// </summary>
        /// <param name="left">The left side element being evaluated</param>
        /// <param name="right">The right side element being evaluated</param>
        /// <returns>Returns true if both fingerprint instances have the same ID</returns>
        public static bool operator ==(in Fingerprint left, in Fingerprint right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Check if both fingerprints are not equal
        /// </summary>
        /// <param name="left">The left side element being evaluated</param>
        /// <param name="right">The right side element being evaluated</param>
        /// <returns>Returns true if both fingerprint instances have different IDs</returns>
        public static bool operator !=(in Fingerprint left, in Fingerprint right)
        {
            return left.Equals(right) == false;
        }
    }
}
