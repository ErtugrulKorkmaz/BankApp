using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankApp.Models
{
    public class BankAccount
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(32)]
        public string AccountNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        public virtual ApplicationUser? ApplicationUser { get; set; }

        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}


