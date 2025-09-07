using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankApp.Models
{
    public class Transaction
    {
        public int Id { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public TransactionType Type { get; set; }

        [MaxLength(256)]
        public string? Description { get; set; }

        public int BankAccountId { get; set; }

        public virtual BankAccount? BankAccount { get; set; }
    }
}


