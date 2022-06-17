using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBDesign.Model
{
    [Table("friends")]
    public class Friend
    {
        [Key, Column("sess_id")]
        public int SessionId { get; set; }
        [Column("user1")]
        public int User1Id { get; set; }
        [Column("user2")]
        public int User2Id { get; set; }
        public User User1 { get; set; }
        public User User2 { get; set; }
    }
}