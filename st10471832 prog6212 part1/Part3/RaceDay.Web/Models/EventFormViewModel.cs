using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class EventFormViewModel
{
    public int EventID { get; set; }

    [Required(ErrorMessage = "Please enter the event name.")]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose a date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Event date")]
    public DateOnly EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location.")]
    public string Location { get; set; } = string.Empty;

    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Required]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";
}