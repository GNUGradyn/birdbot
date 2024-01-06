using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goatbot.Data.Models;

[Table("VoidMutes")]
public class VoidMutes
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("userId")]
    public ulong UserId { get; set; }
}