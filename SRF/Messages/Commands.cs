namespace SRF.Messages
{

    public enum Commands : byte
    {

        /// <summary>
        /// Hello. (0x01)
        /// </summary>
        Hello      = 0x01,
        /// <summary>
        /// OK. (0x02)
        /// </summary>
        OK         = 0x02,
        /// <summary>
        /// File. (0x11)
        /// </summary>
        File       = 0x11,
        /// <summary>
        /// FileSystem. (0x21)
        /// </summary>
        FileSystem = 0x21,
        /// <summary>
        /// End. (0xFD)
        /// </summary>
        End        = 0xFD,
        /// <summary>
        /// Error. (0xFE)
        /// </summary>
        Error      = 0xFE

    }

}