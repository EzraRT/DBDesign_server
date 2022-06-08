using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBDesign.Model
{
    [Table("group")]
    public class Group
    {
        [Key, Column("gid")]
        public int Id { get; set; }
        [Column("gname")]
        public string Name { get; set; } = "";
        public List<UserGroup> UserGroups { get; set; }
        public List<User> Users { get; set; }
    }
}