using System.Runtime.CompilerServices;
using System.Reflection;

namespace BatchFilePipelineCLI.Utility.Preserve
{
    /// <summary>
    /// Reflect through the loaded assemblies and ensure that all <see cref="PreserveAttribute"/> marked types
    /// are explicitly handled and preserved in a build
    /// </summary>
    internal static class TypePreserver
    {
        /*----------Functions----------*/
        //PUBLIC

        /// <summary>
        /// Force the static type to be generated
        /// </summary>
        public static void Init() {}

        //PRIVATE

        /// <summary>
        /// Find all of the types in the project with the attribute and mark them as included
        /// </summary>
        static TypePreserver()
        {
            foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()))
            {
                // Check the type has the attribute attached
                if (type.GetCustomAttribute<PreserveAttribute>(inherit: false) is null)
                {
                    continue;
                }

                // Mark the type as used
                RuntimeHelpers.RunClassConstructor(type.TypeHandle);
                _ = type;
            }
        }
    }
}
