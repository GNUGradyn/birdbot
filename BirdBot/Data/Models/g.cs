using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goatbot.Data.Models;

[Table("g")]
public class g
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("Word")]
    public string Word { get; set; }
}