using System.ComponentModel.DataAnnotations;

namespace BelarusHeritage.Models.Domain;

public class ObjectTagMap
{
    public int ObjectId { get; set; }
    public HeritageObject? Object { get; set; }

    public int TagId { get; set; }
    public Tag? Tag { get; set; }
}
