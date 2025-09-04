using System.ComponentModel.DataAnnotations;

namespace UsersMgmt.Entities

{
    public class User
    {
        
        [Key]
        public int Id { get; set; }
        public required byte[] ImageData { get; set; }
        
        [MaxLength(25)]
        public required string Name { get; set; }

        [MaxLength(40)]
        public required string Email { get; set; }

        public required DateTime DateOfBirth { get; set; }

    }
}
