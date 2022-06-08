using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBDesign.Model
{
    [Table("users")]
    public class User
    {
        [Key, Column("qid")]
        public int Id { get; set; }
        [Column("qname")]
        public string Name { get; set; } = " ";
        [Column("qpassword")]
        public string Password { get; set; } = " ";
        [Column("qstate")]
        public int State { get; set; }
        public List<UserGroup> UserGroups { get; set; }
        public List<Group> Groups { get; set; }
    }
}
