using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBDesign.Model
{

    [Table("U2Umess")]
    public class UserMessage
    {
        [Key]
        [Column("mess_id")]
        public int Id { get; set; }
        [ForeignKey("Friend")]
        [Column("sess_id")]
        public int SessionID { get; set; }
        [Column("talker_id")]
        public int SenderId { get; set; }
        public User Sender { get; set; }
        [Column("sendtime")]
        public DateTime SendTime { get; set; }
        [Column("content")]
        public string Content { get; set; }
    }
}