namespace BatchFilePipelineCLI.Utility.Comparison
{
    /// <summary>
    /// Provide a function that can be used to perform dynamic comparison operations based on supplied values
    /// </summary>
    public static class ComparisonUtility
    {
        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Compare the supplied values as required and generate the result
        /// </summary>
        /// <param name="left">The left side value that is to be compared</param>
        /// <param name="mode">The mode that will be used for making the comparison</param>
        /// <param name="right">The right side value that is to be compared</param>
        /// <param name="result">Passes out the int result of the comparison operation</param>
        /// <returns>Returns the logical value for the comparison operation</returns>
        public static bool Compare(IComparable left,
                                   ComparisonMode mode,
                                   IComparable right,
                                   out int result)
        {
            // Check that they are the same type
            if (left.GetType().IsAssignableFrom(right.GetType()) == false)
            {
                left = (Convert.ChangeType(left, right.GetType()) as IComparable)!;
            }

            // Perform the comparison
            result = left.CompareTo(right);
            switch (mode)
            {
                case ComparisonMode.Equal: return result == 0;
                case ComparisonMode.NotEqual: return result != 0;
                case ComparisonMode.LessThan: return result < 0;
                case ComparisonMode.LessThanOrEqual: return result <= 0;
                case ComparisonMode.GreaterThan: return result > 0;
                case ComparisonMode.GreaterThanOrEqual: return result >= 0;
                default: throw new ArgumentException($"Unknown comparison mode '{mode}'", nameof(mode));
            }
        }
    }
}
