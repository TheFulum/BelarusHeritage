using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class ObjectRelation
{
    public int ObjectId { get; set; }
    public HeritageObject? Object { get; set; }

    public int RelatedId { get; set; }
    public HeritageObject? Related { get; set; }

    [MaxLength(200)]
    public string? Reason { get; set; }
}
