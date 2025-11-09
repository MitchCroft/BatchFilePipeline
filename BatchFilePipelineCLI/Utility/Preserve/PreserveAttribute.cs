namespace BatchFilePipelineCLI.Utility.Preserve
{
    /// <summary>
    /// An attribute that can be used to mark types as required for operation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class PreserveAttribute : Attribute {}
}
