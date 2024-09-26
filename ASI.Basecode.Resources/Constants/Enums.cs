namespace ASI.Basecode.Resources.Constants
{
    /// <summary>
    /// Class for enumerated values
    /// </summary>
    public class Enums
    {
        /// <summary>
        /// API Result Status
        /// </summary>
        public enum Status
        {
            Success,
            Error,
            CustomErr,
        }

        public enum TicketStatus
        {
            OPEN = 1,
            ONGOING = 2,
            WAITINGRESPONSE = 3,
            RESOLVED = 4,
            CLOSED = 5
        }

        /// <summary>
        /// Login Result
        /// </summary>
        public enum LoginResult
        {
            Success = 0,
            Failed = 1,
        }
    }
}
