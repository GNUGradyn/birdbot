using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goatbot.Data.Models;

[Table("upvotes")]
public class Upvotes
{
    [Column("Id")]
    [Key]
    public int Id { get; init; }
    [Column("User")]
    public ulong User { get; init; }
    [Column("Count")]
    public int Count { get; set; }
}