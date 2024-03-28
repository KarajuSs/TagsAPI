using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TagsAPI.Model
{
    public class Tag
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Autoinkrementacja
        public int Id { get; set; } // Klucz główny
        public string Name { get; set; }
        public int Count { get; set; }
    }
}
