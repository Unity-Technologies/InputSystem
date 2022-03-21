using SharpYaml.Serialization.Logging;

namespace SharpYaml.Serialization
{
    /// <summary>
    /// Some parameters that can be transmitted from caller
    /// </summary>
    public class SerializerContextSettings
    {
        public static readonly SerializerContextSettings Default = new SerializerContextSettings();

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerContextSettings"/> class.
        /// </summary>
        public SerializerContextSettings()
        {
            MemberMask = YamlMemberAttribute.DefaultMask;
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>
        /// The logger.
        /// </value>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the member mask that will be used to filter <see cref="YamlMemberAttribute.Mask"/>.
        /// </summary>
        /// <value>
        /// The member mask.
        /// </value>
        public uint MemberMask { get; set; }
    }
}