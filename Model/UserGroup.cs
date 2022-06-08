using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBDesign.Model
{

    [Table("usergroup")]
    public class UserGroup
    {
        [Column("qid"), ForeignKey("User")]
        public int UserId { get; set; }
        [Required]
        public User User { get; set; }

        [Column("gid"), ForeignKey("Group")]
        public int GroupId { get; set; }
        [Required]
        public Group Group { get; set; }
        [Column("auth")]
        public int Authority { get; set; }
    }
}