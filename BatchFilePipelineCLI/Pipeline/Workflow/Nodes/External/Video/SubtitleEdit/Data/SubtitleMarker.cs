namespace BatchFilePipelineCLI.Pipeline.Workflow.Nodes.External.Video.SubtitleEdit.Data
{
    /// <summary>
    /// Define a subtitle marker in a file that determines text that should be shown at a certain time
    /// </summary>
    public readonly struct SubtitleMarker
    {
        /*----------Variables----------*/
        //PUBLIC

        /// <summary>
        /// The index of the marker within the subtitle data being displayed
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// The duration into the video clip where this subtitle should be displayed
        /// </summary>
        public readonly TimeSpan Start;

        /// <summary>
        /// The duration into the video clip where this subtitle should stop being displayed
        /// </summary>
        public readonly TimeSpan End;

        /// <summary>
        /// The text that is to be shown as the subtitle for this display
        /// </summary>
        public readonly string? Text;

        /*----------Properties----------*/
        //PUBLIC

        /// <summary>
        /// Flag for if the marker contains valid text that can be used
        /// </summary>
        public bool IsValid => string.IsNullOrWhiteSpace(Text) == false;

        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Create this marker with the required information
        /// </summary>
        public SubtitleMarker(int index, TimeSpan start, TimeSpan end, string? text)
        {
            Index = index;
            Start = start;
            End = end;
            Text = text;
        }

        /// <summary>
        /// Format the contained information into a representation of the marker
        /// </summary>
        public override string ToString() => $"[{Index}] {Start} -> {End} = {Text}";
    }
}
