using System;
using System.Xml.Linq;

namespace ChaiTrackingSystem.Models
{
    public class Request
    {
        public int Id { get; set; }
        public string Drink { get; set; }
        public int Cups { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }

        public int UserId { get; set; }
    }
}