using System.ComponentModel.DataAnnotations;

namespace ManufacturingScheduler.Core.Models.Requests
{
    public class BatchDeleteScheduleRequest
    {
        public List<int>? ScheduleIds { get; set; }

        /// <summary>
        /// Delete the last N schedules (ordered by creation date)
        /// </summary>
        [Range(1, 100, ErrorMessage = "LastCount must be between 1 and 100")]
        public int? LastCount { get; set; }

        /// <summary>
        /// Optional: Only delete schedules created by this user
        /// </summary>
        public string? CreatedByFilter { get; set; }

        /// <summary>
        /// Optional: Only delete schedules created before this date
        /// </summary>
        public DateTime? CreatedBeforeDate { get; set; }

        /// <summary>
        /// Safety flag - must be true to proceed with deletion
        /// </summary>
        [Required]
        public bool ConfirmDeletion { get; set; }
    }

    public class BatchDeleteResponse
    {
        public int DeletedCount { get; set; }
        public List<int> DeletedScheduleIds { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}