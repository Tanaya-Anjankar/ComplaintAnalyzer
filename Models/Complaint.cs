namespace ComplaintAnalyzer
{
    public class Complaint
    {
        /// <summary>
        /// Unique identifier for the complaint.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of the customer who submitted the complaint.
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        /// Description of the complaint.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Date and time when the complaint was submitted.
        /// </summary>
        public DateTime SubmittedDate { get; set; }

        /// <summary>
        /// Category of the complaint (e.g., "Delivery", "Payment", "Refund").
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Current status of the complaint (e.g., "Open", "Closed", "Pending").
        /// </summary>
        public string Status { get; set; }

    }
}
