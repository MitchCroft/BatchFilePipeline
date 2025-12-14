namespace BatchFilePipelineCLI.Utility.ExecutionState
{
    /// <summary>
    /// Markers of the different execution states that can be applied
    /// </summary>
    [Flags]
    public enum EXECUTION_STATE : uint
    {
        ES_AWAYMODE_REQUIRED    = 0x00000040,
        ES_CONTINUOUS           = 0x80000000,
        ES_DISPLAY_REQUIRED     = 0x00000002,
        ES_SYSTEM_REQUIRED      = 0x00000001
    }
}
