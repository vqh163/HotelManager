using System;

namespace HotelManager.Models
{
    public class LICH_LAM_VIEC
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ShiftType { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string AssignedArea { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = "Sắp diễn ra";
        public string Notes { get; set; } = string.Empty;
    }
}